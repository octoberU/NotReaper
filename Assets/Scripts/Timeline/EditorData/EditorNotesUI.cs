using NotReaper.MapPreview;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NotReaper.Repeaters;
using NotReaper.Tools;
using NotReaper.Tools.PathBuilder;

namespace NotReaper.MapEditor.Notes
{
    /// <summary>
    /// Responsible for the visual representation of targets.
    /// </summary>
    public class EditorNotesUI : MonoBehaviour
    {
        //Dualines
        private List<LineRenderer> dualNoteTraceLines = new();
        private LineRenderer dualinePrefab;
        private const int PoolSize = 10;

        //Cue darts
        private LineRenderer leftTraceLine;
        private LineRenderer rightTraceLine;

        //Cue Dart Settings
        private QNT_Duration cueLookAheadTime = QNT_Duration.FromBeatTime(1);
        private QNT_Duration cueResetTime = QNT_Duration.FromBeatTime(4);
        private QNT_Duration sameHandMaxDistance = QNT_Duration.FromBeatTime(2);

        private float cueSmoothAmount = 5f;
        private float cueFadeInTime = .5f;
        private float cueFadeOutTime = .25f;
        private float endCueAlpha = .75f;
        private float cueDartLength = .8f;

        [NRInject] private Preview3DManager previewer;
        [NRInject] private RepeaterManager repeaters;

        private Vector2 CameraOffset => new(cam.position.x, cam.position.y - .5f);
        private Transform cam;

        private Color leftColor;
        private Color rightColor;

        internal bool IsShowingVisuals { get; private set; } = true;

        /// <summary>
        /// Creates a pool of dualines and initializes cue darts.
        /// </summary>
        private void Start()
        {
            cam = CameraProvider.main.transform;
            dualinePrefab = Resources.Load<LineRenderer>("Dualine");
            for (int i = 0; i < PoolSize; i++)
                SpawnDualine(i);

            NRSettings.OnLoad(() =>
            {
                leftColor = NRSettings.config.leftColor;
                rightColor = NRSettings.config.rightColor;
                LineRenderer traceLine = Resources.Load<LineRenderer>("TraceLine");
                leftTraceLine = Instantiate(traceLine);
                rightTraceLine = Instantiate(traceLine);

                var color = leftColor;
                leftTraceLine.startColor = color;
                color.a = .25f;
                leftTraceLine.endColor = color;
                leftTraceLine.enabled = false;

                color = rightColor;
                rightTraceLine.startColor = color;
                color.a = .25f;
                rightTraceLine.endColor = color;
                leftTraceLine.enabled = false;
            });

            NRSettings.onSettingsSaved += UpdateColors;

            EditorAudio.onPlaybackToggled += (bool isPlaying) =>
            {
                leftTraceLine.enabled = isPlaying;
                rightTraceLine.enabled = isPlaying;

                if (!isPlaying)
                {
                    Vector3[] pos = new Vector3[] { Vector3.zero, Vector3.zero };
                    leftTraceLine.SetPositions(pos);
                    rightTraceLine.SetPositions(pos);
                }
            };
        }

        private void UpdateColors(NRJsonSettings config)
        {
            leftColor = config.leftColor;
            rightColor = config.rightColor;
        }

        /// <summary>
        /// Updates the color of all targets 
        /// </summary>
        public void UpdateTargetColors()
        {
            foreach (var target in EditorNotes.OrderedNotes)
            {
                target.gridTargetIcon.UpdateColors();
                target.timelineTargetIcon.UpdateColors();
            }
        }
        /// <summary>
        /// Updates a sustain length from the buttons next to sustains.
        /// </summary>
        /// <param name="target">The target to affect</param>
        /// <param name="increase">If true, increase by one beat snap, if false, the opposite.</param>
        public void UpdateSustainLength(Target target, bool increase)
        {
            if (!target.data.supportsBeatLength) return;
            QNT_Duration increment = EditorBeatSnap.Duration;
            QNT_Duration targetLength = target.data.beatLength;
            if (increase)
            {
                if (targetLength < increment)
                {
                    targetLength = new QNT_Duration(0);
                }
                targetLength += increment;
            }
            else
            {
                targetLength -= increment;
            }
            UndoRedoManager.AddAction(new NRActionChangeBeatLength(target, target.data.beatLength, targetLength));
        }

