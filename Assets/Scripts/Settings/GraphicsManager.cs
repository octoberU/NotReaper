using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper
{
    public class GraphicsManager : MonoBehaviour
    {
        private void Start()
        {
            NRSettings.OnLoad(UpdateSettings);
            NRSettings.onSettingsSaved += UpdateSettings;
        }

        private void UpdateSettings() => UpdateSettings(NRSettings.config);
        
        private void UpdateSettings(NRJsonSettings settings)
        {
            QualitySettings.vSyncCount = settings.vsync ? 1 : 0;
        }
    }
}