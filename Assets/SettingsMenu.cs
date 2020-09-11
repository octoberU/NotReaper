﻿using NotReaper;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{

    [SerializeField] Toggle richPresence;
    [SerializeField] Toggle clearCacheOnStartup;
    [SerializeField] Toggle enableTraceLines;
    [SerializeField] Toggle enableDualines;
    [SerializeField] Toggle useAutoZOffsetWith360;
    [SerializeField] Toggle useBouncyAnimations;

    public void UpdateUI()
    {
        richPresence.isOn = NRSettings.config.useDiscordRichPresence;
        clearCacheOnStartup.isOn = NRSettings.config.clearCacheOnStartup;
        enableTraceLines.isOn = NRSettings.config.enableTraceLines;
        enableDualines.isOn = NRSettings.config.enableDualines;
        useAutoZOffsetWith360.isOn = NRSettings.config.useAutoZOffsetWith360;
        useBouncyAnimations.isOn = NRSettings.config.useBouncyAnimations;
    }

    public void ApplyValues()
    {
        NRSettings.config.useDiscordRichPresence = richPresence.isOn;
        NRSettings.config.clearCacheOnStartup = clearCacheOnStartup.isOn;
        NRSettings.config.enableTraceLines = enableTraceLines.isOn;
        NRSettings.config.enableDualines = enableDualines.isOn;
        NRSettings.config.useAutoZOffsetWith360 = useAutoZOffsetWith360.isOn;
        NRSettings.config.useBouncyAnimations = useBouncyAnimations.isOn;
    }
}
