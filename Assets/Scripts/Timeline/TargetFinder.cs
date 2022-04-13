using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper
{
    public static class TargetFinder
    {
        public static TargetData FindTargetData(QNT_Timestamp time, TargetBehavior behavior, TargetHandType handType)
        {
            BinarySearchResult res = BinarySearchOrderedNotes(time);
            if (res.found == false)
            {
                //Debug.LogWarning("Couldn't find note with time " + time);
                return null;
            }

            for (int i = res.index; i < EditorNotes.OrderedNotes.Count; ++i)
            {
                Target t = EditorNotes.OrderedNotes[i];
                if (t.data.time == time &&
                    t.data.behavior == behavior &&
                    t.data.handType == handType)
                {
                    return t.data;
                }
            }

            //Debug.LogWarning("Couldn't find note with time " + time + " and index " + res.index);
            return null;
        }

        public static Target FindNote(TargetData data)
        {
            BinarySearchResult res = BinarySearchOrderedNotes(data.time);
            if (res.found == false)
            {
                //Debug.LogWarning("Couldn't find note with time " + data.time);
                return null;
            }

            for (int i = res.index; i < EditorNotes.OrderedNotes.Count; ++i)
            {
                Target t = EditorNotes.OrderedNotes[i];
                if (t.data.ID == data.ID)
                {
                    return t;
                }
            }

            //Debug.LogWarning("Couldn't find note with time " + data.time + " and index " + res.index);
            return null;
        }

        public static List<Target> FindNotes(QNT_Timestamp time)
        {
            List<Target> foundNotes = new();
            foreach(var target in new NoteEnumerator(time, time))
            {
                foundNotes.Add(target);
            }
            foreach(var target in new NoteEnumerator(new(0), time))
            {
                if (target.data.behavior != TargetBehavior.Sustain)
                    continue;

                if (target.data.time + target.data.beatLength >= time)
                    foundNotes.Add(target);
            }
            return foundNotes;
        }

        public static List<Target> FindNotes(List<TargetData> targetDataList)
        {
            List<Target> foundNotes = new List<Target>();
            foreach (TargetData data in targetDataList)
            {
                foundNotes.Add(FindNote(data));
            }
            return foundNotes;
        }

        public static Target FindChainStart(TargetData chain)
        {
            var target = FindNote(chain);

            if (target == null)
                return null;

            return FindChainStart(target);
        }

        public static Target FindChainStart(Target chain)
        {
            if (chain.data.behavior == TargetBehavior.ChainStart)
            {
                return chain;
            }
            else if (chain.data.behavior == TargetBehavior.ChainNode)
            {
                NoteEnumerator notes = new NoteEnumerator(new QNT_Timestamp(0), chain.data.time);
                notes.reverse = true;
                foreach (var note in notes)  //find the first chainstart of the same handtype
                {
                    if (note.data.behavior != TargetBehavior.ChainStart) continue;

                    if (note.data.handType == chain.data.handType)
                    {
                        return note;
                    }
                }
            }

            return null;
        }

        public static Target FindPreviousTargetWithHand(TargetData target, TargetHandType hand, bool includeChains = false)
        {
            if (EditorNotes.OrderedNotes.Count == 0)
                return null;

            NoteEnumerator notes = new NoteEnumerator(new(0), target.time);
            notes.reverse = true;
            foreach(var note in notes)
            {
                if (note.data.time == target.time) continue;
                if (note.data.handType != hand) continue;
                if (note.data.behavior.IsMeleeOrMine()) continue;
                if (!includeChains && note.data.behavior == TargetBehavior.ChainNode) continue;

                return note;
            }
            return null;
        }

        public static Target FindNextTargetWithHand(TargetData target, TargetHandType hand, bool includeChains = false)
        {
            if(EditorNotes.OrderedNotes.Count == 0) 
                return null;

            NoteEnumerator notes = new NoteEnumerator(target.time, EditorNotes.OrderedNotes.Last().data.time);
            foreach(var note in notes)
            {
                if (note.data.time == target.time) continue;
                if (note.data.handType != hand) continue;
                if (note.data.behavior.IsMeleeOrMine()) continue;
                if (!includeChains && note.data.behavior == TargetBehavior.ChainNode) continue;

                return note;
            }
            return null;
        }

        public static BinarySearchResult BinarySearchOrderedNotes(QNT_Timestamp cueTime)
        {
            BinarySearchResult result;

            int min = 0;
            int max = EditorNotes.OrderedNotes.Count - 1;
            while (min <= max)
            {
                int mid = (min + max) / 2;
                QNT_Timestamp midCueTime = EditorNotes.OrderedNotes[mid].data.time;
                if (cueTime == midCueTime)
                {
                    while (mid != 0 && EditorNotes.OrderedNotes[mid - 1].data.time == cueTime)
                    {
                        --mid;
                    }

                    result.index = mid;
                    result.found = true;
                    return result;
                }
                else if (cueTime < midCueTime)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            result.index = min;
            result.found = false;
            return result;
        }

        
    }
}

namespace NotReaper.Utility
{
    public struct BinarySearchResult
    {
        public bool found;
        public int index;
    }
}
