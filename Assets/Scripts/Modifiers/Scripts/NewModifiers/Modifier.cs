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
        [SerializeField] private float defaultSpriteWidth = .5f;
        [SerializeField] private float nonDraggableSpriteWidth = .3f;
        [SerializeField] private SpriteRenderer indicatorRenderer;
        [SerializeField] private SpriteRenderer selectedRenderer;
        [SerializeField] private Sprite nonDraggableSprite;
        [SerializeField] private Sprite nonDraggableSelectedSprite;
        
        public Data Data { get; private set; }

        public override int Type => (int)Data.type;
        public ModifierType ModifierType => Data.type;
        public override TimelineType TimelineType => TimelineType.Modifier;
        public bool SupportsEndTime => ModifierUtility.SupportsEndTime(ModifierType, Data.option1, Data.option2);
        
        public float[] LeftHandColor => Data.leftHandColor ?? new float[] { 1, 1, 1};
        public float[] RightHandColor => Data.rightHandColor ?? new float[] { 1, 1, 1 };
        public override ContentData GetData() => Data;

        public override void Initialize(Track track, QNT_Timestamp? start)
        {
            Data = new Data
            {
                type = (ModifierType)track.Type
            };
            base.Initialize(track, start);

            if (SupportsEndTime)
            {
                if(start.HasValue)
                    endTime = start.Value + Constants.EighthNoteDuration;
            }
        }

        public void SetupSprites()
        {
            if(!SupportsEndTime)
                SetSprites(nonDraggableSprite, nonDraggableSelectedSprite, nonDraggableSpriteWidth);
        }

        private void SetSprites(Sprite indicator, Sprite selected, float width)
        {
            indicatorRenderer.sprite = indicator;
            selectedRenderer.sprite = selected;
            var size = indicatorRenderer.size;
            size.x = width;
            indicatorRenderer.size = size;
            selectedRenderer.size = size;
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

        protected override void UpdateSize()
        {
            if (endTime > startTime)
            {
                var size = indicators[0].size;
                size.x = endTime.ToBeatTime() - startTime.ToBeatTime();
                foreach (var indicator in indicators)
                {
                    indicator.size = size;
                }
            }
        }

        protected override void ResetSize()
        {
            var size = indicators[0].size;
            size.x = SupportsEndTime ? defaultSpriteWidth : nonDraggableSpriteWidth;
            foreach (var indicator in indicators)
            {
                indicator.size = size;
            }
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
