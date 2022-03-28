using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NotReaper.MapPreview
{
    public class ModifierPreview : MonoBehaviour
    {
        [SerializeField] private TextMeshPro popupPrefab;
        [SerializeField] private Volume postProcessing;
        [SerializeField] private Transform cam;
        private Dictionary<int, TextMeshPro> popups = new();
        private ColorAdjustments hueShift;

        private Material skybox;
        internal Material SkyboxMaterial
        {
            get => skybox;
            set
            {
                Reset();
                skybox = value;
                SetDefaultValues(value);
            }
        }

        private Color originalTint = new Color(.5f, .5f, .5f, 0f);
        private float originalExposure = 1f;
        private float originalRotation;
        private float lastPsyIncrement = 0f;
        private void SetDefaultValues(Material material)
        {
            originalRotation = material.GetFloat("_Rotation");
            postProcessing.profile.TryGet(out hueShift);
        }

        #region Modifier Preview
        internal void CreatePopup(int textIndex, string txt, Vector3 pos, float size)
        {
            var popup = Instantiate(popupPrefab);
            popup.text = txt;
            pos.y += 1.5f;
            pos.z += 5f;
            popup.transform.position = pos;
            popup.fontSize = size;
            popup.transform.LookAt(cam);
            popups.Add(textIndex, popup);
        }

        internal void RemovePopup(int index)
        {
            if (popups.ContainsKey(index))
            {
                Destroy(popups[index].gameObject);
                popups.Remove(index);
            }
        }

        internal void RemoveAllPopups()
        {
            foreach(var entry in popups)
            {
                Destroy(entry.Value.gameObject);
            }
            popups.Clear();
        }

        internal void SetRotation(float amount)
        {
            if (skybox == null) return;
            skybox.SetFloat("_Rotation", amount);
        }
        internal void SetContinuousRotationAmount(float amount)
        {
            if (skybox == null) return;
            amount += skybox.GetFloat("_Rotation");
            SetRotation(amount);
        }

        internal void ResetRotation()
        {
            if (skybox == null) return;
            skybox.SetFloat("_Rotation", originalRotation);
        }

        internal void SetBrightness(float amount)
        {
            if (skybox == null) return;
            skybox.SetFloat("_Exposure", amount);
        }
        internal void ResetBrightness()
        {
            if (skybox == null) return;
            skybox.SetFloat("_Exposure", originalExposure);
        }

        internal void SetSkyboxTint(Color color)
        {
            if (skybox == null) return;
            skybox.SetColor("_Tint", color);
        }

        internal void CyclePsychedelia(float increment)
        {
            var target = (hueShift.hueShift.value + increment) % 180;
            hueShift.hueShift.value = target;
            lastPsyIncrement = increment;
        }

        internal void ResetSkyboxTint()
        {
            if (skybox == null) return;
            skybox.SetColor("_Tint", originalTint);
        }

        internal void StopPsychedelia()
        {
            StartCoroutine(ResetPsy());
        }

        private IEnumerator ResetPsy()
        {
            float start = hueShift.hueShift.value;
            if(lastPsyIncrement == 0f)
            {
                hueShift.hueShift.value = 0f;
                yield break;
            }
            float prevValue = hueShift.hueShift.value;
            while(hueShift.hueShift.value != 0)
            {
                var target = (hueShift.hueShift.value + lastPsyIncrement) % 180;
                hueShift.hueShift.value = target;
                if(prevValue < 0f && hueShift.hueShift.value > 0f)
                {
                    hueShift.hueShift.value = 0f;
                    yield break;
                }
                prevValue = hueShift.hueShift.value;
                yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            }
        }

        public void Reset()
        {
            if (skybox == null) return;
            ResetBrightness();
            hueShift.hueShift.value = 0f;
            ResetRotation();
            ResetSkyboxTint();
        }

        private void OnApplicationQuit()
        {
            Reset();
        }
        #endregion
    }
}
