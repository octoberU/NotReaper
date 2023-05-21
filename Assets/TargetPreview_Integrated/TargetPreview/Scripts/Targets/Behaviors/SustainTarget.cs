using System.Collections;
using System.Collections.Generic;
using TargetPreview.Targets;
using UnityEngine;

public class SustainTarget : GridTarget
{
    [SerializeField] public float 
        sustainSpinDistance = 1f,
        sustainSpinRotation = 720f;
    public override void TimeUpdate(float time)
    {
        base.TimeUpdate(time);

        var targetComplete = TargetManager.Time >= targetData.cue.timeMs;
        if (targetComplete)
        {
            float positiveTemporalDistance = (TargetManager.Time - targetData.cue.timeMs) /
                                             (targetData.cue.timeEndMs - targetData.cue.timeMs);
            float distanceClamped = Mathf.Clamp(positiveTemporalDistance, 0f, 1f);

            physicalTarget.localRotation =
                Quaternion.Euler(-90f, 0f,0f) *
                Quaternion.Euler(0, Mathf.Lerp(0, sustainSpinRotation, distanceClamped * distanceClamped), 0);
            physicalTarget.localPosition = Vector3.Lerp(Vector3.zero, Vector3.forward * sustainSpinDistance, distanceClamped);
            FadePhysicalTarget(distanceClamped);
        }

        targetCenter.enabled = ShouldRender && (!targetComplete);
        
    }

    bool shouldAnimate = true;
    

    public override bool ShouldRender
    {
        get
        {
            var modifiedFlyInTime = ModifiedFlyInTime;
            return TargetManager.Time >= (targetData.cue.timeMs - modifiedFlyInTime) &&
                   TargetManager.Time <= (targetData.cue.timeEndMs);
        }
    }
}
