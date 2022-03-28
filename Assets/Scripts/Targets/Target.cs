using System;
using System.Collections;
using UnityEngine;
using NotReaper.Models;
using NotReaper.Tools.ChainBuilder;
using NotReaper.Timing;
using DG.Tweening;
using System.Collections.Generic;
using NotReaper.UI;
using NotReaper.Grid;
using NotReaper.Tools.PathBuilder;
using NotReaper.UI.Particles;

namespace NotReaper.Targets
{


    public class Target
    {

        public TargetIcon gridTargetIcon;
        public TargetIcon timelineTargetIcon;
        private bool noteIsAnimating = false;
        public TargetData data;
        public bool transient;

        private Transform gridCamera;

        [HideInInspector]
        public bool isPlayingSustains = false;
        //Events and stuff:
        public event Action<Target> DeleteNoteEvent;
        public void DeleteNote()
        {
            DeleteNoteEvent(this);
        }

        public event Action<Target> TargetEnterLoadedNotesEvent;
        public void TargetEnterLoadedNotes()
        {
            TargetEnterLoadedNotesEvent(this);
            ResetAnimationVisuals();
        }

        public event Action<Target> TargetExitLoadedNotesEvent;
        public void TargetExitLoadedNotes()
        {
            TargetExitLoadedNotesEvent(this);
        }

        public event Action<Target> TargetSelectEvent;
        public void MakeTimelineSelectTarget()
        {
            if (!transient)
            {
                TargetSelectEvent(this);
            }
        }
        public event Action<Target, bool> TargetDeselectEvent;
        public void MakeTimelineDeselectTarget()
        {
            if (!transient)
            {
                TargetDeselectEvent(this, false);
            }
        }



        //I'm so good at naming stuff.
        public event Action<Target, bool> MakeTimelineUpdateSustainLengthEvent;
        public void MakeTimelineUpdateSustainLength(bool increase)
        {
            MakeTimelineUpdateSustainLengthEvent(this, increase);
        }


        public Target(TargetData targetData, TargetIcon timelineIcon, TargetIcon gridIcon, bool transient, Transform gridCamera)
        {
            timelineTargetIcon = timelineIcon;
            gridTargetIcon = gridIcon;
            this.gridCamera = gridCamera;
            //timelineTargetIcon.target = this;
            //gridTargetIcon.target = this;

            data = targetData;
            data.PositionChangeEvent += OnGridPositionChanged;
            data.HandTypeChangeEvent += OnHandTypeChanged;
            data.TickChangeEvent += OnTickChanged;
            data.BeatLengthChangeEvent += OnBeatLengthChanged;
            timelineTargetIcon.Init(this, data);
            gridTargetIcon.Init(this, data);

            //Must be after the two init's, unfortunate timing restiction, but the new objects must be active to find the hold target managers
            data.BehaviourChangeEvent += OnBehaviorChanged;

            UpdateTimelineSustainLength();

            gridTargetIcon.OnTryRemoveEvent += DeleteNote;
            timelineTargetIcon.OnTryRemoveEvent += DeleteNote;

            gridTargetIcon.IconEnterLoadedNotesEvent += TargetEnterLoadedNotes;
            gridTargetIcon.IconExitLoadedNotesEvent += TargetExitLoadedNotes;

            timelineTargetIcon.TrySelectEvent += MakeTimelineSelectTarget;
            gridTargetIcon.TrySelectEvent += MakeTimelineSelectTarget;

            timelineTargetIcon.TryDeselectEvent += MakeTimelineDeselectTarget;
            gridTargetIcon.TryDeselectEvent += MakeTimelineDeselectTarget;

            SetOutlineColor(NRSettings.config.selectedHighlightColor);

            this.transient = transient;

            #region Transient Color Change (Commented out)
            /*if(transient) {
				foreach (Renderer r in gridTargetIcon.GetComponentsInChildren<Renderer>(true)) {
					if (r.name == "WhiteRing") {
						var color = r.material.GetColor("_Tint");
						color.r = 0.1f;
						color.g = 0.1f;
						color.b = 0.1f;
						r.material.SetColor("_Tint", color);
					}
				}
				foreach (Renderer r in timelineTargetIcon.GetComponentsInChildren<SpriteRenderer>(true)) {
					if (r.material.HasProperty("_Color"))
					{
						var color = r.material.color;
						color.r = 0.5f;
						color.g = 0.5f;
						color.b = 0.5f;
						r.material.color = color; 
					}
				}
			}*/
            #endregion

            if (data.behavior == TargetBehavior.Legacy_Pathbuilder)
            {
                data.legacyPathbuilderData.InitialAngleChangedEvent += UpdatePathInitialAngle;
                data.legacyPathbuilderData.RecalculateEvent += RecalculatePathbuilderData;
                data.legacyPathbuilderData.RecalculateFinishedEvent += UpdatePath;
                data.legacyPathbuilderData.parentNotes.Add(data);

                UpdatePathInitialAngle();
            }
            if(data.behavior == TargetBehavior.Sustain)
            {
                ResetAnimationVisuals();
            }
        }

