using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework.Constraints;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;

namespace NotReaper.UI.Components
{
    [ExecuteInEditMode]
    public class SkinCreator : MonoBehaviour
    {
        [SerializeField] private string skinName;

        [Space(10), SerializeField, ShowIf("@background == null")]
        private NRBackground background;

        [SerializeField, ShowIf("@button == null")]
        private NRButton button;

        [SerializeField, ShowIf("@buttonPrompt == null")]
        private NRButtonPrompt buttonPrompt;

        [SerializeField, ShowIf("@dropdown == null")]
        private NRDropdown dropdown;

        [SerializeField, ShowIf("@inputField == null")]
        private NRInputField inputField;

        [SerializeField, ShowIf("@iconInputField == null")]
        private NRIconInputField iconInputField;

        [SerializeField, ShowIf("@slider == null")]
        private NRSlider slider;

        [SerializeField, ShowIf("@title == null")]
        private NRTitle title;
        
        [SerializeField, ShowIf("@toggle == null")]
        private NRToggle toggle;

        [SerializeField, ShowIf("@window == null")]
        private NRWindow window;

        [SerializeField, ShowIf("@themeableObject1 == null")]
        private NRThemeableObject themeableObject1;

        [SerializeField, ShowIf("@themeableObject2 == null")]
        private NRThemeableObject themeableObject2;

        [SerializeField, ShowIf("@themeableObject3 == null")]
        private NRThemeableObject themeableObject3;

        [SerializeField, ShowIf("@themeableObject4 == null")]
        private NRThemeableObject themeableObject4;

        [SerializeField, ShowIf("@themeableObject5 == null")]
        private NRThemeableObject themeableObject5;

        [SerializeField, ShowIf("@themeableObject6 == null")]
        private NRThemeableObject themeableObject6;

        [SerializeField, ShowIf("@themeableObject7 == null")]
        private NRThemeableObject themeableObject7;

        [SerializeField, ShowIf("@themeableObject8 == null")]
        private NRThemeableObject themeableObject8;

        [SerializeField, ShowIf("@themeableObject9 == null")]
        private NRThemeableObject themeableObject9;

        [SerializeField, BoxGroup("Skin Data"), ShowIf("@themeData != null")]
        private ThemeData.SkinData<NRSkinBase> skinBase;

        [SerializeField, BoxGroup("Skin Data"), ShowIf("@themeData != null")]
        private ThemeData.SkinData<NRBackgroundSkin> backgroundSkin;

        [SerializeField, BoxGroup("Skin Data"), ShowIf("@themeData != null")]
        private ThemeData.SkinData<NRButtonSkin> buttonSkin;

        [SerializeField, BoxGroup("Skin Data"), ShowIf("@themeData != null")]
        private ThemeData.SkinData<NRButtonPromptSkin> buttonPromptSkin;

        [SerializeField, BoxGroup("Skin Data"), ShowIf("@themeData != null")]
        private ThemeData.SkinData<NRDropdownSkin> dropdownSkin;

        [SerializeField, BoxGroup("Skin Data"), ShowIf("@themeData != null")]
        private ThemeData.SkinData<NRInputFieldSkin> inputFieldSkin;

        [SerializeField, BoxGroup("Skin Data"), ShowIf("@themeData != null")]
        private ThemeData.SkinData<NRIconInputFieldSkin> iconInputFieldSkin;

        [SerializeField, BoxGroup("Skin Data"), ShowIf("@themeData != null")]
        private ThemeData.SkinData<NRSliderSkin> sliderSkin;

        [SerializeField, BoxGroup("Skin Data"), ShowIf("@themeData != null")]
        private ThemeData.SkinData<NRTitleSkin> titleSkin;

        [SerializeField, BoxGroup("Skin Data"), ShowIf("@themeData != null")]
        private ThemeData.SkinData<NRToggleSkin> toggleSkin;

        [SerializeField, BoxGroup("Skin Data"), ShowIf("@themeData != null")]
        private ThemeData.SkinData<NRWindowSkin> windowSkin;

        private const string BaseSkinLocationPath = "Scripts/UI/NRUI/";

        private ThemeData themeData;

        private List<SkinMap> skinMap = new();

        private bool skinExists;

        private bool isEditingExistingSkin;

        private List<NRThemeable> themeables = new();
        private List<INRThemeable> iThemeables = new();

