using TargetPreview.Scripts.Targets;
using UnityEngine;

namespace TargetPreview.Targets
{
    [System.Serializable]
    public struct TargetData
    {
        public float time;
        public TargetBehavior behavior;
        public TargetHandType handType;
        public TargetPosition transformData;
        public TargetCue cue;

        public TargetData(TargetBehavior behavior, TargetHandType color, float time, TargetPosition transformData, TargetCue cue)
        {
            this.behavior = behavior;
            this.handType = color;
            this.time = time;
            this.transformData = transformData;
            this.cue = cue;
        }
        /// <summary>
        /// A constructor for creating placeholder targetData.
        /// </summary>
        /// <param name="placeHolder"></param>
        public TargetData(bool placeHolder)
        {
            this.behavior = TargetBehavior.Standard;
            this.handType = TargetHandType.Left;
            this.time = 500;
            this.transformData = new TargetPosition(new Quaternion(), new Vector3());
            cue = new TargetCue() { children = { } };
        }
    }
}