        public void Destroy(Timeline timeline)
        {
            /*if(gridTargetIcon) {
				UnityEngine.Object.Destroy(gridTargetIcon.gameObject);
			}
			if(timelineTargetIcon) {
				UnityEngine.Object.Destroy(timelineTargetIcon.gameObject);
			}*/

            data.PositionChangeEvent -= OnGridPositionChanged;
            data.HandTypeChangeEvent -= OnHandTypeChanged;
            data.TickChangeEvent -= OnTickChanged;
            data.BeatLengthChangeEvent -= OnBeatLengthChanged;
            data.BehaviourChangeEvent -= OnBehaviorChanged;

            if (data.behavior == TargetBehavior.Legacy_Pathbuilder)
            {
                data.legacyPathbuilderData.InitialAngleChangedEvent -= UpdatePathInitialAngle;
                data.legacyPathbuilderData.RecalculateEvent -= RecalculatePathbuilderData;
                data.legacyPathbuilderData.RecalculateFinishedEvent -= UpdatePath;
                data.legacyPathbuilderData.parentNotes.Remove(data);

                data.legacyPathbuilderData.DeleteCreatedNotes(timeline);
            }
        }

        public void ReplaceData(TargetData newData)
        {
            data.PositionChangeEvent -= OnGridPositionChanged;
            data.HandTypeChangeEvent -= OnHandTypeChanged;
            data.TickChangeEvent -= OnTickChanged;
            data.BeatLengthChangeEvent -= OnBeatLengthChanged;

            data = newData;
            gridTargetIcon.ReplaceData(newData);
            timelineTargetIcon.ReplaceData(newData);

            newData.PositionChangeEvent += OnGridPositionChanged;
            newData.HandTypeChangeEvent += OnHandTypeChanged;
            newData.TickChangeEvent += OnTickChanged;
            newData.BeatLengthChangeEvent += OnBeatLengthChanged;
        }

        public float GetRelativeBeatTime()
        {
            return gridTargetIcon.transform.position.z - gridCamera.position.z - 5f;
        }

        public ParticleSystem GetHoldParticles()
        {
            return gridTargetIcon.holdParticles;
        }

        public void DisplaySustainButtons(bool grid, bool timeline)
        {
            if ((!gridTargetIcon.SustainButtonsActive && grid) || (gridTargetIcon.SustainButtonsActive && !grid))
            {
                gridTargetIcon.sustainButtons.SetActive(grid);
            }
            if ((timelineTargetIcon.SustainButtonsActive && !timeline) || (!timelineTargetIcon.SustainButtonsActive && timeline))
            {
                timelineTargetIcon.sustainButtons.SetActive(timeline);
                if (timeline)
                {
                    Vector3 pos = timelineTargetIcon.sustainButtons.transform.localPosition;
                    pos.y = data.handType == TargetHandType.Right ? -1.25f : -.25f;
                    timelineTargetIcon.sustainButtons.transform.localPosition = pos;
                }
            }
        }

