using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class AutoLightingProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.AutoLighting;
        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.amount.DisplayName = "max brightness";
            displayData.option1.DisplayName = "alternative mode";
        }
    }
}