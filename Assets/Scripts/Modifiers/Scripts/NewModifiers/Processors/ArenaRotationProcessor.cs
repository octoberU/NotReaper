using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class ArenaRotationProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.ArenaRotation;
        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.amount.DisplayName = "rotation amount";
            displayData.option1.DisplayName = "continuous";
            displayData.option2.DisplayName = "incremental";
        }
        
        internal override string Hint => Option1.Get() ? "amount represents rotation speed" : Option2.Get() ? "amount represents speed at end tick" : "";
        internal override Vector2 AmountMinMax => new(-500, 500);
    }
}