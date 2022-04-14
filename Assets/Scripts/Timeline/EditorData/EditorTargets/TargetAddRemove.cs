using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools;
using NotReaper.Tools.ChainBuilder;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.TargetEditor
{
    public class TargetAddRemove
    {
        /// <summary>
        /// Adds a singular target to the map through user input.
        /// </summary>
        /// <param name="position">The target's grid position.</param>
        public void AddTarget(Vector2 position)
        {
            if (!EditorFile.IsAudicaFileLoaded || EditorState.IsInUI || !EditorState.IsOverGrid)
            {
                return;
            }

            TargetData data = new TargetData();
            data.position = position;
            data.handType = EditorState.Hand.Current;
            data.behavior = EditorState.Behavior.Current;

            QNT_Timestamp tempTime = EditorTime.SnappedTime;
            //TempoChange currentTempo = EditorTempo.TempoChanges[0];

            int leftHandMeleeCount = 0;
            int rightHandMeleeCount = 0;
            int meleeCount = 0;
            int targetCount = 0;
            foreach (Target target in EditorNotes.LoadedNotes)
            {
                if (target.data.time == tempTime)
                {
                    if (target.data.behavior != TargetBehavior.Melee && target.data.behavior != TargetBehavior.Mine)
                    {
                        targetCount++;
                    }
                    if (target.data.handType == TargetHandType.Either && target.data.behavior != TargetBehavior.Melee && target.data.behavior != TargetBehavior.Mine)
                    {
                        if (targetCount == 2) return;
                    }
                    if (target.data.handType == EditorState.Hand.Current && EditorState.Behavior.Current != TargetBehavior.Melee)
                    {
                        if (EditorState.Behavior.Current != TargetBehavior.Mine && target.data.handType != TargetHandType.Either) return;
                    }
                    else if (EditorState.Behavior.Current == TargetBehavior.Melee)
                    {
                        if (target.data.x == data.x && target.data.y == data.y) return;

                        if (target.data.behavior == TargetBehavior.Melee)
                        {
                            if (target.data.handType == TargetHandType.Left) leftHandMeleeCount++;
                            else if (target.data.handType == TargetHandType.Right) rightHandMeleeCount++;
                            else meleeCount++;
                        }
                        if (leftHandMeleeCount == 1 && data.handType == TargetHandType.Left && data.behavior == TargetBehavior.Melee)
                        {
                            return;
                        }
                        else if (rightHandMeleeCount == 1 && data.handType == TargetHandType.Right && data.behavior == TargetBehavior.Melee)
                        {
                            return;
                        }
                        else if (meleeCount + rightHandMeleeCount + leftHandMeleeCount == 2 && data.behavior == TargetBehavior.Melee)
                        {
                            return;
                        }
                    }
                }

            }
            if (data.handType == TargetHandType.Either && data.behavior != TargetBehavior.Melee && data.behavior != TargetBehavior.Mine)
            {
                if (targetCount == 2) return;
            }

            data.SetTimeFromAction(EditorTime.SnappedTime);

            //Default sustains length should be more than 0.
            if (data.supportsBeatLength)
            {
                data.beatLength = Constants.QuarterNoteDuration;
            }
            else
            {
                data.beatLength = Constants.SixteenthNoteDuration;
            }

            if (IsTimeInIntroZone(EditorTime.SnappedTime))
            {
                return;
            }
            else if (EditorTargets.WouldHaveDoubledTargets(data, out string reason))
            {
                NotificationCenter.SendNotification($"Can't place target: {reason}");
                return;
            }


            data.velocity = EditorState.Hitsound.Current.ToInternalVelocty();

            UndoRedoManager.AddAction(new NRActionAddNote(data));
            EditorAudio.PlayHitsound(EditorTime.Time);
            EditorScale.ReapplyScale();
        }

        /// <summary>
        /// Adds a target to the map through an action.
        /// </summary>
        /// <param name="data">The data to add.</param>
        /// <remarks>TargetData is kept as a reference NOT copied</remarks>
        public Target AddTargetFromAction(TargetData data, bool transient = false)
        {
            var target = EditorTargetSpawner.SpawnTarget(data, transient);
            EditorNotes.AddNote(target);

            //Subscribe to the delete note event so we can delete it if the user wants. And other events.
            target.DeleteNoteEvent += EditorTargets.DeleteTarget;

            target.TargetSelectEvent += EditorNotes.SelectTarget;
            target.TargetDeselectEvent += EditorNotes.DeselectTarget;

            target.MakeTimelineUpdateSustainLengthEvent += EditorTargets.UpdateSustainLength;

            //Trigger all callbacks on the note
            data.Copy(data);
            //Also generate chains if needed
            if (data.behavior == TargetBehavior.Legacy_Pathbuilder)
            {
                ChainBuilder.GenerateChainNotes(data);
            }
            return target;
        }

        /// <summary>
        /// Deletes a target.
        /// </summary>
        /// <param name="target">The target to delete.</param>
        public void DeleteTarget(TargetData target)
            => UndoRedoManager.AddAction(new NRActionRemoveNote(target));

        /// <summary>
        /// Deletes a target from the map through an action.
        /// </summary>
        /// <param name="data">The target to delete.</param>
        public void DeleteTargetFromAction(TargetData data)
        {
            Target target = TargetFinder.FindNote(data);
            if (target == null) return;
            EditorNotes.RemoveNote(target);
            target.Destroy();
            EditorTargetSpawner.ReturnTarget(target);
            if(data.behavior == TargetBehavior.ChainStart)
            {
                var t = TargetFinder.FindNextTargetWithHand(data, data.handType, true);
                if(t != null)
                {
                    if(t.data.behavior == TargetBehavior.ChainNode)
                    {
                        EditorTargets.UpdateChainConnector(t);
                    }
                }
            }
            else
            {
                var t = TargetFinder.FindPreviousTargetWithHand(data, data.handType, true);
                if(t != null)
                {
                    if(t.data.behavior == TargetBehavior.ChainStart || t.data.behavior == TargetBehavior.ChainNode)
                    {
                        EditorTargets.UpdateChainConnector(t);
                    }
                }

            }
        }

        /// <summary>
        /// Deletes the targets from the map.
        /// </summary>
        /// <param name="targets">The targets to delete.</param>
        public void DeleteTargets(List<TargetData> targets)
            => UndoRedoManager.AddAction(new NRActionMultiRemoveNote(targets));

        /// <summary>
        /// Deletes all targets.
        /// </summary>
        public void DeleteAllTargets()
        {
            var notesTemp = EditorNotes.Notes.ToList();
            EditorNotes.ClearAllNotes();
            foreach (Target target in notesTemp)
            {
                target.Destroy();
                target.Reset();
                EditorTargetSpawner.ReturnTarget(target);
            }
        }

        public bool IsTimeInIntroZone(QNT_Timestamp time)
        {

            TempoChange currentTempo = EditorTempo.GetTempoForTime(time);
            var tempTime = time;

            if (currentTempo.microsecondsPerQuarterNote <= 500000)
            {
                if (tempTime.tick < (currentTempo.timeSignature.Numerator * 2) * Constants.QuarterNoteDuration.tick) // deny if in intro redzone
                {
                    NotificationCenter.SendNotification("Can't place target in intro zone. Targets before the 2 second mark don't properly work in-game.", NotificationType.Info);
                    return true;
                }
            }
            else
            {
                if (tempTime.tick < currentTempo.timeSignature.Numerator * Constants.QuarterNoteDuration.tick)
                {
                    NotificationCenter.SendNotification("Can't place target in intro zone. Targets before the 2 second mark don't properly work in-game.", NotificationType.Info);
                    return true;
                }
            }
            return false;
        }
    }
}
