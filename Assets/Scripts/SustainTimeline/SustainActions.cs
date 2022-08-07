using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.SustainTimeline
{
    public class MultiAddSustainAction : MultiAddContentAction<SustainData>
    {
        public MultiAddSustainAction(List<SustainData> datas) : base(datas)
        {
        }

        protected override Content LoadData(SustainData data, TimelineManager<SustainData> manager)
            => ((SustainTimelineManager)manager).LoadSustainMarker(data);
    }
    
    public class RemoveSustainAction : RemoveContentAction<SustainData>
    {
        public RemoveSustainAction(Content content) : base(content)
        {
        }

        protected override Content LoadData(SustainData data, TimelineManager<SustainData> manager)
            => (manager as SustainTimelineManager).LoadSustainMarker(data);
    }

    public class MultiRemoveSustainAction : MultiRemoveContentAction<SustainData>
    {
        public MultiRemoveSustainAction(List<Content> content) : base(content)
        {
        }

        protected override Content LoadData(SustainData data, TimelineManager<SustainData> manager)
            => (manager as SustainTimelineManager).LoadSustainMarker(data);
    }
}
