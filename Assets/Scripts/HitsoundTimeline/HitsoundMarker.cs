using System.Collections;
using System.Collections.Generic;
using NotReaper.Models;
using NotReaper.Modifiers;
using NotReaper.Targets;
using NotReaper.Timing;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.HitsoundTimeline
{
    public class HitsoundMarker : Content
    {
        [SerializeField] private Image left;
        [SerializeField] private Image right;
        public override int Type => Data == null ? Track.Type : (int)Data.type;
        public HitsoundData Data { get; private set; }
        public override TimelineType TimelineType => TimelineType.Hitsound;

        public override ContentData GetData() => Data;
        
        public delegate void DataChangedDelegate(HitsoundMarker marker);
        public delegate void TrackSwitchedDelegate(HitsoundMarker marker, HitsoundTrack oldTrack);

        public delegate void BehaviorChangedDelegate(HitsoundMarker marker, TargetBehavior oldBehavior);
        public delegate void OnTimeChangedDelegate(HitsoundMarker marker, QNT_Timestamp newTime, QNT_Timestamp oldTime);

        public event DataChangedDelegate onHitsoundChanged;
        public event BehaviorChangedDelegate onBehaviorChanged;
        public event DataChangedDelegate onDestroy;
        public event OnTimeChangedDelegate onTimeChanged;
        public event TrackSwitchedDelegate onTrackSwitched;

        private Track oldTrack;

        public void LoadData(HitsoundData data)
        {
            Data = data;
            SetTime(new QNT_Timestamp((ulong)data.startTick));
            data.targetData.VelocityChangeEvent += OnVelocityChanged;
            data.targetData.TickChangeEvent += OnTimeChanged;
            data.targetData.HandTypeChangeEvent += OnHandTypeChanged;
            data.targetData.BehaviourChangeEvent += OnBehaviorChanged;
            data.target.onDestroy += OnTargetDestroyed;
            SetToSingle();
        }

        private void OnBehaviorChanged(TargetBehavior oldBehavior, TargetBehavior newBehavior) => onBehaviorChanged?.Invoke(this, oldBehavior);

        private void OnHandTypeChanged(TargetHandType hand) => UpdateIndicatorColor();

        private void OnVelocityChanged(InternalTargetVelocity oldVelocity, InternalTargetVelocity newVelocity) => onHitsoundChanged?.Invoke(this);

        private void OnTimeChanged(QNT_Timestamp newTime, QNT_Timestamp oldTime) => onTimeChanged?.Invoke(this, newTime, oldTime);

        protected override void UpdateTimeData()
        {
            Data.startTick = (int)startTime.tick;
            Data.endTick = (int)endTime.tick;
        }

        public override void SetStartTime(QNT_Timestamp time) => SetTime(time);
        public override void SetEndTime(QNT_Timestamp time) => SetTime(time);
        public override void SetTime(Timeframe timeframe) => SetTime(timeframe.StartTime);

        private void SetTime(QNT_Timestamp time)
        {
            startTime = time;
            endTime = time;
            UpdateSize();
            UpdateTimeData();
        }

        protected override void UpdateSize()
        {
            var position = transform.localPosition;
            position.x = startTime.ToBeatTime();
            transform.localPosition = position;
        }

        protected override void ResetData()
        {
            if (Data == null) return;

            Data.targetData.VelocityChangeEvent -= OnVelocityChanged;
            Data.targetData.TickChangeEvent -= OnTimeChanged;
            Data.targetData.HandTypeChangeEvent -= OnHandTypeChanged;
            Data.targetData.BehaviourChangeEvent -= OnBehaviorChanged;
            Data.target.onDestroy -= OnTargetDestroyed;
            Data = null;
        }

        public override bool SwitchTrack(Track newTrack)
        {
            var myType = Data.type;
            oldTrack = Track;
            var otherType = (TimelineHitsound)newTrack.Type;
            if (myType.IsMelee() != otherType.IsMelee()) return false;
            base.SwitchTrack(newTrack);
            onTrackSwitched?.Invoke(this, oldTrack as HitsoundTrack);
            return true;
        }

        public override void SetSelected(bool selected)
        {
            base.SetSelected(selected);
            if (selected)
            {
                EditorNotes.SelectTarget(Data.target);
            }
            else
            {
                EditorNotes.DeselectTarget(Data.target);
            }
        }

        private void UpdateIndicatorColor()
        {
            if (Data == null) return;
            
            left.color = right.color = NRSettings.GetColorForHandType(Data.targetData.handType);
        }

        public void SetToDual(bool selectable)
        {
            if (Data.targetData.behavior.IsMeleeOrMine()) return;

            left.color = NRSettings.GetColorForHandType(TargetHandType.Left);
            right.color = NRSettings.GetColorForHandType(TargetHandType.Right);
            boxCollider.enabled = selectable;
            Data.isDual = true;
        }

        public void SetToSingle()
        {
            boxCollider.enabled = true;
            Data.isDual = false;
            UpdateIndicatorColor();
        }

        public override void OnScaleChanged(float scaleAmount)
        {
            var scale = transform.localScale;
            scale.x = scaleAmount;
            transform.localScale = scale;
        }

        public void OnTargetDestroyed(Target _) => onDestroy?.Invoke(this);

        public override bool IsInsideBounds(Bounds other) => boxCollider.enabled && base.IsInsideBounds(other);
    }
}