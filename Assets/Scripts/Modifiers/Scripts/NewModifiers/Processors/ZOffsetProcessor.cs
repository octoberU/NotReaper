using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Targets;
using TMPro;
using UnityEngine;

namespace NotReaper.Modifiers.Processors
{
    public class ZOffsetProcessor : Processor
    {
        protected override ModifierType Type => ModifierType.zOffset;
        protected override void InitializeDisplayData(ref DisplayData displayData)
        {
            displayData.amount.DisplayName = "zoffset";
            displayData.value1.DisplayName = "transition target amount";
            displayData.value1.ContentType = TMP_InputField.ContentType.IntegerNumber;
            displayData.extraButton.DisplayName = "count targets";
        }
        internal override Vector2 AmountMinMax => new(-100, 500);

        internal override void OnExtraButtonPressed()
        {
            if (Modifier == null) return;
            
            Modifier.Data.value1 = EditorNotes.OrderedNotes.Count(t => t.data.time >= Modifier.startTime && t.data.time <= EditorTime.Time).ToString();
            uiHandler.RefreshProcessor();
        }
    }
}