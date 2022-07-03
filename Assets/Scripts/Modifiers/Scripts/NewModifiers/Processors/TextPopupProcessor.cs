using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class TextPopupProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.TextPopup;
        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.value1.DisplayName = "text";
            displayData.value2.DisplayName = "size";
            displayData.value3.DisplayName = "x offset";
            displayData.value4.DisplayName = "y offset";
            displayData.value5.DisplayName = "z offset";
            displayData.option1.DisplayName = "glow";
            displayData.option3.DisplayName = "face forward";

            displayData.value2.ContentType = TMP_InputField.ContentType.DecimalNumber;
            displayData.value3.ContentType = TMP_InputField.ContentType.DecimalNumber;
            displayData.value4.ContentType = TMP_InputField.ContentType.DecimalNumber;
            displayData.value5.ContentType = TMP_InputField.ContentType.DecimalNumber;
        }
    }
}