using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Michsky.UI.ModernUIPack;

namespace NotReaper.UI.Components
{
    public class NRThemeableObject : MonoBehaviour, INRThemeable
    {
        [SerializeField] private ColorMode colorMode = ColorMode.Primary;
        [SerializeField] private TransparencyMode transparencyMode = TransparencyMode.Semi;
        private Image image;
        private SpriteRenderer sprite;
        private TextMeshProUGUI text;
        private MeshRenderer mesh;

        private Color primary;
        private Color accent;
        private Color background;

        private void Awake()
        {
            RegisterThemeable();
            GetReferences();
            
        }
        private void GetReferences()
        {
            image = GetComponent<Image>();
            sprite = GetComponent<SpriteRenderer>();
            text = GetComponent<TextMeshProUGUI>();
            mesh = GetComponent<MeshRenderer>();
        }
        public void ApplyDarkTheme(ThemeData theme)
        {
            ApplyColors(theme.skinBase.dark);
        }

        public void ApplyLightTheme(ThemeData theme)
        {
            ApplyColors(theme.skinBase.light);
            
        }

        private void ApplyColors(NRSkinBase skin)
        {
            primary = skin.primaryColor;
            accent = skin.accentColor;
            background = skin.defaultBackgroundColor;
            var alpha = (float)transparencyMode / 100f;
            primary.a = alpha;
            accent.a = alpha;
            background.a = alpha;
        }

        public void RegisterThemeable()
        {
            ThemeManager.RegisterThemeable(this);
        }

        public void UnregisterThemeable()
        {
            ThemeManager.RegisterThemeable(this);
        }

        public void UpdateSkin()
        {
            var theme = ThemeManager.GetSelectedTheme();
            if (ThemeManager.SelectedMode == ThemeMode.Light)
                ApplyLightTheme(theme);
            else
                ApplyDarkTheme(theme);

            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            switch (colorMode)
            {
                case ColorMode.Primary:
                    UpdateColor(primary);
                    break;
                case ColorMode.Accent:
                    UpdateColor(accent);
                    break;
                case ColorMode.Background:
                    UpdateColor(background);
                    break;
            }
        }

        private void UpdateColor(Color color)
        {
        #if UNITY_EDITOR
            GetReferences();
        #endif
            if (image != null)
            {
                image.color = color;
            }
            else if(sprite != null)
            {
                sprite.color = color;
            }
            else if(text != null)
            {
                text.color = color;
            }
            else if(mesh != null)
            {
                mesh.material.color = color;
            }
        }

        public enum ColorMode
        {
            Background,
            Primary,
            Accent
        }

        public enum TransparencyMode
        {
            Full = 100,
            Semi = 50,
            Quarter = 25
        }
    }
}
