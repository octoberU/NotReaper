using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NotReaper.UI.Components;
using UnityEngine;

namespace NotReaper
{
    public class UISettings : MonoBehaviour
    {
        [SerializeField] private NRInputSliderCombo historySlider;
        [SerializeField] private NRDropdown _resolutionDropdown;
        
        [NRInject] private SavingPrompt savingPrompt;
        private bool isQuitting = false;
        private bool wasFullscreen;

        private readonly List<Vector2Int> _validResolutions = new()
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1366, 768),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3200, 1800),
            new Vector2Int(3840, 2160),
            new Vector2Int(5120, 2880),
            new Vector2Int(7680, 4320),
        };

        private List<Resolution> _resolutions = new();

        private void Start()
        {
            _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            
            NRSettings.OnLoad(() =>
            {
                historySlider.value = NRSettings.config.historySize;
                
#if UNITY_EDITOR
                PopulateResolutions(false);
                return;
#endif
                
                if (NRSettings.config.resolutionIndex < 0)
                {
                    PopulateResolutions(true);
                }
                else
                {
                    PopulateResolutions(false);
                    _resolutionDropdown.value = NRSettings.config.resolutionIndex;
                }
            });
            
            historySlider.OnValueChanged.AddListener(value =>
            {
                NRSettings.config.historySize = (int)value;
                NRSettings.SaveSettingsJson();
            });
        }


        private void Update()
        {
            //unity has no event to check if fullscreen changed, so you have to do it like this -_-
            if (Screen.fullScreen != wasFullscreen)
            {
                wasFullscreen = Screen.fullScreen;
                if (Screen.fullScreen)
                {
                    var index = NRSettings.config.resolutionIndex;
                    if (index < 0)
                    {
                        index = 0;
                    }
                    
                    var resolution = _resolutions[index];
                    ApplyResolution(resolution);
                }
            }
        }

        private void PopulateResolutions(bool applyHighest)
        {
            var displayResolutions = Screen.resolutions;

            var checkResolution = new Vector2Int();
            int highestResolutionIndex = 0;
            int index = 0;
            foreach (var resolution in displayResolutions)
            {
                checkResolution.x = resolution.width;
                checkResolution.y = resolution.height;

                if (_validResolutions.Contains(checkResolution))
                {
                    _resolutionDropdown.AddItem(resolution.ToString());
                    _resolutions.Add(resolution);
                    highestResolutionIndex = index;
                    index++;
                }
            }
            
            _resolutionDropdown.RepopulateDropdownList();
            _resolutionDropdown.SelectItem(0, false);

            if (applyHighest)
            {
                _resolutionDropdown.value = highestResolutionIndex;
            }
        }

        private void OnResolutionChanged(int index)
        {
#if UNITY_EDITOR
            return;
#endif
            ApplyResolution(_resolutions[index]);
            NRSettings.config.resolutionIndex = index;
            NRSettings.SaveSettingsJson();
        }

        private void ApplyResolution(Resolution resolution)
        {
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.FullScreenWindow, resolution.refreshRate);
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
