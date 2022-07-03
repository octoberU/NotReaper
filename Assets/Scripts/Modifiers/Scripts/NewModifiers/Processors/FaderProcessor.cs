using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class FaderProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.Fader;
        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.amount.DisplayName = "target brightness";
        }
    }
}