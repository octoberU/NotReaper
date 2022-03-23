using System.Linq;
using System.Collections.Generic;
using NotReaper.Grid;
using NotReaper.Targets;
using NotReaper.UserInput;
using NotReaper.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using NotReaper.Managers;
using NotReaper.Timing;
using System;
using NotReaper.Tools.PathBuilder;
using NotReaper.Notifications;

namespace NotReaper.Tools
{


    public class UndoRedoManager : MonoBehaviour
    {

        /// <summary>
        /// Contains the complete list of actions the user has done recently.
        /// </summary>
        public List<NRAction> actions = new List<NRAction>();

        /// <summary>
        /// Contains the actions the user has "undone" for future use.
        /// </summary>
        public List<NRAction> redoActions = new List<NRAction>();

        public int maxSavedActions = 20;

        public Timeline timeline;

        public void Undo()
        {
            if (actions.Count <= 0) return;

            NRAction action = actions.Last();

            action.UndoAction(timeline);

            redoActions.Add(action);
            actions.RemoveAt(actions.Count - 1);

            timeline.ReapplyScale();
        }

        public void Redo()
        {

            if (redoActions.Count <= 0) return;

            NRAction action = redoActions.Last();

            action.DoAction(timeline);

            actions.Add(action);
            redoActions.RemoveAt(redoActions.Count - 1);
            timeline.ReapplyScale();
        }

        public void AddAction(NRAction action)
        {
            action.DoAction(timeline);
            if (actions.Count <= maxSavedActions)
            {
                actions.Add(action);
            }
            else
            {
                while (maxSavedActions > actions.Count)
                {
                    actions.RemoveAt(0);
                }

                actions.Add(action);
            }

            redoActions = new List<NRAction>();
        }

        public void ClearActions()
        {
            actions = new List<NRAction>();
            redoActions = new List<NRAction>();
        }
    }

    public abstract class NRAction
    {
        public abstract void DoAction(Timeline timeline);
        public abstract void UndoAction(Timeline timeline);
    }

    public class NRActionAddNote : NRAction
    {
        public TargetData targetData;
        public List<TargetData> repeaterData;

        public override void DoAction(Timeline timeline)
        {
            if (timeline.repeaterManager.IsTargetInRepeaterZone(targetData.time))
            {
                timeline.repeaterManager.CreateRepeaterTarget(targetData);
            }
            /*if (targetData.isRepeaterTarget)
            {
                targetData.repeaterData.targetID = targetData.repeaterData.Section.GetCurrentTargetIndexID();
                foreach (var section in timeline.repeaterManager.GetMatchingRepeaterSections(targetData.repeaterData))
                {
                    section.CreateRepeaterTarget(targetData);
                }
            }*/
            else
            {
                timeline.AddTargetFromAction(targetData);
            }

            if (targetData.isPathbuilderTarget)
            {
                timeline.pathbuilder.UpdatePathbuilderTargetFromAction(targetData, targetData.pathbuilderData);
            }

            if (repeaterData == null)
            {
                repeaterData = timeline.GenerateRepeaterTargets(targetData);
            }
            repeaterData.ForEach(data => { timeline.AddTargetFromAction(data); });

        }
        public override void UndoAction(Timeline timeline)
        {
            repeaterData.ForEach(data => { timeline.DeleteTargetFromAction(data); });
            if (targetData.isRepeaterTarget)
            {
                timeline.repeaterManager.DeleteRepeaterTarget(targetData);
                /*foreach (var section in timeline.repeaterManager.GetMatchingRepeaterSections(targetData.repeaterData))
                {
                    section.RemoveRepeaterTarget(targetData);
                }*/
            }
            else
            {
                if (targetData.isPathbuilderTarget)
                {
                    foreach (var segment in targetData.pathbuilderData.Segments)
                    {
                        foreach (var node in segment.generatedNodes)
                        {
                            timeline.DeleteTargetFromAction(node);
                        }
                    }
                }
                timeline.DeleteTargetFromAction(targetData);
            }
        }
    }

    public class NRActionMultiAddNote : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();
        public List<NRActionAddNote> actions;

