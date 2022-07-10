using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using UnityEngine;

namespace NotReaper.HitsoundTimeline
{
    public class HitsoundTrack : Track
    {
        protected override string TypeToDisplayName(int type)
            => ((TimelineHitsound)type).ToDisplayName();
        
        public bool TryGetContent(QNT_Timestamp time, TargetData matchData, out HitsoundMarker marker)
        {
            foreach (var content in Content)
            {
                var m = content as HitsoundMarker;
                if (m.timeframe.Contains(time) && m.Data.targetData == matchData)
                {
                    marker = m;
                    return true;
                }
            }

            marker = null;
            return false;
        }
    }
}
