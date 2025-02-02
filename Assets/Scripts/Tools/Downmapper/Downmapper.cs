using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotReaper;
using NotReaper.Targets;
using NotReaper.UI;
using NotReaper.Managers;
using NotReaper.Models;
using NotReaper.Timing;
using System.Linq;
using NotReaper.HitsoundTimeline;
using NotReaper.MapEditor.Notes;
using NotReaper.Tools.ChainBuilder;
using NotReaper.Notifications;
using NotReaper.Repeaters;
using NotReaper.Tools;
using NotReaper.Tools.PathBuilder;
using Sirenix.Utilities;

namespace NotReaper.Downmap
{
    public class Downmapper : MonoBehaviour
    {
        /// index   
        ///         0 = expert
        ///         1 = advanced
        ///         2 = standard
        ///         3 = beginner

        public static Downmapper Instance = null;
        public GameObject window;
        //public GameObject confirmButton;
        //public GameObject cancelButton;
        private DownmapConfig config = DownmapConfig.Instance;
        [NRInject] private RepeaterManager repeaterManager;
        [NRInject] private Timeline timeline;
        [NRInject] private HitsoundManager hitsoundManager;
        [NRInject] private Pathbuilder pathbuilder;

        private void Start()
        {
            if (Instance is null) Instance = this;
            else
            {
                Debug.LogWarning("LowerDifficultyManager already exists.");
                return;
            }
            
            Activate();
        }

        public void Activate()
        {
            window.SetActive(true);
        }

        public void Downmap()
        {
            if (!CanGenerate())
            {
                NotificationCenter.SendNotification("Can't Generate difficulty: No targets available.", NotificationType.Error);
                return;
            }

            EditorNotes.SortOrderedNotes();
            DownmapConfig.DownmapPrefrences prefs = DownmapConfig.Instance.Preferences;

            List<Target> targets = new();
            foreach (var target in EditorNotes.OrderedNotes)
            {
                if (target.data.isRepeaterTarget && !target.data.repeaterData.Section.isParent) continue;
                if (target.transient) continue;
                targets.Add(target);
            }

            if (prefs.Melees.enabled)
            {
                if (prefs.Melees.deleteAll) DeleteMelees(targets);
                EnforcePauseBeforeMelees(targets, prefs.Melees.leadinTime);
                EnforcePauseAfterMelees(targets, prefs.Melees.pauseTime);
            }
            if (prefs.Slots.enabled)
            {
                if (prefs.Slots.convert) ConvertSlots(targets);
                EnforceSlotsLeadinTime(targets, prefs.Slots.leadinHorizontal, prefs.Slots.leadinVertical);
            }
            if (prefs.Sustains.enabled)
            {
                EnforcePauseAfterSustains(targets, prefs.Sustains.pauseAfter);
                if ((int)DifficultyManager.Instance.LoadedDifficulty > 1) ConvertShortSustains(targets);
            }
            if (prefs.Chains.enabled)
            {
                if (prefs.Chains.convert) ConvertAllChainsToTargets(targets);
                if (prefs.Chains.isolate) EnforceChainIsolation(targets);
                EnforcePauseAfterChains(targets, prefs.Chains.pauseSameHand, prefs.Chains.pauseOtherHand == 0, prefs.Chains.pauseOtherHand);
                EnforcePauseBeforeChains(targets, prefs.Chains.leadinTime);
            }
            if (prefs.Doubles.enabled)
            {
                EnforceDistanceBetweenDoubles(targets, prefs.Doubles.maxDistance, prefs.Doubles.uncross);
                EnforcePauseBeforeDoubles(targets, prefs.Doubles.leadinTime);
            }
            if (prefs.Streams.enabled)
            {
                DeleteStreams(targets, prefs.Streams.maxConsecutiveTargets, prefs.Streams.maxStreamSpeed, prefs.Streams.stream2Chain);
            }
            if (prefs.SingleTargetSpacing.enabled)
            {
                var half = new DistanceConstraint(960, prefs.SingleTargetSpacing.halfNote);
                var quarter = new DistanceConstraint(480, prefs.SingleTargetSpacing.quarterNote);
                var eighth = new DistanceConstraint(240, prefs.SingleTargetSpacing.eighthNote);
                var sixteenth = new DistanceConstraint(120, prefs.SingleTargetSpacing.sixteenthNote);

                EnforceDistanceBetweenSingleTargets(targets, half, quarter, eighth, sixteenth);
            }
            CheckForDoubledChainsAndMelees();
        }

