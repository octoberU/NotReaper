using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifiers.Processors;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class ColorUpdateProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.ColorUpdate;
        protected override void InitializeDisplayData(ref DisplayData displayData) { }
        internal override ColorPickerType ColorPickerType => ColorPickerType.HSV;
    }
}
