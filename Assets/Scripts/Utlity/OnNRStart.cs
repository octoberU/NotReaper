using NotReaper;
using NotReaper.IO;
using NotReaper.UserInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class OnNRStart : MonoBehaviour
{
    private void Awake()
    {
        Physics.autoSyncTransforms = false;
        NRSettings.LoadSettingsJson();
    }
}