        private static void CheckForDoubledChainsAndMelees()
        {
            bool hasDeletedNote = false;
            List<Target> targets = EditorNotes.OrderedNotes;
            Target prevTarget = null;
            foreach (Target curTarget in targets)
            {
                if (prevTarget is null)
                {
                    prevTarget = curTarget;
                    continue;
                }
                if (prevTarget.data.time == curTarget.data.time && prevTarget.data.handType == curTarget.data.handType)
                {
                    //melees
                    if (prevTarget.data.behavior == TargetBehavior.Melee && curTarget.data.behavior == TargetBehavior.Melee)
                    {
                        if (prevTarget.data.position == curTarget.data.position)
                        {
                            EditorTargets.DeleteTarget(prevTarget);
                            hasDeletedNote = true;
                        }
                    }
                    //chains
                    if ((prevTarget.data.behavior == TargetBehavior.ChainNode && curTarget.data.behavior == TargetBehavior.ChainNode) ||
                        (prevTarget.data.behavior == TargetBehavior.ChainStart && curTarget.data.behavior == TargetBehavior.ChainStart) ||
                        (prevTarget.data.behavior == TargetBehavior.Legacy_Pathbuilder && curTarget.data.behavior == TargetBehavior.Legacy_Pathbuilder))
                    {
                        if (prevTarget.data.velocity == curTarget.data.velocity)
                        {
                            EditorTargets.DeleteTarget(prevTarget);
                            hasDeletedNote = true;
                        }
                        else if (prevTarget.data.behavior == TargetBehavior.ChainStart)
                        {
                            if (prevTarget.data.velocity == InternalTargetVelocity.Snare || prevTarget.data.velocity == InternalTargetVelocity.Percussion) EditorTargets.DeleteTarget(curTarget);
                            else EditorTargets.DeleteTarget(prevTarget);
                            hasDeletedNote = true;
                        }
                        else if (prevTarget.data.behavior == TargetBehavior.ChainNode)
                        {
                            if (prevTarget.data.velocity == InternalTargetVelocity.Snare || prevTarget.data.velocity == InternalTargetVelocity.Percussion || prevTarget.data.velocity == InternalTargetVelocity.ChainStart) EditorTargets.DeleteTarget(curTarget);
                            else EditorTargets.DeleteTarget(prevTarget);
                            hasDeletedNote = true;
                        }


                    }
                }
            }

            if (hasDeletedNote) CheckForDoubledChainsAndMelees();

        }
        
        private void ConvertShortSustains(List<Target> targets)
        {
            List<Target> affectedTargets = new();
            for (int i = 0; i < targets.Count - 1; i++)
            {
                var target = targets[i];
                if (target.data.behavior != TargetBehavior.Sustain) continue;

                if (target.data.beatLength.tick <= 480)
                {
                    affectedTargets.Add(target);
                }
            }
            ConvertBehaviorAsAction(affectedTargets, TargetBehavior.Standard);
        }

        private void UncrossAllTargets(List<Target> targets)
        {
            for (int i = 0; i < targets.Count - 1; i++)
            {
                var target = targets[i];
                if (!IsRegularNote(target)) continue;
                if (i + 1 < targets.Count)
                {
                    var nextTarget = targets[i + 1];
                    if (target.data.x < nextTarget.data.x && target.data.handType == TargetHandType.Right)
                    {
                        target.data.x *= -1;
                        nextTarget.data.x *= -1;
                    }
                    else if (target.data.x > nextTarget.data.x && target.data.handType == TargetHandType.Left)
                    {
                        nextTarget.data.x *= -1;
                        target.data.x *= -1;
                    }
                }
            }
        }

        private void DeleteMelees(List<Target> targets)
        {
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (target.data.behavior != TargetBehavior.Melee) continue;
                DeleteTarget(target);
            }
        }

