using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.SustainTimeline
{
    public class SustainTrack : Track
    {
        protected override string TypeToDisplayName(int type)
            => ((Pitch)type).ToDisplayName();
    }
}