        public void UpdateSustainLengthFromAction(Target target, QNT_Duration targetLength)
        {
            target.data.beatLength = targetLength;

            if (target.data.isRepeaterTarget)
            {
                foreach (var repeaterTarget in repeaters.GetMatchingRepeaterTargets(target.data))
                {
                    repeaterTarget.data.beatLength = targetLength;
                }
            }
        }

        /// <summary>
        /// Updates chain connector lines for all targets.
        /// </summary>
        public void UpdateChainConnectors(QNT_Timestamp startTime, QNT_Timestamp endTime)
        {
            var notes = new NoteEnumerator(startTime,endTime);
            notes.reverse = true;
            List<Target> chain = new();
            foreach (var note in notes)
            {
                if (note.data.behavior is not TargetBehavior.ChainStart) continue;
                var nodes = new NoteEnumerator(note.data.time, EditorAudio.SongEndTime);
                var hand = note.data.handType;
                chain.Clear();
                chain.Add(note);
                foreach (var node in nodes)
                {
                    if (node.data.time == note.data.time) continue; //skip our own target
                    if (hand != node.data.handType) continue; //skip targets of the wrong handtype
                    if (node.data.behavior is not TargetBehavior.ChainNode) break; //break once we don't have a chain on the same hand anymore
                    chain.Add(node);
                }
                
                if (chain.Count == 1)
                {
                    //return because chain only has a chainstart
                    note.gridTargetIcon.DisableChainConnector();
                    continue;
                }
                
                chain.Last().gridTargetIcon.DisableChainConnector(); //disable connector on the last node in case it still had a line connecting to something
                for (int i = chain.Count - 2; i >= 0; i--)
                {
                    chain[i].gridTargetIcon.ConnectChain(chain[i + 1], note); //hook up the chain
                }
            }
        }

        public void UpdateSingleChainConnector(TargetData data)
        {
            
        }
        