        private void CleanupChains()
        {
            EditorNotes.SortOrderedNotes();
            var targets = EditorNotes.OrderedNotes;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (target.transient) continue;
                if (target.data.behavior != TargetBehavior.ChainNode) continue;
                if (TargetFinder.FindChainStart(target) == null)
                {
                    DeleteTarget(target);
                }
            }
        }

        private void ConvertSlots(List<Target> targets)
        {
            List<Target> affectedTargets = new();
            for (int i = 0; i < targets.Count - 1; i++)
            {
                var target = targets[i];
                if (IsSlot(target, out _))
                {
                    affectedTargets.Add(target);
                }
            }
            ConvertBehaviorAsAction(affectedTargets, TargetBehavior.Standard);
        }

        private void ConvertBehaviorAsAction(Target target, TargetBehavior newBehavior) => ConvertBehaviorAsAction(new List<Target> { target }, newBehavior);
        private void ConvertBehaviorAsAction(TargetBehavior newBehavior, params Target[] targets) => ConvertBehaviorAsAction(targets.ToList(), newBehavior);
        private void ConvertBehaviorAsAction(List<Target> targets, TargetBehavior newBehavior)
        {
            if (targets.Count == 0) return;
            List<TargetData> affectedTargets = new();
            List<TargetSetHitsoundIntent> hitsoundIntents = new();

            foreach (var target in targets)
            {
                affectedTargets.Add(target.data);
                hitsoundIntents.Add(new(target, target.data.velocity, target.data.velocity));
            }

            if (affectedTargets.Count > 0)
            {
                NRActionSetTargetBehavior behaviorAction = new(affectedTargets);
                behaviorAction.newBehavior = newBehavior;
                behaviorAction.DoAction(timeline);

                NRActionSetTargetHitsound hitsoundAction = new(hitsoundManager, hitsoundIntents);
                hitsoundAction.DoAction(timeline);
            }
        }

        private void EnforcePauseBeforeMelees(List<Target> targets, ulong pauseLength)
        {
            if (pauseLength == 0) return;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (i == 0) break;
                var target = targets[i];
                if (target.data.behavior != TargetBehavior.Melee) continue;
                for (int j = i - 1; j >= 0; j--)
                {
                    var nextTarget = targets[j];
                    if (nextTarget.data.behavior == TargetBehavior.Melee || nextTarget.data.behavior == TargetBehavior.Mine) continue;
                    if (nextTarget.data.time == target.data.time)
                    {
                        DeleteTarget(target);
                        continue;
                    }
                    var timeBetween = GetTicksBetweenTargets(target, nextTarget);
                    var pause = GetCheckValue(pauseLength, target);
                    if (timeBetween < pause)
                    {
                        if (nextTarget.data.behavior == TargetBehavior.ChainNode)
                        {
                            DeleteTarget(target);
                        }
                        else
                        {
                            DeleteTarget(GetWeakerBeatTarget(target, nextTarget));
                        }
                        break;
                    }
                    else break;
                }
            }
        }

        private void EnforcePauseAfterMelees(List<Target> targets, ulong pauseLength)
        {
            if (pauseLength == 0) return;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (i == 0) break;
                var target = targets[i];
                if (target.data.behavior != TargetBehavior.Melee) continue;
                if (i + 1 >= targets.Count) continue;
                for (int j = i + 1; j < targets.Count - 1; j++)
                {
                    var nextTarget = targets[j];
                    if (nextTarget.data.behavior == TargetBehavior.Melee || nextTarget.data.behavior == TargetBehavior.Mine) continue;
                    var pause = GetCheckValue(pauseLength, target);
                    var duration = GetTicksBetweenTargets(target, nextTarget);
                    if (duration < pause)
                    {
                        if (nextTarget.data.behavior == TargetBehavior.Melee)
                        {
                            if (duration.tick >= pause.tick / 2) continue;
                        }
                        if (nextTarget.data.behavior == TargetBehavior.ChainStart)
                        {
                            DeleteTarget(target);
                            break;
                        }
                        else
                        {
                            DeleteTarget(GetWeakerBeatTarget(target, nextTarget));
                            break;
                        }
                    }
                    else break;
                }


            }
        }

        private void EnforcePauseAfterSustains(List<Target> targets, ulong pauseLength)
        {
            if (pauseLength == 0) return;
            List<Target> targetsToConvert = new();
            
            for (int i = 0; i < targets.Count - 1; i++)
            {
                if (i + 1 >= targets.Count) break;
                var target = targets[i];
                if (target.data.behavior != TargetBehavior.Sustain) continue;
                var nextTarget = targets[i + 1];
                if (target.data.handType != nextTarget.data.handType) continue;
                var timeBetween = GetTicksBetweenTargets(target, nextTarget);
                var pause = GetCheckValue(pauseLength, target);
                if (timeBetween < pause)
                {
                    var newLength = pause.tick - timeBetween.tick;
                    if (target.data.beatLength.tick - newLength <= 240)
                    {
                        targetsToConvert.Add(target);
                    }
                    else
                    {
                        NRActionChangeBeatLength action = new(target, target.data.beatLength, target.data.beatLength - new QNT_Duration(newLength));
                        action.DoAction(timeline);
                    }
                }
            }
            
            ConvertBehaviorAsAction(targetsToConvert, TargetBehavior.Standard);
        }

        private void EnforceChainIsolation(List<Target> targets)
        {
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (i == 0) break;
                var target = targets[i];
                if (target.data.behavior != TargetBehavior.ChainStart && target.data.behavior != TargetBehavior.Legacy_Pathbuilder) continue;
                GetChainDuration(targets, i, out int chainEndIndex);
                IsolateChain(targets, i, chainEndIndex);
            }
        }

        private void IsolateChain(List<Target> targets, int chainStartIndex, int chainEndIndex)
        {
            TargetHandType handType = targets[chainStartIndex].data.handType;
            for (int i = chainEndIndex; i >= chainStartIndex; i--)
            {
                var target = targets[i];
                if (target.data.handType == handType) continue;
                if (target.data.behavior == TargetBehavior.ChainStart || target.data.behavior == TargetBehavior.Legacy_Pathbuilder || target.data.behavior == TargetBehavior.ChainNode || target.data.behavior == TargetBehavior.Mine) continue;
                DeleteTarget(target);
            }
        }

        private void EnforcePauseBeforeDoubles(List<Target> targets, ulong pauseLength)
        {
            if (pauseLength == 0) return;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (i > targets.Count - 1) break;
                var target = targets[i];
                if (!IsRegularNote(target, true)) continue;
                if (i - 1 > 0)
                {
                    var nextTarget = targets[i - 1];
                    if (!IsRegularNote(target, true)) continue;
                    if (target.data.time != nextTarget.data.time) continue;
                    if (i - 2 >= 0)
                    {
                        for (int j = i - 2; j >= 0; j--)
                        {
                            var nextNextTarget = targets[j];
                            if (IsRegularNote(nextNextTarget, true))
                            {
                                if (j - 1 >= 0)
                                {
                                    var nextNextNextTarget = targets[j - 1];
                                    if (IsRegularNote(nextNextNextTarget, true))
                                    {
                                        if (nextNextTarget.data.time == nextNextNextTarget.data.time)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }


                            var pause = GetCheckValue(pauseLength, nextNextTarget);
                            var duration = GetTicksBetweenTargets(nextTarget, nextNextTarget);
                            if (duration < pause)
                            {
                                if (nextNextTarget.data.behavior == TargetBehavior.ChainNode)
                                {
                                    DeleteTarget(nextTarget);
                                    DeleteTarget(target);
                                    break;
                                }
                                else
                                {
                                    if (nextNextTarget.data.behavior == TargetBehavior.ChainStart)
                                    {
                                        DeleteChain(targets, j);
                                        break;
                                    }
                                    else
                                    {
                                        DeleteTarget(nextNextTarget);
                                        break;
                                    }
                                }
                            }
                            else break;
                        }


                    }
                }
            }
        }

        private bool DeleteTarget(Target target)
        {
            if (!EditorNotes.OrderedNotes.Contains(target))
            {
                return false;
            }

            if (target.data.isRepeaterTarget && !target.data.repeaterData.Section.isParent) return false;
            EditorTargets.DeleteTarget(target);
            return true;
        }

        private void DeleteChain(List<Target> targets, int chainStartIndex)
        {
            GetChainDuration(targets, chainStartIndex, out int chainEndIndex);
            TargetHandType hand = targets[chainStartIndex].data.handType;
            for (int i = chainEndIndex; i >= chainStartIndex; i--)
            {
                var target = targets[i];
                if (target.data.handType != hand) continue;
                if (target.data.behavior != TargetBehavior.ChainNode && target.data.behavior != TargetBehavior.ChainStart) break;
                DeleteTarget(target);
            }
        }

        private void EnforcePauseBeforeChains(List<Target> targets, ulong pauseLength)
        {
            if (pauseLength == 0) return;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (i == 0) break;
                var target = targets[i];
                if (target.data.behavior != TargetBehavior.ChainStart) continue;

                for (int j = i - 1; j >= 0; j--)
                {
                    var nextTarget = targets[j];
                    if (nextTarget.data.behavior == TargetBehavior.ChainNode || nextTarget.data.behavior == TargetBehavior.Mine || nextTarget.data.behavior == TargetBehavior.ChainStart) continue;
                    var pause = GetCheckValue(pauseLength, target);
                    var duration = GetTicksBetweenTargets(target, nextTarget);
                    if (duration < pause)
                    {
                        var chainDuration = GetChainDuration(targets, i, out int endIndex);
                        if (chainDuration.tick <= 960)
                        {
                            var weaker = GetWeakerBeatTarget(target, nextTarget);
                            if (weaker.data.behavior == TargetBehavior.ChainStart)
                            {
                                ConvertChainToTarget(targets, i, endIndex);
                                break;
                            }
                        }
                        DeleteTarget(nextTarget);
                        break;
                    }
                    else break;
                }

            }
        }

        private void ConvertAllChainsToTargets(List<Target> targets)
        {
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (target.data.behavior == TargetBehavior.ChainStart || target.data.behavior == TargetBehavior.Legacy_Pathbuilder)
                {
                    var chainDuration = GetChainDuration(targets, i, out int chainLength);
                    if (chainDuration.tick <= 960)
                    {
                        ConvertChainToTarget(targets, i, chainLength);
                    }
                    else
                    {
                        ConvertChainToTarget(targets, i, chainLength, true, chainDuration.tick);
                    }
                }

            }
        }

        private void ConvertChainToTarget(List<Target> targets, int chainStartIndex, int chainEndIndex, bool convertToSustain = false, ulong duration = 120)
        {
            TargetHandType handType = targets[chainStartIndex].data.handType;
            List<Target> targetsToConvert = new();
            for (int i = chainEndIndex; i >= chainStartIndex; i--)
            {
                var target = targets[i];
                if (target.data.handType != handType) continue;
                if (target.data.handType == handType && target.data.behavior != TargetBehavior.ChainNode && target.data.behavior != TargetBehavior.ChainStart) break;
                if (target.data.behavior == TargetBehavior.ChainStart)
                {
                    if (target.data.isPathbuilderTarget)
                    {
                        pathbuilder.RemovePathbuilderTarget(target.data);
                    }
                
                    targetsToConvert.Add(target);
                }
                else
                {
                    DeleteTarget(target);
                }
            }
            
            ConvertBehaviorAsAction(targetsToConvert, convertToSustain ? TargetBehavior.Sustain : TargetBehavior.Standard);
            if (convertToSustain)
            {
                QNT_Duration newDuration = new(duration);
                foreach (var target in targetsToConvert)
                {
                    NRActionChangeBeatLength action = new(target, target.data.beatLength, newDuration);
                    action.DoAction(timeline);
                }
            }
        }

        private QNT_Timestamp GetChainDuration(List<Target> targets, int chainStartIndex, out int endIndex)
        {
            endIndex = chainStartIndex + 1;
            //endTick = targets[chainStartIndex].data.time.tick;
            if (targets.Count < endIndex)
            {
                Debug.LogError($"Not enough targets supplied - a chain is probably broken. This is not good :x", this);
                return new(0);
            }
            TargetHandType handType = targets[chainStartIndex].data.handType;
            for (int i = chainStartIndex + 1; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target.data.handType != handType) continue;
                if (target.data.behavior != TargetBehavior.ChainNode) break;
                endIndex = i;
                //endTick = i;
            }
            return GetTicksBetweenTargets(targets[chainStartIndex], targets[endIndex]);
        }

        private string EnforcePauseAfterChains(List<Target> targets, ulong pauseLength, bool onlySameHand, ulong pauseLengthOther)
        {
            if (pauseLength == 0) return "";
            int count = 0;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (target.data.behavior != TargetBehavior.ChainNode) continue;
                if (i + 1 >= targets.Count) continue;
                if (targets[i + 1].data.behavior == TargetBehavior.ChainNode) continue;
                if (i + 2 < targets.Count)
                {
                    if (targets[i + 2].data.behavior == TargetBehavior.ChainNode) continue;
                }
                for (int j = i + 1; j < targets.Count - 1; j++)
                {
                    var nextTarget = targets[j];
                    if (nextTarget.data.behavior == TargetBehavior.Mine) continue;
                    if (nextTarget.data.behavior == TargetBehavior.ChainStart || nextTarget.data.behavior == TargetBehavior.ChainNode) break;
                    ulong length = pauseLength;
                    if (!onlySameHand && target.data.handType != nextTarget.data.handType && pauseLengthOther > 0) length = pauseLengthOther;
                    var pause = GetCheckValue(length, target);
                    var duration = GetTicksBetweenTargets(target, nextTarget);
                    if (duration < pause)
                    {
                        //check if next target can be converted to chain
                        if (i - 1 >= 0)
                        {
                            var previousTarget = targets[i - 1];
                            if (previousTarget.data.behavior != TargetBehavior.ChainNode && previousTarget.data.behavior != TargetBehavior.ChainStart)
                            {
                                if (i - 2 > 0)
                                {
                                    previousTarget = targets[i - 2];
                                }
                            }
                            if (previousTarget.data.behavior == TargetBehavior.ChainNode || previousTarget.data.behavior == TargetBehavior.ChainStart)
                            {
                                var distBetweenChains = GetTicksBetweenTargets(target, previousTarget);
                                if (distBetweenChains == duration)
                                {
                                    if (target.transient)
                                    {
                                        var chainStart = TargetFinder.FindChainStart(target);
                                        if (chainStart != null)
                                        {
                                            if (chainStart.data.isPathbuilderTarget)
                                            {
                                                var distance = new QNT_Duration(nextTarget.data.time.tick - target.data.time.tick);
                                                DeleteTarget(nextTarget);
                                                
                                                if (chainStart.data.pathbuilderData.Mode is PathbuilderMode.Simple)
                                                {
                                                    chainStart.data.pathbuilderData.SimpleData.beatLength += distance;
                                                }
                                                else
                                                {
                                                    chainStart.data.pathbuilderData.Segments.Last().beatLength += distance;
                                                }
                                                NRActionUpdatePathbuilderTarget pbAction = new(chainStart.data, pathbuilder, chainStart.data.pathbuilderData);
                                                pbAction.DoAction(timeline);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        ConvertBehaviorAsAction(nextTarget, TargetBehavior.ChainNode);
                                        if (target.data.handType != nextTarget.data.handType)
                                        {
                                            NRActionSwapNoteColors swapAction = new(new List<TargetData> { nextTarget.data });
                                            swapAction.DoAction(timeline);
                                        }

                                        List<TargetGridMoveIntent> intents = new();
                                        TargetGridMoveIntent intent = new();
                                        Vector2 posDiff = target.data.position - previousTarget.data.position;
                                        intent.target = nextTarget.data;
                                        intent.startingPosition = nextTarget.data.position;
                                        intent.intendedPosition = target.data.position + posDiff;
                                        intents.Add(intent);

                                        if (nextTarget.data.isRepeaterTarget)
                                        {
                                            intents.AddRange(GetChildIntents(intent));
                                        }
                                        
                                        NRActionGridMoveNotes moveAction = new(intents);
                                        moveAction.DoAction(timeline);
                                    }
                                    break;
                                }
                            }
                        }
                        if (onlySameHand && target.data.handType != nextTarget.data.handType) break;
                        DeleteTarget(nextTarget);
                        count++;
                        break;
                    }
                    else break;
                }
            }
            return $"Deleted {count} notes";
        }

        private string EnforceDistanceBetweenDoubles(List<Target> targets, float maxDistance, bool fixCrossovers)
        {
            if (maxDistance == 0) return "";
            int count = 0;
            for (int i = 0; i < targets.Count - 1; i++)
            {
                if (targets[i + 1] is null) break;
                var target = targets[i];
                var nextTarget = targets[i + 1];

                //Decrease distance on doubles
                if (target.data.time == nextTarget.data.time)
                {
                    if (IsRegularNote(target, true) && IsRegularNote(nextTarget, true))
                    {
                        if (fixCrossovers)
                        {
                            if ((target.data.position.x < nextTarget.data.position.x && target.data.handType == TargetHandType.Right) ||
                                (target.data.position.x > nextTarget.data.position.x && target.data.handType == TargetHandType.Left))
                            {
                                EditorTargets.SwapTargetColors(target, nextTarget);
                            }
                        }
                        DecreaseDistance(target, nextTarget, maxDistance);
                    }
                }
            }
            return $"Decreased distance on {count} doubles";
        }

        private void EnforceDistanceBetweenSingleTargets(List<Target> targets, params DistanceConstraint[] constraints)
        {
            List<DistanceConstraint> sortedConstraints = constraints.ToList();
            int count = 0;
            for (int i = 0; i < targets.Count - 1; i++)
            {
                if (targets[i + 1] is null) break;
                var target = targets[i];
                if (!IsRegularNote(target, true)) continue;
                for (int j = i + 1; j < targets.Count - 1; j++)
                {
                    var nextTarget = targets[j];
                    if (!IsRegularNote(nextTarget, true)) continue;
                    if (target.data.time == nextTarget.data.time) continue;
                    var constraint = GetAppropriateConstraint(GetTicksBetweenTargets(target, nextTarget), sortedConstraints);
                    count++;
                    DecreaseDistance(target, nextTarget, constraint.distance);
                    break;
                }
            }
        }

        private DistanceConstraint GetAppropriateConstraint(QNT_Timestamp timeBetween, List<DistanceConstraint> constraints)
        {
            return constraints.Aggregate((c1, c2) => Mathf.Abs(c1.timeBetween - timeBetween.tick) < Mathf.Abs(c2.timeBetween - timeBetween.tick) ? c1 : c2);
        }

        private void DeleteStreams(List<Target> targets, int maxAllowed, ulong maxTimeBetween, bool stream2Chains)
        {
            if (maxAllowed == 0 || maxTimeBetween == 0) return;
            int count = 0;
            int deletedNotes = 0;

            List<int> targetsToConvert = new List<int>();
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (i == 0) break;
                var target = targets[i];
                if (!IsRegularNote(target, true)) continue;
                var ticks = GetCheckValue(maxTimeBetween, target);
                ulong cutoff = ticks.tick / 2;
                Target nextTarget = null;
                for (int j = i - 1; j >= 0; j--)
                {
                    nextTarget = targets[j];
                    if (IsRegularNote(nextTarget, true)) break;
                }
                if (nextTarget is null) continue;
                QNT_Timestamp duration = GetTicksBetweenTargets(target, nextTarget);
                count++;
                if (duration.tick <= cutoff && duration.tick > 0)
                {
                    if (stream2Chains)
                    {
                        targetsToConvert.Add(i);
                        count = 0;
                    }
                    else count = maxAllowed;

                }
                if (duration.tick > cutoff || i == 1)
                {
                    if (targetsToConvert.Count > 0)
                    {
                        bool deleteCachedTargets = targetsToConvert.Count <= 1;
                        if (!deleteCachedTargets)
                        {

                            if (i == 1)
                            {
                                if (GetTicksBetweenTargets(target, nextTarget).tick <= cutoff)
                                {
                                    if (IsRegularNote(nextTarget, true))
                                    {
                                        targetsToConvert.Add(i - 1);
                                    }
                                }
                            }
                            else
                            {
                                targetsToConvert.Add(i);
                            }
                            var start = targets[targetsToConvert.Last()];
                            var end = targets[targetsToConvert.First()];
                            float numChains = targetsToConvert.Count - 1;
                            var velocity = start.data.velocity;
                            var hand = start.data.handType;
                            ConvertBehaviorAsAction(start, TargetBehavior.ChainStart);
                            int convertedCount = 1;
                            List<Target> chainConvertTargets = new();
                            for (int j = targetsToConvert.Count - 2; j >= 0; j--)
                            {
                                chainConvertTargets.Add(targets[targetsToConvert[j]]);
                                convertedCount++;
                            }
                            
                            ConvertBehaviorAsAction(chainConvertTargets, TargetBehavior.ChainNode);
                            List<TargetGridMoveIntent> intents = new();
                            for (int j = targetsToConvert.Count - 2; j >= 0; j--)
                            {
                                TargetGridMoveIntent intent = new();
                                var t = chainConvertTargets[j].data;
                                intent.target = t;
                                intent.startingPosition = t.position;
                                intent.intendedPosition = Vector2.Lerp(start.data.position, end.data.position, (j + 1 ) / numChains);
                                intents.Add(intent);

                                if (t.isRepeaterTarget)
                                {
                                    intents.AddRange(GetChildIntents(intent));
                                }
                            }
                            NRActionGridMoveNotes moveAction = new(intents);
                            moveAction.DoAction(timeline);

                            List<TargetData> swapTargets = new();
                            foreach (var t in chainConvertTargets)
                            {
                                if (t.data.handType != hand)
                                {
                                    swapTargets.Add(t.data);
                                }
                            }
                            if (swapTargets.Count > 0)
                            {
                                NRActionSwapNoteColors swapAction = new(swapTargets);
                                swapAction.DoAction(timeline);
                            }
                        }
                        else
                        {
                            for (int j = 0; j < targetsToConvert.Count - 1; j++)
                            {
                                DeleteTarget(targets[targetsToConvert[j]]);
                            }
                        }
                        targetsToConvert.Clear();
                        count = 0;
                    }
                }
                if (duration > ticks) count = 0;

                if (count >= maxAllowed)
                {
                    if(DeleteTarget(GetWeakerBeatTarget(target, nextTarget)))
                    {
                        deletedNotes++;
                    }
                    count = 0;
                }
            }
            if (deletedNotes > 0)
            {
                EditorNotes.SortOrderedNotes();
                DeleteStreams(targets, maxAllowed, maxTimeBetween, stream2Chains);
            }
            //return $"Deleted {deletedNotes} notes";
        }

        private List<TargetGridMoveIntent> GetChildIntents(TargetGridMoveIntent parentIntent)
        {
            List<TargetGridMoveIntent> childIntents = new();
            var children = repeaterManager.GetMatchingRepeaterTargets(parentIntent.target);
            foreach (var child in children)
            {
                TargetGridMoveIntent childIntent = new();
                childIntent.target = child;
                childIntent.startingPosition = child.position;

                Vector2 newPos = parentIntent.intendedPosition;
                if (child.repeaterData.Section.mirrorHorizontally) newPos.x *= -1f;
                if (child.repeaterData.Section.mirrorVertically) newPos.y *= -1f;
                
                childIntent.intendedPosition = newPos;
                childIntents.Add(childIntent);
            }
            return childIntents;
        }

        private Target GetWeakerBeatTarget(Target target1, Target target2)
        {
            bool useBeat = false;
            if (DownmapConfig.Instance.Preferences.Doubles.hitsoundsOverBeat)
            {
                if (target1.data.velocity == target2.data.velocity) useBeat = true;
                else if (target1.data.velocity == InternalTargetVelocity.Snare && target2.data.velocity == InternalTargetVelocity.Percussion) useBeat = true;
                else if (target1.data.velocity == InternalTargetVelocity.Percussion && target2.data.velocity == InternalTargetVelocity.Snare) useBeat = true;
                else if (target1.data.velocity == InternalTargetVelocity.Silent && target2.data.velocity == InternalTargetVelocity.Melee) useBeat = true;
                else if (target1.data.velocity == InternalTargetVelocity.Melee && target2.data.velocity == InternalTargetVelocity.Silent) useBeat = true;
                else if (target1.data.velocity == InternalTargetVelocity.Silent) return target2;
                else if (target2.data.velocity == InternalTargetVelocity.Silent) return target1;
                else if (target1.data.velocity == InternalTargetVelocity.Melee) return target2;
                else if (target2.data.velocity == InternalTargetVelocity.Melee) return target1;
                else if (target1.data.velocity == InternalTargetVelocity.Chain) return target2;
                else if (target2.data.velocity == InternalTargetVelocity.Chain) return target1;
                else if (target1.data.velocity == InternalTargetVelocity.ChainStart) return target2;
                else if (target2.data.velocity == InternalTargetVelocity.ChainStart) return target1;
                else if (target1.data.velocity == InternalTargetVelocity.Kick) return target2;
                else if (target2.data.velocity == InternalTargetVelocity.Kick) return target1;
                else if (target1.data.velocity == InternalTargetVelocity.Snare) return target2;
                else if (target2.data.velocity == InternalTargetVelocity.Snare) return target1;
                else if (target1.data.velocity == InternalTargetVelocity.Percussion) return target2;
                else if (target2.data.velocity == InternalTargetVelocity.Percussion) return target1;
            }

            if (useBeat || !DownmapConfig.Instance.Preferences.Doubles.hitsoundsOverBeat)
            {
                if (target1.data.time.tick % 960 == 0) return target2;
                else if (target2.data.time.tick % 960 == 0) return target1;
                else if (target1.data.time.tick % 480 == 0) return target2;
                else if (target2.data.time.tick % 480 == 0) return target1;
                else if (target1.data.time.tick % 240 == 0) return target2;
                else return target1;
            }

            return target1;
        }

        private string EnforceSlotsLeadinTime(List<Target> targets, ulong leadinHorizontal, ulong leadinVertical)
        {
            int count = 0;
            int convertCount = 0;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (i == 0) break;
                var target = targets[i];
                if (IsSlot(target, out bool isHorizontal))
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        var nextTarget = targets[j];
                        if (!IsRegularNote(nextTarget)) continue;
                        if (IsSlot(nextTarget, out bool isNextHorizontal))
                        {
                            if (isHorizontal == isNextHorizontal) continue;
                        }

                        var duration = GetTicksBetweenTargets(target, nextTarget);
                        var ticks = GetCheckValue((isHorizontal ? leadinHorizontal : leadinVertical), target);
                        if (duration < ticks)
                        {
                            if (nextTarget.data.behavior == TargetBehavior.ChainNode)
                            {
                                ConvertBehaviorAsAction(target, TargetBehavior.Standard);
                                convertCount++;
                            }
                            else
                            {
                                DeleteTarget(GetWeakerBeatTarget(target, nextTarget));
                                count++;
                                break;
                            }

                        }
                    }

                }
            }
            return $"Deleted {count} notes, converted {convertCount} notes";
        }

        //Double values if tempo > 150
        private QNT_Timestamp GetCheckValue(ulong defaultValue, Target target)
        {
            var tempo = EditorTempo.GetBpmFromTime(target.data.time);
            ulong val = tempo >= 150d ? defaultValue * 2 : defaultValue;
            return new QNT_Timestamp(val);
        }

        //Get ticks between 2 targets
        private QNT_Timestamp GetTicksBetweenTargets(Target target1, Target target2)
        {
            ulong t1 = target1.data.time.tick;
            if (target1.data.supportsBeatLength) t1 += target1.data.beatLength.tick;
            ulong t2 = target2.data.time.tick;
            ulong diff = t1 > t2 ? t1 - t2 : t2 - t1;
            return new QNT_Timestamp(diff);
        }
        private void DecreaseDistance(Target target1, Target target2, float distance)
        {
            while (IsDistanceBigger(target1, target2, distance))
            {
                target1.data.position *= .95f;
                target2.data.position *= .95f;
            }

            TargetGridMoveIntent intent1 = new();
            TargetGridMoveIntent intent2 = new();

            intent1.target = target1.data;
            intent1.startingPosition = target1.data.position;
            intent1.intendedPosition = target1.data.position;

            intent2.target = target2.data;
            intent2.startingPosition = target2.data.position;
            intent2.intendedPosition = target2.data.position;

            List<TargetGridMoveIntent> intents = new()
            {
                intent1,
                intent2
            };
            NRActionGridMoveNotes gridMoveAction = new(intents);

            if (target1.data.isRepeaterTarget)
            {
                gridMoveAction.targetGridMoveIntents.AddRange(GetChildIntents(intent1));
            }

            if (target2.data.isRepeaterTarget)
            {
                gridMoveAction.targetGridMoveIntents.AddRange(GetChildIntents(intent2));
            }

            gridMoveAction.DoAction(timeline);
        }

        private bool IsDistanceBigger(Target target1, Target target2, float distance)
        {
            var cue1 = target1.ToCue();
            var cue2 = target2.ToCue();
            //horizontal distance
            var horizontal = Mathf.Abs(cue1.pitch % 12 - cue2.pitch % 12);
            //vertical distance
            var vertical = Mathf.Abs((cue1.pitch / 11) - (cue2.pitch / 11));
            //combined distance
            if (distance <= 1f) distance += 1f;
            var combined = (distance - 1) * 2f;
            return horizontal > distance || vertical > distance || (horizontal + vertical) > combined;
        }

        private bool CanGenerate() => EditorNotes.OrderedNotes.Count > 0;

        private bool IsRegularNote(Target target, bool includeChainStart = false)
            => target.data.behavior != TargetBehavior.Melee &&
                target.data.behavior != TargetBehavior.ChainNode &&
                target.data.behavior != TargetBehavior.Mine &&
                (includeChainStart ? target.data.behavior != TargetBehavior.ChainStart : true);


        private bool IsSlot(Target target, out bool isHorizontal)
        {
            isHorizontal = target.data.behavior == TargetBehavior.Horizontal;
            return target.data.behavior == TargetBehavior.Horizontal || target.data.behavior == TargetBehavior.Vertical;
        }

        private struct DistanceConstraint
        {
            public ulong timeBetween;
            public float distance;

            public DistanceConstraint(ulong timeBetween, float distance)
            {
                this.timeBetween = timeBetween;
                this.distance = distance;
            }
        }
    }

}
