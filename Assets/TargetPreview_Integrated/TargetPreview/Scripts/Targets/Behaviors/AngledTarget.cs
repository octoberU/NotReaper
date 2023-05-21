using UnityEngine;

namespace TargetPreview.Targets
{
    public class AngledTarget : GridTarget
    {
        public override void UpdateVisuals(TargetData newData)
        {
            base.UpdateVisuals(newData);
            targetCenter.transform.localRotation =
                Quaternion.Euler(new Vector3(0, 0, newData.behavior == TargetBehavior.Horizontal ? 0 : 90));
            
            //Fix orientation for angled targets
            if (newData.behavior == TargetBehavior.Vertical)
            {
                approachRing.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -90));
                telegraph.transform.Rotate(-180, -90, 180, Space.Self);
            }
            else if (newData.behavior == TargetBehavior.Horizontal)
            {
                physicalTarget.Rotate(-180, -90, 180, Space.Self);
                telegraph.transform.localRotation = Quaternion.Euler(new Vector3(-90, 0, 0));
            }
        }
    }
}