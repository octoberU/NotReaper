using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Targets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.Tools
{
    public class NRActionGridMoveNotes : NRAction
    {
        public override string ActionName => "Move grid targets";
        
        public List<TargetGridMoveIntent> targetGridMoveIntents = new List<TargetGridMoveIntent>();
        public NRActionGridMoveNotes(List<TargetGridMoveIntent> intents) : base(intents.FirstOrDefault()?.target.time ?? new(0))
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
                FindChainStart(intent.target);
            });
            TransformTool.instance.UpdateOverlay();
            UpdateChainConnectors();
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
                
                FindChainStart(intent.target);

            });
            TransformTool.instance.UpdateOverlay();
            UpdateChainConnectors();
        }
    }

    public class NRActionTimelineMoveNotes : NRAction
    {
        public override string ActionName => "Move timeline targets";
        
        public List<TargetTimelineMoveIntent> targetTimelineMoveIntents = new List<TargetTimelineMoveIntent>();
        
        public NRActionTimelineMoveNotes(List<TargetTimelineMoveIntent> intents) : base(intents.FirstOrDefault()?.intendedTick ?? new(0))
        {
            targetTimelineMoveIntents = intents;
        }

        public override void DoAction(Timeline timeline)
        {
            bool canMove = true;

            if(targetTimelineMoveIntents.Any(intent => EditorTargets.IsTimeInIntroZone(intent.intendedTick)))
            {
                UndoAction(timeline);
                return;
            }

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
            else if (targetTimelineMoveIntents.Any(t => t.targetData.isRepeaterTarget ?
             timeline.repeaterManager.IsTargetInRepeaterZone(t.intendedTick, t.targetData.repeaterData.Section.startTime) :
             timeline.repeaterManager.IsTargetInRepeaterZone(t.intendedTick)))
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
                UndoRedoManager.RemoveAction(this);
                return;
            }

            targetTimelineMoveIntents.ForEach(intent =>
            {
                //Move the actual note
                intent.targetData.SetTimeFromAction(intent.intendedTick);
                if (intent.targetData.isRepeaterTarget)
                {
                    intent.targetData.repeaterData.RelativeTime = intent.targetData.time - intent.targetData.repeaterData.Section.startTime;
                    intent.targetData.repeaterData.Section.UpdateActiveNotes();
                }

                if (intent.targetData.behavior.IsMelee())
                    TargetFinder.FindNote(intent.targetData)?.TryPairMelee();

                FindChainStart(intent.targetData);
            });

            EditorNotes.SortOrderedNotes();
            TransformTool.instance.UpdateOverlay();
            UpdateChainConnectors();
            CheckForStackedTargets(timeline, "move targets", targetTimelineMoveIntents.Select(intent => intent.targetData).ToList());
        }
        public override void UndoAction(Timeline timeline)
        {
            targetTimelineMoveIntents.ForEach(intent =>
            {
                //First, we move the actual note
                intent.targetData.SetTimeFromAction(intent.startTick);
                if (intent.targetData.isRepeaterTarget)
                {
                    intent.targetData.repeaterData.RelativeTime = intent.targetData.time - intent.targetData.repeaterData.Section.startTime;
                    intent.targetData.repeaterData.Section.UpdateActiveNotes();
                }
                
                if (intent.targetData.behavior.IsMelee())
                    TargetFinder.FindNote(intent.targetData)?.TryPairMelee();
                
                FindChainStart(intent.targetData);
            });
            TransformTool.instance.UpdateOverlay();
            EditorNotes.SortOrderedNotes();
            UpdateChainConnectors();
        }
    }
}