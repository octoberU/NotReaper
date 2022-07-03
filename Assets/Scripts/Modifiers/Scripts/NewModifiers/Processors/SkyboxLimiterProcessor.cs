using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class SkyboxLimiterProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.SkyboxLimiter;
        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.amount.DisplayName = "limit";
        }
        
        internal override string Hint => "limits brightness to never go above the set %";
    }
}