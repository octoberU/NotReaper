using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.SustainTimeline
{
    public class SustainUndoRedo : TimelineUndoRedo<SustainData>
    {
        protected override TimelineManager<SustainData> GetManager()
            => NRDependencyInjector.Get<SustainTimelineManager>();
    }
}