        public void EnableSustainButtons()
        {
            gridTargetIcon.sustainButtons.SetActive(true);
            timelineTargetIcon.sustainButtons.SetActive(true);
            Vector3 pos = timelineTargetIcon.sustainButtons.transform.localPosition;
            pos.y = data.handType == TargetHandType.Right ? -1.25f : -.25f;
            timelineTargetIcon.sustainButtons.transform.localPosition = pos;
        }

        public void DisableSustainButtons()
        {
            gridTargetIcon.sustainButtons.SetActive(false);
            timelineTargetIcon.sustainButtons.SetActive(false);
        }

        private void OnGridPositionChanged(float x, float y)
        {
            var pos = gridTargetIcon.transform.localPosition;

            pos.x = x;
            pos.y = y;

            gridTargetIcon.transform.localPosition = pos;


            if (data.behavior == TargetBehavior.Sustain)
            {
                //var holdEnd = gridTargetIcon.GetComponentInChildren<HoldTargetManager>().endMarker;
                //if (holdEnd) holdEnd.transform.localPosition = new Vector3 (x, y, holdEnd.transform.localPosition.z);
            }

            if (data.behavior == TargetBehavior.Legacy_Pathbuilder && data.legacyPathbuilderData.generatedNotes.Count > 0)
            {
                var firstNote = data.legacyPathbuilderData.generatedNotes[0];
                var delta = firstNote.position - new Vector2(x, y);

                foreach (TargetData note in data.legacyPathbuilderData.generatedNotes)
                {
                    note.position -= delta;
                }

                gridTargetIcon.UpdatePath();
            }
            UpdateChainConnector();
            updateAnimation = true;
        }

        private void UpdateChainConnector()
        {
            if (data.behavior == TargetBehavior.ChainNode || data.behavior == TargetBehavior.ChainStart)
            {
                Timeline.instance.UpdateChainConnector(data);
            }
        }

        public void SetOutlineColor(Color color)
        {
            timelineTargetIcon.SetOutlineColor(color);
            gridTargetIcon.SetOutlineColor(color);
        }

        //Wrapper function for setting the hand types of both targets.
        private void OnHandTypeChanged(TargetHandType newType)
        {
            //Handedness changes how the note is visually displayed in the timeline
            float xOffset = timelineTargetIcon.transform.localPosition.x;
            float yOffset = 0;
            float zOffset = 0;

            switch (data.handType)
            {
                case TargetHandType.Left:
                    yOffset = 0.1f;
                    zOffset = 0.1f;
                    break;
                case TargetHandType.Right:
                    yOffset = -0.1f;
                    zOffset = 0.2f;
                    break;
                case TargetHandType.Either:
                    yOffset = 0.0f;
                    zOffset = 0.0f;
                    break;
            }

            timelineTargetIcon.transform.localPosition = new Vector3(xOffset, yOffset, zOffset);

            if (data.behavior == TargetBehavior.Legacy_Pathbuilder)
            {
                foreach (TargetData note in data.legacyPathbuilderData.generatedNotes)
                {
                    note.handType = newType;
                }
            }
            else if (data.isPathbuilderTarget)
            {
                data.pathbuilderData.UpdateNodeHandType(newType);
            }

            UpdateChainConnector();
        }

