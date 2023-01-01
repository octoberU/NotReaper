using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Targets;
using NotReaper.Timing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using I18N.Common;
using NotReaper.HitsoundTimeline;
using NotReaper.Tools.PathBuilder;
using UnityEngine;

namespace NotReaper.Tools
{
    public class NRActionSwapNoteColors : NRAction
    {
        public override string ActionName => "Swap target colors";
        
        public List<TargetData> affectedTargets = new List<TargetData>();
        public NRActionSwapNoteColors(List<TargetData> targets) : base(FirstTargetTime(targets))
            => affectedTargets = targets;

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
                    continue;
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
        public override string ActionName => "Flip targets horizontal";
        
        public List<TargetData> affectedTargets = new List<TargetData>();
        public NRActionHFlipNotes(List<TargetData> targets) : base(FirstTargetTime(targets))
            => affectedTargets = targets;


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
                if (targetData.isRepeaterTarget)
                {
                    foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                    {
                        target.x *= -1;
                        
                        if(target.isPathbuilderTarget)
                            target.pathbuilderData.Flip(new Vector2(-1, 1));
                        
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
        public override string ActionName => "Flip targets vertical";
        
        public List<TargetData> affectedTargets = new List<TargetData>();
        public NRActionVFlipNotes(List<TargetData> targets) : base(FirstTargetTime(targets))
            => affectedTargets = targets;
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
                    if (targetData.isRepeaterTarget)
                    {
                        foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                        {
                            target.y *= -1;
                            
                            if(target.isPathbuilderTarget)
                                target.pathbuilderData.Flip(new Vector2(1, -1));
                            
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
        public override string ActionName => "Scale targets";
        
        public List<TargetData> affectedTargets = new List<TargetData>();
        public Vector2 scale;
        
        public NRActionScale(List<TargetData> targets, Vector2 scale): base(FirstTargetTime(targets))
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
                        if (targetData.pathbuilderData.Mode == PathbuilderMode.Advanced)
                        {
                            targetData.pathbuilderData.Scale(targetData, scale, true);
                        }
                        else
                        {
                            targetData.pathbuilderData.SimpleData.stepDistance *= scale.x;
                            targetData.pathbuilderData.SimpleData.stepDistance *= scale.y;
                        }
                        timeline.pathbuilder.UpdatePathbuilderTargetFromAction(targetData, targetData.pathbuilderData);
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
                        if (targetData.pathbuilderData.Mode == PathbuilderMode.Advanced)
                        {
                            targetData.pathbuilderData.Scale(targetData, scale, false);
                        }
                        else
                        {
                            targetData.pathbuilderData.SimpleData.stepDistance /= scale.x;
                            targetData.pathbuilderData.SimpleData.stepDistance /= scale.y;
                        }
                        timeline.pathbuilder.UpdatePathbuilderTargetFromAction(targetData, targetData.pathbuilderData);
                    }

                    if (targetData.isRepeaterTarget)
                    {
                        foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                        {
                            if (target.isPathbuilderTarget)
                            {
                                if (target.pathbuilderData.Mode == PathbuilderMode.Advanced)
                                {
                                    target.pathbuilderData.Scale(target, scale, false);
                                }
                                else
                                {
                                    target.pathbuilderData.SimpleData.stepDistance /= scale.x;
                                    target.pathbuilderData.SimpleData.stepDistance /= scale.y;
                                }
                                timeline.pathbuilder.UpdatePathbuilderTargetFromAction(target, target.pathbuilderData);
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
        public override string ActionName => "Rotate targets";
        
        public List<TargetData> affectedTargets = new List<TargetData>();

        public float rotateAngle = 0;

        public Vector2? rotateCenter = Vector2.zero;
        
        public NRActionRotate(List<TargetData> targets, float angle, Vector2? center) : base(FirstTargetTime(targets))
        {
            affectedTargets = targets;
            rotateAngle = angle;
            rotateCenter = center;
        }

        public void NRRotate(TargetData data, Vector2 center, float angle)
        {

            if (data.isPathbuilderTarget)
            {
                if (data.pathbuilderData.Mode == PathbuilderMode.Advanced)
                {
                    data.pathbuilderData.Rotate(data, center, angle);
                    return;
                }
                else
                {
                    data.pathbuilderData.SimpleData.initialAngle -= angle;
                }
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
            rotateCenter ??= Vector2.zero;

            affectedTargets.ForEach(targetData =>
            {
                if (targetData.behavior != TargetBehavior.Melee)
                {
                    NRRotate(targetData, rotateCenter.Value, rotateAngle);
                    if (targetData.isPathbuilderTarget)
                    {
                        timeline.pathbuilder.UpdatePathbuilderTargetFromAction(targetData, targetData.pathbuilderData);
                    }
                    
                    if (targetData.isRepeaterTarget)
                    {
                        foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                        {
                            NRRotate(target, rotateCenter.Value, target.repeaterData.Section.mirrorHorizontally ? -rotateAngle : rotateAngle);
                            
                            if(target.isPathbuilderTarget)
                                timeline.pathbuilder.UpdatePathbuilderTargetFromAction(target, target.pathbuilderData);
                            
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
            rotateCenter ??= Vector2.zero;
            
            affectedTargets.ForEach(targetData =>
            {
                if (targetData.behavior != TargetBehavior.Melee)
                {
                    NRRotate(targetData, rotateCenter.Value, -rotateAngle);
                    if (targetData.isPathbuilderTarget)
                    {
                        timeline.pathbuilder.UpdatePathbuilderTargetFromAction(targetData, targetData.pathbuilderData);
                    }

                    if (targetData.isRepeaterTarget)
                    {
                        foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(targetData))
                        {
                            NRRotate(target, rotateCenter.Value, -rotateAngle);
                            
                            if(target.isPathbuilderTarget)
                                timeline.pathbuilder.UpdatePathbuilderTargetFromAction(target, target.pathbuilderData);
                            
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
        public override string ActionName => "Reverse targets";
        
        public List<TargetData> affectedTargets = new List<TargetData>();
        NRActionTimelineMoveNotes moveAction;
        
        public NRActionReverse(List<TargetData> targets) : base(FirstTargetTime(targets))
            => affectedTargets = targets;

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

                moveAction = new(intents);
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
        public override string ActionName => "Set hitsound";
        
        public List<TargetSetHitsoundIntent> targetSetHitsoundIntents = new List<TargetSetHitsoundIntent>();
        public NRActionSetTargetHitsound(HitsoundManager hitsoundManager, List<TargetSetHitsoundIntent> intents) : base(intents.FirstOrDefault()?.target.data.time ?? new(0))
        {
            this.hitsoundManager = hitsoundManager;
            targetSetHitsoundIntents = intents;
        }

        public HitsoundManager hitsoundManager;
        
        public override void DoAction(Timeline timeline)
        {

            bool showTargetHitsoundWarning = false;
            bool showMeleeHitsoundWarning = false;
            InternalTargetVelocity errorVelocity = InternalTargetVelocity.Kick;
            foreach (var intent in targetSetHitsoundIntents)
            {
                var data = intent.target.data;
                data.velocity = intent.newVelocity;
                
                if (data.isPathbuilderTarget)
                {
                    data.pathbuilderData.SetHitsound(intent.newVelocity, data.behavior);
                }
                
                if (data.isRepeaterTarget)
                {
                    foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(data))
                    {
                        target.velocity = intent.newVelocity;
                        
                        if(target.isPathbuilderTarget)
                            data.pathbuilderData.SetHitsound(intent.newVelocity, data.behavior);
                    }
                }

                if (!showTargetHitsoundWarning && intent.newVelocity == InternalTargetVelocity.Melee && !data.behavior.IsMeleeOrMine())
                {
                    showTargetHitsoundWarning = true;
                }
                else if (!showMeleeHitsoundWarning && data.behavior == TargetBehavior.Melee && intent.newVelocity != InternalTargetVelocity.Melee && intent.newVelocity != InternalTargetVelocity.Snare)
                {
                    errorVelocity = intent.newVelocity;
                    showMeleeHitsoundWarning = true;
                }
            }

            if (showMeleeHitsoundWarning)
            {
                NotificationCenter.SendNotification($"{errorVelocity} hitsound doesn't work properly on melees. Use Melee or Snare hitsound instead.", NotificationType.Warning);
            }
            if (showTargetHitsoundWarning)
            {
                NotificationCenter.SendNotification("Melee hitsound doesn't work properly on standard targets. Use silent hitsound instead.", NotificationType.Warning, false);
            }
        }
        public override void UndoAction(Timeline timeline)
        {
            foreach(var intent in targetSetHitsoundIntents)
            {
                var data = intent.target.data;
                data.velocity = intent.startingVelocity;

                if (data.isPathbuilderTarget)
                {
                    intent.target.data.pathbuilderData.SetHitsound(intent.startingVelocity, data.behavior);
                }
                
                if (data.isRepeaterTarget)
                {
                    foreach (var target in timeline.repeaterManager.GetMatchingRepeaterTargets(data))
                    {
                        target.velocity = intent.startingVelocity;
                        
                        if(target.isPathbuilderTarget)
                            data.pathbuilderData.SetHitsound(intent.startingVelocity, data.behavior);
                    }
                }
                
                hitsoundManager.TrySelectContentFromTarget(intent.target);
            }
        }
    }

    public class NRActionSetTargetBehavior : NRAction
    {
        public override string ActionName => "Set behavior";
        
        public List<TargetData> affectedTargets = new List<TargetData>();
        public TargetBehavior newBehavior;

        List<TargetBehavior> oldBehavior = new List<TargetBehavior>();
        List<TargetHandType> oldHandTypes = new List<TargetHandType>();
        List<InternalTargetVelocity> oldVelocities = new List<InternalTargetVelocity>();
        List<QNT_Duration> oldBeatLength = new List<QNT_Duration>();
        private List<PathbuilderData> oldPathbuilderData = new();
        private List<List<TargetData>> oldChains = new();

        private bool hasPerformedUndo = false;

        public NRActionSetTargetBehavior(List<TargetData> targets) : base(FirstTargetTime(targets))
            => affectedTargets = targets;

        public override void DoAction(Timeline timeline)
        {
            oldBehavior = new List<TargetBehavior>();

            if (!hasPerformedUndo)
            {
                List<TargetData> temp = new();
                foreach(var target in affectedTargets)
                {
                    temp.Add(target);

                    if (target.isRepeaterTarget)
                    {
                        temp.AddRange(timeline.repeaterManager.GetMatchingRepeaterTargets(target));
                    } 
                }
                affectedTargets = temp;
            }

            
            foreach(var targetData in affectedTargets)
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
                else if (newBehavior is TargetBehavior.Standard or TargetBehavior.Sustain or TargetBehavior.Horizontal or TargetBehavior.Vertical)
                {
                    velocity = InternalTargetVelocity.Kick;
                }

                //Path notes and regular notes both use the same beat length
                oldBeatLength.Add(targetData.beatLength);
                oldBehavior.Add(targetData.behavior);
                oldHandTypes.Add(targetData.handType);
                oldVelocities.Add(targetData.velocity);
                oldPathbuilderData.Add(targetData.pathbuilderData);

                //if (velocity != InternalTargetVelocity.Silent) targetData.velocity = velocity;
                if (newBehavior.IsMeleeOrMine() || newBehavior is TargetBehavior.ChainStart or TargetBehavior.ChainNode)
                {
                    targetData.velocity = velocity;
                }
                
                if (newBehavior is TargetBehavior.Sustain && oldBehavior.Last() is TargetBehavior.ChainStart && !targetData.isPathbuilderTarget)
                {
                    var chain = TargetFinder.FindChain(targetData);
                    if (chain.Count > 0)
                    {
                        oldChains.Add(chain);
                        targetData.beatLength = new(chain.Last().time.tick - targetData.time.tick);
                    }
                }
                else
                {
                    oldChains.Add(new());
                }

                targetData.behavior = newBehavior;


                if (targetData.isPathbuilderTarget)
                {
                    oldChains.Add(new());
                    if (newBehavior is TargetBehavior.Sustain)
                    {
                        var pbBeatLength = targetData.pathbuilderData.Mode is PathbuilderMode.Advanced ? targetData.pathbuilderData.BeatLength : targetData.pathbuilderData.SimpleData.beatLength;

                        timeline.pathbuilder.RemovePathbuilderTarget(targetData);
                        targetData.pathbuilderData = null;
                        targetData.isPathbuilderTarget = false;
                        targetData.beatLength = pbBeatLength;
                    }
                    else
                    {
                        targetData.pathbuilderData.SetBehavior(newBehavior);
                        if (velocity != InternalTargetVelocity.Silent) targetData.pathbuilderData.SetHitsound(velocity, newBehavior);
                    }
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
            }

            foreach (var chain in oldChains)
            {
                foreach (var node in chain)
                {
                    EditorTargets.DeleteTargetFromAction(node);
                }
            }
            
            UpdateChainConnectors();
            CheckForStackedTargets(timeline, "convert behavior", affectedTargets);
        }
        public override void UndoAction(Timeline timeline)
        {
            foreach (var chain in oldChains)
            {
                foreach (var node in chain)
                {
                    EditorTargets.AddTargetFromAction(node);
                }
            }
            oldChains.Clear();


            hasPerformedUndo = true;
            for (int i = 0; i < affectedTargets.Count; ++i)
            {
                
                affectedTargets[i].behavior = oldBehavior[i];
                affectedTargets[i].handType = oldHandTypes[i];
                affectedTargets[i].velocity = oldVelocities[i];

                if (oldPathbuilderData[i] != null)
                {
                    affectedTargets[i].pathbuilderData = oldPathbuilderData[i];
                    affectedTargets[i].isPathbuilderTarget = true;
                    timeline.pathbuilder.UpdatePathbuilderTargetFromAction(affectedTargets[i], affectedTargets[i].pathbuilderData);
                }
                
                if (affectedTargets[i].isPathbuilderTarget)
                {
                    affectedTargets[i].pathbuilderData.SetBehavior(oldBehavior[i]);
                    affectedTargets[i].pathbuilderData.SetHitsound(oldVelocities[i], oldBehavior[i]);
                }
                affectedTargets[i].beatLength = oldBeatLength[i];
                FindChainStart(affectedTargets[i]);
            }

            oldPathbuilderData.Clear();
            UpdateChainConnectors();
        }
    }

    public class NRActionDeselectBehavior : NRAction
    {
        public override string ActionName => "Deselect behavior";
        public override bool Browsable => false;
        
        public TargetBehavior behaviorToDeselect;
        Target[] deselectedTargets;
        
        public NRActionDeselectBehavior(TargetBehavior behavior) : base(new(0))
            => behaviorToDeselect = behavior;

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
        public override string ActionName => "Deselect hand";
        public override bool Browsable => false;
        
        public TargetHandType handToDeselect;
        Target[] deselectedTargets;
        
        public NRActionDeselectHand(TargetHandType hand) : base(new(0))
            => handToDeselect = hand;

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
