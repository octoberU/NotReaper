using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Targets;
using NotReaper.Timing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.Tools
{
    public class NRActionSwapNoteColors : NRAction
    {
        public List<TargetData> affectedTargets = new List<TargetData>();

        public NRActionSwapNoteColors() { }
        public NRActionSwapNoteColors(List<TargetData> targets) => affectedTargets = targets;

        public override void DoAction(Timeline timeline)
        {
            foreach (var targetData in affectedTargets)
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
                FindChainStart(targetData);
            }

            UpdateChainConnectors();
            CheckForStackedTargets(timeline, "swap targets", affectedTargets);

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
                    if (newBehavior.IsMeleeOrMine() || newBehavior == TargetBehavior.ChainStart || newBehavior == TargetBehavior.ChainNode)
                    {
                        targetData.velocity = velocity;

                    }
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

}