        private void OnTickChanged(QNT_Timestamp newTime, QNT_Timestamp oldTime)
        {
            var pos = gridTargetIcon.transform.localPosition;
            pos.z = newTime.ToBeatTime();
            gridTargetIcon.transform.localPosition = pos;

            var timelinePos = timelineTargetIcon.transform.localPosition;
            timelinePos.x = newTime.ToBeatTime();
            timelineTargetIcon.transform.localPosition = timelinePos;

            if (data.behavior == TargetBehavior.Legacy_Pathbuilder && data.legacyPathbuilderData.generatedNotes.Count > 0)
            {
                var firstNote = data.legacyPathbuilderData.generatedNotes[0];
                var delta = firstNote.time - newTime;

                foreach (TargetData note in data.legacyPathbuilderData.generatedNotes)
                {
                    //Force set the time, since these transient notes will get generated for all pathbuilders in repeaters
                    note.SetTimeFromAction(note.time - delta);
                }
            }
            else if (data.isPathbuilderTarget)
            {
                var delta = oldTime - newTime;
                //pathbuilder.UpdatePathbuilderTarget(data);
                foreach (var segment in data.pathbuilderData.Segments)
                {
                    foreach (var node in segment.generatedNodes)
                    {
                        node.SetTimeFromAction(node.time - delta);
                    }
                }
            }

            UpdateChainConnector();
            updateAnimation = true;
        }

        private void OnBeatLengthChanged(QNT_Duration newBeatLength)
        {
            if (!data.supportsBeatLength) return;

            if (data.behavior == TargetBehavior.Legacy_Pathbuilder)
            {
                ChainBuilder.GenerateChainNotes(data);
            }
        }

        private void OnBehaviorChanged(TargetBehavior oldBehavior, TargetBehavior newBehavior)
        {
            if (data.supportsBeatLength)
            {

                if (!TargetData.BehaviorSupportsBeatLength(oldBehavior, data.isPathbuilderTarget))
                {
                    var gridHoldTargetManager = gridTargetIcon.GetComponentInChildren<HoldTargetManager>();

                    if (gridHoldTargetManager != null)
                    {
                        gridHoldTargetManager.sustainLength = data.isPathbuilderTarget ? data.pathbuilderData.BeatLength : data.beatLength;
                        gridHoldTargetManager.LoadSustainController();

                        gridHoldTargetManager.OnTryChangeSustainEvent += MakeTimelineUpdateSustainLength;
                    }
                }

                gridTargetIcon.UpdatePath();
            }
            else
            {
                DisableSustainButtons();

                if (TargetData.BehaviorSupportsBeatLength(oldBehavior, data.isPathbuilderTarget))
                {
                    var gridHoldTargetManager = gridTargetIcon.GetComponentInChildren<HoldTargetManager>(true);
                    if (gridHoldTargetManager != null)
                    {
                        gridHoldTargetManager.UnloadSustainController();
                        gridHoldTargetManager.OnTryChangeSustainEvent -= MakeTimelineUpdateSustainLength;
                    }
                }
            }

            if (data.behavior == TargetBehavior.Legacy_Pathbuilder)
            {
                data.legacyPathbuilderData.InitialAngleChangedEvent += UpdatePathInitialAngle;
                data.legacyPathbuilderData.RecalculateEvent += RecalculatePathbuilderData;
                data.legacyPathbuilderData.RecalculateFinishedEvent += UpdatePath;
            }

            if (oldBehavior == TargetBehavior.Legacy_Pathbuilder)
            {
                data.legacyPathbuilderData.InitialAngleChangedEvent -= UpdatePathInitialAngle;
                data.legacyPathbuilderData.RecalculateEvent -= RecalculatePathbuilderData;
                data.legacyPathbuilderData.RecalculateFinishedEvent -= UpdatePath;
            }

            if (data.behavior != TargetBehavior.Legacy_Pathbuilder && oldBehavior == TargetBehavior.Legacy_Pathbuilder)
            {
                data.legacyPathbuilderData.parentNotes.Remove(data);
            }
            else if (data.behavior == TargetBehavior.Legacy_Pathbuilder)
            {
                data.legacyPathbuilderData.parentNotes.Add(data);
            }

            if (data.behavior == TargetBehavior.Sustain && NRSettings.config.enableSustainAnimation)
            {
                ResetAnimationVisuals();
                Timeline.instance.StartCoroutine(CheckProximity());
            }

            UpdateChainConnector();
        }

        public void UpdateTimelineSustainLength()
        {
            if (!data.supportsBeatLength) return;
            timelineTargetIcon.UpdateTimelineSustainLength();
        }

