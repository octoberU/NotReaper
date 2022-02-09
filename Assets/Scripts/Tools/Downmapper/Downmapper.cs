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
using NotReaper.Tools.ChainBuilder;
using NotReaper.Notifications;

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

    private void Start()
    {
        if (Instance is null) Instance = this;
        else
        {
            Debug.LogWarning("LowerDifficultyManager already exists.");
            return;
        }

        if (PlayerPrefs.HasKey("l_diffs"))
        {
            if (PlayerPrefs.GetInt("l_diffs") == 1) Activate();
        }
        else
        {
            window.SetActive(false);
        }
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
        List<Target> pathbuilderTargets = Timeline.orderedNotes.Where(target => target.data.pathBuilderData != null).ToList();
        foreach (Target target in pathbuilderTargets)
        {
            Timeline.instance.SelectTarget(target);
            ChainBuilder.Instance.BakePathFromSelectedNote();
            Timeline.instance.DeselectAllTargets();
        }
        Timeline.instance.SortOrderedList();
        DownmapConfig.DownmapPrefrences prefs = DownmapConfig.Instance.Preferences;
        var targets = Timeline.orderedNotes;
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
            if (DifficultyManager.I.loadedIndex > 1) ConvertShortSustains(targets);
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
       
        /*switch (index)
        {
            case 1:
                GenerateAdvanced(Timeline.orderedNotes);
                break;
            case 2:
                GenerateStandard(Timeline.orderedNotes);
                break;
            case 3:
                GenerateBeginner(Timeline.orderedNotes);
                break;
            default:
                break;
        }*/
    }

    private static void CheckForDoubledChainsAndMelees()
    {
        bool hasDeletedNote = false;
        List<Target> targets = Timeline.orderedNotes;
        Target prevTarget = null;
        foreach(Target curTarget in targets)
        {
            if(prevTarget is null)
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
                        Timeline.instance.DeleteTarget(prevTarget);
                        hasDeletedNote = true;
                    }
                }
                //chains
                if ((prevTarget.data.behavior == TargetBehavior.Chain && curTarget.data.behavior == TargetBehavior.Chain) ||
                    (prevTarget.data.behavior == TargetBehavior.ChainStart && curTarget.data.behavior == TargetBehavior.ChainStart) ||
                    (prevTarget.data.behavior == TargetBehavior.NR_Pathbuilder && curTarget.data.behavior == TargetBehavior.NR_Pathbuilder))
                {
                    if (prevTarget.data.velocity == curTarget.data.velocity)
                    {
                        Timeline.instance.DeleteTarget(prevTarget);
                        hasDeletedNote = true;
                    }
                    else if(prevTarget.data.behavior == TargetBehavior.ChainStart)
                    {
                        if (prevTarget.data.velocity == TargetVelocity.Snare || prevTarget.data.velocity == TargetVelocity.Percussion) Timeline.instance.DeleteTarget(curTarget);
                        else Timeline.instance.DeleteTarget(prevTarget);
                        hasDeletedNote = true;
                    }
                    else if(prevTarget.data.behavior == TargetBehavior.Chain)
                    {
                        if (prevTarget.data.velocity == TargetVelocity.Snare || prevTarget.data.velocity == TargetVelocity.Percussion || prevTarget.data.velocity == TargetVelocity.ChainStart) Timeline.instance.DeleteTarget(curTarget);
                        else Timeline.instance.DeleteTarget(prevTarget);
                        hasDeletedNote = true;
                    }

                   
                }
            }
        }

        if (hasDeletedNote) CheckForDoubledChainsAndMelees();
        
    }
    /*
    private void GenerateAdvanced(List<Target> targets)
    {
        
        DeleteStreams(targets, 3, 120, 60);
        Timeline.instance.SortOrderedList();
        EnforceSlotsLeadinTime(targets);
        Timeline.instance.SortOrderedList();
        EnforcePauseAfterChains(targets, 480, true);
        Timeline.instance.SortOrderedList();
        EnforcePauseAfterSustains(targets, 480);
        Timeline.instance.SortOrderedList();
        DistanceConstraint conSixteenth = new DistanceConstraint(120, 1f);
        DistanceConstraint conEigth = new DistanceConstraint(240, 2f);
        DistanceConstraint conQuarter = new DistanceConstraint(480, 3f);
        DistanceConstraint conQuarterDot = new DistanceConstraint(720, 4f);
        DistanceConstraint conHalf = new DistanceConstraint(960, 5f);

        
        EnforceDistanceBetweenSingleTargets(targets, conSixteenth, conEigth, conQuarter, conQuarterDot, conHalf);
        
        EnforceDistanceBetweenDoubles(targets, 4f, false);

        Debug.Log("Generated advanced.");
    }

    private void GenerateStandard(List<Target> targets)
    {
        ConvertSlots(targets);
        
        DeleteStreams(targets, 2, 240, 120);
        
        EnforcePauseBeforeDoubles(targets, 960);
        EnforcePauseBeforeChains(targets, 960);
        EnforcePauseAfterChains(targets, 960, false);
        EnforceChainIsolation(targets);
        EnforcePauseAfterSustains(targets, 960);
        EnforcePauseBeforeMelees(targets, 960);
        EnforcePauseAfterMelees(targets, 960);
        ConvertShortSustains(targets);
        DistanceConstraint conSixteenth = new DistanceConstraint(120, 1f);
        DistanceConstraint conEigth = new DistanceConstraint(240, 2f);
        DistanceConstraint conQuarter = new DistanceConstraint(480, 2f);
        DistanceConstraint conHalf = new DistanceConstraint(960, 3f);
        
        EnforceDistanceBetweenSingleTargets(targets, conSixteenth, conEigth, conQuarter, conHalf);
        
        EnforceDistanceBetweenDoubles(targets, 3f, true);
        

        Debug.Log("Generated standard.");
    }

    private void GenerateBeginner(List<Target> targets)
    {
        DeleteMelees(targets);
        ConvertSlots(targets);
        ConvertAllChainsToTargets(targets);
        CleanupChains(targets);
        EnforcePauseAfterSustains(targets, 480);
        
        DeleteStreams(targets, 2, 480, 240);
        
        UncrossAllTargets(targets);
        DistanceConstraint constraint = new DistanceConstraint(480, 1.5f);
        DistanceConstraint constraint2 = new DistanceConstraint(960, 3f);
        
        EnforceDistanceBetweenSingleTargets(targets, constraint, constraint2);
        
        EnforceDistanceBetweenDoubles(targets, 2f, true);
        Debug.Log("Generated beginner.");
    }
    */
    private void ConvertShortSustains(List<Target> targets)
    {
        for(int i = 0; i < targets.Count - 1; i++)
        {
            var target = targets[i];
            if (target.data.behavior != TargetBehavior.Hold) continue;

            if(target.data.beatLength.tick <= 480)
            {
                var velocity = target.data.velocity;
                target.data.behavior = TargetBehavior.Standard;
                target.data.velocity = velocity;
            }
        }
    }

    private void UncrossAllTargets(List<Target> targets)
    {
        for(int i = 0; i < targets.Count - 1; i++)
        {
            var target = targets[i];
            if (!IsRegularNote(target)) continue;
            if(i + 1 < targets.Count)
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
        for(int i = targets.Count - 1; i >= 0; i--)
        {
            var target = targets[i];
            if (target.data.behavior != TargetBehavior.Melee) continue;
            DeleteTarget(target);
        }
    }

    private void CleanupChains(List<Target> targets)
    {
        for(int i = targets.Count - 1; i >= 0; i--)
        {
            var target = targets[i];
            if (target.data.behavior == TargetBehavior.Chain)
            {
                DeleteTarget(target);
            }
            else if(target.data.behavior == TargetBehavior.ChainStart || target.data.behavior == TargetBehavior.NR_Pathbuilder)
            {
                var velocity = target.data.velocity;
                target.data.behavior = TargetBehavior.Standard;
                target.data.velocity = velocity;
            }
        }
    }

    private void ConvertSlots(List<Target> targets)
    {
        for(int i = 0; i < targets.Count - 1; i++)
        {
            var target = targets[i];
            if (IsSlot(target, out _))
            {
                var velocity = target.data.velocity;
                target.data.behavior = TargetBehavior.Standard;
                target.data.velocity = velocity;
            }
        }
    }

    private void EnforcePauseBeforeMelees(List<Target> targets, ulong pauseLength)
    {
        if (pauseLength == 0) return;
        for(int i = targets.Count - 1; i >= 0; i--)
        {
            if (i == 0) break;
            var target = targets[i];
            if (target.data.behavior != TargetBehavior.Melee) continue;
            for(int j = i - 1; j >= 0; j--)
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
                    if (nextTarget.data.behavior == TargetBehavior.Chain)
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
        for(int i = 0; i < targets.Count - 1; i++)
        {
            if (i + 1 >= targets.Count) break;
            var target = targets[i];
            if (target.data.behavior != TargetBehavior.Hold) continue;
            var nextTarget = targets[i + 1];
            if (target.data.handType != nextTarget.data.handType) continue;
            var timeBetween = GetTicksBetweenTargets(target, nextTarget);
            var pause = GetCheckValue(pauseLength, target);
            if(timeBetween < pause)
            {
                var newLength = pause.tick - timeBetween.tick;
                if(target.data.beatLength.tick - newLength <= 240)
                {
                    target.data.behavior = TargetBehavior.Standard;
                }
                else
                {
                    target.data.beatLength -= new QNT_Duration(newLength);
                }
            }

        }
    }

    private void EnforceChainIsolation(List<Target> targets)
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (i == 0) break;
            var target = targets[i];
            if (target.data.behavior != TargetBehavior.ChainStart && target.data.behavior != TargetBehavior.NR_Pathbuilder) continue;
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
            if (target.data.behavior == TargetBehavior.ChainStart || target.data.behavior == TargetBehavior.NR_Pathbuilder || target.data.behavior == TargetBehavior.Chain || target.data.behavior == TargetBehavior.Mine) continue;
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
                if(i - 2 >= 0)
                {
                    for(int j = i - 2; j >= 0; j--)
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
                            if (nextNextTarget.data.behavior == TargetBehavior.Chain)
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

    private void DeleteTarget(Target target)
    {
        if (!Timeline.orderedNotes.Contains(target))
        {
            return;
        }
        Timeline.instance.DeleteTarget(target);
    }

    private void DeleteChain(List<Target> targets, int chainStartIndex)
    {
        GetChainDuration(targets, chainStartIndex, out int chainEndIndex);
        TargetHandType hand = targets[chainStartIndex].data.handType;
        for (int i = chainEndIndex; i >= chainStartIndex; i--)
        {
            var target = targets[i];
            if (target.data.handType != hand) continue;
            if (target.data.behavior != TargetBehavior.Chain && target.data.behavior != TargetBehavior.ChainStart) break;
            DeleteTarget(target);
        }
    }

    private void EnforcePauseBeforeChains(List<Target> targets, ulong pauseLength)
    {
        if (pauseLength == 0) return;
        for(int i = targets.Count - 1; i >= 0; i--)
        {
            if (i == 0) break;
            var target = targets[i];
            if (target.data.behavior != TargetBehavior.ChainStart && target.data.behavior != TargetBehavior.NR_Pathbuilder) continue;
           
            for (int j = i - 1; j >= 0; j--)
            {
                var nextTarget = targets[j];
                if (nextTarget.data.behavior == TargetBehavior.Chain || nextTarget.data.behavior == TargetBehavior.Mine || nextTarget.data.behavior == TargetBehavior.ChainStart) continue;
                var pause = GetCheckValue(pauseLength, target);
                var duration = GetTicksBetweenTargets(target, nextTarget);
                if (duration < pause)
                {
                    var chainDuration = GetChainDuration(targets, i, out int endIndex);
                    if(chainDuration.tick <= 960)
                    {
                        var weaker = GetWeakerBeatTarget(target, nextTarget);
                        if(weaker.data.behavior == TargetBehavior.ChainStart)
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
            if (target.data.behavior == TargetBehavior.ChainStart || target.data.behavior == TargetBehavior.NR_Pathbuilder)
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
        for (int i = chainEndIndex; i >= chainStartIndex; i--)
        {
            var target = targets[i];
            if (target.data.handType != handType) continue;
            if (target.data.handType == handType && target.data.behavior != TargetBehavior.Chain && target.data.behavior != TargetBehavior.ChainStart) break;
            if(target.data.behavior == TargetBehavior.ChainStart || target.data.behavior == TargetBehavior.NR_Pathbuilder)
            {
                var velocity = target.data.velocity;
                if (convertToSustain)
                {
                    target.data.behavior = TargetBehavior.Hold;
                    target.data.beatLength = new QNT_Duration(duration);
                }
                else
                {
                    target.data.behavior = TargetBehavior.Standard;
                }
                target.data.velocity = velocity;
            }
            else
            {
                DeleteTarget(target);
            }
        }
    }

    private QNT_Timestamp GetChainDuration(List<Target> targets, int chainStartIndex, out int endIndex)
    {
        endIndex = chainStartIndex + 1;
        //endTick = targets[chainStartIndex].data.time.tick;
        TargetHandType handType = targets[chainStartIndex].data.handType;
        for(int i = chainStartIndex + 1; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target.data.handType != handType) continue;
            if (target.data.behavior != TargetBehavior.Chain) break;
            endIndex = i;
            //endTick = i;
        }
        return GetTicksBetweenTargets(targets[chainStartIndex], targets[endIndex]);
    }

    private string EnforcePauseAfterChains(List<Target> targets, ulong pauseLength, bool onlySameHand, ulong pauseLengthOther)
    {
        if (pauseLength == 0) return "";
        int count = 0;
        for(int i = targets.Count - 1; i >= 0; i--)
        {
            var target = targets[i];
            if (target.data.behavior != TargetBehavior.Chain) continue;
            if (i + 1 >= targets.Count) continue;
            if (targets[i + 1].data.behavior == TargetBehavior.Chain) continue;
            if(i + 2 < targets.Count)
            {
                if (targets[i + 2].data.behavior == TargetBehavior.Chain) continue;
            }
            for(int j = i + 1; j < targets.Count - 1; j++)
            {
                var nextTarget = targets[j];
                if (nextTarget.data.behavior == TargetBehavior.Mine) continue;
                if (nextTarget.data.behavior == TargetBehavior.ChainStart || nextTarget.data.behavior == TargetBehavior.Chain) break;
                ulong length = pauseLength;
                if (!onlySameHand && target.data.handType != nextTarget.data.handType && pauseLengthOther > 0) length = pauseLengthOther;
                var pause = GetCheckValue(length, target);
                var duration = GetTicksBetweenTargets(target, nextTarget);
                if (duration < pause)
                {
                    //if (nextTarget.data.behavior == TargetBehavior.ChainStart) break;
                    //check if next target can be converted to chain
                    if (i - 1 >= 0)
                    {
                        var previousTarget = targets[i - 1];
                        if (previousTarget.data.behavior != TargetBehavior.Chain && previousTarget.data.behavior != TargetBehavior.ChainStart)
                        {
                            if (i - 2 > 0)
                            {
                                previousTarget = targets[i - 2];
                            }
                        }
                        if (previousTarget.data.behavior == TargetBehavior.Chain || previousTarget.data.behavior == TargetBehavior.ChainStart)
                        {
                            var distBetweenChains = GetTicksBetweenTargets(target, previousTarget);
                            if (distBetweenChains == duration)
                            {
                                var velocity = nextTarget.data.velocity;
                                if (velocity == TargetVelocity.Melee) velocity = TargetVelocity.Snare;
                                nextTarget.data.behavior = TargetBehavior.Chain;
                                nextTarget.data.handType = target.data.handType;
                                nextTarget.data.velocity = velocity;
                                Vector2 posDiff = target.data.position - previousTarget.data.position;
                                nextTarget.data.position = target.data.position + posDiff;
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
                        if((target.data.position.x < nextTarget.data.position.x && target.data.handType == TargetHandType.Right) ||
                            (target.data.position.x > nextTarget.data.position.x && target.data.handType == TargetHandType.Left))
                        {
                            Timeline.instance.SwapTargets(new List<Target>() { target, nextTarget });
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
        for(int i = 0; i < targets.Count - 1; i++)
        {
            if (targets[i + 1] is null) break;
            var target = targets[i];
            if (!IsRegularNote(target, true)) continue;
            for(int j = i + 1; j < targets.Count -1; j++)
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
            for(int j = i - 1; j >= 0; j--)
            {
                nextTarget = targets[j];
                if (IsRegularNote(nextTarget, true)) break;
            }
            if (nextTarget is null) continue;
            QNT_Timestamp duration = GetTicksBetweenTargets(target, nextTarget);
            count++;
            if(duration.tick <= cutoff && duration.tick > 0)
            {
                if(stream2Chains)
                {
                    targetsToConvert.Add(i);
                    count = 0;
                }
                else count = maxAllowed;

            }
            if(duration.tick > cutoff || i == 1)
            {
                if(targetsToConvert.Count > 0)
                {
                    bool deleteCachedTargets = targetsToConvert.Count <= 1;
                    if(!deleteCachedTargets)
                    {
                        
                        if (i == 1)
                        {
                            if(GetTicksBetweenTargets(target, nextTarget).tick <= cutoff)
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
                        start.data.behavior = TargetBehavior.ChainStart;
                        start.data.velocity = velocity;
                        int convertedCount = 1;
                        for(int j = targetsToConvert.Count - 2; j >= 0; j--)
                        {
                            var t = targets[targetsToConvert[j]];
                            velocity = t.data.velocity;
                            t.data.handType = hand;
                            t.data.behavior = TargetBehavior.Chain;
                            t.data.velocity = velocity;
                            t.data.position = Vector2.Lerp(end.data.position, start.data.position, j / numChains);
                            convertedCount++;
                        }
                    }
                    else
                    {
                        for(int j = 0; j < targetsToConvert.Count - 1; j++)
                        {
                            DeleteTarget(targets[targetsToConvert[j]]);
                        }
                    }
                    targetsToConvert.Clear();
                    count = 0;
                }
            }
            if (duration > ticks) count = 0;

            if(count >= maxAllowed)
            {
                DeleteTarget(GetWeakerBeatTarget(target, nextTarget));
                deletedNotes++;
                count = 0;
            }
        }
        if (deletedNotes > 0)
        {
            Timeline.instance.SortOrderedList();
            DeleteStreams(targets, maxAllowed, maxTimeBetween, stream2Chains);
        }
        //return $"Deleted {deletedNotes} notes";
    }

    private Target GetWeakerBeatTarget(Target target1, Target target2)
    {
        bool useBeat = false;
        if (DownmapConfig.Instance.Preferences.Doubles.hitsoundsOverBeat)
        {
            if (target1.data.velocity == target2.data.velocity) useBeat = true;
            else if (target1.data.velocity == TargetVelocity.Snare && target2.data.velocity == TargetVelocity.Percussion) useBeat = true;
            else if (target1.data.velocity == TargetVelocity.Percussion && target2.data.velocity == TargetVelocity.Snare) useBeat = true;
            else if (target1.data.velocity == TargetVelocity.None && target2.data.velocity == TargetVelocity.Melee) useBeat = true;
            else if (target1.data.velocity == TargetVelocity.Melee && target2.data.velocity == TargetVelocity.None) useBeat = true;
            else if (target1.data.velocity == TargetVelocity.None) return target2;
            else if (target2.data.velocity == TargetVelocity.None) return target1;
            else if (target1.data.velocity == TargetVelocity.Melee) return target2;
            else if (target2.data.velocity == TargetVelocity.Melee) return target1;
            else if (target1.data.velocity == TargetVelocity.Chain) return target2;
            else if (target2.data.velocity == TargetVelocity.Chain) return target1;
            else if (target1.data.velocity == TargetVelocity.ChainStart) return target2;
            else if (target2.data.velocity == TargetVelocity.ChainStart) return target1;
            else if (target1.data.velocity == TargetVelocity.Standard) return target2;
            else if (target2.data.velocity == TargetVelocity.Standard) return target1;
            else if (target1.data.velocity == TargetVelocity.Snare) return target2;
            else if (target2.data.velocity == TargetVelocity.Snare) return target1;
            else if (target1.data.velocity == TargetVelocity.Percussion) return target2;
            else if (target2.data.velocity == TargetVelocity.Percussion) return target1;
        }
        
        if(useBeat || !DownmapConfig.Instance.Preferences.Doubles.hitsoundsOverBeat)
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
        for(int i = targets.Count - 1; i >= 0; i--)
        {
            if (i == 0) break;
            var target = targets[i];
            if (IsSlot(target, out bool isHorizontal))
            {
                for(int j = i - 1; j >= 0; j--)
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
                        if (nextTarget.data.behavior == TargetBehavior.Chain)
                        {
                            var velocity = target.data.velocity;
                            target.data.behavior = TargetBehavior.Standard;
                            target.data.velocity = velocity;
                            convertCount++;
                        }
                        else
                        {
                            //Timeline.instance.DeleteTarget(GetWeakerBeatTarget(target, nextTarget));
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
        var tempo = Timeline.instance.GetBpmFromTime(target.data.time);
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
        /*for(int i = 0; i < 10; i++)
        {
            if (!IsDistanceBigger(target1, target2, distance)) break;            
            //Timeline.instance.Scale(new List<Target>() { target1 }, 0.9f);
            //target1.data.position *= .9f;
            target2.data.position = Vector2.Lerp(target2.data.position, target1.data.position, 0.2f);
            if (target3 != null) target3.data.position = Vector2.Lerp(target3.data.position, target1.data.position, 0.2f);
            //if (target3 != null) Timeline.instance.Scale(new List<Target>() { target3 }, .9f);
            
        }*/
        List<Target> targets = new List<Target>() { target1, target2 };
        while(IsDistanceBigger(target1, target2, distance))
        {
            target1.data.position *= .95f;
            target2.data.position *= .95f;
        }
        
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

    private bool CanGenerate()
    {
        return Timeline.orderedNotes.Count > 0;
    }

    private bool IsRegularNote(Target target, bool includeChainStart = false)
    {
        return target.data.behavior != TargetBehavior.Melee && 
            target.data.behavior != TargetBehavior.Chain && 
            target.data.behavior != TargetBehavior.Mine && 
            (includeChainStart ? target.data.behavior != TargetBehavior.ChainStart : true);
    }

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
