using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace NotReaper.UI
{
    public class OnPressRelease : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public OnPointerDownHandler onPressed;
        public OnPointerUpHandler onReleased;

        public void OnPointerDown(PointerEventData eventData)
        {
            onPressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onReleased?.Invoke();
        }

        [Serializable]
        public class OnPointerDownHandler : UnityEvent
        {
            public OnPointerDownHandler OnEvent;
        }
        [Serializable]
        public class OnPointerUpHandler : UnityEvent
        {
            public OnPointerUpHandler OnEvent;
        }
    }
}
