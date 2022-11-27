using NotReaper;
using NotReaper.IO;
using NotReaper.UserInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class OnNRStart : MonoBehaviour
{
    private static List<Action> prendingActions = new();
    private static bool hasStarted = false;
    public delegate void OnUnityStart();
    private static event OnUnityStart onUnityStart;
    private void Awake()
    {
        Physics.autoSyncTransforms = false;
        NRSettings.LoadSettingsJson();
        RuntimeHelpers.RunClassConstructor(typeof(EditorTargets).TypeHandle);
    }

    private void Start()
    {
        hasStarted = true;
        onUnityStart?.Invoke();
    }
    /// <summary>
    /// Called after Unity's Start function has been called.
    /// </summary>
    /// <param name="callback">The acttion you want to perform.</param>
    public static void OnStart(OnUnityStart callback)
    {
        if (hasStarted)
            callback?.Invoke();
        else
            onUnityStart += callback;
        
    }
}
