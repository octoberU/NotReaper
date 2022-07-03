using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class OverlaySetterProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.OverlaySetter;

        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.value1.DisplayName = "Song Info";
            displayData.value2.DisplayName = "Mapper";
        }
    }
}