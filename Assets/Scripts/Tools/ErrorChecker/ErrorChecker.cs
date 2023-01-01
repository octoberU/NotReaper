using NotReaper.IO;
using NotReaper.Managers;
using NotReaper.Models;
using NotReaper.Targets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using NotReaper.Timing;
using NotReaper.UI;
using UnityEngine.InputSystem;

namespace NotReaper.Tools.ErrorChecker
{

    public class ErrorChecker : MonoBehaviour
    {

        //Hidden public values
        //[HideInInspector] public static AudicaFile audicaFile;

        [SerializeField] private DifficultyManager difficultyManager;


        private List<ErrorData> currentErrors = new();
        public int CurrentErrorIndex { get; private set; } = -1;
        private ErrorData currentError;

        private QNT_Duration chainLeadTime = new QNT_Duration(360);
        private QNT_Duration sustainLeadTime = new QNT_Duration(360);
        [NRInject] private ErrorCheckerUI ui;

        internal bool initialized;
        
        //method that gets all errors and logs them
        

        public List<ErrorData> GetErrors() => ParseCues(EditorNotes.OrderedNotes.ToList(), difficultyManager.LoadedDifficulty, difficultyManager.LoadedDifficulty.ToString().ToLower());

        public List<Target> GetStackedAndHeadlessChains()
        {


            TargetData prevRHTarget = new();
            TargetData prevLHTarget = new();
            TargetData prevTarget = new();
            Target previousTarget = null;
            List<Target> evilChains = new();
            
            var targets = EditorNotes.OrderedNotes;
            foreach (var curTarget in targets)
            {
                //headless chains
                if (curTarget.data.behavior is TargetBehavior.ChainNode)
                {
                    if (curTarget.data.handType is TargetHandType.Left)
                    {
                        CheckHeadlessChain(prevLHTarget);
                    }
                    else if (curTarget.data.handType is TargetHandType.Right)
                    {
                        CheckHeadlessChain(prevRHTarget);
                    }

                    void CheckHeadlessChain(TargetData previous)
                    {
                        if (previous.behavior is not TargetBehavior.ChainNode and not TargetBehavior.ChainStart)
                            evilChains.Add(curTarget);
                    }
                }
                
                
                if(prevTarget.time == curTarget.data.time && prevTarget.handType == curTarget.data.handType)
                {
                    if (prevTarget.handType == curTarget.data.handType)
                    {
                        //mines
                        if (prevTarget.behavior.IsMine() && curTarget.data.behavior.IsMine())
                        {
                            evilChains.Add(curTarget);
                        }
                        //melees
                        else if(prevTarget.behavior.IsMelee() && curTarget.data.behavior.IsMelee())
                        {
                            if(prevTarget.data.position == curTarget.data.position)
                            {
                                evilChains.Add(curTarget);
                            }
                        }
                        //normal targets
                        else if(prevTarget.behavior == curTarget.data.behavior)
                        {
                            evilChains.Add(curTarget);
                        }
                    }
                }
                
                // update prev target
                if (!curTarget.data.behavior.IsMeleeOrMine())
                {
                    if (curTarget.data.handType.Equals(TargetHandType.Right))
                    {
                        prevRHTarget = curTarget.data;
                    }
                    else
                    {
                        prevLHTarget = curTarget.data;
                    }
                }

                //Update previous target reference
                prevTarget = curTarget.data;
            }

            return evilChains;
        }

        public void RunErrorCheck()
        {
            initialized = true;
            EditorState.SelectMode(EditorMode.Compose);
            /* retrieve orderedNotes
             * parse notes for errors
             * export error messages to txt
             */
            
            currentErrors.Clear();
            currentError = null;
            //retrieve orderedNotes and difficulty label
            List<Target> notes = EditorNotes.OrderedNotes;
            Difficulty difficulty = difficultyManager.LoadedDifficulty;
            currentErrors = ParseCues(notes, difficulty, difficulty.ToString().ToLower());

            ui.FillErrorList(currentErrors);
            
            EnableErrorCheckingUI();

            UpdateErrorCount();

            CurrentErrorIndex = -1;
            if (currentErrors.Count == 0) ui.SetErrorBody("Everything is looking good: No Errors found!", "");
            NextError();
        }

