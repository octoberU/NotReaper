using TargetPreview.Math;
using TargetPreview.ScriptableObjects;
using TargetPreview.Scripts;
using TargetPreview.Targets;
using UnityEngine;

public class ChainNode : GridTarget
{
    public Vector3 PhysicalPosition => physicalTarget.position;
    public float lastFlyInDistance = 0;
    public bool shouldRenderChild;

    public override void AnimateFlyIn(float distance)
    {
        return;
    }

    public override void TimeUpdate(float time)
    {
        return;
    }

    public override void UpdateVisuals(TargetData newData)
    {
        meshFilter.mesh = AssetContainer.GetMeshForBehavior(newData.behavior);
        currentHandColor = VisualConfig.GetColorForHandType(newData.handType);
        physicalTargetPropertyBlock =
            AssetContainer.Instance.GetPropertyBlockPhysicalTarget(newData.behavior, currentHandColor);
        meshRenderer.SetPropertyBlock(physicalTargetPropertyBlock);
        transform.localPosition = newData.transformData.position;
        transform.localRotation = newData.transformData.rotation;
    }

    public override void Awake()
    {
        return;
    }

    public void AnimateChildFlyIn(float distance)
    {
        physicalTarget.transform.localPosition = TargetTransform.Parabola(Vector3.zero, GetFlyInPosition(), flyInDistance * verticalInfluence,
            distance);

        lastFlyInDistance = distance;
        
        physicalTargetPropertyBlock.SetColor("_Color",
            Color.Lerp(currentHandColor, Color.black, distance * distance));
        meshRenderer.SetPropertyBlock(physicalTargetPropertyBlock);
        
        shouldRenderChild = TimeController.Time <= targetData.time && distance < 0.999f;
        physicalTarget.gameObject.SetActive(shouldRenderChild);

    }
    
    
}