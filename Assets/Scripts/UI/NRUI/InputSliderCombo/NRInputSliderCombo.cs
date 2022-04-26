using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.UI.Components
{
    public class NRInputSliderCombo : MonoBehaviour
    {
        public float value
        {
            get
            {
                return value;
            }
            set
            {
                slider.value = value;
                inputField.text = value.ToString();
            }
        }

        public NRInputField inputField;
        public Slider slider;

        public event Action<float> OnValueChanged = delegate { };


        // Start is called before the first frame update
        void Start()
        {
            var slider = this.slider.GetComponent<Slider>();
            slider.onValueChanged.AddListener(delegate { SliderValueChangeCheck(); });

            inputField.text = slider.value.ToString();
            inputField.onValueChanged.AddListener(delegate { TextValueChangeCheck(); });
        }

        public void SliderValueChangeCheck()
        {
            var slider = this.slider.GetComponent<Slider>();
            float value = slider.value;

            inputField.text = value.ToString(); ;
            OnValueChanged(value);
        }

        public void TextValueChangeCheck()
        {
            var text = inputField.text;
            float.TryParse(text, out float newValue);
            slider.value = newValue;

            OnValueChanged(newValue);
        }

        public void Initialize()
        {
            inputField.Initialize();
            slider.GetComponent<NRSlider>().Initialize();
        }
    }
}

