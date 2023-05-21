using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TargetPreview.ScriptableObjects
{
    [CreateAssetMenu(menuName = "TargetPreview/Telegraph Preset")]
    public class TelegraphPreset : ScriptableObject
    {
        [Header("Cloud")]
        public Texture2D cloudTexture;
        public float cloudSize = 1.15f;
        public float twirlAmount = -6.9f;
        public float spinSpeed = -30f;
        public float spherizeAmount = 2.2f;

        [Header("Mask")]
        public Texture2D maskTexture;
        public float maskSize = 1.19f;
        public int textureArrayIndex = 0;
        

        public MaterialPropertyBlock GetMaterialPropertyBlock()
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            //block.SetTexture("_MainTex", cloudTexture);
            block.SetFloat("_CloudSize", cloudSize);
            block.SetFloat("_TwirlAmount", twirlAmount);
            block.SetFloat("_SpinSpeed", spinSpeed);
            block.SetFloat("_SpherizeAmount", spherizeAmount);
            //block.SetTexture("_MaskTex", maskTexture);
            block.SetFloat("_TextureIndex", textureArrayIndex);
            block.SetFloat("_MaskSize", maskSize);
            return block;
        }
    }

}