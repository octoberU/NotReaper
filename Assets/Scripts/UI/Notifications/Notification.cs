using NotReaper.UI;
using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Notifications
{
    public abstract class Notification : MonoBehaviour, INRThemeable
    {
        public int ID { get; private set; }

        [Header("Icons")]
        [SerializeField] protected Sprite successSprite;
        [SerializeField] protected Sprite infoSprite;
        [SerializeField] protected Sprite errorSprite;
        [SerializeField] protected Image iconHolder;
        [Space, Header("References")]
        [SerializeField] protected TextMeshProUGUI notificationText;
        [SerializeField] protected Image background;
        [SerializeField] protected Image closeButton;
        protected CanvasGroup canvas;
        protected NotificationType type;

        private Color backgroundColor;
        private Color textColor;
        private Color iconColor;

        private bool initialized = false;

        protected virtual void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized)
                return;
            
            canvas = GetComponent<CanvasGroup>();
            RegisterThemeable();
            initialized = true;
        }
        
        protected void Setup(NotificationType type, string text, int id)
        {
            Initialize();
            this.type = type;
            iconHolder.sprite = type == NotificationType.Info ? infoSprite : type == NotificationType.Success ? successSprite : errorSprite;
            iconHolder.SetNativeSize();
            iconHolder.color = NotificationCenter.GetNotificationColor(type);
            notificationText.text = text;
            this.ID = id;
        }

        internal NotificationType GetNotificationType()
        {
            return type;
        }

        internal abstract void Show(NotificationType type, string text, int id);
        public abstract void Close();

        public void ApplyLightTheme(ThemeData theme)
        {
            backgroundColor = theme.background.light.backgroundColor;
            textColor = theme.window.light.textColor;
            iconColor = theme.button.light.defaultIconColor;
        }

        public void ApplyDarkTheme(ThemeData theme)
        {
            backgroundColor = theme.background.dark.backgroundColor;
            textColor = theme.window.dark.textColor;
            iconColor = theme.button.light.defaultIconColor;
        }
        public void UpdateVisuals()
        {
            background.color = backgroundColor;
            notificationText.color = textColor;
            closeButton.color = iconColor;
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

        protected virtual void OnDestroy()
        {
            UnregisterThemeable();
        }

    }

}
