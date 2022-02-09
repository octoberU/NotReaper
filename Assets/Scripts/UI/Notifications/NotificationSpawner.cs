using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NotReaper.Notifications
{
    public class NotificationSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NotificationItem prefab;
        [SerializeField] private Transform contentParent;
        [SerializeField] private NotificationPopup popup;

        public UnityEvent<int> OnNotificationRemoved = new UnityEvent<int>();

        private List<NotificationItem> pool = new List<NotificationItem>();
        private List<NotificationItem> activeItems = new List<NotificationItem>();

        public bool RequestPopup(NotificationType type, bool forceShow, out NotificationPopup popup)
        {
            if(forceShow && !this.popup.IsForceShowing)
            {
                popup = this.popup;
                return true;
            }
            else if(!this.popup.IsForceShowing)
            {
                popup = !this.popup.IsActive || (this.popup.GetNotificationType() < type && this.popup.IsActive) ? this.popup : null;
                return popup != null;

            }
            popup = null;
            return false;
        }

        public NotificationItem SpawnNotification()
        {
            return GetNotificationItem();
        }

        public void RemoveNotification(NotificationItem notification)
        {
            if (activeItems.Contains(notification))
            {
                activeItems.Remove(notification);
                pool.Add(notification);
                OnNotificationRemoved.Invoke(notification.ID);
            }
        }

        private NotificationItem GetNotificationItem()
        {
            NotificationItem notification;
            if(pool.Count == 0)
            {
                notification = InstantiateNotification();
                notification.SetSpawner(this);
            }
            else
            {
                notification = pool[0];
                pool.RemoveAt(0);
            }
            activeItems.Add(notification);
            notification.transform.SetAsFirstSibling();
            
            return notification;
        }
        private NotificationItem InstantiateNotification()
        {
            return Instantiate(prefab, contentParent);
        }
    }
}

