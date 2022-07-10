using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Timing;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Modifiers
{
    public class Modifier : Content
    {

        public Data Data { get; private set; }

        public override int Type => (int)Data.type;
        public ModifierType ModifierType => Data.type;
        public override TimelineType TimelineType => TimelineType.Modifier;
        public bool SupportsEndTime => ModifierUtility.SupportsEndTime(ModifierType, Data.option1, Data.option2);
        
        public float[] LeftHandColor => Data.leftHandColor ?? new float[] { 1, 1, 1};
        public float[] RightHandColor => Data.rightHandColor ?? new float[] { 1, 1, 1 };
        public override ContentData GetData() => Data;

        public override void Initialize(Track track)
        {
            Data = new Data
            {
                type = (ModifierType)track.Type
            };
            base.Initialize(track);
        }

        public void LoadData(Data data)
        {
            Data = data;
            SetTime(new(data.startTick, data.endTick));
        }

        public override void ResetDuration()
        {
            if (SupportsEndTime) return;
            
            base.ResetDuration();
        }
        
        protected override void UpdateTimeData()
        {
            Data.startTick = (int)startTime.tick;
            Data.endTick = (int)endTime.tick;
        }

        protected override void ResetData()
        {
            Data = null;
        }

        public override void OnScaleChanged(float scaleAmount)
        {
            
        }
       
    }
}
