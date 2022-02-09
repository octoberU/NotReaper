using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NotReaper.Notifications;
public class InputCatcher : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private NotificationPanel panel;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (NotificationPanel.IsOpen)
        {
            panel.ToggleShow();
        }
    }
}
