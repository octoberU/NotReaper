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
    public class UndoRedoManager : Singleton<UndoRedoManager>
    {

        /// <summary>
        /// Contains the complete list of actions the user has done recently.
        /// </summary>
        private static List<NRAction> actions = new List<NRAction>();

        /// <summary>
        /// Contains the actions the user has "undone" for future use.
        /// </summary>
        private static List<NRAction> redoActions = new List<NRAction>();

        private const int MaxSavedActions = 20;

        private static Timeline timeline;

        private void Start()
        {
            timeline = NRDependencyInjector.Get<Timeline>();
            EditorState.OnEditorReset += ClearActions;
        }

        /// <summary>
        /// Undo the last action performed by the user.
        /// </summary>
        public static void Undo()
        {
            if (actions.Count <= 0) return;

            NRAction action = actions.Last();

            action.UndoAction(timeline);

            redoActions.Add(action);
            actions.RemoveAt(actions.Count - 1);

            EditorScale.ReapplyScale();
        }
        /// <summary>
        /// Redo the last action the user has undone.
        /// </summary>
        public static void Redo()
        {

            if (redoActions.Count <= 0) return;

            NRAction action = redoActions.Last();

            action.DoAction(timeline);

            actions.Add(action);
            redoActions.RemoveAt(redoActions.Count - 1);
            EditorScale.ReapplyScale();
        }
        /// <summary>
        /// Add an action that can be un- and redone.
        /// </summary>
        /// <param name="action">The actino to add.</param>
        public static void AddAction(NRAction action)
        {
            action.DoAction(timeline);
            if (actions.Count <= MaxSavedActions)
            {
                actions.Add(action);
            }
            else
            {
                while (MaxSavedActions > actions.Count)
                {
                    actions.RemoveAt(0);
                }
                actions.Add(action);
            }
            redoActions = new List<NRAction>();
        }

        public static void ClearActions()
        {
            actions = new List<NRAction>();
            redoActions = new List<NRAction>();
        }
    }

    public abstract class NRAction
    {
        protected List<Target> chainStarts = new();
        public abstract void DoAction(Timeline timeline);
        public abstract void UndoAction(Timeline timeline);

        protected void FindChainStart(TargetData data)
        {
            var start = TargetFinder.FindChainStart(data);
            if (start != null && !chainStarts.Contains(start))
                chainStarts.Add(start);
        }

        protected void UpdateChainConnectors()
        {
            foreach (var start in chainStarts)
                EditorTargets.UpdateChainConnector(start);
        }
            
    }

    public class NRActionAddNote : NRAction
    {
        public TargetData targetData;

        public NRActionAddNote() { }
        public NRActionAddNote(TargetData data) => targetData = data;

        public override void DoAction(Timeline timeline)
        {
            if (timeline.repeaterManager.IsTargetInRepeaterZone(targetData, out RepeaterData repeaterData))
            {
                var parent = timeline.repeaterManager.GetParentRepeater(repeaterData.Section);
                targetData.SetTimeFromAction(new(parent.startTime.tick + repeaterData.RelativeTime.tick));
                if (repeaterData.Section.flipTargetColors)
                {
                    if(targetData.handType == TargetHandType.Left)
                    {
                        targetData.handType = TargetHandType.Right;
                    }
                    else if(targetData.handType == TargetHandType.Right)
                    {
                        targetData.handType = TargetHandType.Left;
                    }
                }
                if (repeaterData.Section.mirrorHorizontally)
                {
                    targetData.x *= -1;
                }
                if (repeaterData.Section.mirrorVertically)
                {
                    targetData.y *= -1;
                }
                timeline.repeaterManager.CreateRepeaterTarget(targetData);
            }
            else
            {
                //timeline.AddTargetFromAction(targetData);
                EditorTargets.AddTargetFromAction(targetData);
            }

            if (targetData.isPathbuilderTarget)
            {
                timeline.pathbuilder.UpdatePathbuilderTargetFromAction(targetData, targetData.pathbuilderData);
            }
        }
        public override void UndoAction(Timeline timeline)
        {
            if (targetData.isRepeaterTarget)
            {
                timeline.repeaterManager.DeleteRepeaterTarget(targetData);
            }
            else
            {
                if (targetData.isPathbuilderTarget)
                {
                    foreach (var segment in targetData.pathbuilderData.Segments)
                    {
                        foreach (var node in segment.generatedNodes)
                        {
                            EditorTargets.DeleteTargetFromAction(node);
                        }
                    }
                }
                EditorTargets.DeleteTargetFromAction(targetData);
            }
        }
    }

    public class NRActionMultiAddNote : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();
        public List<NRActionAddNote> actions;

        public NRActionMultiAddNote() { }
        public NRActionMultiAddNote(List<TargetData> targets) => affectedTargets = targets;

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

        public NRActionRemoveNote() { }
        public NRActionRemoveNote(TargetData data) => targetData = data;
        public override void DoAction(Timeline timeline)
        {

            if (targetData.isRepeaterTarget) targetData = timeline.repeaterManager.GetParentTarget(targetData);


            if (targetData.isPathbuilderTarget)
            {
                timeline.pathbuilder.RemovePathbuilderTarget(targetData);
            }

            if (targetData.isRepeaterTarget)
            {
                timeline.repeaterManager.DeleteRepeaterTarget(targetData);
            }
            else
            {
                EditorTargets.DeleteTargetFromAction(targetData);
            }


            TransformTool.instance.UpdateOverlay();
        }
        public override void UndoAction(Timeline timeline)
        {

            if (targetData.isRepeaterTarget)
            {
                timeline.repeaterManager.CreateRepeaterTarget(targetData);
                if (targetData.isPathbuilderTarget)
                {
                    timeline.pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(targetData, targetData.pathbuilderData);
                }               
            }
            else
            {
                EditorTargets.AddTargetFromAction(targetData);
                if (targetData.isPathbuilderTarget)
                    timeline.pathbuilder.UpdatePathbuilderTargetFromAction(targetData, targetData.pathbuilderData);
            }

            TransformTool.instance.UpdateOverlay();
        }
    }

    public class NRActionMultiRemoveNote : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();
        public List<NRActionRemoveNote> actions;

        public NRActionMultiRemoveNote() { }
        public NRActionMultiRemoveNote(List<TargetData> targets) => affectedTargets = targets;

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

        public NRActionGridMoveNotes() { }
        public NRActionGridMoveNotes(List<TargetGridMoveIntent> intents)
        {
            targetGridMoveIntents = intents;
        }

        public override void DoAction(Timeline timeline)
        {
            targetGridMoveIntents.ForEach(intent =>
            {

                intent.target.position = intent.intendedPosition;

                if (intent.hasPerformedUndo)
                {
                    var amount = intent.intendedPosition - intent.startingPosition;
                    if (intent.target.data.isPathbuilderTarget)
                    {
                        intent.target.data.pathbuilderData.MoveBy(amount);
                    }
                }
            });
            TransformTool.instance.UpdateOverlay();
        }
        public override void UndoAction(Timeline timeline)
        {
            targetGridMoveIntents.ForEach(intent =>
            {

                intent.target.position = intent.startingPosition;

                var amount = intent.startingPosition - intent.intendedPosition;
                if (intent.target.data.isPathbuilderTarget)
                {
                    intent.target.data.pathbuilderData.MoveBy(amount);
                }
                intent.hasPerformedUndo = true;

            });
            TransformTool.instance.UpdateOverlay();
        }
    }

    public class NRActionTimelineMoveNotes : NRAction
    {
        public List<TargetTimelineMoveIntent> targetTimelineMoveIntents = new List<TargetTimelineMoveIntent>();

        public NRActionTimelineMoveNotes() { }
        public NRActionTimelineMoveNotes(List<TargetTimelineMoveIntent> intents)
        {
            targetTimelineMoveIntents = intents;
        }

        public override void DoAction(Timeline timeline)
        {
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
                        FindChainStart(repeaterTarget);
                    }
                    intent.targetData.repeaterData.Section.UpdateActiveNotes();
                }
                FindChainStart(intent.targetData);
            });
            EditorNotes.SortOrderedNotes();
            TransformTool.instance.UpdateOverlay();
            UpdateChainConnectors();
        }
        public override void UndoAction(Timeline timeline)
        {
            targetTimelineMoveIntents.ForEach(intent =>
            {
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
                        FindChainStart(repeaterTarget);
                    }
                    intent.targetData.repeaterData.Section.UpdateActiveNotes();
                }
                FindChainStart(intent.targetData);
            });
            TransformTool.instance.UpdateOverlay();
            UpdateChainConnectors();
        }
    }

    public class NRActionSwapNoteColors : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();

        public NRActionSwapNoteColors() { }
        public NRActionSwapNoteColors(List<TargetData> targets) => affectedTargets = targets;

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
                        //if(target.behavior.IsChain())
                        //   EditorTargets.UpdateChainConnector(target);
                        FindChainStart(target);

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

                //if(targetData.behavior.IsChain())
                //   EditorTargets.UpdateChainConnector(targetData);
                FindChainStart(targetData);
            });

            UpdateChainConnectors();
        }
        public override void UndoAction(Timeline timeline)
        {
            DoAction(timeline); //Swap is symmetrical
        }
    }

    public class NRActionHFlipNotes : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();
        public NRActionHFlipNotes() { }
        public NRActionHFlipNotes(List<TargetData> targets) => affectedTargets = targets;


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
                        FindChainStart(target);
                    }
                }

                FindChainStart(targetData);

            });
            TransformTool.instance.UpdateOverlay();

            UpdateChainConnectors();
        }
        public override void UndoAction(Timeline timeline)
        {
            DoAction(timeline); //Swap is symmetrical
        }
    }

    public class NRActionVFlipNotes : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();

        public NRActionVFlipNotes() { }
        public NRActionVFlipNotes(List<TargetData> targets) => affectedTargets = targets;
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
                            FindChainStart(target);
                        }
                    }

                    FindChainStart(targetData);
                }

            });
            TransformTool.instance.UpdateOverlay();

            UpdateChainConnectors();
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

        public NRActionScale() { }
        public NRActionScale(List<TargetData> targets, Vector2 scale)
        {
            affectedTargets = targets;
            this.scale = scale;
        }

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
                        if (target.isPathbuilderTarget)
                        {
                            target.pathbuilderData.Scale(target, scale, true);
                        }
                        else
                        {
                            target.y *= scale.y;
                            target.x *= scale.x;
                        }

                        FindChainStart(target);
                    }
                }

                FindChainStart(targetData);
            });
            TransformTool.instance.UpdateOverlay();
            UpdateChainConnectors();
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

                    if (targetData.isRepeaterTarget)
                    {
                        foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                        {
                            if (target.isPathbuilderTarget)
                            {
                                target.pathbuilderData.Scale(target, scale, false);
                            }
                            else
                            {
                                target.y /= scale.y;
                                target.x /= scale.x;
                            }

                            FindChainStart(target);
                        }
                    }

                }
                FindChainStart(targetData);
            });
            TransformTool.instance.UpdateOverlay();
            UpdateChainConnectors();
        }
    }

    public class NRActionRotate : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();

        public float rotateAngle = 0;

        public Vector2? rotateCenter = Vector2.zero;

        public NRActionRotate() { }
        public NRActionRotate(List<TargetData> targets, float angle, Vector2? center)
        {
            affectedTargets = targets;
            rotateAngle = angle;
            rotateCenter = center;
        }

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
            if (rotateCenter == null)
                rotateCenter = Vector2.zero;

            affectedTargets.ForEach(targetData =>
            {
                if (targetData.behavior != TargetBehavior.Melee)
                {
                    NRRotate(targetData, rotateCenter.Value, rotateAngle);
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
                            NRRotate(target, rotateCenter.Value, rotateAngle);
                            FindChainStart(target);
                        }
                    }
                }
                FindChainStart(targetData);
            });
            TransformTool.instance.UpdateOverlay();
            UpdateChainConnectors();
        }
        public override void UndoAction(Timeline timeline)
        {
            affectedTargets.ForEach(targetData =>
            {
                if (targetData.behavior != TargetBehavior.Melee)
                {
                    NRRotate(targetData, rotateCenter.Value, -rotateAngle);
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
                            NRRotate(target, rotateCenter.Value, -rotateAngle);
                            FindChainStart(target);
                        }
                    }
                }
                FindChainStart(targetData);
            });
            TransformTool.instance.UpdateOverlay();
            UpdateChainConnectors();
        }
    }


    public class NRActionReverse : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();
        NRActionTimelineMoveNotes moveAction;

        public NRActionReverse() { }
        public NRActionReverse(List<TargetData> targets) => affectedTargets = targets;

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

                moveAction = new();
                moveAction.targetTimelineMoveIntents = intents;
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

        public NRActionSetTargetHitsound() { }
        public NRActionSetTargetHitsound(List<TargetSetHitsoundIntent> intents) => targetSetHitsoundIntents = intents;

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
                FindChainStart(targetData);
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
                            FindChainStart(target);
                            target.handType = targetData.handType;
                            target.beatLength = targetData.beatLength;
                            target.behavior = targetData.behavior;
                        }
                    }
                }
            });
            UpdateChainConnectors();
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
                            FindChainStart(target);
                        }
                    }
                }

                affectedTargets[i].beatLength = oldBeatLength[i];
                FindChainStart(affectedTargets[i]);
            }
            UpdateChainConnectors();
        }
    }

    public class NRActionDeselectBehavior : NRAction
    {
        public TargetBehavior behaviorToDeselect;
        Target[] deselectedTargets;

        public NRActionDeselectBehavior() { }
        public NRActionDeselectBehavior(TargetBehavior behavior) => behaviorToDeselect = behavior;

        public override void DoAction(Timeline timeline)
        {
            deselectedTargets = EditorNotes.SelectedNotes
                                .Where(target => target.data.behavior == behaviorToDeselect)
                                .ToArray();

            EditorNotes.DeselectTargets(deselectedTargets);
            TransformTool.instance.UpdateOverlay();
        }

        public override void UndoAction(Timeline timeline)
        {
            EditorNotes.SelectTargets(deselectedTargets);
            TransformTool.instance.UpdateOverlay();
        }
    }

    public class NRActionDeselectHand : NRAction
    {
        public TargetHandType handToDeselect;
        Target[] deselectedTargets;

        public NRActionDeselectHand() { }
        public NRActionDeselectHand(TargetHandType hand) => handToDeselect = hand;

        public override void DoAction(Timeline timeline)
        {
            deselectedTargets = EditorNotes.SelectedNotes.Where(target => target.data.handType == handToDeselect).ToArray();
            EditorNotes.DeselectTargets(deselectedTargets);
            TransformTool.instance.UpdateOverlay();
        }

        public override void UndoAction(Timeline timeline)
        {
            EditorNotes.SelectTargets(deselectedTargets);
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
                        EditorTargets.DeleteTargetFromAction(node);
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
                    EditorTargets.DeleteTargetFromAction(node);
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
                            EditorTargets.DeleteTargetFromAction(node);
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
            data.legacyPathbuilderData.DeleteCreatedNotes();
            data.behavior = data.legacyPathbuilderData.behavior;
            data.velocity = data.legacyPathbuilderData.velocity;
            data.handType = data.legacyPathbuilderData.handType;
            data.beatLength = oldBeatLength;
            data.legacyPathbuilderData = null;

            if (data.isRepeaterTarget)
            {
                foreach(var sibling in timeline.repeaterManager.GetMatchingRepeaterTargets(data))
                {
                    sibling.legacyPathbuilderData.DeleteCreatedNotes();
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
                EditorTargets.AddTargetFromAction(newData);
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
                        EditorTargets.AddTargetFromAction(newNode);
                        sibling.repeaterData.Section.AddExistingTargetToRepeater(newNode);
                    }
                    EditorTargets.DeleteTargetFromAction(sibling);
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
                var foundData = TargetFinder.FindTargetData(genData.time, genData.behavior, genData.handType);
                if (foundData != null)
                {
                    EditorTargets.DeleteTargetFromAction(foundData);
                }
            }
            ChainBuilder.ChainBuilder.GenerateChainNotes(removeNoteAction.targetData);
        }
    }
}