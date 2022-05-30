using NotReaper.Models;
using NotReaper.Targets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.Tools
{

    public class NRActionAddNote : NRAction
    {
        public TargetData targetData;
        public bool updateChainConnector = true;
        public NRActionAddNote() { }
        public NRActionAddNote(TargetData data) => targetData = data;

        public Target createdTarget { get; private set; } = null;

        public override void DoAction(Timeline timeline)
        {
            if (timeline.repeaterManager.IsTargetInRepeaterZone(targetData, out RepeaterData repeaterData))
            {
                var parent = timeline.repeaterManager.GetParentRepeater(repeaterData.Section);
                targetData.SetTimeFromAction(parent.startTime + repeaterData.RelativeTime);
                if (repeaterData.Section.flipTargetColors)
                {
                    if (targetData.handType == TargetHandType.Left)
                    {
                        targetData.handType = TargetHandType.Right;
                    }
                    else if (targetData.handType == TargetHandType.Right)
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
                createdTarget = timeline.repeaterManager.CreateRepeaterTarget(targetData);
            }
            else
            {
                createdTarget = EditorTargets.AddTargetFromAction(targetData, false, updateChainConnector);
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

        public List<Target> createdTargets { get; private set; } = new();

        public override void DoAction(Timeline timeline)
        {
            if (actions == null)
            {
                actions = affectedTargets.Select(targetData =>
                {
                    var action = new NRActionAddNote();
                    action.targetData = targetData;
                    action.updateChainConnector = false;
                    return action;
                }).ToList();
                affectedTargets = null;
            }
            actions.ForEach(action => { action.DoAction(timeline); });

            foreach(var action in actions)
            {
                createdTargets.Add(action.createdTarget);
            }

            TransformTool.instance.UpdateOverlay();
            CheckForStackedTargets(timeline, "add targets", actions.Select(action => action.targetData).ToList());
        }
        public override void UndoAction(Timeline timeline)
        {
            actions.ForEach(action => { action.UndoAction(timeline); });
            createdTargets.Clear();
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
            Debug.Log("Removing target!");
            if (targetData.isRepeaterTarget) targetData = timeline.repeaterManager.GetParentTarget(targetData);


            if (targetData.isPathbuilderTarget)
            {
                Debug.Log("removing PB target");
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

}
