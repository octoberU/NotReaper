using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class ArenaBrightnessProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.ArenaBrightness;
        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.amount.DisplayName = "brightness";
            displayData.option1.DisplayName = "continuous";
            displayData.option2.DisplayName = "strobe";

            if (Option2.Get())
            {
                displayData.value1.DisplayName = "off brightness";
                displayData.value2.DisplayName = "on brightness";
                displayData.value1.ContentType = TMP_InputField.ContentType.IntegerNumber;
                displayData.value2.ContentType = TMP_InputField.ContentType.IntegerNumber;
            }
            else
            {
                displayData.value1.Hide();
                displayData.value2.Hide();
            }
        }
        
        internal override string Hint => Option1.Get() ? "amount represents speed" : Option2.Get() ? "amount represents flashes per beat (1/4)" : "0-100%";
        internal override Vector2 AmountMinMax => Option2.Get() ? new (1, 128) : new(0, 100);
        internal override bool RefreshOnSelect => true;
        internal override void OnOption1Changed() => uiHandler.RefreshProcessor();
        internal override void OnOption2Changed() => uiHandler.RefreshProcessor();
    }
}