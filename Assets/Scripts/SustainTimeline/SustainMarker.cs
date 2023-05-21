using System.Collections;
using System.Collections.Generic;
using NotReaper.Targets;
using NotReaper.Timing;
using UnityEngine;

namespace NotReaper.SustainTimeline
{
    public class SustainMarker : Content
    {
        public SustainData Data { get; private set; }
        public override int Type => (int)Data.type;
        public override TimelineType TimelineType => TimelineType.Sustain;
        public override ContentData GetData() => Data;


        public override void Initialize(Track track, QNT_Timestamp? start)
        {
            Data = new SustainData
            {
                type = (Pitch)track.Type
            };
            base.Initialize(track, start);
            UpdateSize();
        }

        protected override void UpdateTimeData()
        {
            Data.startTick = (int)startTime.tick;
            Data.endTick = (int)endTime.tick;
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
            size.x = .5f;
            foreach (var indicator in indicators)
            {
                indicator.size = size;
            }
        }

        public void LoadData(SustainData data)
        {
            Data = data;
            SetTime(new(data.startTick, data.endTick));
            UpdateSize();
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