        private bool themeablesPopulated => themeables.Count > 0;

        private void OnValidate()
            => skinExists = !string.IsNullOrEmpty(skinName)
                            && Directory.Exists(Path.Combine(Application.dataPath, BaseSkinLocationPath, "Theme/Themes", skinName));
        
        private void Start()
        {
            PopulateThemeables();
        }

        [Button(ButtonSizes.Medium, Name = "Refresh References")]
        public void PopulateThemeables()
        {
            themeables.Clear();
            iThemeables.Clear();
            
            themeables.Add(background);
            themeables.Add(button);
            themeables.Add(buttonPrompt);
            themeables.Add(dropdown);
            themeables.Add(inputField);
            themeables.Add(iconInputField);
            themeables.Add(slider);
            themeables.Add(title);
            themeables.Add(toggle);
            themeables.Add(window);
            
            iThemeables.Add(themeableObject1);
            iThemeables.Add(themeableObject2);
            iThemeables.Add(themeableObject3);
            iThemeables.Add(themeableObject4);
            iThemeables.Add(themeableObject5);
            iThemeables.Add(themeableObject6);
            iThemeables.Add(themeableObject7);
            iThemeables.Add(themeableObject8);
            iThemeables.Add(themeableObject9);
        }


        [PropertySpace, GUIColor(1f, 1f, 0), Button(ButtonSizes.Large), ShowIf("@skinExists")]
        public void LoadSkinSet()
        {
            skinMap.Clear();
            var themePath = Path.Combine("Assets", BaseSkinLocationPath, "Theme/Themes", skinName, skinName + ".asset");
            themeData = AssetDatabase.LoadAssetAtPath<ThemeData>(Path.Combine(themePath));

            LoadSkin("Background", themeData.background);
            LoadSkin("Button", themeData.button);
            LoadSkin("ButtonPrompt", themeData.buttonPrompt);
            LoadSkin("Dropdown", themeData.dropdown);
            LoadSkin("InputField", themeData.inputField);
            LoadSkin("IconInputField", themeData.iconinputField);
            LoadSkin("Slider", themeData.slider);
            LoadSkin("Title", themeData.title);
            LoadSkin("Toggle", themeData.toggle);
            LoadSkin("Window", themeData.window);
            
            TransferSkinToInspector();
            isEditingExistingSkin = true;
        }

        [PropertySpace, GUIColor(1f, 1f, 0), Button(ButtonSizes.Large), ShowIf("@!skinExists")]
        public void InitializeNewSkinSet()
        {
            if (string.IsNullOrEmpty(skinName))
            {
                Debug.LogError("Please enter a skin name.");
                return;
            }

            if (Directory.Exists(Path.Combine(Application.dataPath, GetPath("Background"))))
            {
                Debug.LogError("Skin already exists - please enter a different name.");
                return;
            }

            skinMap.Clear();
            themeData = ScriptableObject.CreateInstance<ThemeData>();

            var baseSkinLight = ScriptableObject.CreateInstance<NRSkinBase>();
            var baseSkinDark = ScriptableObject.CreateInstance<NRSkinBase>();
            skinMap.Add(new("Base", "", baseSkinLight, baseSkinDark));
            themeData.skinBase = new ThemeData.SkinData<NRSkinBase>() { light = baseSkinLight, dark = baseSkinDark };
            themeData.skinName = skinName;
            themeData.background = AddSkin<NRBackgroundSkin>("Background");
            themeData.button = AddSkin<NRButtonSkin>("Button");
            themeData.buttonPrompt = AddSkin<NRButtonPromptSkin>("ButtonPrompt");
            themeData.dropdown = AddSkin<NRDropdownSkin>("Dropdown");
            themeData.inputField = AddSkin<NRInputFieldSkin>("InputField");
            themeData.iconinputField = AddSkin<NRIconInputFieldSkin>("IconInputField");
            themeData.slider = AddSkin<NRSliderSkin>("Slider");
            themeData.title = AddSkin<NRTitleSkin>("Title");
            themeData.toggle = AddSkin<NRToggleSkin>("Toggle");
            themeData.window = AddSkin<NRWindowSkin>("Window");

            TransferSkinToInspector();
            isEditingExistingSkin = false;
        }

