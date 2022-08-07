using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper;
using NotReaper.Targets;
using UnityEngine;

namespace NotReaper.SustainTimeline
{
    public class SustainData : ContentData
    {
        public Pitch type;

        protected override ContentData CloneData()
            => new SustainData
            {
                type = type
            };
    }
}
