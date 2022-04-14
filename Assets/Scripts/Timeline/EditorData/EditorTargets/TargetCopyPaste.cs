using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.TargetEditor
{
    public class TargetCopyPaste
    {
        private List<TargetData> clipboard = new();

        /// <summary>
        /// Copies selected targets.
        /// </summary>
        /// <param name="copyTimestamp">True if you want to copy the current tick in the editor, too.</param>
        public void CopySelectedTargets(bool copyTimestamp = true)
        {
            if (copyTimestamp)
                CopyTimestampToClipboard();

            clipboard = new List<TargetData>();
            bool displayWarning = false;
            foreach (var target in EditorNotes.SelectedNotes)
            {
                if (target.data.isRepeaterTarget)
                {
                    displayWarning = true;
                    continue;
                }
                clipboard.Add(target.data);
            }
            if (displayWarning)
            {
                NotificationCenter.SendNotification("Repeater targets can't be copied.", NotificationType.Warning);
            }
        }
        /// <summary>
        /// Pastes the currently copied targets.
        /// </summary>
        public void PasteCopiedTargets()
        {
            if (clipboard.Count == 0)
            {
                NotificationCenter.SendNotification("You don't have any copied targets.", NotificationType.Warning);
                return;
            }
            EditorNotes.DeselectAllTargets();
            PasteCues(clipboard, EditorTime.Time);
        }
        /// <summary>
        /// Copies the supplied targets.
        /// </summary>
        /// <param name="targets">The targets to copy.</param>
        public void CopyTargets(List<TargetData> targets) => clipboard = targets;
        /// <summary>
        /// Cuts the selected targets.
        /// </summary>
        public void CutSelectedTargets()
        {
            CopySelectedTargets(false);
            DeleteSelectedTargets();
        }
        /// <summary>
        /// Deletes the currently selected targets.
        /// </summary>
        public void DeleteSelectedTargets()
        {
            if (EditorNotes.SelectedNotes.Count > 0)
                EditorTargets.DeleteSelectedTargets();
            
        }

        /// <summary>
        /// Pastes the supplied cues at the specified time.
        /// </summary>
        /// <param name="cues">The cues to paste.</param>
        /// <param name="pasteBeatTime">The time to paste them at.</param>
        public void PasteCues(List<TargetData> cues, QNT_Timestamp pasteBeatTime)
        {
            if (EditorTargets.IsTimeInIntroZone(pasteBeatTime))
            {
                return;
            }

            // paste new targets in the original locations
            var targetDataList = cues.Select(copyData =>
            {
                var data = new TargetData();
                data.Copy(copyData);
                if (data.behavior == TargetBehavior.Legacy_Pathbuilder)
                {
                    data.legacyPathbuilderData = new LegacyPathbuilderData();
                    data.legacyPathbuilderData.Copy(copyData.legacyPathbuilderData);
                }
                else if (data.isPathbuilderTarget)
                {
                    data.pathbuilderData = new PathbuilderData();
                    data.pathbuilderData.Copy(copyData.pathbuilderData);
                }

                return data;
            }).ToList();

            // find the soonest target in the selection
            QNT_Timestamp earliestTargetBeatTime = new QNT_Timestamp(long.MaxValue);
            foreach (TargetData data in targetDataList)
            {
                QNT_Timestamp time = data.time;
                if (time < earliestTargetBeatTime)
                {
                    earliestTargetBeatTime = time;
                }
            }

            // shift all by the amount needed to move the earliest note to now
            Relative_QNT diff = pasteBeatTime - earliestTargetBeatTime;
            foreach (TargetData data in targetDataList)
            {
                data.SetTimeFromAction(data.time + diff);
            }

            if (WouldHaveDoubledTargets(targetDataList, out string reason))
            {
                NotificationCenter.SendNotification($"Can't paste: {reason}", NotificationType.Warning);
                return;
            }

            UndoRedoManager.AddAction(new NRActionMultiAddNote(targetDataList));
            EditorNotes.DeselectAllTargets();
            EditorNotes.SelectTargets(TargetFinder.FindNotes(targetDataList));
        }
        /// <summary>
        /// Copies the current timestamp to the system copy buffer.
        /// </summary>
        private void CopyTimestampToClipboard() => GUIUtility.systemCopyBuffer = "**" + EditorTime.Time.ToString() + "**" + " - ";

        /// <summary>
        /// Checks if doubled targets (e.g. 2 left hand targets on the same tick) would occur.
        /// </summary>
        /// <param name="targets">The targets to check for.</param>
        /// <returns>True if any of the targets in the list would lead to doubled targets when added.</returns>
        public bool WouldHaveDoubledTargets(List<TargetData> targets, out string reason)
        {
            reason = "";
            foreach(var target in targets)
            {
                if (target.behavior == TargetBehavior.Mine)
                    continue;

                if(target.behavior == TargetBehavior.Sustain)
                {
                    foreach(var note in new NoteEnumerator(target.time, target.time + target.beatLength))
                    {
                        if (note.data.time == target.time || note.data.behavior.IsMeleeOrMine())
                            continue;

                        if(note.data.handType == target.handType)
                        {
                            reason = $"Sustain can't be placed at {target.time} because a target of the same hand is present at {note.data.time}.";
                            return true;
                        }
                    }
                }


                var foundNotes = TargetFinder.FindNotes(target.time);

                foreach(var note in foundNotes)
                {
                    if(target.handType == note.data.handType)
                    {
                        if(target.behavior == TargetBehavior.Melee && note.data.behavior == TargetBehavior.Melee)
                        {
                            if (target.position == note.data.position)
                            {
                                reason = $"Melee already exists in the same position at time {target.time}.";
                                return true;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else if(note.data.behavior == TargetBehavior.Sustain && note.data.time != target.time)
                        {
                            reason = $"Sustain with same handtype is active.";
                        }
                        else
                        {
                            reason = $"Target with the same hand already exists at time {target.time}.";
                        }
                        return true;
                    }
                }        
            }
            return false;
        }
        /// <summary>
        /// Checks if a doubled target would occur if a target with the specific hand was present.
        /// </summary>
        /// <param name="target">The target to check for.</param>
        /// <param name="hand">The hand type the target wants.</param>
        /// <param name="reason">Stores the reason for why a doubled target occured. Empty if no doubled targets occur.</param>
        /// <returns>True if doubled target would occur.</returns>
        public bool WouldHaveDoubledTargets(TargetData target, TargetHandType hand, out string reason)
        {
            reason = "";

            if (target.behavior == TargetBehavior.Mine)
                return false;

            var foundNotes = TargetFinder.FindNotes(target.time);

            foreach (var note in foundNotes)
            {
                if (note.data == target)
                    continue;

                if(note.data.handType == hand)
                {
                    reason = $"Target would be stacked at {target.time}.";
                    return true;
                }
            }
            
            return false;
        }
        /// <summary>
        /// Checks if a doubled target would occur if a target at the specific time was present.
        /// </summary>
        /// <param name="target">The target to check for.</param>
        /// <param name="time">The time the target wants.</param>
        /// <param name="reason">Stores the reason for why a doubled target occured. Empty if no doubled targets occur.</param>
        /// <returns>True if doubled target would occur.</returns>
        public bool WouldHaveDoubledTargets(TargetTimelineMoveIntent intent, out string reason)
        {
            reason = "";
            var target = intent.targetData;
            var intendedTime = intent.intendedTick;
            if (target.behavior == TargetBehavior.Mine)
                return false;

            var foundNotes = TargetFinder.FindNotes(intendedTime);

            foreach (var note in foundNotes)
            {
                if (note.data == target)
                    continue;

                if (note.data.handType == target.handType)
                {
                    reason = $"Target would be stacked at {intendedTime}.";
                    return true;
                }
            }

            return false;
        }
    }
}

