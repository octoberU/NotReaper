using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifiers.Processors;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class ColorSwapProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.ColorSwap;

        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            
        }
    }
}