        public void Select()
        {
            timelineTargetIcon.EnableSelected(data.behavior);
            gridTargetIcon.EnableSelected(data.behavior);
        }

        public void Deselect()
        {
            timelineTargetIcon.DisableSelected();
            gridTargetIcon.DisableSelected();
        }

        public void UpdatePath()
        {
            gridTargetIcon.UpdatePath();
        }

        public void RecalculatePathbuilderData()
        {
            if (data.behavior != TargetBehavior.Legacy_Pathbuilder) return;
            ChainBuilder.CalculateChainNotes(data);
        }

        public void UpdatePathInitialAngle()
        {
            if (data.behavior != TargetBehavior.Legacy_Pathbuilder) return;

            gridTargetIcon.UpdatePathInitialAngle(data.legacyPathbuilderData.initialAngle);
        }

        public void OnNoteHit()
        {

            if (Timeline.instance.paused) return;

            if (data.behavior != TargetBehavior.Mine && data.behavior != TargetBehavior.Melee)
            {
                GridParticles.Emit(this);
            }

            if (noteIsAnimating) return;
            noteIsAnimating = true;

            if (data.behavior == TargetBehavior.Melee)
            {
                if (ParallaxBG.I != null) ParallaxBG.I.OnMeleeHit(data.x);
                GridParticles.ShatterMelee(this);
                noteIsAnimating = false;
            }
            else
            {
                if (NRSettings.config.useBouncyAnimations)
                {
                    Timeline.instance.StartCoroutine(AnimateNoteBounce());
                }
            }
        }

        private void StartAnimateSustain()
        {
            if (data.behavior == TargetBehavior.Sustain)
            {
                Timeline.instance.StartCoroutine(AnimateSustain());
            }
        }

        private void StopAnimateSustain()
        {
            if(data.behavior == TargetBehavior.Sustain)
            {
                //ResetSpinAnimation();
            }
        }
        private bool isAnimatingSustain;
        private IEnumerator CheckProximity()
        {
            var waitTime = new WaitForEndOfFrame();
            //Relative_QNT offset = new((long)Constants.QuarterNoteDuration.tick);
            while (true)
            {
                
                if (!isAnimatingSustain)
                {
                    //var start = data.time - offset;
                    //var end = data.time + data.beatLength + offset;
                    if(Timeline.time >= data.time && Timeline.time <= (data.time + data.beatLength))
                    {
                        ResetAnimationVisuals();
                        isAnimatingSustain = true;
                        StartAnimateSustain();
                    }
                }
                else
                {
                    if(Timeline.time < data.time || Timeline.time > (data.time + data.beatLength))
                    {
                        isAnimatingSustain = false;
                        StopAnimateSustain();
                    }
                }
                

                yield return waitTime;
            }
        }
        private bool updateAnimation = false;
        private IEnumerator AnimateSustain()
        {
            var startTime = data.time;
            var endTime = data.time + data.beatLength;
            var startRotation = Quaternion.identity;
            var startPosition = gridTargetIcon.transform.position;
            var targetPosition = startPosition;
            var startScale = Vector3.one * .7f;
            var targetScale = Vector3.one * .3f;
            GridParticles.StartEmitSustain(this);
            //gridTargetIcon.holdEndTrans.gameObject.SetActive(true);
            while (Timeline.time >= startTime && Timeline.time <= endTime)
            {
                //update start and end time so it still animates correctly if we change beatlength / move the target on the timeline
                startTime = data.time;
                endTime = data.time + data.beatLength;

                targetPosition.z = endTime.ToBeatTime();

                float duration = endTime.ToBeatTime() - startTime.ToBeatTime();

                float zRotation = data.handType == TargetHandType.Right ? 180f * Mathf.Clamp(duration, 1f, Mathf.Infinity) : 180f * Mathf.Clamp(duration, 1f, Mathf.Infinity) * -1f;
                float currentTime = Timeline.time.ToBeatTime();

                float percentage = (currentTime - startTime.ToBeatTime()) / duration;

                gridTargetIcon.note.transform.position = Vector3.Lerp(startPosition, targetPosition, percentage);
                gridTargetIcon.note.transform.rotation = Quaternion.Euler(startRotation.x, startRotation.y, Mathf.SmoothStep(startRotation.z, zRotation, percentage));
                gridTargetIcon.note.transform.localScale = Vector3.Lerp(startScale, targetScale, percentage);

                if (updateAnimation)
                {
                    GridParticles.StartEmitSustain(this);
                    updateAnimation = false;
                    startTime = data.time;
                    endTime = data.time + data.beatLength;
                    startPosition = gridTargetIcon.transform.position;
                    targetPosition = startPosition;
                    targetPosition.z = endTime.ToBeatTime();
                }
                yield return null;
            }
            GridParticles.StopEmitSustain(this);
            if (Timeline.time < startTime)
            {
                gridTargetIcon.note.transform.position = startPosition;
                gridTargetIcon.note.transform.rotation = startRotation;
                gridTargetIcon.note.transform.localScale = startScale;
            }
            /*if (updateAnimation)
            {
                updateAnimation = false;
                Timeline.instance.StartCoroutine(AnimateSustain());
            }*/
            yield return null;
        }

