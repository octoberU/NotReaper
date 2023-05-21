using System;
using TargetPreview.Targets;
using UnityEngine;

namespace TargetPreview.ScriptableObjects
{
    public class VisualConfig : SingletonScriptableObject<VisualConfig>
    {
        [Header("Colors")]
        public Color 
            leftHandColor,
            rightHandColor,
            noHandColor = Color.grey;
        [Header("Rendering")]
        public float targetBloomAmount = 2.416924f;
        public Material 
            meleeTargetMaterial,
            standardTargetMaterial,
            telegraphMaterial;
        [Header("Settings")]
        public float 
            targetSpeedMultiplier = 1f,
            meleeSpeedMultiplier = 1f,
            meleeRotationSpeed = 0.1f;
        [Header("TargetCenters")] 
        public Sprite 
            squareCenter,
            circleCenter,
            rectangleCenter;

        public static float
            targetSpeedMultiplierStatic,
            meleeSpeedMultiplierStatic;

        [RuntimeInitializeOnLoadMethod]
        public void UpdateStaticValues()
        {
            targetSpeedMultiplierStatic = targetSpeedMultiplier;
            meleeSpeedMultiplierStatic = meleeSpeedMultiplier;
        }


        public static Color GetColorForHandType(TargetHandType handType) =>
            handType switch
            {
                TargetHandType.Left => Instance.leftHandColor,
                TargetHandType.Right => Instance.rightHandColor,
                _ => Instance.noHandColor,
            };

        public static Color GetTelegraphColorForHandType(TargetHandType handType)
        {
            float factor = Mathf.Pow(2, Instance.targetBloomAmount);
            Color handColor = GetColorForHandType(handType);
            return new Color(handColor.r * factor, handColor.g * factor, handColor.b * factor);
        }

        public static Sprite GetTargetCenter(TargetBehavior targetBehavior)
        {
            switch (targetBehavior)
            {
                case TargetBehavior.Standard:
                    return Instance.circleCenter;
                
                case TargetBehavior.Hold:
                case TargetBehavior.ChainStart:
                    return Instance.circleCenter;
                
                case TargetBehavior.Horizontal:
                case TargetBehavior.Vertical:
                    return Instance.rectangleCenter;
                
                default:
                    return null;
            }
        }

        
    }
}
