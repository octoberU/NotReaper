using TargetPreview.Targets;
using UnityEngine;

namespace TargetPreview.ScriptableObjects
{
    /// <summary>
    /// A flyweight pattern for storing all the memory demanding assets.
    /// </summary>
    public class AssetContainer : SingletonScriptableObject<AssetContainer>
    {
        [Header("Audio")]
        public DrumPack drumpack;

        [Header("Mesh data")]
        public Mesh standardMesh;
        public Mesh sustainMesh;
        public Mesh angleMesh;
        public Mesh chainStartMesh;
        public Mesh chainMesh;
        public Mesh meleeMesh;
        public Mesh dodgeMesh;

        [Header("Texture data")]
        public Texture2D standardTexture;
        public Texture2D sustainTexture;
        public Texture2D angleTexture;
        public Texture2D chainStartTexture;
        public Texture2D chainTexture;
        public Texture2D meleeTexture;
        public Texture2D dodgeTexture;

        [Header("Telegraph presets")]
        public TelegraphPreset standardTelegraph;
        public TelegraphPreset sustainTelegraph;
        public TelegraphPreset angleTelegraph;

        [Header("Approach rings")]
        public Mesh circleRing;
        public Mesh squareRing;
        public Mesh angleRing;

        
        [HideInInspector] public Texture2DArray targetTextures;
        [HideInInspector] public Texture2DArray telegraphTextures;

        public static Mesh GetMeshForBehavior(TargetBehavior behavior) =>
            behavior switch
            {
                TargetBehavior.Standard => Instance.standardMesh,
                TargetBehavior.Hold => Instance.sustainMesh,
                TargetBehavior.Horizontal => Instance.angleMesh,
                TargetBehavior.Vertical => Instance.angleMesh,
                TargetBehavior.Chain => Instance.chainMesh,
                TargetBehavior.ChainStart => Instance.chainStartMesh,
                TargetBehavior.Melee => Instance.meleeMesh,
                TargetBehavior.Dodge => Instance.dodgeMesh,
                _ => default,
            };

        public static Texture2D GetTextureForBehavior(TargetBehavior behavior) =>
            behavior switch
            {
                TargetBehavior.Standard => Instance.standardTexture,
                TargetBehavior.Hold => Instance.sustainTexture,
                TargetBehavior.Horizontal => Instance.angleTexture,
                TargetBehavior.Vertical => Instance.angleTexture,
                TargetBehavior.Chain => Instance.chainTexture,
                TargetBehavior.ChainStart => Instance.chainStartTexture,
                TargetBehavior.Melee => Instance.meleeTexture,
                TargetBehavior.Dodge => Instance.dodgeTexture,
                _ => default,
            };

        public static TelegraphPreset GetTelegraphForBehavior(TargetBehavior behavior) =>
        behavior switch
        {
            TargetBehavior.Standard => Instance.standardTelegraph,
            TargetBehavior.Hold => Instance.sustainTelegraph,
            TargetBehavior.Horizontal => Instance.angleTelegraph,
            TargetBehavior.Vertical => Instance.angleTelegraph,
            TargetBehavior.ChainStart => Instance.sustainTelegraph,
            _ => null,
        };

        public static Mesh GetApproachRingForBehavior(TargetBehavior behavior) =>
        behavior switch
        {
            TargetBehavior.Standard => Instance.circleRing,
            TargetBehavior.Hold => Instance.squareRing,
            TargetBehavior.Horizontal => Instance.angleRing,
            TargetBehavior.Vertical => Instance.angleRing,
            TargetBehavior.ChainStart => Instance.squareRing,
            _ => null,
        };
        
        
        public MaterialPropertyBlock GetPropertyBlockPhysicalTarget(TargetBehavior behavior, Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            var textureForBehavior = GetTextureForBehavior(behavior);
            block.SetFloat("_TextureIndex", (float)behavior);
                
            return block;
        }

        public void FillTextureArrays()
        {
            var tempTargetTextures = new Texture2DArray(512, 512, 7, TextureFormat.RGBA32, false);
            
            tempTargetTextures.SetPixels(Instance.standardTexture.GetPixels(0), 0);
            tempTargetTextures.SetPixels(Instance.sustainTexture.GetPixels(0), 3);
            tempTargetTextures.SetPixels(Instance.angleTexture.GetPixels(0), 1);            
            tempTargetTextures.SetPixels(Instance.angleTexture.GetPixels(0), 2);
            tempTargetTextures.SetPixels(Instance.chainTexture.GetPixels(0), 5);
            tempTargetTextures.SetPixels(Instance.chainStartTexture.GetPixels(0), 4);
            tempTargetTextures.Apply();
            targetTextures = tempTargetTextures;
            VisualConfig.Instance.standardTargetMaterial.SetTexture("_TargetTextures", targetTextures);
            
            
            var tempTelegraphTextures = new Texture2DArray(256, 256, 4, TextureFormat.RGBA32, false);
            tempTelegraphTextures.SetPixels(Instance.standardTelegraph.maskTexture.GetPixels(0), 0);
            tempTelegraphTextures.SetPixels(Instance.sustainTelegraph.maskTexture.GetPixels(0), 1);
            tempTelegraphTextures.SetPixels(Instance.angleTelegraph.maskTexture.GetPixels(0), 2);
            tempTelegraphTextures.Apply();
            telegraphTextures = tempTelegraphTextures;
            
            VisualConfig.Instance.telegraphMaterial.SetTexture("_TargetTextures", telegraphTextures);
            VisualConfig.Instance.telegraphMaterial.mainTexture = standardTelegraph.cloudTexture;
    

            Instance.telegraphTextures = new Texture2DArray(512, 512, 3, TextureFormat.RGBA32, false);
        }

    }

}