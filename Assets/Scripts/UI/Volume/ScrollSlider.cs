using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NotReaper.UI.Volume
{
    public class ScrollSlider : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Range(1, 10)] private int incrementInPercent = 1;
        private Slider slider;


        private void Awake()
        {
            slider = GetComponentInChildren<Slider>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            KeybindManager.onScrolled += OnScrolled;
            KeybindManager.DisableKeybind("Scrub");  
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            KeybindManager.onScrolled -= OnScrolled;
        }

        private void OnScrolled(bool forward)
        {
            float step = slider.maxValue * 0.01f * (forward ? 1f : -1f) * incrementInPercent;
            float newValue = Mathf.Clamp(step + slider.value, slider.minValue, slider.maxValue);
            slider.value = newValue;
        }

     


    }
}
