using System.Collections;
using System.Collections.Generic;
using NotReaper;
using UnityEngine;

namespace NotReaper.HitsoundTimeline
{
    public class HitsoundUndoRedo : TimelineUndoRedo<HitsoundData>
    {
        protected override TimelineManager<HitsoundData> GetManager()
            => NRDependencyInjector.Get<HitsoundManager>();
    }
}
