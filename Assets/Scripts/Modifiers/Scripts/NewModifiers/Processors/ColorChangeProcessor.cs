using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifiers.Processors;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class ColorChangeProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.ColorChange;

        protected override void InitializeDisplayData(ref DisplayData displayData) { }
        internal override ColorPickerType ColorPickerType => ColorPickerType.HSV;
    }
}