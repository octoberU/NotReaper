using UnityEngine;

namespace TargetPreview.Targets
{
    public struct TargetPosition
    {
        public Quaternion rotation;
        public Vector3 position;
        public TargetPosition(Quaternion rotation, Vector3 position)
        {
            this.rotation = rotation;
            this.position = position;
        }
    }
}