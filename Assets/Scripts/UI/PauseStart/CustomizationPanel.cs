using DG.Tweening;
using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace NotReaper.UI.Customization
{
    public class CustomizationPanel : MonoBehaviour
    {
        [Header("Skins")]
        [SerializeField] private SkinEntry skinPrefab;
        [SerializeField] private Transform contentParent;
        [Space, Header("Preview")]
        [SerializeField] private CanvasGroup previewWindowCanvas;
        [SerializeField] private NRTitle previewSkinTitle;
        [SerializeField] private NRToggle toggleLight;
        [SerializeField] private NRToggle toggleDark;
        [SerializeField] private List<Components.NRThemeable> previewElements = new();

        private ThemeMode mode = ThemeMode.Dark;

        private ThemeData selectedTheme;
        private CanvasGroup canvas;

        private void Awake()
        {
            canvas = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            NRSettings.OnLoad(() =>
            {
                mode = (ThemeMode)NRSettings.config.themeMode;
                if (mode == ThemeMode.Light) toggleLight.Select();
                else toggleDark.Select();
            });
            previewWindowCanvas.interactable = false;
            previewWindowCanvas.blocksRaycasts = false;
        }

        public void PreviewTheme(ThemeData data)
        {
            selectedTheme = data;
            previewSkinTitle.textContainer.text = data.skinName;
            foreach(var element in previewElements)
            {
                if(mode == ThemeMode.Light)
                {
                    element.ApplyLightTheme(data);
                }
                else
                {
                    element.ApplyDarkTheme(data);
                }
                element.UpdateVisuals();
            }
        }

        public void ApplySelectedTheme()
        {
            if(selectedTheme != null)
            {
                ThemeManager.SelectThemeMode(mode);
                ThemeManager.SelectTheme(selectedTheme);
            }
        }

        private void PopulateSkinMenu()
        {
            foreach(var theme in ThemeManager.GetThemes())
            {
                var entry = Instantiate(skinPrefab, contentParent);
                entry.panel = this;
                entry.skin = theme;
            }
        }

        private bool initialized = false;
        public void Show()
        {
            /*mode = (ThemeMode)NRSettings.config.themeMode;
            if (mode == ThemeMode.Light) toggleLight.Select();
            else toggleDark.Select();*/

            previewWindowCanvas.interactable = true;
            previewWindowCanvas.blocksRaycasts = true;
            canvas.DOFade(1f, .3f);
            canvas.interactable = true;
            canvas.blocksRaycasts = true;
            if (!initialized)
            {
                initialized = true;
                PopulateSkinMenu();
            }
            PreviewTheme(ThemeManager.GetSelectedTheme());
        }

        public void Hide()
        {
            previewWindowCanvas.interactable = false;
            previewWindowCanvas.blocksRaycasts = false;
            canvas.DOFade(0f, .3f);
            canvas.interactable = false;
            canvas.blocksRaycasts = false;
        }

        public void LightSelected()
        {
            mode = ThemeMode.Light;
            var theme = selectedTheme;
            if(theme == null)
            {
                theme = ThemeManager.GetSelectedTheme();
            }
            PreviewTheme(theme);
        }

        public void DarkSelected()
        {
            mode = ThemeMode.Dark;
            var theme = selectedTheme;
            if(theme == null)
            {
                theme = ThemeManager.GetSelectedTheme();
            }
            PreviewTheme(theme);
        }
    }
}