        private void TransferSkinToInspector()
        {
            skinBase = themeData.skinBase;
            backgroundSkin = themeData.background;
            buttonSkin = themeData.button;
            buttonPromptSkin = themeData.buttonPrompt;
            dropdownSkin = themeData.dropdown;
            inputFieldSkin = themeData.inputField;
            iconInputFieldSkin = themeData.iconinputField;
            sliderSkin = themeData.slider;
            titleSkin = themeData.title;
            toggleSkin = themeData.toggle;
            windowSkin = themeData.window;
        }

        [ButtonGroup("Preview"), Button(ButtonSizes.Medium), ShowIf("@themeData != null")]
        public void PreviewLightSkin()
        {
            if(!themeablesPopulated)
                PopulateThemeables();

            foreach (var themeable in themeables)
            {
                themeable.ApplyLightTheme(themeData);
                themeable.UpdateVisuals();
            }
            
            foreach (var themeable in iThemeables)
            {
                themeable.ApplyLightTheme(themeData);
                themeable.UpdateVisuals();
            }
        }

        [ButtonGroup("Preview"), Button(ButtonSizes.Medium), ShowIf("@themeData != null")]
        public void PreviewDarkSkin()
        {
            if(!themeablesPopulated)
                PopulateThemeables();
            
            foreach (var themeable in themeables)
            {
                themeable.ApplyDarkTheme(themeData);
                themeable.UpdateVisuals();
            }
            foreach (var themeable in iThemeables)
            {
                themeable.ApplyDarkTheme(themeData);
                themeable.UpdateVisuals();
            }
        }

        [PropertySpace, GUIColor(0f, 1f, 0), Button(ButtonSizes.Large), ShowIf("@themeData != null && !isEditingExistingSkin")]
        public void SaveSkin()
        {
            if (isEditingExistingSkin)
            {
                Debug.LogError("Can't save existing skin. If you see this error, something is seriously wrong in this code here.");
                return;
            }

            SaveAsset("Background");
            SaveAsset("Button");
            SaveAsset("ButtonPrompt");
            SaveAsset("Dropdown");
            SaveAsset("IconInputField");
            SaveAsset("InputField");
            SaveAsset("Slider");
            SaveAsset("Title");
            SaveAsset("Toggle");
            SaveAsset("Window");
            
            var path = Path.Combine(Application.dataPath, BaseSkinLocationPath, "Theme/Themes", themeData.skinName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var baseSkin = GetSkinMap("Base");
            AssetDatabase.CreateAsset(baseSkin.lightSkin, path + "Light.asset");
            AssetDatabase.CreateAsset(baseSkin.darkSkin, path + "Dark.asset");
            AssetDatabase.CreateAsset(themeData, path + ".asset");
            
            themeData = null;
        }

        private void SaveAsset(string mapID)
        {
            var map = GetSkinMap(mapID);
            var path = Path.Combine(Application.dataPath, map.path);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            AssetDatabase.CreateAsset(map.lightSkin, Path.Combine(path, themeData.skinName + "Light.asset"));
            AssetDatabase.CreateAsset(map.darkSkin, Path.Combine(path, themeData.skinName + "Dark.asset"));
        }

        private ThemeData.SkinData<T> AddSkin<T>(string baseName) where T : ScriptableObject
        {
            var light = ScriptableObject.CreateInstance<T>();
            var dark = ScriptableObject.CreateInstance<T>();
            skinMap.Add(new(baseName, GetPath(baseName), light, dark));
            var skin = new ThemeData.SkinData<T>()
            {
                light = light,
                dark = dark
            };
            return skin;
        }

        private void LoadSkin<T>(string baseName, ThemeData.SkinData<T> skin) where T : ScriptableObject
            => skinMap.Add(new(baseName, GetPath(baseName), skin.light, skin.dark));

        private SkinMap GetSkinMap(string id)
            => skinMap.FirstOrDefault(map => map.id == id);

        [Serializable]
        private struct SkinMap
        {
            public string id;
            public string path;
            public ScriptableObject lightSkin;
            public ScriptableObject darkSkin;

            public SkinMap(string id, string path, ScriptableObject lightSkin, ScriptableObject darkSkin)
            {
                this.id = id;
                this.path = path;
                this.lightSkin = lightSkin;
                this.darkSkin = darkSkin;
            }
        }

        private string GetPath(string baseName)
            => BaseSkinLocationPath + baseName + "/Skin/" + skinName;
    }
}