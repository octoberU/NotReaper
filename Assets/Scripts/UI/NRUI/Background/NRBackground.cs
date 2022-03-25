using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.UI.Components
{
    [RequireComponent(typeof(Image))]
    [ExecuteAlways]
    public class NRBackground : NRThemeable
    {
        [Header("Skin")]
        [SerializeField] private NRBackgroundSkin skin;

        [HideInInspector, SerializeField] public Image background;

        private bool initialized;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void ApplyDarkTheme(ThemeData theme)
        {
            skin = theme.background.dark;
        }

        public override void ApplyLightTheme(ThemeData theme)
        {
            skin = theme.background.light;
        }

        public override void Initialize()
        {
            initialized = true;
            background = GetComponent<Image>();
        }

        public override void UpdateVisuals()
        {
            background.color = skin.backgroundColor;
        }

        protected override void OnValidate()
        {
            if (!initialized)
            {
                Initialize();
            }
            UpdateVisuals();
        }
    }
}

