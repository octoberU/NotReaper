using TargetPreview.Math;
using TargetPreview.ScriptableObjects;
using UnityEngine;

namespace TargetPreview.Targets
{
    public class MineTarget : MeleeTarget
    {
        public override void AnimateFlyIn(float distance) =>
            physicalSphereTransform.localPosition = Vector3.Lerp(Vector3.zero, GetFlyInPosition(), distance);

        public override void UpdateVisuals(TargetData newData)
        {
            base.UpdateVisuals(newData);
            currentHandColor = Color.red;
            approachRing.GetComponent<MeshRenderer>().SetPropertyBlock(AssetContainer.Instance.GetPropertyBlockPhysicalTarget(newData.behavior, currentHandColor));//optimize
        }
    }
}