        /// <summary>
        /// Updates chain connector lines for a target.
        /// </summary>
        /// <param name="data">The target do update the connector line for.</param>
        public void UpdateChainConnector(TargetData data)
        {
            var notes = new NoteEnumerator(new(0), data.time); //get all targets from start up until the supplied target
            notes.reverse = true;   //reverse selection to find chainstart
            List<Target> chain = new();
            Target chainStart = TargetFinder.FindChainStart(data);
            if (chainStart != null) //if we found chainstart..
            {
                chain.Add(chainStart);
                notes = new NoteEnumerator(chainStart.data.time, EditorNotes.OrderedNotes.Last().data.time); //..we get all notes from chain start until the last target
                foreach (var note in notes)
                {
                    if (note.data.time == chainStart.data.time) continue; //skip our own target (chainstart)
                    if (note.data.handType != chainStart.data.handType) continue; //skip if it's not the same hand
                    if (note.data.behavior == TargetBehavior.Melee || note.data.behavior == TargetBehavior.Mine) continue; //skip if it's a mine or melee
                    if (note.data.behavior != TargetBehavior.ChainNode) break; //finally, break if it's not a node, since that means the chain has ended by now
                    chain.Add(note); //add the found node to the chain
                }

                if (chain.Count == 1)
                {
                    //return because chain only has a chainstart
                    chainStart.gridTargetIcon.DisableChainConnector();
                    return;
                }

                chain.Last().gridTargetIcon.DisableChainConnector(); //disable connector on the last node in case it still had a line connecting to something
                for (int i = chain.Count - 2; i >= 0; i--)
                {
                    chain[i].gridTargetIcon.ConnectChain(chain[i + 1], chainStart); //hook up the chain
                }
            }
            else
            {
                var target = TargetFinder.FindNote(data);
                if (target != null)
                    target.gridTargetIcon.DisableChainConnector();
            }

            notes = new NoteEnumerator(new(0), data.time);
            notes.reverse = true;
            foreach(var note in notes)
            {
                if (note.data.handType == data.handType || note.data.time == data.time)
                    continue;

                if(note.data.behavior == TargetBehavior.ChainStart)
                {
                    UpdateChainConnector(note.data);
                    break;
                }
            }
            
        }
        /// <summary>
        /// Enables or disables sustain length buttons depending on their musical distance.
        /// </summary>
        public void EnableNearSustainButtons()
        {
            foreach (Target target in EditorNotes.LoadedNotes)
            {
                if (!EditorTargets.IsSimplePathbuilderTarget(target))
                {
                    if (!target.data.supportsBeatLength || target.data.isPathbuilderTarget) continue;
                }
                bool shouldDisplayGrid = !EditorAudio.IsPlaying; //Need to be paused
                                                 //Be in drag select, or be a path builder note in path builder mode
                shouldDisplayGrid &= EditorState.Tool.Current == EditorTool.DragSelect || (target.data.behavior == TargetBehavior.Legacy_Pathbuilder && EditorState.Tool.Current == EditorTool.ChainBuilder);
                shouldDisplayGrid &= target.GetRelativeBeatTime() < 2 && target.GetRelativeBeatTime() > -2; //Target needs to be "near"
                target.DisplaySustainButtons(shouldDisplayGrid);
            }
        }
        public void DisableNearSustainButtons()
        {
            foreach(var target in EditorNotes.LoadedNotes)
            {
                target.DisplaySustainButtons(false);
            }
        }
        /// <summary>
        /// Shows or hides timeline targets.
        /// </summary>
        /// <param name="show">True to show, false to hide.</param>
        public void ShowTimelineTargets(bool show)
        {
            foreach (var target in EditorNotes.OrderedNotes)            
                target.timelineTargetIcon.gameObject.SetActive(show);   
        }
        /// <summary>
        /// Updates connector lines between doubles.
        /// </summary>
        public void UpdateDualines()
        {
            if (!NRSettings.config.enableDualines) return;
            
            foreach (var line in dualNoteTraceLines)
            {
                line.enabled = false;
            }

            if (!IsShowingVisuals) return;

            int index = 0;
            var backIt = new NoteEnumerator(EditorTime.Time - Relative_QNT.FromBeatTime(0.3f), EditorTime.Time + Relative_QNT.FromBeatTime(1.7f));
            Target lastTarget = null;
            foreach (Target t in backIt)
            {
                if (lastTarget != null &&
                    t.data.behavior != TargetBehavior.ChainNode && lastTarget.data.behavior != TargetBehavior.ChainNode &&
                    t.data.handType != TargetHandType.Either && t.data.handType != TargetHandType.None &&
                    lastTarget.data.handType != TargetHandType.Either && lastTarget.data.handType != TargetHandType.None
                )
                {
                    TargetHandType expected = TargetHandType.Left;
                    if (lastTarget.data.handType == expected)
                    {
                        expected = TargetHandType.Right;
                    }

                    if (t.data.time == lastTarget.data.time && t.data.handType == expected)
                    {
                        var dualNoteTraceLine = SpawnDualine(index++);
                        dualNoteTraceLine.enabled = true;

                        float alphaVal = 0.0f;
                        if (EditorTime.Time > t.data.time)
                        {
                            alphaVal = 1.0f - ((EditorTime.Time - t.data.time).ToBeatTime() / 0.3f);
                        }
                        else
                        {
                            alphaVal = 1.0f - ((t.data.time - EditorTime.Time).ToBeatTime() / 1.7f);
                        }

                        Vector2 leftPos = t.data.position;
                        Vector2 rightPos = lastTarget.data.position;
                        if (t.data.handType == TargetHandType.Right)
                        {
                            Vector2 temp = rightPos;
                            rightPos = leftPos;
                            leftPos = temp;
                        }

                        Vector3[] positions = new Vector3[2];
                        positions[0] = new Vector3(leftPos.x, leftPos.y, 0.05f);
                        positions[1] = new Vector3(rightPos.x, rightPos.y, 0.05f);
                        dualNoteTraceLine.positionCount = positions.Length;
                        dualNoteTraceLine.SetPositions(positions);

                        Gradient gradient = new Gradient();
                        gradient.SetKeys(
                            new GradientColorKey[] { new GradientColorKey(leftColor, 0.0f), new GradientColorKey(rightColor, 1.0f) },
                            new GradientAlphaKey[] { new GradientAlphaKey(alphaVal, 0.0f), new GradientAlphaKey(alphaVal, 1.0f) }
                        );
                        dualNoteTraceLine.colorGradient = gradient;
                    }
                }

                lastTarget = t;
            }
        }
        /// <summary>
        /// Spawns a dualine.
        /// </summary>
        /// <param name="index">The index of the dualine we want.</param>
        /// <returns>A pooled or newly created dualine.</returns>
        private LineRenderer SpawnDualine(int index)
        {
            while (dualNoteTraceLines.Count <= index)
            {
                var renderer = Instantiate(dualinePrefab);
                renderer.enabled = false;
                dualNoteTraceLines.Add(renderer);
            }

            return dualNoteTraceLines.ElementAt(index);
        }
        /// <summary>
        /// Updates the cue darts.
        /// </summary>
        /// <param name="time">The current time in the song.</param>
        public void UpdateCueDarts(QNT_Timestamp time)
        {
            if (!EditorAudio.IsPlaying || !NRSettings.config.enableTraceLines || previewer.IsActive || !IsShowingVisuals)
            {
                if (leftTraceLine.enabled || rightTraceLine.enabled)
                {
                    leftTraceLine.enabled = false;
                    rightTraceLine.enabled = false;
                }
                return;
            }

            var lookAheadTime = time + cueLookAheadTime;
            NoteEnumerator notes = new(time, lookAheadTime);
            UpdateCueDart(TargetHandType.Left, leftTraceLine, notes);
            UpdateCueDart(TargetHandType.Right, rightTraceLine, notes);
        }
        /// <summary>
        /// Updates cue dart for the specified hand.
        /// </summary>
        /// <param name="hand">The hand to update the cue dart for.</param>
        /// <param name="renderer">The approriate renderer for the supplied hand</param>
        /// <param name="notes">Nearby notes</param>
        private void UpdateCueDart(TargetHandType hand, LineRenderer renderer, NoteEnumerator notes)
        {
            Target startTarget = null;
            Target previousTarget = null;
            foreach (var note in notes)
            {
                if (note.data.behavior == TargetBehavior.ChainNode || note.data.behavior.IsMeleeOrMine() || note.data.handType != hand)
                    continue;

                startTarget = note;

                var lastSameHandCue = TargetFinder.FindPreviousTargetWithHand(note.data, hand);
                
                if(lastSameHandCue != null)
                {
                    //we first check if we have a target of the same hand in the allowed timeframe
                    if(lastSameHandCue.data.time + sameHandMaxDistance >= startTarget.data.time)
                    {
                        previousTarget = lastSameHandCue;
                        break;
                    }
                }
                var lastOtherHandCue = TargetFinder.FindPreviousTargetWithHand(note.data, hand == TargetHandType.Left ? TargetHandType.Right : TargetHandType.Left);
                if(lastOtherHandCue != null && lastSameHandCue != null)
                {
                    //check if we have any same hand targets within the max time.
                    if(lastSameHandCue.data.time >= lastOtherHandCue.data.time && lastSameHandCue.data.time + cueResetTime >= startTarget.data.time)
                    {
                        previousTarget = lastSameHandCue;
                        break;
                    }
                    //if we don't, we check if we have a cue from the other hand that appears before a same hand target and doesn't go over the reset time.
                    else if(lastOtherHandCue.data.time > lastSameHandCue.data.time && lastOtherHandCue.data.time + cueResetTime >= startTarget.data.time)
                    {
                        previousTarget = lastOtherHandCue;
                        break;
                    }
                }
                if(lastOtherHandCue != null)
                {
                    //in case the target is the first of it's color (meaning lastSameHandCue will be null), we still want to check for other hand cues.
                    if(lastOtherHandCue.data.time + cueResetTime >= startTarget.data.time)
                    {
                        previousTarget = lastOtherHandCue;
                        break;
                    }
                }
                break;
            }

            if (startTarget == null)
            {
                renderer.enabled = false;
                return;
            }

            //we might end up here without having found anything, which means the reset time has been reached. In that case, we start the cue from Vector2.zero.

            //set start and end time and position for the cue dart
            var startTime = startTarget.data.time - cueLookAheadTime;
            var endTime = startTarget.data.time;
            Vector3 startPos = startTarget.data.position - CameraOffset;
            Vector3 targetPos = previousTarget == null ? Vector2.zero - CameraOffset : previousTarget.data.position - CameraOffset;

            
            //the progress we made on this cuedart so far
            float percentage = (float)(EditorTime.Time.tick - startTime.tick) / (endTime.tick - startTime.tick);
            percentage = Mathf.Clamp01(percentage);
            //smooth it out to get a snappier feel
            float smoothProgress = Mathf.Pow(percentage, cueSmoothAmount);
            //shorten the target position so we don't end up with a cue dart that extends to the previous one
            Vector3 shortenedEnd = Vector3.Lerp(startPos, targetPos, cueDartLength);
            //finally, calculate the actual position the cuedart points at
            Vector3 endPos = Vector3.Lerp(startPos, shortenedEnd, 1f - smoothProgress);

            
            startPos.z = 0;
            endPos.z = 0;
            

            //apply the positions to the line renderer
            renderer.SetPosition(0, startPos);
            renderer.SetPosition(1, endPos);

            //to make it feel smoother, we want to fade the cuedarts in and out depending on progress.
            //Color startColor = renderer.startColor;
            //Color endColor = renderer.endColor;
            Color c = hand == TargetHandType.Left ? leftColor : rightColor;
            Color startColor = c;
            Color endColor = c;

            //calculate the fade-in amount
            float colorFadeInAmount = percentage / cueFadeInTime;
            colorFadeInAmount = Mathf.Clamp01(colorFadeInAmount);
            //calculate the fade-out amount
            float colorFadeOutAmount = 1f - percentage + (1f - cueFadeOutTime);
            colorFadeOutAmount = Mathf.Clamp01(colorFadeOutAmount);

            //apply fade-in and -out values to alpha. We also multiply end color with a seperate value endCueAlpha so we get a gradient
            startColor.a = colorFadeInAmount * colorFadeOutAmount;
            endColor.a = colorFadeInAmount * endCueAlpha * colorFadeOutAmount;
            //finally, apply the modified colors back to the line renderer
            renderer.startColor = startColor;
            renderer.endColor = endColor;
            //since we're using exponential smoothing, it's likely that we won't quite reach 1, which is why we disable the linerenderer a tad early.
            //the cue dart will already be hidden behind the target at this point, so it's not visible.
            renderer.enabled = percentage <= .95f;
        }