        public override void DoAction(Timeline timeline)
        {
            if (actions == null)
            {
                actions = affectedTargets.Select(targetData => { var action = new NRActionAddNote(); action.targetData = targetData; return action; }).ToList();
                affectedTargets = null;
            }
            actions.ForEach(action => { action.DoAction(timeline); });
            TransformTool.instance.UpdateOverlay();
        }
        public override void UndoAction(Timeline timeline)
        {
            actions.ForEach(action => { action.UndoAction(timeline); });
            TransformTool.instance.UpdateOverlay();
        }
    }

    public class NRActionRemoveNote : NRAction
    {
        public TargetData targetData;
        public List<TargetData> repeaterData;
        public override void DoAction(Timeline timeline)
        {
            if (repeaterData == null)
            {
                repeaterData = timeline.FindRepeaterTargets(targetData);
            }
            repeaterData.ForEach(data => { timeline.DeleteTargetFromAction(data); });

            if (targetData.isPathbuilderTarget)
            {
                timeline.pathbuilder.RemovePathbuilderTarget(targetData);
            }

            if (targetData.isRepeaterTarget)
            {
                timeline.repeaterManager.DeleteRepeaterTarget(targetData);
                /*targetData = timeline.repeaterManager.GetParentTarget(targetData);
                foreach (var section in timeline.repeaterManager.GetMatchingRepeaterSections(targetData.repeaterData))
                {
                    section.RemoveRepeaterTarget(targetData);
                }*/
            }
            else
            {
                timeline.DeleteTargetFromAction(targetData);
            }


            TransformTool.instance.UpdateOverlay();
        }
        public override void UndoAction(Timeline timeline)
        {
            repeaterData.ForEach(data => { timeline.AddTargetFromAction(data); });

            if (targetData.isRepeaterTarget)
            {
                //targetData.repeaterData.targetID = targetData.repeaterData.Section.GetCurrentTargetIndexID();
                //PathbuilderData repeaterState = new PathbuilderData();
                //repeaterState.Copy(targetData.pathbuilderData);
                //targetData = targetData.repeaterData.Section.CreateRepeaterTarget(targetData);
                timeline.repeaterManager.CreateRepeaterTarget(targetData);
                if (targetData.isPathbuilderTarget)
                {
                    timeline.pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(targetData, targetData.pathbuilderData);
                }
                /*foreach (var section in timeline.repeaterManager.GetMatchingRepeaterSections(targetData.repeaterData))
                {
                    if (targetData.repeaterData.Section == section) continue;
                    var t = section.CreateRepeaterTarget(targetData);
                    if (t.isPathbuilderTarget)
                    {
                        var repeaterState = new PathbuilderData();
                        repeaterState.Copy(targetData.pathbuilderData);
                        //now we flip the previously unflipped state again if necessary
                        if (section.mirrorHorizontally)
                        {
                            repeaterState.Flip(new Vector2(-1f, 1f));
                        }
                        if (section.mirrorVertically)
                        {
                            repeaterState.Flip(new Vector2(1f, -1f));
                        }
                        t.pathbuilderData = repeaterState;
                        timeline.pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(t, repeaterState);
                    }
                }*/
            }
            else
            {
                timeline.AddTargetFromAction(targetData);
            }

            TransformTool.instance.UpdateOverlay();
        }
    }

    public class NRActionMultiRemoveNote : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();
        public List<NRActionRemoveNote> actions;

        public override void DoAction(Timeline timeline)
        {
            if (actions == null)
            {
                actions = affectedTargets.Select(targetData => { var action = new NRActionRemoveNote(); action.targetData = targetData; return action; }).ToList();
                affectedTargets = null;
            }

            actions.ForEach(action => { action.DoAction(timeline); });
        }
        public override void UndoAction(Timeline timeline)
        {
            actions.ForEach(action => { action.UndoAction(timeline); });
        }
    }

    public class NRActionGridMoveNotes : NRAction
    {
        public List<TargetGridMoveIntent> targetGridMoveIntents = new List<TargetGridMoveIntent>();

        public override void DoAction(Timeline timeline)
        {
            targetGridMoveIntents.ForEach(intent =>
            {

                intent.target.position = intent.intendedPosition;

                if (intent.target.isRepeaterTarget)
                {
                    foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(intent.target))
                    {
                        var pos = intent.intendedPosition;

                        if (target.repeaterData.Section.mirrorHorizontally || (target.repeaterData.Section.isParent && intent.target.repeaterData.Section.mirrorHorizontally))
                        {
                            pos.x *= -1f;
                        }
                        if (target.repeaterData.Section.mirrorVertically || (target.repeaterData.Section.isParent && intent.target.repeaterData.Section.mirrorVertically))
                        {
                            pos.y *= -1f;
                        }


                        target.position = pos;
                    }
                }

            });
            timeline.UpdateState();
            TransformTool.instance.UpdateOverlay();
        }
        public override void UndoAction(Timeline timeline)
        {
            targetGridMoveIntents.ForEach(intent =>
            {

                intent.target.position = intent.startingPosition;

                if (intent.target.isRepeaterTarget)
                {
                    foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(intent.target))
                    {
                        var pos = intent.intendedPosition;

                        if (target.repeaterData.Section.mirrorHorizontally || (target.repeaterData.Section.isParent && intent.target.repeaterData.Section.mirrorHorizontally))
                        {
                            pos.x *= -1f;
                        }
                        if (target.repeaterData.Section.mirrorVertically || (target.repeaterData.Section.isParent && intent.target.repeaterData.Section.mirrorVertically))
                        {
                            pos.y *= -1f;
                        }


                        target.position = pos;
                    }
                }

            });
            timeline.UpdateState();
            TransformTool.instance.UpdateOverlay();
        }
    }

    public class NRActionTimelineMoveNotes : NRAction
    {
        public List<TargetTimelineMoveIntent> targetTimelineMoveIntents = new List<TargetTimelineMoveIntent>();

        public override void DoAction(Timeline timeline)
        {
            //First, we destroy all siblings (either because we moved out of a repeater, or because we moved too far into a repeater that another section didn't cover)
            targetTimelineMoveIntents.ForEach(intent =>
            {
                intent.startSiblingsToBeDestroyed.ForEach(data => { timeline.DeleteTargetFromAction(data); });
            });
            bool canMove = true;
            if (targetTimelineMoveIntents.Any(i => i.targetData.isPathbuilderTarget || i.targetData.legacyPathbuilderData != null))
            {
                if (targetTimelineMoveIntents.Any(i => i.targetData.isPathbuilderTarget))
                {
                    var pathbuilderTargets = targetTimelineMoveIntents.Where(i => i.targetData.isPathbuilderTarget).Select(d => d).ToList();
                    foreach (var t in pathbuilderTargets)
                    {
                        if (t.targetData.isRepeaterTarget)
                        {
                            if (!t.targetData.repeaterData.Section.Contains(t.targetData.pathbuilderData.Segments.Last().generatedNodes.Last().time) || !t.targetData.repeaterData.Section.Contains(t.targetData.time))
                            {
                                canMove = false;
                                break;
                            }
                        }
                        else
                        {
                            if (timeline.repeaterManager.IsTargetInRepeaterZone(t.targetData.pathbuilderData.Segments.Last().generatedNodes.Last().time) || timeline.repeaterManager.IsTargetInRepeaterZone(t.targetData.time))
                            {
                                canMove = false;
                                break;
                            }
                        }
                    }
                }
                if (targetTimelineMoveIntents.Any(i => i.targetData.legacyPathbuilderData != null))
                {
                    var pathbuilderTargets = targetTimelineMoveIntents.Where(i => i.targetData.legacyPathbuilderData != null).Select(d => d).ToList();
                    foreach (var t in pathbuilderTargets)
                    {
                        if (t.targetData.isRepeaterTarget)
                        {
                            if (!t.targetData.repeaterData.Section.Contains(t.targetData.legacyPathbuilderData.generatedNotes.Last().time) || !t.targetData.repeaterData.Section.Contains(t.targetData.time))
                            {
                                canMove = false;
                                break;
                            }
                        }
                        else
                        {
                            if (timeline.repeaterManager.IsTargetInRepeaterZone(t.targetData.legacyPathbuilderData.generatedNotes.Last().time) || timeline.repeaterManager.IsTargetInRepeaterZone(t.targetData.time))
                            {
                                canMove = false;
                                break;
                            }
                        }
                    }
                }
            }
            else if (targetTimelineMoveIntents.Any(t => (t.targetData.isRepeaterTarget ?
                 !t.targetData.repeaterData.Section.Contains(t.intendedTick) :
                 timeline.repeaterManager.IsTargetInRepeaterZone(t.intendedTick))))
            {
                canMove = false;
            }

            if (!canMove)
            {
                foreach (var intent in targetTimelineMoveIntents)
                {
                    intent.targetData.SetTimeFromAction(intent.startTick);
                }
                NotificationCenter.SendNotification("Can't move target into repeater zone.", NotificationType.Warning);
                return;
            }


            targetTimelineMoveIntents.ForEach(intent =>
            {
                //Move the actual note
                intent.targetData.SetTimeFromAction(intent.intendedTick);
                if (intent.targetData.isRepeaterTarget)
                {
                    intent.targetData.repeaterData.RelativeTime = new QNT_Timestamp(intent.targetData.time.tick - intent.targetData.repeaterData.Section.startTime.tick);
                    foreach (var repeaterTarget in timeline.repeaterManager.GetMatchingRepeaterTargets(intent.targetData))
                    {
                        QNT_Timestamp newTime = new QNT_Timestamp(repeaterTarget.repeaterData.Section.startTime.tick + intent.targetData.repeaterData.RelativeTime.tick);
                        repeaterTarget.SetTimeFromAction(newTime);
                        repeaterTarget.repeaterData.RelativeTime = intent.targetData.repeaterData.RelativeTime;
                        repeaterTarget.repeaterData.Section.UpdateActiveNotes();
                    }
                    intent.targetData.repeaterData.Section.UpdateActiveNotes();
                }

                //Then, we move all the siblings by the delta
                intent.startSiblingsToBeMoved.ForEach(sibling => { sibling.SetTimeFromAction(sibling.time + (intent.intendedTick - intent.startTick)); });

                //Finally, create targets in the ending section (if any exist)
                intent.endRepeaterSiblingsToBeCreated.ForEach(data => { timeline.AddTargetFromAction(data); });
            });
            timeline.SortOrderedList();
            timeline.UpdateState();
            TransformTool.instance.UpdateOverlay();
        }
        public override void UndoAction(Timeline timeline)
        {
            //First, destroy targets in the ending section (if any exist)
            targetTimelineMoveIntents.ForEach(intent =>
            {
                intent.endRepeaterSiblingsToBeCreated.ForEach(data => { timeline.DeleteTargetFromAction(data); });
            });

            targetTimelineMoveIntents.ForEach(intent =>
            {
                //Then, we move all the siblings by the delta
                intent.startSiblingsToBeMoved.ForEach(sibling => { sibling.SetTimeFromAction(sibling.time + (intent.startTick - intent.intendedTick)); });

                //First, we move the actual note
                intent.targetData.SetTimeFromAction(intent.startTick);
                if (intent.targetData.isRepeaterTarget)
                {
                    intent.targetData.repeaterData.RelativeTime = new QNT_Timestamp(intent.targetData.time.tick - intent.targetData.repeaterData.Section.startTime.tick);
                    foreach (var repeaterTarget in timeline.repeaterManager.GetMatchingRepeaterTargets(intent.targetData))
                    {
                        QNT_Timestamp newTime = new QNT_Timestamp(repeaterTarget.repeaterData.Section.startTime.tick + intent.targetData.repeaterData.RelativeTime.tick);
                        repeaterTarget.SetTimeFromAction(newTime);
                        repeaterTarget.repeaterData.RelativeTime = intent.targetData.repeaterData.RelativeTime;
                        repeaterTarget.repeaterData.Section.UpdateActiveNotes();
                    }
                    intent.targetData.repeaterData.Section.UpdateActiveNotes();
                }

                //Next, we create all siblings (either because we moved out of a repeater, or because we moved too far into a repeater that another section didn't cover)
                intent.startSiblingsToBeDestroyed.ForEach(data => { timeline.AddTargetFromAction(data); });
            });
            timeline.SortOrderedList();
            timeline.UpdateState();
            TransformTool.instance.UpdateOverlay();
        }
    }

    public class NRActionSwapNoteColors : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();

        public override void DoAction(Timeline timeline)
        {
            affectedTargets.ForEach(targetData =>
            {

                if (targetData.isRepeaterTarget)
                {
                    var parent = timeline.repeaterManager.GetParentTarget(targetData);
                    if (parent.handType == TargetHandType.Left)
                    {
                        parent.handType = TargetHandType.Right;
                    }
                    else if (parent.handType == TargetHandType.Right)
                    {
                        parent.handType = TargetHandType.Left;
                    }
                    foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(parent))
                    {
                        if (target.repeaterData.Section.flipTargetColors)
                        {
                            if (parent.handType == TargetHandType.Left)
                            {
                                target.handType = TargetHandType.Right;
                            }
                            else if (parent.handType == TargetHandType.Right)
                            {
                                target.handType = TargetHandType.Left;
                            }
                        }
                        else
                        {
                            target.handType = parent.handType;
                        }
                    }
                    if (parent.behavior == TargetBehavior.Legacy_Pathbuilder)
                    {
                        switch (parent.legacyPathbuilderData.handType)
                        {
                            case TargetHandType.Left:
                                parent.legacyPathbuilderData.handType = TargetHandType.Right;
                                break;

                            case TargetHandType.Right:
                                parent.legacyPathbuilderData.handType = TargetHandType.Left;
                                break;
                        }

                        parent.handType = targetData.handType;
                        ChainBuilder.ChainBuilder.GenerateChainNotes(parent);
                    }
                    return;
                }
                else
                {
                    switch (targetData.handType)
                    {
                        case TargetHandType.Left:
                            targetData.handType = TargetHandType.Right;
                            break;

                        case TargetHandType.Right:
                            targetData.handType = TargetHandType.Left;
                            break;
                    }
                }


                if (targetData.behavior == TargetBehavior.Legacy_Pathbuilder)
                {
                    switch (targetData.legacyPathbuilderData.handType)
                    {
                        case TargetHandType.Left:
                            targetData.legacyPathbuilderData.handType = TargetHandType.Right;
                            break;

                        case TargetHandType.Right:
                            targetData.legacyPathbuilderData.handType = TargetHandType.Left;
                            break;
                    }

                    targetData.handType = targetData.handType;
                    ChainBuilder.ChainBuilder.GenerateChainNotes(targetData);
                }
            });
        }
        public override void UndoAction(Timeline timeline)
        {
            DoAction(timeline); //Swap is symmetrical
        }
    }

    public class NRActionHFlipNotes : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();

        public float FlipAngle(float angle)
        {
            angle = ((angle + 180) % 360) - 180;
            return -angle;
        }

        public override void DoAction(Timeline timeline)
        {
            affectedTargets.ForEach(targetData =>
            {
                targetData.x *= -1;
                if (targetData.isPathbuilderTarget)
                {
                    targetData.pathbuilderData.Flip(new Vector2(-1, 1));
                }
                if (targetData.behavior == TargetBehavior.Legacy_Pathbuilder)
                {
                    targetData.legacyPathbuilderData.initialAngle = FlipAngle(targetData.legacyPathbuilderData.initialAngle);
                    targetData.legacyPathbuilderData.angle *= -1;
                    targetData.legacyPathbuilderData.angleIncrement *= -1;

                    ChainBuilder.ChainBuilder.GenerateChainNotes(targetData);
                }
                if (targetData.isRepeaterTarget)
                {
                    foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                    {
                        target.x *= -1;
                    }
                }
            });
            TransformTool.instance.UpdateOverlay();
        }
        public override void UndoAction(Timeline timeline)
        {
            DoAction(timeline); //Swap is symmetrical
        }
    }

    public class NRActionVFlipNotes : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();

        public float FlipAngle(float angle)
        {
            angle = ((angle + 180) % 360) - 180;

            if (angle >= 0)
                return 180 - angle;
            else
                return -180 - angle;
        }

        public override void DoAction(Timeline timeline)
        {
            affectedTargets.ForEach(targetData =>
            {
                if (targetData.behavior != TargetBehavior.Melee)
                {
                    targetData.y *= -1;
                    if (targetData.isPathbuilderTarget)
                    {
                        targetData.pathbuilderData.Flip(new Vector2(1, -1));
                    }
                    if (targetData.behavior == TargetBehavior.Legacy_Pathbuilder)
                    {
                        targetData.legacyPathbuilderData.initialAngle = FlipAngle(targetData.legacyPathbuilderData.initialAngle);
                        targetData.legacyPathbuilderData.angle *= -1;
                        targetData.legacyPathbuilderData.angleIncrement *= -1;

                        ChainBuilder.ChainBuilder.GenerateChainNotes(targetData);
                    }
                    if (targetData.isRepeaterTarget)
                    {
                        foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                        {
                            target.y *= -1;
                        }
                    }
                }

            });
            TransformTool.instance.UpdateOverlay();
        }
        public override void UndoAction(Timeline timeline)
        {
            DoAction(timeline); //Swap is symmetrical
        }
    }

    public class NRActionScale : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();

        public Vector2 scale;

        public override void DoAction(Timeline timeline)
        {
            affectedTargets.ForEach(targetData =>
            {
                if (targetData.behavior != TargetBehavior.Melee)
                {
                    if (!targetData.isPathbuilderTarget)
                    {
                        targetData.y *= scale.y;
                        targetData.x *= scale.x;
                    }
                    else
                    {
                        targetData.pathbuilderData.Scale(targetData, scale, true);
                        timeline.pathbuilder.UpdatePathbuilderTargetFromAction(targetData, targetData.pathbuilderData);
                    }

                    if (targetData.behavior == TargetBehavior.Legacy_Pathbuilder)
                    {
                        targetData.legacyPathbuilderData.stepDistance *= scale.x;

                        ChainBuilder.ChainBuilder.GenerateChainNotes(targetData);
                    }
                }
                if (targetData.isRepeaterTarget)
                {
                    foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                    {
                        target.y *= scale.y;
                        target.x *= scale.x;
                    }
                }
            });
            TransformTool.instance.UpdateOverlay();
        }
        public override void UndoAction(Timeline timeline)
        {
            affectedTargets.ForEach(targetData =>
            {
                if (targetData.behavior != TargetBehavior.Melee)
                {
                    if (!targetData.isPathbuilderTarget)
                    {
                        targetData.y /= scale.y;
                        targetData.x /= scale.x;
                    }
                    else
                    {
                        targetData.pathbuilderData.Scale(targetData, scale, false);
                        timeline.pathbuilder.UpdatePathbuilderTargetFromAction(targetData, targetData.pathbuilderData);
                    }
                    if (targetData.behavior == TargetBehavior.Legacy_Pathbuilder)
                    {
                        targetData.legacyPathbuilderData.stepDistance /= scale.x;

                        ChainBuilder.ChainBuilder.GenerateChainNotes(targetData);
                    }
                }
            });
            TransformTool.instance.UpdateOverlay();
        }
    }

    public class NRActionRotate : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();

        public float rotateAngle = 0;

        public Vector2 rotateCenter = Vector2.zero;

        public void NRRotate(TargetData data, Vector2 center, float angle)
        {
            if (data.isPathbuilderTarget)
            {
                data.pathbuilderData.Rotate(data, center, angle);
                return;
            }
           
            data.x -= center.x;
            data.y -= center.y;
            angle = -angle;
            

            Vector2 rotate;

            rotate.x = (float)(data.x * Math.Cos(angle / 180f * Math.PI) + data.y * Math.Sin(angle / 180f * Math.PI));
            rotate.y = (float)(data.x * -Math.Sin(angle / 180f * Math.PI) + data.y * Math.Cos(angle / 180f * Math.PI));
            rotate.x += center.x;
            rotate.y += center.y;

            
            data.x = rotate.x;
            data.y = rotate.y;
            
            

        }
        public override void DoAction(Timeline timeline)
        {
            affectedTargets.ForEach(targetData =>
            {
                if (targetData.behavior != TargetBehavior.Melee)
                {
                    NRRotate(targetData, rotateCenter, rotateAngle);
                    if (targetData.isPathbuilderTarget)
                    {
                        timeline.pathbuilder.UpdatePathbuilderTargetFromAction(targetData, targetData.pathbuilderData);
                    }
                    if (targetData.behavior == TargetBehavior.Legacy_Pathbuilder)
                    {
                        targetData.legacyPathbuilderData.initialAngle -= rotateAngle;

                        ChainBuilder.ChainBuilder.GenerateChainNotes(targetData);
                    }
                    if (targetData.isRepeaterTarget)
                    {
                        foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                        {
                            NRRotate(target, rotateCenter, rotateAngle);
                        }
                    }
                }
            });
            TransformTool.instance.UpdateOverlay();
        }
        public override void UndoAction(Timeline timeline)
        {
            affectedTargets.ForEach(targetData =>
            {
                if (targetData.behavior != TargetBehavior.Melee)
                {
                    NRRotate(targetData, rotateCenter, -rotateAngle);
                    if (targetData.isPathbuilderTarget)
                    {
                        timeline.pathbuilder.UpdatePathbuilderTargetFromAction(targetData, targetData.pathbuilderData);
                    }
                    if (targetData.behavior == TargetBehavior.Legacy_Pathbuilder)
                    {
                        targetData.legacyPathbuilderData.initialAngle += rotateAngle;

                        ChainBuilder.ChainBuilder.GenerateChainNotes(targetData);
                    }
                    if (targetData.isRepeaterTarget)
                    {
                        foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                        {
                            NRRotate(target, rotateCenter, -rotateAngle);
                        }
                    }
                }
            });
            TransformTool.instance.UpdateOverlay();
        }
    }


    public class NRActionReverse : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();
        NRActionTimelineMoveNotes moveAction;

        public override void DoAction(Timeline timeline)
        {
            if (moveAction == null)
            {
                bool first = true;

                ulong firstTick = 0, lastTick = 0;

                //Find the first and last note in the sequence
                foreach (TargetData data in affectedTargets)
                {

                    if (first)
                    {
                        firstTick = data.time.tick;
                        lastTick = data.time.tick;
                        first = false;
                    }

                    else if (data.time.tick > lastTick)
                    {
                        lastTick = data.time.tick;
                    }

                    else if (data.time.tick < firstTick)
                    {
                        firstTick = data.time.tick;
                    }

                }

                List<TargetTimelineMoveIntent> intents = new List<TargetTimelineMoveIntent>();
                foreach (TargetData data in affectedTargets)
                {
                    ulong amt = data.time.tick - firstTick;
                    TargetTimelineMoveIntent intent = new TargetTimelineMoveIntent();
                    intent.targetData = data;
                    intent.startTick = data.time;
                    intent.intendedTick = new QNT_Timestamp(lastTick - amt);
                    intents.Add(intent);
                }

                moveAction = timeline.GenerateMoveTimelineAction(intents);
            }

            moveAction.DoAction(timeline);
        }
        public override void UndoAction(Timeline timeline)
        {
            moveAction.UndoAction(timeline);
        }
    }

    public class NRActionSetTargetHitsound : NRAction
    {
        public List<TargetSetHitsoundIntent> targetSetHitsoundIntents = new List<TargetSetHitsoundIntent>();

        public override void DoAction(Timeline timeline)
        {
            targetSetHitsoundIntents.ForEach(intent =>
            {
                intent.target.velocity = intent.newVelocity;
                if (intent.target.isPathbuilderTarget)
                {
                    if (intent.target.behavior != TargetBehavior.ChainStart)
                    {
                        intent.target.data.pathbuilderData.SetHitsound(intent.newVelocity);
                    }
                }
                if (intent.target.isRepeaterTarget)
                {
                    foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(intent.target))
                    {
                        target.velocity = intent.newVelocity;
                    }
                }
            });
        }
        public override void UndoAction(Timeline timeline)
        {
            targetSetHitsoundIntents.ForEach(intent =>
            {
                intent.target.velocity = intent.startingVelocity;
                if (intent.target.isPathbuilderTarget)
                {
                    if (intent.target.behavior != TargetBehavior.ChainStart)
                    {
                        intent.target.data.pathbuilderData.SetHitsound(intent.startingVelocity);
                    }
                }
                if (intent.target.isRepeaterTarget)
                {
                    foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(intent.target))
                    {
                        target.velocity = intent.startingVelocity;
                    }
                }
            });
        }
    }

    public class NRActionSetTargetBehavior : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();
        public TargetBehavior newBehavior;

        List<TargetBehavior> oldBehavior = new List<TargetBehavior>();
        List<TargetHandType> oldHandTypes = new List<TargetHandType>();
        List<InternalTargetVelocity> oldVelocities = new List<InternalTargetVelocity>();
        List<QNT_Duration> oldBeatLength = new List<QNT_Duration>();

        public override void DoAction(Timeline timeline)
        {
            oldBehavior = new List<TargetBehavior>();

            affectedTargets.ForEach(targetData =>
            {
                InternalTargetVelocity velocity = InternalTargetVelocity.Silent;

                if (newBehavior == TargetBehavior.ChainStart)
                {
                    velocity = InternalTargetVelocity.ChainStart;
                }
                else if (newBehavior == TargetBehavior.ChainNode)
                {
                    velocity = InternalTargetVelocity.Chain;
                }
                else if (newBehavior == TargetBehavior.Melee)
                {
                    velocity = InternalTargetVelocity.Melee;
                }
                else if (newBehavior == TargetBehavior.Mine)
                {
                    velocity = InternalTargetVelocity.Mine;
                }
                else if (newBehavior == TargetBehavior.Standard ||
                        newBehavior == TargetBehavior.Sustain ||
                        newBehavior == TargetBehavior.Horizontal ||
                        newBehavior == TargetBehavior.Vertical)
                {
                    velocity = InternalTargetVelocity.Kick;
                }

                //Path notes and regular notes both use the same beat length
                oldBeatLength.Add(targetData.beatLength);

                if (targetData.behavior == TargetBehavior.Legacy_Pathbuilder)
                {
                    oldBehavior.Add(targetData.legacyPathbuilderData.behavior);
                    oldHandTypes.Add(targetData.legacyPathbuilderData.handType);
                    oldVelocities.Add(targetData.legacyPathbuilderData.velocity);

                    targetData.legacyPathbuilderData.behavior = newBehavior;
                    if (velocity != InternalTargetVelocity.Silent) targetData.legacyPathbuilderData.velocity = velocity;

                    //Fix hand type when going to melee
                    if (newBehavior == TargetBehavior.Melee)
                    {
                        targetData.legacyPathbuilderData.handType = TargetHandType.Either;
                    }

                    //Fixup hand type when coming from melee
                    if (oldBehavior.Last() == TargetBehavior.Melee)
                    {
                        targetData.legacyPathbuilderData.handType = targetData.handType;
                    }

                    ChainBuilder.ChainBuilder.GenerateChainNotes(targetData);
                }
                else
                {
                    oldBehavior.Add(targetData.behavior);
                    oldHandTypes.Add(targetData.handType);
                    oldVelocities.Add(targetData.velocity);

                    //if (velocity != InternalTargetVelocity.Silent) targetData.velocity = velocity;
                    if (newBehavior.IsMeleeOrMine()) targetData.velocity = velocity;
                    targetData.behavior = newBehavior;


                    if (targetData.isPathbuilderTarget)
                    {
                        targetData.pathbuilderData.SetBehavior(newBehavior);
                        if (velocity != InternalTargetVelocity.Silent) targetData.pathbuilderData.SetHitsound(velocity);
                    }

                    //Fix hand type when going to melee
                    if (newBehavior == TargetBehavior.Melee)
                    {
                        targetData.handType = TargetHandType.Either;
                    }

                    //Fixup hand type when coming from melee
                    if (oldBehavior.Last() == TargetBehavior.Melee)
                    {
                        targetData.handType = TargetHandType.Left;
                    }

                    if (TargetData.BehaviorSupportsBeatLength(newBehavior, targetData.isPathbuilderTarget) && targetData.beatLength < Constants.QuarterNoteDuration)
                    {
                        targetData.beatLength = Constants.QuarterNoteDuration;
                    }

                    if (targetData.isRepeaterTarget)
                    {
                        foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                        {
                            target.handType = targetData.handType;
                            target.beatLength = targetData.beatLength;
                            target.behavior = targetData.behavior;
                        }
                    }
                }
            });
        }
        public override void UndoAction(Timeline timeline)
        {
            for (int i = 0; i < affectedTargets.Count; ++i)
            {
                if (affectedTargets[i].behavior == TargetBehavior.Legacy_Pathbuilder)
                {
                    affectedTargets[i].legacyPathbuilderData.behavior = oldBehavior[i];
                    affectedTargets[i].legacyPathbuilderData.velocity = oldVelocities[i];
                    affectedTargets[i].legacyPathbuilderData.handType = oldHandTypes[i];
                }
                else
                {
                    affectedTargets[i].behavior = oldBehavior[i];
                    affectedTargets[i].handType = oldHandTypes[i];
                    affectedTargets[i].velocity = oldVelocities[i];
                    if (affectedTargets[i].isPathbuilderTarget)
                    {
                        affectedTargets[i].pathbuilderData.SetBehavior(oldBehavior[i]);
                        affectedTargets[i].pathbuilderData.SetHitsound(oldVelocities[i]);
                    }
                    if (affectedTargets[i].isRepeaterTarget)
                    {
                        foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(affectedTargets[i]))
                        {
                            target.behavior = oldBehavior[i];
                            target.handType = oldHandTypes[i];
                            target.velocity = oldVelocities[i];
                            target.beatLength = oldBeatLength[i];
                        }
                    }
                }

                affectedTargets[i].beatLength = oldBeatLength[i];
            }
        }
    }

    public class NRActionDeselectBehavior : NRAction
    {
        public TargetBehavior behaviorToDeselect;
        Target[] deselectedTargets;
        public override void DoAction(Timeline timeline)
        {
            deselectedTargets = timeline.selectedNotes
                                .Where(target => target.data.behavior == behaviorToDeselect)
                                .ToArray();
            foreach (var target in deselectedTargets)
            {
                target.Deselect();
                timeline.selectedNotes.Remove(target);
            }
            TransformTool.instance.UpdateOverlay();
        }

        public override void UndoAction(Timeline timeline)
        {
            foreach (var target in deselectedTargets)
            {
                target.Select();
                timeline.selectedNotes.Add(target);
            }
            TransformTool.instance.UpdateOverlay();
        }
    }

    public class NRActionDeselectHand : NRAction
    {
        public TargetHandType handToDeselect;
        Target[] deselectedTargets;
        public override void DoAction(Timeline timeline)
        {
            deselectedTargets = timeline.selectedNotes.Where(target => target.data.handType == handToDeselect).ToArray();
            foreach (var target in deselectedTargets)
            {
                target.Deselect();
                timeline.selectedNotes.Remove(target);
            }
            TransformTool.instance.UpdateOverlay();
        }

        public override void UndoAction(Timeline timeline)
        {
            foreach (var target in deselectedTargets)
            {
                target.Select();
                timeline.selectedNotes.Add(target);
            }
            TransformTool.instance.UpdateOverlay();
        }
    }

    public class NRActionUpdatePathbuilderTarget : NRAction
    {
        private TargetData targetData;
        private Pathbuilder pathbuilder;

        private PathbuilderData oldState;
        private PathbuilderData newState;

        public NRActionUpdatePathbuilderTarget(TargetData targetData, Pathbuilder pathbuilder, PathbuilderData data)
        {
            this.targetData = targetData;
            this.newState = data;
            this.oldState = targetData.pathbuilderData;
            this.pathbuilder = pathbuilder;
        }
        public override void DoAction(Timeline timeline)
        {
            if (targetData.isRepeaterTarget)
            {
                var parent = timeline.repeaterManager.GetParentTarget(targetData);

                foreach (var segment in newState.Segments)
                {
                    foreach (var node in segment.generatedNodes)
                    {
                        timeline.DeleteTargetFromAction(node);
                    }
                }

                //we un-flip the state so we can apply it to the parent
                if (targetData.repeaterData.Section.mirrorHorizontally)
                {
                    newState.Flip(new Vector2(-1f, 1f));
                }
                if (targetData.repeaterData.Section.mirrorVertically)
                {
                    newState.Flip(new Vector2(1f, -1f));
                }
                if (parent == targetData)
                {
                    pathbuilder.UpdatePathbuilderTargetFromAction(parent, newState);
                }
                else
                {
                    targetData = parent;
                    pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(parent, newState);
                }
                foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(parent))
                {

                    PathbuilderData repeaterState = new PathbuilderData();
                    repeaterState.Copy(newState);
                    //now we flip the previously unflipped state again if necessary
                    if (target.repeaterData.Section.mirrorHorizontally)
                    {
                        repeaterState.Flip(new Vector2(-1f, 1f));
                    }
                    if (target.repeaterData.Section.mirrorVertically)
                    {
                        repeaterState.Flip(new Vector2(1f, -1f));
                    }
                    target.isPathbuilderTarget = targetData.isPathbuilderTarget;
                    if (target == targetData && targetData != parent)
                    {
                        pathbuilder.UpdatePathbuilderTargetFromAction(target, repeaterState);
                    }
                    else
                    {
                        pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(target, repeaterState);
                    }
                    target.repeaterData.Section.UpdateActiveNotes();
                }
                parent.repeaterData.Section.UpdateActiveNotes();
            }
            else
            {
                pathbuilder.UpdatePathbuilderTargetFromAction(targetData, newState);
            }
        }

        public override void UndoAction(Timeline timeline)
        {
            pathbuilder.UpdatePathbuilderTargetFromAction(targetData, oldState);
            if (targetData.isRepeaterTarget)
            {
                foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                {
                    if (target.isPathbuilderTarget)
                    {
                        PathbuilderData repeaterState = new PathbuilderData();
                        repeaterState.Copy(oldState);
                        target.isPathbuilderTarget = targetData.isPathbuilderTarget;
                        pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(target, repeaterState);
                    }
                }
            }
        }
    }

    public class NRActionBakePathbuilderTarget : NRAction
    {
        private TargetData targetData;
        private Pathbuilder pathbuilder;
        private PathbuilderData oldState;
        private Dictionary<QNT_Timestamp, PathbuilderData> oldRepeaterState;

        public NRActionBakePathbuilderTarget(TargetData targetData, Pathbuilder pathbuilder)
        {
            this.targetData = targetData;
            this.pathbuilder = pathbuilder;
            this.oldState = targetData.pathbuilderData;
            oldRepeaterState = new Dictionary<QNT_Timestamp, PathbuilderData>();
        }

        public override void DoAction(Timeline timeline)
        {
            oldRepeaterState.Clear();
            pathbuilder.BakeTarget(targetData);
            if (targetData.isRepeaterTarget)
            {
                foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                {
                    oldRepeaterState.Add(target.time, target.pathbuilderData);
                    if (target.isPathbuilderTarget)
                    {
                        pathbuilder.BakeTarget(target, true);
                    }
                }
            }
        }

        public override void UndoAction(Timeline timeline)
        {
            foreach (var segment in oldState.Segments)
            {
                foreach (var node in segment.generatedNodes)
                {
                    timeline.DeleteTargetFromAction(node);
                }
            }
            pathbuilder.UpdatePathbuilderTargetFromAction(targetData, oldState);

            if (targetData.isRepeaterTarget)
            {
                foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                {
                    var state = oldRepeaterState[target.time];
                    foreach (var segment in state.Segments)
                    {
                        foreach (var node in segment.generatedNodes)
                        {
                            timeline.DeleteTargetFromAction(node);
                        }
                    }
                    pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(target, state);
                }
            }
        }
    }

    public class NRActionConvertNoteToLegacyPathbuilder : NRAction
    {
        public TargetData data;
        public QNT_Duration oldBeatLength;
        public LegacyPathbuilderData pathBuilderData = new LegacyPathbuilderData();

        public override void DoAction(Timeline timeline)
        {
            if (data.isRepeaterTarget)
            {
                data = timeline.repeaterManager.GetParentTarget(data);
                pathBuilderData.behavior = data.behavior;
                pathBuilderData.velocity = data.velocity;
                oldBeatLength = data.beatLength;
                if (data.beatLength < Constants.QuarterNoteDuration)
                {
                    data.beatLength = Constants.QuarterNoteDuration;
                }
                if (data.repeaterData.Section.flipTargetColors)
                {
                    if (data.handType == TargetHandType.Left)
                    {
                        pathBuilderData.handType = TargetHandType.Right;
                    }
                    else if (data.handType == TargetHandType.Right)
                    {
                        pathBuilderData.handType = TargetHandType.Left;
                    }
                }
                else
                {
                    pathBuilderData.handType = data.handType;
                }
                data.legacyPathbuilderData = pathBuilderData;
                data.behavior = TargetBehavior.Legacy_Pathbuilder;

                foreach (var sibling in timeline.repeaterManager.GetMatchingRepeaterTargets(data))
                {
                    sibling.beatLength = data.beatLength;
                    LegacyPathbuilderData siblingData = new LegacyPathbuilderData();
                    siblingData.Copy(pathBuilderData);
                    sibling.legacyPathbuilderData = siblingData;
                    sibling.behavior = TargetBehavior.Legacy_Pathbuilder;
                }
                ChainBuilder.ChainBuilder.GenerateChainNotes(data);
            }
            else
            {
                pathBuilderData.behavior = data.behavior;
                pathBuilderData.velocity = data.velocity;
                pathBuilderData.handType = data.handType;
                data.legacyPathbuilderData = pathBuilderData;

                //Ensure the path builder always starts with a quarter note of build time
                oldBeatLength = data.beatLength;
                if (data.beatLength < Constants.QuarterNoteDuration)
                {
                    data.beatLength = Constants.QuarterNoteDuration;
                }

                data.behavior = TargetBehavior.Legacy_Pathbuilder;
                ChainBuilder.ChainBuilder.GenerateChainNotes(data);
            }


        }
        public override void UndoAction(Timeline timeline)
        {
            data.legacyPathbuilderData.DeleteCreatedNotes(timeline);
            data.behavior = data.legacyPathbuilderData.behavior;
            data.velocity = data.legacyPathbuilderData.velocity;
            data.handType = data.legacyPathbuilderData.handType;
            data.beatLength = oldBeatLength;
            data.legacyPathbuilderData = null;

            if (data.isRepeaterTarget)
            {
                foreach(var sibling in timeline.repeaterManager.GetMatchingRepeaterTargets(data))
                {
                    sibling.legacyPathbuilderData.DeleteCreatedNotes(timeline);
                    sibling.behavior = sibling.legacyPathbuilderData.behavior;
                    sibling.velocity = sibling.legacyPathbuilderData.velocity;
                    sibling.handType = sibling.legacyPathbuilderData.handType;
                    sibling.beatLength = oldBeatLength;
                    sibling.legacyPathbuilderData = null;
                }
            }
        }
    }

    public class NRActionBakePath : NRAction
    {
        public NRActionRemoveNote removeNoteAction;

        public override void DoAction(Timeline timeline)
        {
            //Generate and create real notes
            ChainBuilder.ChainBuilder.GenerateChainNotes(removeNoteAction.targetData);
            foreach (TargetData genData in removeNoteAction.targetData.legacyPathbuilderData.generatedNotes)
            {
                TargetData newData = new(genData);
                timeline.AddTargetFromAction(newData);
                if (removeNoteAction.targetData.isRepeaterTarget)
                {
                    removeNoteAction.targetData.repeaterData.Section.AddExistingTargetToRepeater(newData);
                }
            }
            if (removeNoteAction.targetData.isRepeaterTarget)
            {
                foreach (var sibling in timeline.repeaterManager.GetMatchingRepeaterTargets(removeNoteAction.targetData))
                {
                    foreach (var node in sibling.legacyPathbuilderData.generatedNotes)
                    {
                        TargetData newNode = new(node);
                        timeline.AddTargetFromAction(newNode);
                        sibling.repeaterData.Section.AddExistingTargetToRepeater(newNode);
                    }
                    timeline.DeleteTargetFromAction(sibling);
                }
            }

            //Destroy the path builder note (and all the generated transient notes

            removeNoteAction.DoAction(timeline);
        }
        public override void UndoAction(Timeline timeline)
        {
            removeNoteAction.UndoAction(timeline);

            //Recalculate the notes, and remove the "real" notes
            ChainBuilder.ChainBuilder.CalculateChainNotes(removeNoteAction.targetData);
            foreach (TargetData genData in removeNoteAction.targetData.legacyPathbuilderData.generatedNotes)
            {
                var foundData = timeline.FindTargetData(genData.time, genData.behavior, genData.handType);
                if (foundData != null)
                {
                    timeline.DeleteTargetFromAction(foundData);
                }
            }
            ChainBuilder.ChainBuilder.GenerateChainNotes(removeNoteAction.targetData);
        }
    }

    public class NRActionAddRepeaterSection : NRAction
    {
        public RepeaterSection section;
        public NRActionMultiAddNote addTargets = new NRActionMultiAddNote();
        public NRActionMultiRemoveNote removeTargets = new NRActionMultiRemoveNote();

        public override void DoAction(Timeline timeline)
        {
            addTargets.DoAction(timeline);
            removeTargets.DoAction(timeline);
            timeline.AddRepeaterSectionFromAction(section);
        }

        public override void UndoAction(Timeline timeline)
        {
            timeline.RemoveRepeaterSectionFromAction(section);
            addTargets.UndoAction(timeline);
            removeTargets.UndoAction(timeline);
        }
    };

    public class NRActionRemoveRepeaterSection : NRAction
    {
        public RepeaterSection section;

        public override void DoAction(Timeline timeline)
        {
            timeline.RemoveRepeaterSectionFromAction(section);
        }

        public override void UndoAction(Timeline timeline)
        {
            timeline.AddRepeaterSectionFromAction(section);
        }
    };
}