        public void RerunErrorCheck()
        {
            currentError?.Select(false);
            currentErrors.Clear();
            currentError = null;
            List<Target> notes = EditorNotes.OrderedNotes;
            Difficulty difficulty = difficultyManager.LoadedDifficulty;
            currentErrors = ParseCues(notes, difficulty, difficulty.ToString().ToLower());
            ui.FillErrorList(currentErrors);
            UpdateErrorCount();
            CurrentErrorIndex--;
            if (CurrentErrorIndex <= 0) ui.SetErrorBody("Everything is looking good: No errors found!", "");
            NextError();
        }


        public void EnableErrorCheckingUI() {
            ui.Show();
        }

        public void DisableErrorCheckingUI() {
            ui.Hide();
        }

        public void SelectError(int index)
        {
            
            currentError?.Select(false);
            
            if (currentErrors.Count <= 0) return;
            if (CurrentErrorIndex >= currentErrors.Count - 1) return;

            CurrentErrorIndex = index;

            currentError = currentErrors[CurrentErrorIndex];

            if (currentError == null) return;
	        
            ui.SetErrorBody(currentError.Description, currentError.Time.ToString());
	        
            if (EditorAudio.IsPlaying) EditorAudio.TogglePlay();
            //timeline.JumpToX(currentError.beatTime);

            //StartCoroutine(timeline.AnimateSetTime(currentError.time));
            EditorAudio.JumpToTime(currentError.Time);
	        
	        
            //Select the targets

            currentError.Select(true);
        }

        public void NextError() {
	        
	        //Deselect any previous targets
	        currentError?.Select(false);
	        
	        
	        if (currentErrors.Count <= 0) return;

	        if (CurrentErrorIndex >= currentErrors.Count - 1) return;

	        CurrentErrorIndex++;

	        currentError = currentErrors[CurrentErrorIndex];

	        if (currentError == null) return;
	        
	        ui.SetErrorBody(currentError.Description, currentError.Time.ToString());
	        
	        if (EditorAudio.IsPlaying) EditorAudio.TogglePlay();
            //timeline.JumpToX(currentError.beatTime);

            //StartCoroutine(timeline.AnimateSetTime(currentError.time));
            EditorAudio.JumpToTime(currentError.Time);
	        
	        
	        //Select the targets
            currentError.Select(true);
        }

        public void PrevError() {

            currentError?.Select(false);
            
	        if (currentErrors.Count <= 0) return;
            if (CurrentErrorIndex <= 0) return;

	        CurrentErrorIndex--;

	        currentError = currentErrors[CurrentErrorIndex];

	        if (currentError == null) return;

            ui.SetErrorBody(currentError.Description, currentError.Time.ToString());
	        
	        if (EditorAudio.IsPlaying) EditorAudio.TogglePlay();

            // timeline.SetBeatTime(time);
            //StartCoroutine(timeline.AnimateSetTime(currentError.time));
            EditorAudio.JumpToTime(currentError.Time);

            currentError.Select(true);
        }

        public void MarkCurrentFixed() {
	        currentErrors.Remove(currentError);
            currentError = null;
	        NextError();
	        UpdateErrorCount();
            if(CurrentErrorIndex <= 0) ui.SetErrorBody("Everything is looking good: No errors found!", "");
        }

        public void UpdateErrorCount() {
	        ui.SetErrorCount(currentErrors.Count);
        }
        
        
        
        
        
        

