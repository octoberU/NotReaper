using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class PsychedeliaUpdateProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.PsychedeliaUpdate;
        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.amount.DisplayName = "cycle speed";
        }
        internal override string Hint => "how fast psychedelia cycles";
        internal override Vector2 AmountMinMax => new(0, 10000);
    }
}