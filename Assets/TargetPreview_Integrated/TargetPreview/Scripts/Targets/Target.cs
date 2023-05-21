using System;
using UnityEngine;
using TargetPreview.ScriptableObjects;
using TargetPreview.Math;
using System.Runtime.CompilerServices;
using TargetPreview.Display;

namespace TargetPreview.Targets
{
    public abstract class Target : MonoBehaviour
    {
        protected Color currentHandColor;
        protected TargetData targetData;
        [HideInInspector] public TargetPool creator;

        /// <summary>
        /// Contains all target data which influences the target's appearance.
        /// </summary>
        /// <remarks>Setting this to a new value will update all the visuals.</remarks>
        public TargetData TargetData
        {
            get => targetData;
            set
            {
                targetData = value;
                UpdateVisuals(value);
            }
        }

        public abstract void TimeUpdate(float time);

        public abstract float TargetFlyInTime { get; }
        public abstract float ModifiedFlyInTime { get; }

        public virtual bool ShouldRender
        {
            get
            {
                var temporalDistance = TemporalDistance;
                return temporalDistance > 0.01f && temporalDistance < 1;
            }
        }

        public float TemporalDistance
        {
            get
            {
                float timeDifference = TargetData.time - TargetManager.Time;
                return (Mathf.Clamp(timeDifference, 0f, ModifiedFlyInTime) / ModifiedFlyInTime); 
            }
        }

        public abstract void UpdateVisuals(TargetData newData);
        
        public virtual void OnReturnedToPool()
        {
            // Do nothing
        }

        public abstract Transform GetPhysicalTargetTransform();

    }
}