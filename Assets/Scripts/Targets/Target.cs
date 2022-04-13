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

        //public event Action<Target> TargetEnterLoadedNotesEvent;
        public void TargetEnterLoadedNotes()
        {
            //TargetEnterLoadedNotesEvent(this);
            gridTargetIcon.ResetAnimationVisuals();
        }

        /*public event Action<Target> TargetExitLoadedNotesEvent;
        public void TargetExitLoadedNotes()
        {
            TargetExitLoadedNotesEvent(this);
        }*/

        public event Action<Target> TargetSelectEvent;
        public void MakeTimelineSelectTarget()
        {
            if (!transient)
            {
                TargetSelectEvent(this);
            }
        }
        //public event Action<Target, bool> TargetDeselectEvent;
        public event Action<Target> TargetDeselectEvent;
        public void MakeTimelineDeselectTarget()
        {
            if (!transient)
            {
                //TargetDeselectEvent(this, false);
                TargetDeselectEvent(this);
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
            //gridTargetIcon.IconExitLoadedNotesEvent += TargetExitLoadedNotes;

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
                gridTargetIcon.ResetAnimationVisuals();
            }
        }

        public void Destroy()
        {
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

                data.legacyPathbuilderData.DeleteCreatedNotes();
            }
            gridTargetIcon.stopAnimating = true;
            gridTargetIcon.StopCheckProximity();
            gridTargetIcon.StopAnimatingSustain();
            gridTargetIcon.ResetAnimationVisuals();
        }

        public void Reset()
        {
            timelineTargetIcon.data = null;
            timelineTargetIcon.target = null;
            gridTargetIcon.data = null;
            gridTargetIcon.target = null;
            data = null;
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

        public void DisplaySustainButtons(bool grid)
        {
            if ((!gridTargetIcon.SustainButtonsActive && grid) || (gridTargetIcon.SustainButtonsActive && !grid))
            {
                gridTargetIcon.sustainButtons.SetActive(grid);
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
            EditorTargets.UpdateDualines();
            gridTargetIcon.updateAnimation = true;
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
            else if(data.behavior == TargetBehavior.Sustain)
            {
                GridParticles.StopEmitSustain(newType == TargetHandType.Left ? TargetHandType.Right : TargetHandType.Left);
                if (data.time >= EditorTime.Time && data.time + data.beatLength < EditorTime.Time)
                    GridParticles.StartEmitSustain(this);
            }
            EditorTargets.UpdateDualines();
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
            EditorTargets.UpdateDualines();
            gridTargetIcon.updateAnimation = true;
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
            if (newBehavior == TargetBehavior.Sustain && NRSettings.config.enableSustainAnimation)
            {
                gridTargetIcon.ResetAnimationVisuals();
                gridTargetIcon.StartCheckProximity();
            }
            else if(newBehavior != TargetBehavior.Sustain && oldBehavior == TargetBehavior.Sustain)
            {
                gridTargetIcon.KillSustainAnimation();
            }
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

            if (!EditorAudio.IsPlaying) return;

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
                    Timeline.Instance.StartCoroutine(AnimateNoteBounce());
                }
            }
        }

        private IEnumerator AnimateNoteBounce()
        {
            DOTween.To((float scale) => {
            gridTargetIcon.transform.localScale = new Vector3(scale, scale, 1f);
            }, .48f, .4f, 0.3f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(0.3f);
            noteIsAnimating = false;
        }

        public void AddTargetIconsCloseToPointAtTime(List<TargetIcon> icons, QNT_Timestamp time, Vector2 point, TargetIconLocation location)
        {
            if(location == TargetIconLocation.Grid)
            {
                if (gridTargetIcon.IsInValidTime(time) && gridTargetIcon.IsCloseToPoint(point))
                {
                    icons.Add(gridTargetIcon);
                }
            }
            else
            {
                if (timelineTargetIcon.IsInValidTime(time) && timelineTargetIcon.IsCloseToPoint(point))
                {
                    icons.Add(timelineTargetIcon);
                }
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