using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools.PathBuilder;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Models;
using NotReaper.Repeaters;
using UnityEngine;

namespace NotReaper.Tools
{
    public class NRActionUpdatePathbuilderTarget : NRAction
    {
        public override string ActionName => "Update pathbuilder";
        
        private TargetData targetData;
        private Pathbuilder pathbuilder;

        private PathbuilderData oldState;
        private PathbuilderData newState;

        public NRActionUpdatePathbuilderTarget(TargetData targetData, Pathbuilder pathbuilder, PathbuilderData data) : base(targetData.time)
        {
            this.targetData = targetData;
            newState = data;
            oldState = targetData.pathbuilderData;
            this.pathbuilder = pathbuilder;
        }
        public override void DoAction(Timeline timeline)
        {
            if (targetData.isRepeaterTarget)
            {
                var original = targetData;
                bool isTargetParent = false;
                var parent = timeline.repeaterManager.GetParentTarget(targetData);
                
                timeline.pathbuilder.RemoveAllNodes(oldState);
                timeline.pathbuilder.RemoveAllNodes(newState);

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
                    isTargetParent = true;
                }
                else
                {
                    targetData = parent;
                    oldState = parent.pathbuilderData;
                    pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(parent, newState);
                }

                foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(parent))
                {
                    PathbuilderData repeaterState = new PathbuilderData();
                    repeaterState.Copy(newState);
                    repeaterState.ShiftTime(target.time - parent.time);
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
                        if (!isTargetParent && target == original)
                        {
                            target.pathbuilderData = repeaterState;
                        }
                        
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
                        if (target.repeaterData.Section.mirrorHorizontally)
                        {
                            repeaterState.Flip(new(-1, 1));
                        }
                        if (target.repeaterData.Section.mirrorVertically)
                        {
                            repeaterState.Flip(new(1, -1));
                        }
                        if (target.repeaterData.Section.flipTargetColors)
                        {
                            repeaterState.UpdateNodeHandType(timeline.repeaterManager.GetParentTarget(target).handType == TargetHandType.Left ? TargetHandType.Right : TargetHandType.Left);
                        }
                        pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(target, repeaterState);
                    }
                }
            }
        }
    }

    public class NRActionMovePathbuilderStartNode : NRAction
    {
        public override string ActionName => "Move pathbuilder start";
        
        private List<TargetGridMoveIntent> intents;
        public NRActionMovePathbuilderStartNode(List<TargetGridMoveIntent> intents) : base(intents.FirstOrDefault()?.target.time ?? new(0))
            => this.intents = intents;
        
        public override void DoAction(Timeline timeline)
        {
            foreach (var intent in intents)
            {
                intent.target.position = intent.intendedPosition;
                intent.target.pathbuilderData.Segments[0].startPoint = intent.target.position;
                timeline.pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(intent.target, intent.target.pathbuilderData);
            }
            timeline.pathbuilder.TryUpdateActiveTarget();
        }

        public override void UndoAction(Timeline timeline)
        {
            foreach (var intent in intents)
            {
                intent.target.position = intent.startingPosition;
                intent.target.pathbuilderData.Segments[0].startPoint = intent.target.position;
                timeline.pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(intent.target, intent.target.pathbuilderData);
            }

            timeline.pathbuilder.TryUpdateActiveTarget();
        }
    }

    public class NRActionMovePathbuilderTarget : NRAction
    {
        public override string ActionName => "Move pathbuilder";
        
        private struct MoveIntent
        {
            public readonly Vector2 start;
            public readonly Vector2 target;
            public readonly Vector2 amount;

            public MoveIntent(Vector2 start, Vector2 target)
            {
                this.start = start;
                this.target = target;
                amount = target - start;
            }
        }
        private Dictionary<TargetData, MoveIntent> moveDict = new();
        private Vector2 startPosition;
        private Vector2 targetPosition;
        public NRActionMovePathbuilderTarget(TargetData data, Vector2 moveAmount, RepeaterManager repeaterManager) : base(data.time)
        {
            if (!data.isRepeaterTarget)
            {
                moveDict.Add(data, new(data.position, data.position + moveAmount));
            }
            else
            {
                //first, get the parent target
                var parent = repeaterManager.GetParentTarget(data);
                //then flip move amount if necessary so we get "original" values for the parent section
                var section = data.repeaterData.Section;
                if (section.mirrorHorizontally) moveAmount.x *= -1f;
                if (section.mirrorVertically) moveAmount.y *= -1f;
                
                moveDict.Add(parent, new(parent.position, parent.position + moveAmount));

                foreach (var target in repeaterManager.GetMatchingRepeaterTargets(parent))
                {
                    //now, add all child repeater sections and flip where necessary.
                    var childSection = target.repeaterData.Section;
                    Vector2 mult = new(childSection.mirrorHorizontally ? -1 : 1, childSection.mirrorVertically ? -1 : 1);
                    moveDict.Add(target, new(target.position, target.position + moveAmount * mult));
                }
            }
        }
        
        public override void DoAction(Timeline timeline)
        {
            foreach (var kvp in moveDict)
            {
                kvp.Key.position += kvp.Value.amount;
                kvp.Key.pathbuilderData.MoveBy(kvp.Value.amount);
                timeline.pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(kvp.Key, kvp.Key.pathbuilderData);
            }
        }

        public override void UndoAction(Timeline timeline)
        {
            foreach (var kvp in moveDict)
            {
                kvp.Key.position -= kvp.Value.amount;
                kvp.Key.pathbuilderData.MoveBy(-kvp.Value.amount);
                timeline.pathbuilder.UpdatePathbuilderRepeaterTargetFromAction(kvp.Key, kvp.Key.pathbuilderData);
            }
        }
    }

    public class NRActionBakePathbuilderTarget : NRAction
    {
        public override string ActionName => "Bake pathbuilder";
        
        private Target target;
        private TargetData targetData;
        private Pathbuilder pathbuilder;
        private PathbuilderData oldState;
        private Dictionary<QNT_Timestamp, PathbuilderData> oldRepeaterState;

        public NRActionBakePathbuilderTarget(Target target, Pathbuilder pathbuilder) : base(target.data.time)
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
            EditorTargets.UpdateChainConnectors();
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
            EditorTargets.UpdateChainConnectors();
        }
    }
}
