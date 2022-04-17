using NAudio.Midi;
using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper
{
    public static class EditorTempo
    {
        public static List<TempoChange> TempoChanges
        {
            get => tempoChanges;
        }
        private static List<TempoChange> tempoChanges = new();
        public static bool HasTempoChanges => tempoChanges.Count > 0;
        struct UpdateTiming
        {
            public TargetData data;
            public QNT_Timestamp newTime;
        }
        struct TempoFixup
        {
            public int tempoId;
            public float time;
        }

        static EditorTempo()
        {
            EditorState.OnEditorReset += ClearTempi;
        }

        public static void ClearTempi()
        {
            tempoChanges.Clear();
        }

        //When we shift a previous tempo, we still need to keep the other tempo changes
        // at the same point in time
        private static List<TempoFixup> GatherTempoFixups(int tempoIndex)
        {
            List<TempoFixup> tempoFixes = new List<TempoFixup>();
            for (int i = tempoIndex + 1; i < TempoChanges.Count; ++i)
            {
                TempoFixup fixup;
                fixup.tempoId = i;
                fixup.time = TempoChanges[i].time.ToSeconds();
                tempoFixes.Add(fixup);
            }

            return tempoFixes;
        }

        //Go through each future tempo, adjust it so that it is still at the same point in time, 
        // then adjust all of the notes in that tempo so that they are still aligned
        private static void FixupTempoTimings(List<TempoFixup> tempoFixes, List<UpdateTiming> updateTimings)
        {
            foreach (TempoFixup fixup in tempoFixes)
            {
                QNT_Timestamp newTime = QNT_Timestamp.ShiftTick(0, fixup.time);
                TempoChange change = TempoChanges[fixup.tempoId];
                Relative_QNT changeOffset = newTime - change.time;

                QNT_Timestamp endTime = new QNT_Timestamp(UInt64.MaxValue);
                if (fixup.tempoId + 1 < TempoChanges.Count)
                {
                    endTime = TempoChanges[fixup.tempoId + 1].time;
                }

                var enumerator = new NoteEnumerator(change.time, endTime);
                enumerator.endInclusive = false;

                foreach (Target target in enumerator)
                {
                    UpdateTiming t;
                    t.data = target.data;
                    t.newTime = t.data.time + changeOffset;
                    updateTimings.Add(t);
                }

                change.time = newTime;
                TempoChanges[fixup.tempoId] = change;
            }

            tempoChanges = tempoChanges.OrderBy(tempo => tempo.time.tick).ToList();

            //Fixup secondsFromStart
            for (int i = 0; i < TempoChanges.Count; ++i)
            {
                TempoChange c = TempoChanges[i];
                c.secondsFromStart = c.time.ToSeconds();
                TempoChanges[i] = c;
            }
        }

        static void ShiftNotesByBPM(UInt64 prevMicrosecondPerQuarterNote, QNT_Timestamp time, List<TempoFixup> tempoFixes)
        {
            int tempoIndex = BinarySearch.GetCurrentBPMIndex(time);
            var newTempo = TempoChanges[tempoIndex];

            UInt64 newMicrosecondPerQuarterNote = newTempo.microsecondsPerQuarterNote;

            if (prevMicrosecondPerQuarterNote == 0)
            {
                if (tempoIndex > 0)
                {
                    prevMicrosecondPerQuarterNote = TempoChanges[tempoIndex - 1].microsecondsPerQuarterNote;
                }
                //We are at the beginning, but no previous bpm. We can't shift anything
                else
                {
                    return;
                }
            }

            float p = (float)prevMicrosecondPerQuarterNote / newMicrosecondPerQuarterNote;
            List<UpdateTiming> updateTimings = new List<UpdateTiming>();

            QNT_Timestamp recalcStart = time;
            QNT_Timestamp recalcEnd = new QNT_Timestamp(UInt64.MaxValue);
            if (tempoIndex + 1 < TempoChanges.Count)
            {
                recalcEnd = TempoChanges[tempoIndex + 1].time;
            }

            //Recalc all notes in the zone
            var enumerator = new NoteEnumerator(recalcStart, recalcEnd);
            enumerator.endInclusive = false;

            foreach (Target target in enumerator)
            {
                QNT_Duration tempoTimeDifference = new QNT_Duration(target.data.time.tick - recalcStart.tick);
                QNT_Duration duration_from_start = new QNT_Duration((UInt64)(tempoTimeDifference.tick * p));

                UpdateTiming t;
                t.data = target.data;
                t.newTime = recalcStart + duration_from_start;
                updateTimings.Add(t);
            }

            FixupTempoTimings(tempoFixes, updateTimings);

            //Update all notes
            foreach (var t in updateTimings)
            {
                t.data.SetTimeFromAction(t.newTime);
            }
        }

        public static int GetCurrentBPMIndex(QNT_Timestamp time)
            => BinarySearch.GetCurrentBPMIndex(time);

        public static void ShiftNearestBPMToCurrentTime()
        {
            int tempoIndex = BinarySearch.GetCurrentBPMIndex(EditorTime.Time);
            if (tempoIndex == -1)
            {
                return;
            }

            Relative_QNT offset = EditorTime.Time - tempoChanges[tempoIndex].time;
            if (tempoIndex + 1 < tempoChanges.Count)
            {
                Relative_QNT next = EditorTime.Time - tempoChanges[tempoIndex + 1].time;
                if (Math.Abs(next.tick) < Math.Abs(offset.tick))
                {
                    tempoIndex += 1;
                    offset = next;
                }
            }

            //If we try to shift the first tempo marker, don't do that
            if (tempoIndex == 0)
            {
                NotificationCenter.SendNotification("Cannot shift first bpm marker!", NotificationType.Error);
                return;
            }

            List<UpdateTiming> updateTimings = new List<UpdateTiming>();

            var nextTempo = tempoChanges[tempoIndex];
            QNT_Timestamp recalcStart = nextTempo.time;
            QNT_Timestamp recalcEnd = new QNT_Timestamp(UInt64.MaxValue);
            if (tempoIndex + 1 < tempoChanges.Count)
            {
                TempoChange nextChange = tempoChanges[tempoIndex + 1];
                recalcEnd = nextChange.time;
            }

            //Update the notes in the recalc area
            var enumerator = new NoteEnumerator(recalcStart, recalcEnd);
            enumerator.endInclusive = false;

            foreach (Target target in enumerator)
            {
                UpdateTiming t;
                t.data = target.data;
                t.newTime = t.data.time + offset;
                updateTimings.Add(t);
            }

            List<TempoFixup> tempoFixes = GatherTempoFixups(tempoIndex);

            //Change the tempo
            nextTempo.time = EditorTime.Time;
            tempoChanges[tempoIndex] = nextTempo;

            FixupTempoTimings(tempoFixes, updateTimings);

            //Update all notes
            foreach (var t in updateTimings)
            {
                t.data.SetTimeFromAction(t.newTime);
            }
            Timeline.Instance.RegenerateBPMTimelineData();
        }

        public static void SetBPM(QNT_Timestamp time, UInt64 microsecondsPerQuarterNote, bool shiftFutureEvents, uint Numerator = 4, uint Denominator = 4)
        {

            TimeSignature signature = new TimeSignature(Numerator, Denominator);


            TempoChange c = new TempoChange();
            c.time = time;
            c.microsecondsPerQuarterNote = microsecondsPerQuarterNote;
            c.timeSignature = signature;
            c.ExplicitSignature = false;
            c.secondsFromStart = time.ToSeconds();

            UInt64 prevMicrosecondPerQuarterNote = 0;

            int foundIndex = -1;
            for (int i = 0; i < tempoChanges.Count; ++i)
            {
                if (tempoChanges[i].time == time)
                {
                    foundIndex = i;
                    break;
                }
            }

            //Never attempt to remove the first bpm marker
            if (foundIndex == 0 && microsecondsPerQuarterNote == 0)
            {
                NotificationCenter.SendNotification("Cannot remove initial bpm!", NotificationType.Error);
                return;
            }

            if (foundIndex == -1 && microsecondsPerQuarterNote == 0)
            {
                NotificationCenter.SendNotification("Cannot add 0 bpm!", NotificationType.Error);
                return;
            }

            List<TempoFixup> tempoFixes = new List<TempoFixup>();

            //Found a bpm, set it to the new value
            if (foundIndex != -1)
            {
                prevMicrosecondPerQuarterNote = tempoChanges[foundIndex].microsecondsPerQuarterNote;

                //Remove marker
                if (microsecondsPerQuarterNote == 0)
                {
                    tempoFixes = GatherTempoFixups(foundIndex);
                    tempoChanges.RemoveAt(foundIndex);

                    //Fixup the indices, since they're off by 1 now
                    for (int i = 0; i < tempoFixes.Count; ++i)
                    {
                        TempoFixup newFixup = tempoFixes[i];
                        newFixup.tempoId -= 1;
                        tempoFixes[i] = newFixup;
                    }
                }
                //Set to new tempo
                else
                {
                    tempoFixes = GatherTempoFixups(foundIndex);
                    tempoChanges[foundIndex] = c;
                }
            }
            else
            {
                int index = BinarySearch.GetCurrentBPMIndex(time);
                tempoFixes = GatherTempoFixups(index);
                tempoChanges.Add(c);

                //Fixup the indices, since they're off by 1 now
                for (int i = 0; i < tempoFixes.Count; ++i)
                {
                    TempoFixup newFixup = tempoFixes[i];
                    newFixup.tempoId += 1;
                    tempoFixes[i] = newFixup;
                }
            }

            tempoChanges = tempoChanges.OrderBy(tempo => tempo.time.tick).ToList();

            //Move all future targets back
            if (shiftFutureEvents)
            {
                //ShiftNotesByBPM(prevMicrosecondPerQuarterNote, time, tempoFixes);
                List<UpdateTiming> updateTimings = new List<UpdateTiming>();
                FixupTempoTimings(tempoFixes, updateTimings);

                //Update all notes
                foreach (var t in updateTimings)
                {
                    t.data.SetTimeFromAction(t.newTime);
                }
            }

            Timeline.Instance.RegenerateBPMTimelineData();
        }


        public static TempoChange GetTempoForTime(QNT_Timestamp t)
        {
            int idx = BinarySearch.GetCurrentBPMIndex(t);
            if (idx == -1)
            {
                TempoChange change;
                change.time = t;
                change.microsecondsPerQuarterNote = Constants.OneMinuteInMicroseconds / 60;
                change.ExplicitSignature = false;
                change.timeSignature = new TimeSignature(4, 4);
                change.secondsFromStart = t.ToSeconds();
                return change;
            }

            return tempoChanges[idx];
        }

        public static double GetBpmFromTime(QNT_Timestamp t)
        {
            int idx = BinarySearch.GetCurrentBPMIndex(t);
            if (idx != -1)
            {
                return Constants.GetBPMFromMicrosecondsPerQuaterNote(tempoChanges[idx].microsecondsPerQuarterNote);
            }
            else
            {
                return 120.0;
            }
        }

        public static void LoadFromFile(MidiFile midi, float bpm, float fallbackTempo)
        {
            if (midi != null)
            {
                foreach (var eventList in midi.Events)
                {
                    foreach (var e in eventList)
                    {
                        if (e is TempoEvent)
                        {
                            TempoEvent tempo = (e as TempoEvent);
                            QNT_Timestamp time = new QNT_Timestamp((UInt64)tempo.AbsoluteTime);
                            EditorTempo.SetBPM(time, (UInt64)tempo.MicrosecondsPerQuarterNote, false);
                        }
                    }
                }

                //Now, try to match up time signatures with existing tempo markers
                foreach (var eventList in midi.Events)
                {
                    foreach (MidiEvent e in eventList)
                    {
                        if (e is TimeSignatureEvent)
                        {
                            TimeSignatureEvent timeSignatureEvent = (e as TimeSignatureEvent);
                            QNT_Timestamp time = new QNT_Timestamp((UInt64)timeSignatureEvent.AbsoluteTime);
                            TimeSignature signature = new TimeSignature((uint)timeSignatureEvent.Numerator, (uint)(1 << timeSignatureEvent.Denominator));

                            bool found = false;
                            for (int i = 0; i < tempoChanges.Count; ++i)
                            {
                                if (tempoChanges[i].time == time)
                                {
                                    TempoChange change = tempoChanges[i];
                                    change.timeSignature = signature;
                                    change.ExplicitSignature = true;
                                    tempoChanges[i] = change;
                                    found = true;
                                    break;
                                }
                            }

                            //If there is no tempo change with this time signature, add whatever the current tempo was at that point
                            if (!found)
                            {
                                EditorTempo.SetBPM(time, EditorTempo.GetTempoForTime(time).microsecondsPerQuarterNote, false, signature.Numerator, signature.Denominator);
                            }
                        }

                        //go back over the tempo changes and apply previous time signature if one is not explicitly specified
                        else if (e is TempoEvent)
                        {
                            TimeSignature previousTS = new TimeSignature(4, 4);
                            for (int i = 0; i < tempoChanges.Count; ++i)
                            {
                                if (tempoChanges[i].ExplicitSignature)
                                {
                                    previousTS = tempoChanges[i].timeSignature;
                                }
                                else
                                {
                                    TempoChange foundTempo = tempoChanges[i];
                                    foundTempo.timeSignature = previousTS;
                                    tempoChanges[i] = foundTempo;
                                }
                            }

                        }
                    }
                }
            }
            if (!HasTempoChanges)
            {
                //If we didn't load any bpm, set it from the song desc
                int zeroBPMIndex = BinarySearch.GetCurrentBPMIndex(new QNT_Timestamp(0));
                if (zeroBPMIndex == -1)
                {
                    EditorTempo.SetBPM(new QNT_Timestamp(0), Constants.MicrosecondsPerQuarterNoteFromBPM(fallbackTempo), false);
                }

                if (bpm > 0f)
                {
                    EditorTempo.SetBPM(new QNT_Timestamp(0), Constants.MicrosecondsPerQuarterNoteFromBPM(bpm), true, 4, 4);
                }
            }
        }

        public static void ShiftEverythingByTime(Relative_QNT shiftAmount)
        {
            //Shift tempo markers
            var tempoChanges = EditorTempo.TempoChanges;
            for (int i = 0; i < tempoChanges.Count; ++i)
            {
                TempoChange newChange = tempoChanges[i];
                if (newChange.time.tick != 0)
                {
                    newChange.time += shiftAmount;
                }

                tempoChanges[i] = newChange;
            }
            EditorTempo.tempoChanges = tempoChanges;
            //Shift notes
            foreach (Target note in EditorNotes.OrderedNotes)
            {
                note.data.SetTimeFromAction(note.data.time + shiftAmount);
            }
        }
    }

}
