using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace NotReaper.UI
{
    public class OnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public OnHoverHandler onHover;

        public void OnPointerEnter(PointerEventData eventData)
        {
            onHover?.Invoke(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onHover?.Invoke(false);
        }

        [Serializable]
        public class OnHoverHandler : UnityEvent<bool>
        {
            OnHoverHandler OnEvent;
        }
    }

}
