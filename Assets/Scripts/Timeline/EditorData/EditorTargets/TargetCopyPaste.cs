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

        public void DeleteSelectedTargets()
        {
            if (EditorNotes.SelectedNotes.Count > 0)
            {
                EditorTargets.DeleteSelectedTargets();
            }
        }

        /// <summary>
        /// Pastes the supplied cues at the specified time.
        /// </summary>
        /// <param name="cues">The cues to paste.</param>
        /// <param name="pasteBeatTime">The time to paste them at.</param>
        public void PasteCues(List<TargetData> cues, QNT_Timestamp pasteBeatTime)
        {
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

            UndoRedoManager.AddAction(new NRActionMultiAddNote(targetDataList));
            EditorNotes.DeselectAllTargets();
            EditorNotes.SelectTargets(TargetFinder.FindNotes(targetDataList));
        }
        /// <summary>
        /// Copies the current timestamp to the system copy buffer.
        /// </summary>
        private void CopyTimestampToClipboard() => GUIUtility.systemCopyBuffer = "**" + EditorTime.Time.ToString() + "**" + " - ";
    }
}

