using System;
using System.Diagnostics;
using System.IO;
using NotReaper.UI.Components;
using UnityEngine;

namespace NotReaper
{
    public class UISettings : MonoBehaviour
    {
        [SerializeField] private NRInputSliderCombo historySlider;
        
        [NRInject] private SavingPrompt savingPrompt;
        private bool isQuitting = false;


        private void Start()
        {
            NRSettings.OnLoad(() =>
            {
                historySlider.value = NRSettings.config.historySize;
            });
            
            historySlider.OnValueChanged.AddListener(value =>
            {
                NRSettings.config.historySize = (int)value;
                NRSettings.SaveSettingsJson();
            });
        }

        public void Exit()
        {
            if (isQuitting)
                return;

            isQuitting = true;
            
            
            
            savingPrompt.ShowPrompt(response =>
            {
                switch (response)
                {
                    case SavingPrompt.Response.Cancel:
                        isQuitting = false;
                        return;
                    case SavingPrompt.Response.Accept:
                        NRSettings.SaveSettingsJson();
                        EditorIO.SaveMap(Application.Quit);
                        break;
                    case SavingPrompt.Response.Decline:
                        Application.Quit();
                        break;
                }
            });
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
}
