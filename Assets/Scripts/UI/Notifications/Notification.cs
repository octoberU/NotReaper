using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Notifications
{
    public abstract class Notification : MonoBehaviour
    {
        public int ID { get; private set; }

        [Header("Icons")]
        [SerializeField] protected Sprite successSprite;
        [SerializeField] protected Sprite infoSprite;
        [SerializeField] protected Sprite errorSprite;
        [SerializeField] protected Image iconHolder;
        [Space, Header("References")]
        [SerializeField] protected TextMeshProUGUI notificationText;
        protected CanvasGroup canvas;
        protected NotificationType type;

        protected virtual void Awake()
        {
            canvas = GetComponent<CanvasGroup>();
        }
        protected void Setup(NotificationType type, string text, int id)
        {
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
    }

}
