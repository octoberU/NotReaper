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
        public delegate void TargetEventHandler(Target target);
        public event TargetEventHandler onBeforeTargetDeleted;
        public event TargetEventHandler onTargetAdded;

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

            if (EditorTargets.IsTimeInIntroZone(EditorTime.SnappedTime))
            {
                return;
            }

            if (!NRSettings.config.allowStackedNotes && CheckForTargetAtSameTime(data))
                return;
            
            
            data.velocity = EditorState.Hitsound.Current.ToInternalVelocty();
            var action = new NRActionAddNote(data);
            UndoRedoManager.AddAction(action);
            action.CheckForStackedTargets(Timeline.Instance, "add target", action.targetData);
            if (!action.hasStackedTargets)
                EditorAudio.PlayHitsound(EditorTime.Time);
        }

        private bool CheckForTargetAtSameTime(TargetData data)
        {
            var targets = TargetFinder.FindNotes(data.time);
            var myBehavior = data.behavior;
            var myHand = data.handType;

            switch (myBehavior)
            {
                case TargetBehavior.Melee:
                    int meleeCount = 0;
                    foreach (var target in targets)
                    {
                        if (target.data == data)
                            continue;

                        if (target.data.behavior.IsMelee())
                            meleeCount++;
                    }

                    if (meleeCount >= 2)
                    {
                        NotificationCenter.SendNotification("2 melees are already present at this location.", NotificationType.Warning);
                        return true;
                    }
                    
                    break;
                case TargetBehavior.Mine:
                    foreach (var target in targets)
                    {
                        if (target.data == data)
                            continue;

                        if (target.data.behavior.IsMine())
                        {
                            NotificationCenter.SendNotification("Another mine is already present.", NotificationType.Warning); 
                            return true;
                        }
                    }
                    break;
                default:
                    foreach (var target in targets)
                    {
                        if (target.data.behavior is TargetBehavior.Sustain && target.data.handType == myHand)
                        {
                            NotificationCenter.SendNotification("Can't place target during sustain of the same hand type.", NotificationType.Warning); 
                            return true;
                        }

                        if (target.data.behavior.IsMeleeOrMine() || target.data.time != data.time)
                            continue;

                        if (target.data.handType == myHand)
                        {
                            NotificationCenter.SendNotification("Another target of the same hand is already present.", NotificationType.Warning); 
                            return true;
                        }
                    }

                    break;
            }

            return false;
        }

        /// <summary>
        /// Adds a target to the map through an action.
        /// </summary>
        /// <param name="data">The data to add.</param>
        /// <remarks>TargetData is kept as a reference NOT copied</remarks>
        public Target AddTargetFromAction(TargetData data, bool transient, bool updateChainConnector)
        {
            var target = EditorTargetSpawner.SpawnTarget(data, transient);
            EditorNotes.AddNote(target);

            //Trigger all callbacks on the note
            //data.Copy(data);
            //Also generate chains if needed
            if (data.behavior.IsChain() && !data.isPathbuilderTarget && updateChainConnector && !EditorTargets.IsLoadingTargets)
            {                
                 EditorTargets.UpdateChainConnectors(); 
            }
            onTargetAdded?.Invoke(target);
            return target;
        }

        /// <summary>
        /// Deletes a target.
        /// </summary>
        /// <param name="target">The target to delete.</param>
        public void DeleteTarget(TargetData target, bool ignoreRepeaters = false)
            => UndoRedoManager.AddAction(new NRActionRemoveNote(target, ignoreRepeaters));

        /// <summary>
        /// Deletes a target from the map through an action.
        /// </summary>
        /// <param name="data">The target to delete.</param>
        public void DeleteTargetFromAction(TargetData data, bool updateChainConnector = true)
        {
            Target target = TargetFinder.FindNote(data);
            if (target == null) return;
            onBeforeTargetDeleted?.Invoke(target);
            EditorNotes.RemoveNote(target);
            target.Destroy();
            EditorTargetSpawner.ReturnTarget(target);
            if (data.isPathbuilderTarget)
                return;
            
            if (updateChainConnector && data.behavior.IsChain() && !data.isPathbuilderTarget)
            {
                EditorTargets.UpdateChainConnectors();
            }

            if (data.behavior.IsMelee())
            {
                foreach (var t in TargetFinder.FindNotes(data.time))
                {
                    if (t.data.behavior.IsMelee())
                    {
                        t.PairMelee(null);
                        break;
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