        private QNT_Timestamp lastTime = new(0);
        /// <summary>
        /// Plays on-hit effects on all targets we passed since the last tick update.
        /// </summary>
        /// <param name="currentTime">The current time in the song.</param>
        public void OnTargetHit(QNT_Timestamp currentTime)
        {
            if (!IsShowingVisuals || (!NRSettings.config.playNoteSoundsWhileScrolling && !EditorAudio.IsPlaying))
                return;

            foreach (var target in new NoteEnumerator(lastTime, currentTime))           
                target.OnNoteHit();
            

            lastTime = currentTime;
        }

        public void ShowVisuals(bool show)
        {
            IsShowingVisuals = show;
            UpdateDualines();
            UpdateCueDarts(EditorTime.Time);
            
        }
    }

    public class NRActionChangeBeatLength : NRAction
    {
        private QNT_Duration initialBeatLength;
        private QNT_Duration newBeatLength;
        private Target target;
        public NRActionChangeBeatLength(Target target, QNT_Duration initialBeatLength, QNT_Duration newBeatLength)
        {
            this.target = target;
            this.initialBeatLength = initialBeatLength;
            this.newBeatLength = newBeatLength;
        }
        public override void DoAction(Timeline timeline)
        {
            EditorTargets.UpdateSustainLength(target, newBeatLength);
            CheckForStackedTargets(timeline, "change beat length", target.data);
        }

        public override void UndoAction(Timeline timeline)
            => EditorTargets.UpdateSustainLength(target, initialBeatLength);
    }
}
