using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class SpeedProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.Speed;
        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.amount.DisplayName = "speed";
        }
        internal override Vector2 AmountMinMax => new(10, 200);
    }
}