        private List<ErrorData> ParseCues(List<Target> targetCues, Difficulty difficulty, string label)
        {
            //error log
            List<ErrorData> errorLog = new List<ErrorData>();

            //references to previous targets to help with parsing
            Target previousTarget = null;
            Target previousTargetRH = null;
            Target previousTargetLH = null;
            TargetData prevTarget = new TargetData();       //dual purpose reference. This is the previous target regardless if it's RH or LH; also used in the RH/LH backtrack checks so I don't have to copy paste code.
            TargetData prevRHTarget = new TargetData();
            TargetData prevLHTarget = new TargetData();
            TargetData prevMeleeTarget = new TargetData();
            TargetData prevMineTarget = new TargetData();
            TargetData lastLastTarget = new TargetData();
            int consecutiveCounter = 0;
            
            
            
            
            //Check for a preview point:
            if (EditorFile.AudicaFile.desc.previewStartSeconds == 0) {
	            errorLog.Add(new (new QNT_Timestamp(0), "No preview start point has been added. Go to a point in the song and press P to set it."));
            }
            

            ////////////////////////
            // main parsing block //
            ////////////////////////

            foreach (Target curTarget in targetCues)
            {


                ///////////////////////
                // standalone checks //
                ///////////////////////
                

                //cues without hitsounds
                if (!HasHitSound(curTarget))
                {
                    errorLog.Add(new(curTarget.data.time, $"{curTarget.data.behavior} has an invalid hitsound.", () =>
                    {
                        curTarget.data.velocity = curTarget.data.behavior switch
                        {
                            TargetBehavior.Standard => InternalTargetVelocity.Kick,
                            TargetBehavior.Vertical => InternalTargetVelocity.Kick,
                            TargetBehavior.Horizontal => InternalTargetVelocity.Kick,
                            TargetBehavior.Sustain => InternalTargetVelocity.Kick,
                            TargetBehavior.ChainStart => InternalTargetVelocity.ChainStart,
                            TargetBehavior.ChainNode => InternalTargetVelocity.Chain,
                            TargetBehavior.Melee => InternalTargetVelocity.Melee,
                            TargetBehavior.Mine => InternalTargetVelocity.Mine,
                            TargetBehavior.None => throw new ArgumentOutOfRangeException("TargetBehavior", "Behavior is set to none. This should never happen."),
                            TargetBehavior.Legacy_Pathbuilder => throw new ArgumentOutOfRangeException("TargetBehavior", "Behavior is Legacy_Pathbuilder. This should never happen."),
                            _ => throw new ArgumentOutOfRangeException()
                        };
                    }, curTarget));
                }

                //////////////////////////////
                // general backtrack checks //
                //////////////////////////////

                // consecutive rhythm check
                // if lower difficulties and not chain node
                if (difficulty != 0 && (!prevTarget.behavior.Equals(TargetBehavior.ChainNode) && !curTarget.data.behavior.Equals(TargetBehavior.ChainNode)))
                {
                    QNT_Duration rhythmLimit = SetRhythmLimit(difficulty);
                    int countLimit = SetCountLimit(difficulty);
                    QNT_Duration beatTimeDiff = new QNT_Duration(curTarget.data.time.tick - prevTarget.time.tick);
                    if (beatTimeDiff.tick > rhythmLimit.tick)
                    {
                        //reset counter
                        consecutiveCounter = 0;
                    }
                    else if(beatTimeDiff != 0 && beatTimeDiff < rhythmLimit)
                    {
                        //straight up too fast
                        errorLog.Add(new (curTarget.data.time, $"For {label}, this target happens too soon after the previous target.", () =>
                        {
                            EditorTargets.DeleteTarget(curTarget);
                        }, curTarget, previousTarget));
                    }
                    else if(beatTimeDiff == rhythmLimit)
                    {
                        // consecutive 8th notes on one hand
                        if(difficulty == Difficulty.Standard && prevTarget.handType.Equals(curTarget.data.handType))
                        {
                            errorLog.Add(new(curTarget.data.time, $"For {label}, consecutive 8th notes on one hand are not recommended.", () =>
                            {
                                EditorTargets.SwapTargetColors(curTarget);
                            }, curTarget, previousTarget));
                        }
                        
                        //increment counter, if it gets above the countLimit, log an error
                        consecutiveCounter++;

                        if(consecutiveCounter >= countLimit)
                        {
                            //TODO convert rhythmLimit to quarter note, eigth note, etc.
                            errorLog.Add(new (curTarget.data.time, $"For {label}, having more than {countLimit} consecutive {rhythmLimit} targets is not recommended.", () =>
                            {
                               EditorTargets.DeleteTarget(curTarget);
                            }, curTarget, previousTarget));
                        }
                    }

                }

                //check for stacked targets
                if(prevTarget.time == curTarget.data.time && prevTarget.handType == curTarget.data.handType)
                {
                    if (prevTarget.handType == curTarget.data.handType)
                    {
                        //mines
                        if (prevTarget.behavior.IsMine() && curTarget.data.behavior.IsMine())
                        {
                            errorLog.Add(new(curTarget.data.time, $"Got some stacked mines here.", () =>
                            {
                                EditorTargets.DeleteTarget(curTarget);
                            }, curTarget, previousTarget));
                        }
                        //melees
                        else if(prevTarget.behavior.IsMelee() && curTarget.data.behavior.IsMelee())
                        {
                            if(prevTarget.data.position == curTarget.data.position)
                            {
                                errorLog.Add(new(curTarget.data.time, $"Stacked melees!", () =>
                                {
                                    EditorTargets.DeleteTarget(curTarget);
                                }, curTarget, previousTarget));
                            }
                        }
                        //normal targets
                        else if(prevTarget.behavior == curTarget.data.behavior)
                        {
                            errorLog.Add(new(curTarget.data.time, $"Stacked {curTarget.data.behavior}!", () =>
                            {
                                EditorTargets.DeleteTarget(curTarget);
                            }, curTarget, previousTarget));
                        }
                    }
                    
                    // ADVANCED and lower
                    if (difficulty > 0)
                    {
                        //simultaneous shot and melee
                        if (IsSimultaneousShotAndMelee(prevTarget,curTarget))
                        {
                            errorLog.Add(new(curTarget.data.time, $"For {label}, simultaneous melee and targets are not recommended.", () =>
                            {
                                if(curTarget.data.behavior.IsMelee()) EditorTargets.DeleteTarget(curTarget);
                                else EditorTargets.DeleteTarget(prevTarget);
                            }, curTarget, previousTarget));
                        }
                        //simultaneous targets must be within 4 spaces apart for Advanced, 3 for Standard/Beginner
                        else
                        {
                            float distance = (difficulty == Difficulty.Advanced ? 4 : 3);
                            if (!IsCloseEnough(prevTarget, curTarget, distance))
                            {
                                errorLog.Add(new(curTarget.data.time, $"For {label}, simultaneous targets more than {distance} spaces apart are not recommended.", () =>
                                {
                                    var target = curTarget.data.behavior.IsMeleeOrMine() ? prevTarget : curTarget.data;
                                    TargetGridMoveIntent intent = new()
                                    {
                                        target = target,
                                        startingPosition = target.position,
                                        intendedPosition = (prevTarget.position - curTarget.data.position).normalized * distance
                                    };
                                    EditorTargets.MoveGridTargets(new List<TargetGridMoveIntent>{intent});
                                }, curTarget, previousTarget));
                            }
                        }
                    }
                }

                ////////////////////////////////////
                // lower difficulty lead-in times //
                ////////////////////////////////////

                // slotted notes
                if (IsSlottedNote(curTarget.data) && difficulty != 0)
                {
                    //ADVANCED
                    if (difficulty == Difficulty.Advanced)
                    {
                        if (!IsSlottedNote(prevTarget) && InsufficientBreakAfterPreviousTarget(prevTarget, curTarget, new QNT_Duration(Constants.PulsesPerQuarterNote * 2))) 
                        {
                            errorLog.Add(new (prevTarget.time, $"For {label}, it is recommended to have at least 2 beats of lead-in time before introducing a slotted note.", () =>
                            {
                                EditorTargets.DeleteTarget(previousTarget);
                            }, curTarget, previousTarget));
                        }
                    }
                    else // no slotted notes for STANDARD or BEGINNER
                    {
                        errorLog.Add(new (curTarget.data.time, $"For {label}, use of slotted notes is not recommended.", () =>
                        {
                            NRActionSetTargetBehavior behaviorAction = new(new(){curTarget.data});
                            behaviorAction.newBehavior = TargetBehavior.Standard;
                            EditorTargets.SetTargetBehaviors(behaviorAction);
                        }));
                    }
                }

                //////////////////
                // melee checks //
                //////////////////

                //prev low solo melee check
                if (prevTarget.behavior.Equals(TargetBehavior.Melee) && IsLowMelee(prevTarget))
                {
                    if (IsLowSoloMelee(lastLastTarget, prevTarget, curTarget.data))
                    {
                        errorLog.Add(new(prevTarget.time, $"A single melee should always bein the higher slot. Only use the lower melee slot for simultaneous melees on top of each other.", () =>
                        {
                            EditorTargets.FlipTargetsVertical(new List<Target>{previousTarget});
                        }, previousTarget));
                    }
                }

                if (curTarget.data.behavior.Equals(TargetBehavior.Melee)) {
                    //non melee hitsound
                    if (!IsMeleeHitSound(curTarget))
                    {
                        errorLog.Add(new(curTarget.data.time, "Melee doesn't have a melee hitsound.", () =>
                        {
                            TargetSetHitsoundIntent intent = new()
                            {
                                target = curTarget,
                                startingVelocity = curTarget.data.velocity,
                                newVelocity = InternalTargetVelocity.Melee
                            };
                            EditorTargets.SetTargetHitsounds(new List<TargetSetHitsoundIntent>{intent});
                        }));
                    }

                    //low melee
                    if (IsLowMelee(curTarget.data))
                    {
                        // won't log an ERROR, yet.
                        // will check on the next cycle of the parser if we have a low solo melee.
                        // for now, save an extra reference to prevTarget.
                        lastLastTarget = prevTarget;
                    }

                    //update previous melee reference
                    prevMeleeTarget = curTarget.data;
                }

                ////////////////////////////
                // LH/RH backtrack checks //
                ////////////////////////////

                prevTarget = curTarget.data.handType.Equals(TargetHandType.Right) ? prevRHTarget : prevLHTarget;
                var prevSHTarget = TargetFinder.FindNote(prevTarget);

                if (!curTarget.data.behavior.Equals(TargetBehavior.Melee))
                {

                    //short break after sustain 
                    if (prevTarget.behavior.Equals(TargetBehavior.Sustain))
                    {
                        if (InsufficientBreakAfterSustain(prevTarget,curTarget,sustainLeadTime))
                        {
                            List<Target> affected = new() { curTarget };
                            
                            if(prevSHTarget != null)
                                affected.Add(prevSHTarget);
                            
                            errorLog.Add(new(curTarget.data.time, $"Time between the end of the sustain target and this target on the same hand is very short: " +
                                                                  $"recommended time is at least {sustainLeadTime}.", () =>
                            {
                                EditorTargets.DeleteTarget(curTarget);
                            }, affected));
                        }
                    }
                   
                    //short break after chain node
                    if (prevTarget.behavior.Equals(TargetBehavior.ChainNode) && !curTarget.data.behavior.Equals(TargetBehavior.ChainNode))
                    {
                        List<Target> affected = new() { curTarget };
                            
                        if(prevSHTarget != null)
                            affected.Add(prevSHTarget);
                        
                        if (InsufficientBreakAfterPreviousTarget(prevTarget,curTarget,chainLeadTime)) 
                        {
                            errorLog.Add(new (prevTarget.time, $"Time between the end of the chain and this target on the same hand is very short: " +
                                                               $"recommended time is at least {chainLeadTime}.", () =>
                            {
                                EditorTargets.DeleteTarget(curTarget);
                            }, affected));
                        }
                    }


                    //headless chains
                    if (curTarget.data.behavior is TargetBehavior.ChainNode)
                    {
                        if (curTarget.data.handType is TargetHandType.Left)
                        {
                            CheckHeadlessChain(prevLHTarget);
                        }
                        else if (curTarget.data.handType is TargetHandType.Right)
                        {
                           CheckHeadlessChain(prevRHTarget);
                        }

                        void CheckHeadlessChain(TargetData previous)
                        {
                            if (previous.behavior is not TargetBehavior.ChainNode and not TargetBehavior.ChainStart)
                            {
                                errorLog.Add(new(curTarget.data.time, "This chain node has no chain start!", () =>
                                {
                                    EditorTargets.DeleteTarget(curTarget);
                                }, curTarget));
                            }
                        }
                    }

                    // update prev target
                    if (curTarget.data.handType.Equals(TargetHandType.Right))
                    {
                        prevRHTarget = curTarget.data;
                        previousTargetRH = curTarget;
                    }
                    else
                    {
                        prevLHTarget = curTarget.data;
                        previousTargetLH = curTarget;
                    }
                    
                }

                //Update previous target reference
                prevTarget = curTarget.data;
                previousTarget = curTarget;
            }


            // string output
            Debug.Log("Error checker has detected " + errorLog.Count + " errors/warnings.");
          //  foreach(ErrorLogEntry item in errorLog)
           // {
                //output = output + "[" + (item.beatTime*TickBeatConst) + "] " + item.errorDesc + System.Environment.NewLine;
           //}
            return errorLog;
        }



