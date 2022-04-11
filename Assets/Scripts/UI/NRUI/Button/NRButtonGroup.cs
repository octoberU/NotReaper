using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI.Components
{
    public class NRButtonGroup : NRThemeable
    {
        [Header("Skin")]
        public NRButtonSkin skin;
        [Space, Header("Group")]
        public NRButton defaultSelectedButton;
        public bool allowMultipleSelected = false;
        [Space, Header("Animation")]
        [Range(.01f, 1f)] public float animationDuration = .25f;
        public bool growOnHover;
        public bool growOnClick;
        public bool stayOnSelected;
        [Range(0f, 10f)] public float growPercentage = 1f;
        [Range(0f, 20f)] public float moveAmount = .1f;
        public AnimationMode mode;
        [Space, Header("Background")]
        public bool hideBackground;
        [Space, Header("Outline")]
        public bool useOutline = true;
        [Space, Header("Underline")]
        public bool useUnderline = false;
        public Theme underlineTheme = Theme.OutlineColor;
        [Space, Header("Icon")]
        public bool overrideIconColors = false;
        public Color highlightedColor = Color.white;
        public Color pressedColor = Color.white;
        [Space, Header("Text")]
        public float textSize = 15f;
        public bool autoSizeText;
        [Space, Header("Independent Buttons")]
        public List<NRButton> independentButtons = new List<NRButton>();

        private List<NRButton> buttons = new List<NRButton>();
        private NRButton selectedButton;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {            
            foreach(var button in independentButtons)
            {
                button.OverrideStayOnSelected(stayOnSelected, SetSelectedButton);
            }
            
            if(Application.isPlaying)
            {
                if (defaultSelectedButton != null)
                {
                    defaultSelectedButton.SetDefaultSelected();
                }
            }
        }

        public void DeselectActiveButton()
        {
            if(selectedButton != null)
            {
                selectedButton.Deselect();
                selectedButton = null;
            }
        }

        private void OnDisable()
        {
            if(selectedButton != null)
            {
                selectedButton.Deselect();
            }
            selectedButton = null;
        }

        public void RegisterButton(NRButton button)
        {
            if (!buttons.Contains(button))
            {
                buttons.Add(button);
            }
        }

        public void UnregisterButton(NRButton button)
        {
            if (buttons.Contains(button))
            {
                buttons.Remove(button);
            }
        }

        protected override void OnValidate()
        {
            if (Application.isPlaying) return;

            foreach(var button in buttons)
            {
                button.UpdateVisuals();
            }
        }

        public void SetSelectedButton(NRButton button)
        {
            if (allowMultipleSelected || selectedButton == button) return;
            if(selectedButton != null)
            {
                selectedButton.Deselect();
            }
            selectedButton = button;
        }

        public override void Initialize() { }

        public override void UpdateVisuals()
        {
            foreach (var button in buttons)
            {
                button.UpdateVisuals();
            }
        }

        public override void ApplyLightTheme(ThemeData theme)
        {
            skin = theme.button.light;
        }

        public override void ApplyDarkTheme(ThemeData theme)
        {
            skin = theme.button.dark;
        }
    }
}
