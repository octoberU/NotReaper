using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NotReaper;
using NotReaper.Grid;
using NotReaper.Managers;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools.ChainBuilder;
using NotReaper.UI;
using SFB;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using NotReaper.Modifier;
using NotReaper.Notifications;
public class UISettings : MonoBehaviour
{
    //public GameObject bg;
    //public GameObject window;

    public void Start()
    {
        // var t = transform;
        //var position = t.localPosition;
        //t.localPosition = new Vector3(0, position.y, position.z);
        // Deactivate();
    }
    private bool isQuitting = false;
    public void Exit()
    {
        if (isQuitting)
            return;

        isQuitting = true;
        StartCoroutine(WaitForExit());
        
    }

    private IEnumerator WaitForExit()
    {
        //Timeline.Instance.Export();
        EditorIO.SaveMap();
        while (EditorIO.IsSaving)
            yield return null;

        /*while (Timeline.isSaving)
            yield return null;*/

        Application.Quit();
    }

    public void Activate()
    {
        //bg.SetActive(true);
        //window.SetActive(true);
    }

    public void Deactivate()
    {
        //bg.SetActive(false);
        //window.SetActive(false);
    }

    public void OpenSettingsFile()
    {
        string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", Application.companyName, Application.productName, "NRConfig.txt");
        string Arguments = "";

        if ((Application.platform == RuntimePlatform.LinuxEditor) || (Application.platform == RuntimePlatform.LinuxPlayer))
            FilePath = Path.Combine("file://" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.config/unity3d/" + Application.companyName + "/" + Application.productName + "/NRConfig.txt");
        Arguments = "";

        if ((Application.platform == RuntimePlatform.OSXEditor) || (Application.platform == RuntimePlatform.OSXPlayer))
        {
            FilePath = "open";

            if (Application.platform == RuntimePlatform.OSXEditor)
                Arguments = Path.Combine(@"""" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Library/Application Support/" + Application.companyName + "/" + Application.productName + "/NRConfig.txt" + @"""");

            if (Application.platform == RuntimePlatform.OSXPlayer)
                Arguments = Path.Combine(@"""" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Library/Application Support/" + Application.identifier + "/NRConfig.txt" + @"""");
        }

        Process.Start(FilePath, Arguments);
    }


    public void OpenSettingsFolder()
    {
        string Arguments = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", Application.companyName, Application.productName);
        string FileName = "explorer.exe";

        if ((Application.platform == RuntimePlatform.LinuxEditor) || (Application.platform == RuntimePlatform.LinuxPlayer))
        {
            FileName = Path.Combine("file://" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.config/unity3d/" + Application.companyName + "/" + Application.productName);
            Arguments = "";
        }

        if ((Application.platform == RuntimePlatform.OSXEditor) || (Application.platform == RuntimePlatform.OSXPlayer))
        {
            FileName = "open";
            Arguments = Path.Combine(@"""" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Library/Application Support/" + Application.companyName + "/" + Application.productName + "/" + @"""");

            if (Environment.OSVersion.Version.Major >= 18)
                Arguments = Path.Combine(@"""" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Library/Application Support/" + Application.identifier + "/" + @"""");
        }

        Process.Start(FileName, Arguments);
        //EditorUtility.RevealInFinder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "CircuitCubed", "NotReaper", "NRConfig.txt"));
    }

    public void RegenConfig()
    {
        NRSettings.LoadSettingsJson(true);
    }

}
