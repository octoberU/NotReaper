using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.UI;
using NotReaper.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Notifications
{
    public class ChangeHistoryItem : MonoBehaviour, INRThemeable
    {
        [SerializeField] private TextMeshProUGUI actionName;
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private Image icon;

        private Color backgroundColor;
        private Color textColor;
        private Color iconColor;

        private ChangeHistoryManager manager;
        private ChangeHistoryManager.ChangeData data;
        
        private void Awake()
        {
            RegisterThemeable();
        }

        private void OnDestroy()
        {
            UnregisterThemeable();
        }

        internal void Init(ChangeHistoryManager manager, ChangeHistoryManager.ChangeData data)
        {
            this.data = data;
            actionName.SetText(data.actionName);
            
            this.manager = manager;

            var scale = icon.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (data.isUndo ? 1f : -1f);
            icon.transform.localScale = scale;

        }

        public void ApplyLightTheme(ThemeData theme)
        {
            backgroundColor = theme.background.light.backgroundColor;
            textColor = theme.window.light.textColor;
            iconColor = theme.button.light.defaultIconColor;
        }

        public void OnClick() => manager.OnClick(data);

        public void ApplyDarkTheme(ThemeData theme)
        {
            backgroundColor = theme.background.dark.backgroundColor;
            textColor = theme.window.dark.textColor;
            iconColor = theme.button.light.defaultIconColor;
        }
        public void UpdateVisuals()
        {
            background.color = backgroundColor;
            actionText.color = textColor;
            icon.color = iconColor;
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

        public void RegisterThemeable()
            => ThemeManager.RegisterThemeable(this);


        public void UnregisterThemeable()
            => ThemeManager.UnregisterThemeable(this);
    }
}
