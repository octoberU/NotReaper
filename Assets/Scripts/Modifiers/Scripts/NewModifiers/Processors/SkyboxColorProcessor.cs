using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class SkyboxColorProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.SkyboxColor;
        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.option2.DisplayName = "reset";
        }

        internal override ColorPickerType ColorPickerType => ColorPickerType.RGB;
    }
}