        // checker functions
        // TODO refactor smaller functions back into parser code block

        private bool HasHitSound(Target targetCue)
        {
            return Enum.IsDefined(typeof(InternalTargetVelocity), targetCue.data.velocity);
        }

        private bool IsMeleeHitSound(Target targetCue)
        {
            return targetCue.data.velocity.Equals(InternalTargetVelocity.Melee) || targetCue.data.velocity.Equals(InternalTargetVelocity.Snare);
        }

        private bool IsLowMelee(TargetData targetCue)
        {
            return targetCue.position.y < 0;
        }

        private bool IsLowSoloMelee(TargetData prev, TargetData lm, TargetData next)
        {
            if (prev.behavior.Equals(TargetBehavior.Melee) && prev.time == lm.time && prev.position.x * lm.position.x > 0)
            { return false; }
            if (next.behavior.Equals(TargetBehavior.Melee) && next.time == lm.time && next.position.x * lm.position.x > 0)
            { return false; }
                return true;
        }

        private bool InsufficientBreakAfterSustain(TargetData prevTarget, Target curTarget, QNT_Duration leadTime)
        {
            return (int)curTarget.data.time.tick - (int)(prevTarget.time.tick + prevTarget.beatLength.tick) < (int)leadTime.tick;
        }

        private bool InsufficientBreakAfterPreviousTarget(TargetData prevTarget, Target curTarget, QNT_Duration leadTime)
        {
            return curTarget.data.time.tick - prevTarget.time.tick < leadTime.tick;
        }

