using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools.PathBuilder;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Tools
{
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
        private Target target;
        private TargetData targetData;
        private Pathbuilder pathbuilder;
        private PathbuilderData oldState;
        private Dictionary<QNT_Timestamp, PathbuilderData> oldRepeaterState;

        public NRActionBakePathbuilderTarget(Target target, Pathbuilder pathbuilder)
        {
            this.target = target;
            this.targetData = target.data;
            this.pathbuilder = pathbuilder;
            this.oldState = target.data.pathbuilderData;
            oldRepeaterState = new Dictionary<QNT_Timestamp, PathbuilderData>();
        }

        public override void DoAction(Timeline timeline)
        {
            oldRepeaterState.Clear();
            pathbuilder.BakeTarget(target);
            if (targetData.isRepeaterTarget)
            {
                foreach (var repeaterTarget in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                {
                    oldRepeaterState.Add(repeaterTarget.time, repeaterTarget.pathbuilderData);
                    if (repeaterTarget.isPathbuilderTarget)
                    {
                        var t = TargetFinder.FindNote(repeaterTarget);
                        if (t != null)
                        {
                            pathbuilder.BakeTarget(t, true);
                        }
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
}
