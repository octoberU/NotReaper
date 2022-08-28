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
using NotReaper.HitsoundTimeline;
using NotReaper.Tools.PathBuilder;
using NotReaper.UI.Particles;

namespace NotReaper.Targets
{


    public class Target
    {

        public TargetIcon gridTargetIcon;
        public TargetIcon timelineTargetIcon;
        private bool noteIsAnimating = false;
        public TargetData data { get; private set; }

        private bool _transient = false;
        public bool transient
        {
            get { return _transient; }
            set
            {
                _transient = value;
                data.transient = value;
            }
        }

        private Transform gridCamera;
        private HitsoundMarker hitsoundMarker;

        [HideInInspector]
        public bool isPlayingSustains = false;

        public delegate void DestroyDelegate(Target target);
        public event DestroyDelegate onDestroy;

        public delegate void SelectDelegate(bool selected);

        public event SelectDelegate onSelected;

        public void DeleteNote()
        {
            EditorTargets.DeleteTarget(this);
        }

        public void TargetEnterLoadedNotes()
        {
            //TargetEnterLoadedNotesEvent(this);
            gridTargetIcon.ResetAnimationVisuals();
        }

        public void Select()
        {
            if (!transient)
            {
                EditorNotes.SelectTarget(this);
            }
        }

        public void Deselect()
        {
            if (!transient)
            {
                EditorNotes.DeselectTarget(this);
            }
        }

        public void UpdateSustainLength(bool increase)
        {
            EditorTargets.UpdateSustainLength(this, increase);
        }


        public Target(TargetData targetData, TargetIcon timelineIcon, TargetIcon gridIcon, bool transient, Transform gridCamera)
        {
            timelineTargetIcon = timelineIcon;
            gridTargetIcon = gridIcon;
            this.gridCamera = gridCamera;

            data = targetData;
            data.PositionChangeEvent += OnGridPositionChanged;
            data.HandTypeChangeEvent += OnHandTypeChanged;
            data.TickChangeEvent += OnTickChanged;
            data.BeatLengthChangeEvent += OnBeatLengthChanged;
            timelineTargetIcon.Init(this, data);
            gridTargetIcon.Init(this, data);
            
            OnGridPositionChanged(data.x, data.y);
            OnHandTypeChanged(data.handType);
            OnTickChanged(data.time, data.time);
            OnBeatLengthChanged(data.beatLength);
            OnBehaviorChanged(data.behavior, data.behavior);

            //Must be after the two init's, unfortunate timing restiction, but the new objects must be active to find the hold target managers
            data.BehaviourChangeEvent += OnBehaviorChanged;

            UpdateTimelineSustainLength();

            gridTargetIcon.OnTryRemoveEvent += DeleteNote;
            timelineTargetIcon.OnTryRemoveEvent += DeleteNote;

            gridTargetIcon.IconEnterLoadedNotesEvent += TargetEnterLoadedNotes;

            timelineTargetIcon.TrySelectEvent += Select;
            gridTargetIcon.TrySelectEvent += Select;

            timelineTargetIcon.TryDeselectEvent += Deselect;
            gridTargetIcon.TryDeselectEvent += Deselect;

            SetOutlineColor(NRSettings.config.selectedHighlightColor);

            this.transient = transient;

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

            gridTargetIcon.OnTryRemoveEvent -= DeleteNote;
            timelineTargetIcon.OnTryRemoveEvent -= DeleteNote;

            gridTargetIcon.IconEnterLoadedNotesEvent -= TargetEnterLoadedNotes;

            timelineTargetIcon.TrySelectEvent -= Select;
            gridTargetIcon.TrySelectEvent -= Select;

            timelineTargetIcon.TryDeselectEvent -= Deselect;
            gridTargetIcon.TryDeselectEvent -= Deselect;
            
            gridTargetIcon.stopAnimating = true;
            gridTargetIcon.StopCheckProximity();
            gridTargetIcon.StopAnimatingSustain();
            gridTargetIcon.ResetAnimationVisuals();
            gridTargetIcon.SetTransparency(1f);
            onDestroy?.Invoke(this);
        }

        public void Reset()
        {
            isPlayingSustains = false;
            gridTargetIcon.ClearData();
            timelineTargetIcon.ClearData();
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
                    yOffset = 0.25f;
                    zOffset = 0.1f;
                    break;
                case TargetHandType.Right:
                    yOffset = -0.25f;
                    zOffset = 0.2f;
                    break;
                case TargetHandType.Either:
                    yOffset = 0.0f;
                    zOffset = 0.0f;
                    break;
            }

            timelineTargetIcon.transform.localPosition = new Vector3(xOffset, yOffset, zOffset);
            
            if (data.isPathbuilderTarget)
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

                        gridHoldTargetManager.OnTryChangeSustainEvent += UpdateSustainLength;
                    }
                }
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
                        gridHoldTargetManager.OnTryChangeSustainEvent -= UpdateSustainLength;
                    }
                }
            }
            
            if (newBehavior == TargetBehavior.Sustain && NRSettings.config.enableSustainAnimation)
            {
                gridTargetIcon.stopAnimating = false;
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

        public void VisualSelect()
        {
            timelineTargetIcon.EnableSelected(data.behavior);
            gridTargetIcon.EnableSelected(data.behavior);
            onSelected?.Invoke(true);
        }

        public void VisualDeselect()
        {
            timelineTargetIcon.DisableSelected();
            gridTargetIcon.DisableSelected();
            onSelected?.Invoke(false);
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
                if (NRSettings.config.useBouncyAnimations && data.behavior != TargetBehavior.ChainNode)
                {
                    Timeline.Instance.StartCoroutine(AnimateNoteBounce());
                }
            }
        }

        private IEnumerator AnimateNoteBounce()
        {
            var currentScale = gridTargetIcon.transform.localScale.x;
            var targetScale = currentScale + .08f;
            DOTween.To((float scale) => {
            gridTargetIcon.transform.localScale = new Vector3(scale, scale, 1f);
            }, targetScale, currentScale, 0.3f).SetEase(Ease.OutCubic);
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

        public Cue ToCue() => NotePosCalc.ToCue(data, Timeline.offset);
    }
}