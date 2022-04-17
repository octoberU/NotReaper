using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NotReaper.UI.Components
{
    [ExecuteAlways]
    public class ThemeManager : MonoBehaviour
    {

        private static List<NRThemeable> themeables = new();
        private static List<INRThemeable> iThemeables = new();
        [SerializeField] private List<ThemeData> themes = new();

        private static List<ThemeData> _themes = new();

        private static ThemeData selectedTheme;
        private static ThemeMode selectedMode = ThemeMode.Dark;
        private static List<TextMeshProUGUI> textObjects = new();

        public static ThemeMode SelectedMode => selectedMode;

        private static bool hasAppliedThemeOnStart = false;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                _themes = themes;
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var scene = SceneManager.GetSceneByBuildIndex(i);
                    //if (scene.name == "Main" || scene.name == "Notifications") continue;
                    foreach (var root in SceneManager.GetSceneByBuildIndex(i).GetRootGameObjects())
                    {
                        textObjects.AddRange(root.GetComponentsInChildren<TextMeshProUGUI>(true));
                    }
                }
                NRSettings.OnLoad(() =>
                {
                    selectedTheme = themes.First(t => t.skinName == NRSettings.config.selectedTheme);
                    selectedMode = (ThemeMode)NRSettings.config.themeMode;
                    ApplyTheme();
                    hasAppliedThemeOnStart = true;
                });
            }

        }

        public static List<ThemeData> GetThemes()
        {
            return _themes;
        }

        public static ThemeData GetSelectedTheme()
        {
            return selectedTheme;
        }

        public static void SelectTheme(ThemeData theme)
        {
            selectedTheme = theme;
            NRSettings.config.selectedTheme = selectedTheme.skinName;
            NRSettings.SaveSettingsJson();
            ApplyTheme();
        }

        public static void ApplyTheme()
        {
            if (selectedMode == ThemeMode.Light)
            {
                ApplyLightTheme();
            }
            else
            {
                ApplyDarkTheme();
            }
        }

        public static void SelectThemeMode(ThemeMode mode)
        {
            selectedMode = mode;
            NRSettings.config.themeMode = (int)selectedMode;
            NRSettings.SaveSettingsJson();
        }

        private static void ApplyLightTheme()
        {
            //we set every text to NRWindow's desired color. If any themeable object wants a different color for it's text, it will simply override it again afterwards.
            foreach (var text in textObjects)
            {
                text.color = selectedTheme.window.light.textColor;
            }
            foreach (var themeable in themeables)
            {
                themeable.ApplyLightTheme(selectedTheme);
                themeable.UpdateVisuals();
            }

            foreach (var themeable in iThemeables)
            {
                themeable.ApplyLightTheme(selectedTheme);
                themeable.UpdateVisuals();
            }
        }

        private static void ApplyDarkTheme()
        {
            //we set every text to NRWindow's desired color. If any themeable object wants a different color for it's text, it will simply override it again afterwards.
            foreach (var text in textObjects)
            {
                text.color = selectedTheme.window.dark.textColor;
            }
            foreach (var themeable in themeables)
            {
                themeable.ApplyDarkTheme(selectedTheme);
                themeable.UpdateVisuals();
            }
            foreach (var themeable in iThemeables)
            {
                themeable.ApplyDarkTheme(selectedTheme);
                themeable.UpdateVisuals();
            }
        }


        public static void RegisterThemeable(NRThemeable themeable)
        {
            if (!themeables.Contains(themeable))
            {
                themeables.Add(themeable);
            }

            if (hasAppliedThemeOnStart)
            {
                if (SelectedMode == ThemeMode.Light)
                    themeable.ApplyLightTheme(selectedTheme);
                else
                    themeable.ApplyDarkTheme(selectedTheme);

                themeable.UpdateVisuals();
            }
        }

        public static void UnregisterThemeable(NRThemeable themeable)
        {
            if (themeables.Contains(themeable))
            {
                themeables.Remove(themeable);
            }
        }

        public static void RegisterThemeable(INRThemeable themeable)
        {
            if (!iThemeables.Contains(themeable))
                iThemeables.Add(themeable);

            if (hasAppliedThemeOnStart)
            {
                if (SelectedMode == ThemeMode.Light)
                    themeable.ApplyLightTheme(selectedTheme);
                else
                    themeable.ApplyDarkTheme(selectedTheme);

                themeable.UpdateVisuals();
            }
        }
        public static void UnregisterThemeable(INRThemeable themeable)
        {
            if (iThemeables.Contains(themeable))
                iThemeables.Remove(themeable);
        }

    }
    public enum ThemeMode
    {
        Light,
        Dark
    }

}
