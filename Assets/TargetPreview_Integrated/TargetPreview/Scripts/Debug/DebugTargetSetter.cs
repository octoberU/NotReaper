using System.Collections;
using System.Collections.Generic;
using TargetPreview.Targets;
using UnityEngine;

public class DebugTargetSetter : MonoBehaviour
{
    [ContextMenu("SwitchTargetColor")]
    void SwitchTargetColor()
    {
        var target = FindObjectOfType<Target>();
        var targetData = target.TargetData;
        var newHandType = targetData.handType == TargetHandType.Left ? TargetHandType.Right : TargetHandType.Left;
        target.TargetData = new TargetData
            (targetData.behavior, newHandType, targetData.time, targetData.transformData, targetData.cue);
    }

    [ContextMenu("CycleBehavior")]
    void CycleBehavior()
    {
        var target = FindObjectOfType<Target>();
        var targetData = target.TargetData;
        var newBehavior = (int)targetData.behavior == 3 ? TargetBehavior.Standard : (TargetBehavior)((int)targetData.behavior + 1);
        target.TargetData = new TargetData
            (newBehavior, targetData.handType, targetData.time, targetData.transformData, targetData.cue);
    }
}
