using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifiers.Processors;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class ArenaChangeProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.ArenaChange;

        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.value1.DisplayName = "Arena Option 1";
            displayData.value2.DisplayName = "Arena Option 2";
            displayData.option1.DisplayName = "Preload";
        }
    }
}
