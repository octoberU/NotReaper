using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using TargetPreview.ScriptableObjects;
using UnityEngine;

namespace NotReaper.UI
{
    public class PreviewerPanel : MonoBehaviour
    {
        [SerializeField] private NRInputField targetSpeedInput;
        [SerializeField] private NRInputField meleeSpeedInput;
        [SerializeField] private VisualConfig config;
        [SerializeField] private int minMultiplier = 50;
        [SerializeField] private int maxMultiplier = 250;


        private void Start()
        {
            NRSettings.OnLoad(() =>
            {
                float targetSpeed = NRSettings.config.previewTargetSpeedMultiplier;
                float meleeSpeed = NRSettings.config.previewMeleeSpeedMultiplier;
                targetSpeed = Mathf.Clamp(targetSpeed, minMultiplier * .01f, maxMultiplier * .01f);
                meleeSpeed = Mathf.Clamp(meleeSpeed, minMultiplier * .01f, maxMultiplier * .01f);

                config.targetSpeedMultiplier = targetSpeed;
                config.meleeSpeedMultiplier = meleeSpeed;

                targetSpeedInput.text = (targetSpeed * 100f).ToString();
                meleeSpeedInput.text = (meleeSpeed * 100f).ToString();
            });
        }

        public void OnTargetSpeedInputChanged()
        {
            if(int.TryParse(targetSpeedInput.text, out int targetSpeed))
            {
                targetSpeed = Mathf.Clamp(targetSpeed, minMultiplier, maxMultiplier);
                float percentage = targetSpeed * .01f;
                NRSettings.config.previewTargetSpeedMultiplier = percentage;
                config.targetSpeedMultiplier = percentage;
                targetSpeedInput.text = targetSpeed.ToString();
                NRSettings.SaveSettingsJson();
            }
            else
            {
                targetSpeedInput.text = (NRSettings.config.previewTargetSpeedMultiplier * 100f).ToString();
            }
        }

        public void OnMeleeSpeedInputChanged()
        {
            if(int.TryParse(meleeSpeedInput.text, out int meleeSpeed))
            {
                meleeSpeed = Mathf.Clamp(meleeSpeed, minMultiplier, maxMultiplier);
                float percentage = meleeSpeed * .01f;
                NRSettings.config.previewMeleeSpeedMultiplier = percentage;
                config.meleeSpeedMultiplier = percentage;
                meleeSpeedInput.text = meleeSpeed.ToString();
                NRSettings.SaveSettingsJson();
            }
            else
            {
                meleeSpeedInput.text = (NRSettings.config.previewMeleeSpeedMultiplier * 100f).ToString();
            }
        }
    }
}
