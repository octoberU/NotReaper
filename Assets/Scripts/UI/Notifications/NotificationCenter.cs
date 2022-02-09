using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace NotReaper.Notifications
{
    public class NotificationCenter : MonoBehaviour
    {
        [Header("Notification Colors")]
        [SerializeField] private Color info = Color.white;
        [SerializeField] private Color success = Color.green;
        [SerializeField] private Color warning = Color.yellow;
        [SerializeField] private Color error = Color.red;

        private static Dictionary<NotificationType, Color> notificationColors = new Dictionary<NotificationType, Color>();

        private static int index = 0;
        private static Dictionary<int, NotificationItem> notifications = new Dictionary<int, NotificationItem>();

        private static NotificationPanel panel;
        private static NotificationSpawner spawner;

        private static bool isForceShowingPopup = false;

        private void Awake()
        {
            notificationColors.Add(NotificationType.Info, info);
            notificationColors.Add(NotificationType.Success, success);
            notificationColors.Add(NotificationType.Warning, warning);
            notificationColors.Add(NotificationType.Error, error);

            panel = gameObject.GetComponentInChildren<NotificationPanel>();
            spawner = GetComponent<NotificationSpawner>();
            spawner.OnNotificationRemoved.AddListener(OnNotificationRemoved);
        }
        public static int SendNotification(object text, NotificationType type, bool forceShowPopup = false)
        {
            if (!NotificationPanel.IsOpen)
            {
                if (spawner.RequestPopup(type, forceShowPopup, out NotificationPopup popup))
                {
                    popup.IsForceShowing = forceShowPopup;
                    popup.Show(type, text.ToString(), index);
                }
            }
            var notif = spawner.SpawnNotification();
            notif.Show(type, text.ToString(), index);
            notifications.Add(index, notif);
            UpdateBadge(true);
            return index++;
        }

        private static void UpdateBadge(bool playAnimation)
        {
            if (NotificationPanel.IsOpen) return;
            NotificationType highestType = NotificationType.Info;
            bool showBadge = false;
            foreach (var notification in notifications)
            {
                if (notification.Value.Seen) continue;
                highestType = notification.Value.GetNotificationType();
                showBadge = true;
                if (highestType == NotificationType.Error) break;
            }
            if (showBadge)
            {
                panel.UpdateBadge(GetNotificationColor(highestType), playAnimation);
            }
            else
            {
                panel.HideBadge();
            }
        }

        public static void RemoveNotification(int id)
        {
            if (notifications.ContainsKey(id))
            {
                var notif = notifications[id];
                notif.Close();
                UpdateBadge(false);
            }
        }

        public static void SetNotificationSeen(int id)
        {
            if (notifications.ContainsKey(id))
            {
                notifications[id].SetSeen();
                UpdateBadge(true);
            }
        }

        public static void ClearNotifications()
        {
            foreach(var notification in notifications)
            {
                RemoveNotification(notification.Key);
            }
        }

        private void OnNotificationRemoved(int id)
        {
            if (notifications.ContainsKey(id))
            {
                notifications.Remove(id);
            }
        }

        public static Color GetNotificationColor(NotificationType type)
        {
            return notificationColors[type];
        }
    } 

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error 
    }
}

