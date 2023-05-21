using System;
using System.Runtime.CompilerServices;
using TargetPreview.Math;
using TargetPreview.ScriptableObjects;
using UnityEngine;
using UnityEngine.Serialization;

namespace TargetPreview.Targets
{
    public class MeleeTarget : Target
    {
        [Space, Header("Melee")]
        
        /// <summary><see cref="targetFlyInDistance"/></summary>
        public float meleeFlyInDistance = 8f;

        /// <summary> Melee animation length in ms </summary>
        public override float TargetFlyInTime => 1000f;

        public override float ModifiedFlyInTime => TargetFlyInTime / VisualConfig.meleeSpeedMultiplierStatic;

        /// <summary> The influence of <see cref="GridTarget.targetFlyInDistance"/> on horizontal position in the fly-in animation for melees. </summary>
        public float horizontalInfluence = .4f;

        /// <summary> Positional offset for melees. </summary>
        public float horizontalMeleeSpawnOffset = 3f;

        /// <summary> Speed at which melees rotate. </summary>
        public float meleeSpinSpeed = 500f;

        /// <summary>
        /// The influence of <see cref="GridTarget.targetFlyInDistance"/> on vertical position in the fly-in animation for melees.
        /// </summary>
        public float verticalMeleeInfluence = .4f;
        
        protected float flyInDistance => meleeFlyInDistance;
        
        public float approachRingMaxScale = 1.5f;
        
        

        int meleeDirection;

        [SerializeField] MeshRenderer meleeRenderer;
        [SerializeField] protected Transform approachRing;
        [FormerlySerializedAs("meleeSphereTransform")] [SerializeField] protected Transform physicalSphereTransform;
        [SerializeField] TrailRenderer trailRenderer;
        

        public override void UpdateVisuals(TargetData newData)
        {
            meleeDirection = targetData.transformData.position.x > 0 ? 1 : -1;
            currentHandColor = VisualConfig.GetColorForHandType(targetData.handType);
            transform.position = newData.transformData.position;
            transform.rotation = newData.transformData.rotation;
            approachRing.GetComponent<MeshRenderer>().SetPropertyBlock(AssetContainer.Instance.GetPropertyBlockPhysicalTarget(newData.behavior, currentHandColor));//optimize
        }

        public override Transform GetPhysicalTargetTransform() =>
            physicalSphereTransform;

        public override void TimeUpdate(float time)
        {

            bool shouldRender = ShouldRender;
            physicalSphereTransform.localRotation = Quaternion.Euler(
                Vector3.up * (meleeSpinSpeed * time * meleeDirection) +
                Vector3.right * 90 * VisualConfig.Instance
                    .meleeRotationSpeed);

            trailRenderer.startColor = currentHandColor;
            
            var temporalDistance = TemporalDistance;
            AnimateFlyIn(temporalDistance);
            approachRing.localScale = temporalDistance > 0.99f ? Vector3.zero : Vector3.Lerp( Vector3.zero, Vector3.one * approachRingMaxScale, temporalDistance);
            approachRing.position = physicalSphereTransform.position;
            
            
            meleeRenderer.gameObject.SetActive(shouldRender);
            trailRenderer.enabled = shouldRender;
        }


        /// <summary>
        /// Animates the target along an arc.
        /// </summary>
        /// <param name="distance">A 0-1 lerp used to drive the animation time.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AnimateFlyIn(float distance)
        {
            physicalSphereTransform.localPosition = TargetTransform.MeleeParabola(Vector3.zero, GetFlyInPosition(),
                flyInDistance * verticalMeleeInfluence, flyInDistance * horizontalInfluence, distance,
                meleeDirection);
        }

        public Vector3 GetFlyInPosition()
            => new Vector3(transform.position.x + -(horizontalMeleeSpawnOffset * meleeDirection),
                transform.position.y, meleeFlyInDistance);


#if UNITY_EDITOR
        void OnDrawGizmosSelected() //Draw curve helper.
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            const int curveSamples = 20;
            Vector3 lastPos = TargetTransform.MeleeParabola(Vector3.zero, GetFlyInPosition(),
                flyInDistance * verticalMeleeInfluence, flyInDistance * horizontalInfluence, 0,
                meleeDirection);
            
            
            for (int i = 0; i < curveSamples; i++)
            {
                var pos =
                    TargetTransform.MeleeParabola(Vector3.zero, GetFlyInPosition(),
                        flyInDistance * verticalMeleeInfluence, flyInDistance * horizontalInfluence, (float)i / (float)curveSamples,
                        meleeDirection);
                
                Gizmos.DrawLine(lastPos, pos);
                lastPos = pos;
            }
        }
#endif
    }
}