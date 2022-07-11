using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools;
using UnityEngine;
using TargetData = TargetPreview.Targets.TargetData;

namespace NotReaper.HitsoundTimeline
{

    public class AddHitsoundAction : AddContentAction<HitsoundData>
    {
        private Timeframe timeframe;
        public AddHitsoundAction(QNT_Timestamp startTime, QNT_Duration duration, Track track) : base(startTime, track)
        {
            timeframe = new(startTime, startTime + duration);
        }

        public override void DoAction(TimelineManager<HitsoundData> manager)
        {
            if (initialAdd)
            {
                addedContent = manager.PlaceContentFromAction(timeframe, track);
                initialAdd = false;
            }
            else
            {
                addedContent = manager.PlaceContentFromAction(addedContent.timeframe, addedContent.Track);
            }
        }
    }

    public class RemoveHitsoundAction : RemoveContentAction<HitsoundData>
    {
        public RemoveHitsoundAction(Content content) : base(content)
        {
        }
        
        protected override Content LoadData(HitsoundData data, TimelineManager<HitsoundData> manager)
            => ((HitsoundManager)manager).LoadHitsoundMarker(data);
    }

    public class MultiRemoveHitsoundAction : MultiRemoveContentAction<HitsoundData>
    {
        public MultiRemoveHitsoundAction(List<Content> content) : base(content)
        {
        }

        protected override Content LoadData(HitsoundData data, TimelineManager<HitsoundData> manager)
            => ((HitsoundManager)manager).LoadHitsoundMarker(data);
    }
    
}