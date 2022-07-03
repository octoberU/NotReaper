using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class AimAssistProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.AimAssist;

        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.amount.DisplayName = "Aim Assist Amount";
        }

        protected internal string Hint => "0 - 100%";
    }
}