        private bool IsCloseEnough(TargetData prevTarget, Target curTarget, float distance)
        {
            float modDist = distance * 1.1f;
            float x1 = prevTarget.position.x;
            float y1 = prevTarget.position.y;
            float x2 = curTarget.data.position.x;
            float y2 = curTarget.data.position.y;

            float result = (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
            return result <= modDist * modDist;
        }

        private bool IsSimultaneousShotAndMelee(TargetData prevTarget, Target curTarget)
            => (prevTarget.behavior.Equals(TargetBehavior.Melee) && !curTarget.data.behavior.Equals(TargetBehavior.Melee)) ||
               (!prevTarget.behavior.Equals(TargetBehavior.Melee) && curTarget.data.behavior.Equals(TargetBehavior.Melee));

        private bool IsSlottedNote(TargetData target)
            => target.behavior.Equals(TargetBehavior.Horizontal) || target.behavior.Equals(TargetBehavior.Vertical);

        private int SetCountLimit(Difficulty difficulty)
            => difficulty.IsStandard() || difficulty.IsBeginner() ? 2 : 3;

        private QNT_Duration SetRhythmLimit(Difficulty difficulty)
        {
            QNT_Duration limit;
            switch (difficulty)
            {
                case Difficulty.Advanced:
                    limit = Constants.SixteenthNoteDuration;
                    break;
                case Difficulty.Standard:
                    limit = Constants.QuarterNoteDuration / 2;
                    break;
                case Difficulty.Beginner:
                    limit = Constants.QuarterNoteDuration;
                    break;
                default:
                    limit = Constants.SixteenthNoteDuration;
                    break;
            }
            return limit;
        }
        public void Hide() => currentError?.Select(false);
    }
}