        private void ResetAnimationVisuals()
        {
            if (!NRSettings.config.enableSustainAnimation || isAnimatingSustain) return;
            if (data.behavior != TargetBehavior.Sustain) return;

            var startTime = data.time;
            var endTime = data.time + data.beatLength;
            var startRotation = Quaternion.identity;
            var startPosition = gridTargetIcon.transform.position;
            var targetPosition = startPosition;
            targetPosition.z = endTime.ToBeatTime();

            var startScale = Vector3.one * .7f;
            var targetScale = Vector3.one * .3f;
            float duration = endTime.ToBeatTime() - startTime.ToBeatTime();
            float zRotation = data.handType == TargetHandType.Right ? 180f * Mathf.Clamp(duration, 1f, Mathf.Infinity) : 180f * Mathf.Clamp(duration, 1f, Mathf.Infinity) * -1;
            float currentTime = Timeline.time.ToBeatTime();
            float percentage = (currentTime - startTime.ToBeatTime()) / duration;

            gridTargetIcon.note.transform.position = Vector3.Lerp(startPosition, targetPosition, percentage);
            gridTargetIcon.note.transform.rotation = Quaternion.Euler(startRotation.x, startRotation.y, Mathf.SmoothStep(startRotation.z, zRotation, percentage));
            gridTargetIcon.note.transform.localScale = Vector3.Lerp(startScale, targetScale, percentage);
        }
        private IEnumerator AnimateNoteBounce()
        {
            DOTween.To((float scale) => {
            gridTargetIcon.transform.localScale = new Vector3(scale, scale, 1f);
            }, .48f, .4f, 0.3f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(0.3f);
            noteIsAnimating = false;
        }

        public void AddTargetIconsCloseToPointAtTime(List<TargetIcon> icons, QNT_Timestamp time, Vector2 timelinePoint, Vector2 gridPoint)
        {
            if (gridTargetIcon.IsInValidTime(time) && gridTargetIcon.IsCloseToPoint(gridPoint))
            {
                icons.Add(gridTargetIcon);
            }

            if (timelineTargetIcon.IsInValidTime(time) && timelineTargetIcon.IsCloseToPoint(timelinePoint))
            {
                icons.Add(timelineTargetIcon);
            }
        }

        public bool IsInsideRectAtTime(QNT_Timestamp time, Rect rect)
        {
            return (gridTargetIcon.IsInValidTime(time) && gridTargetIcon.IsInsideRect(rect)) ||
                (timelineTargetIcon.IsInValidTime(time) && timelineTargetIcon.IsInsideRect(rect));
        }

        public bool IsTimelineInsideRect(Rect rect)
        {
            return timelineTargetIcon.IsInsideRect(rect);
        }

        public Cue ToCue() => NotePosCalc.ToCue(this, Timeline.offset);
    }
}