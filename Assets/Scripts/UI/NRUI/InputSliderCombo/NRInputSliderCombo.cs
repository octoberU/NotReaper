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
                value = Round(value);
                slider.value = value;
                inputField.text = value.ToString();
            }
        }

        public NRInputField inputField;
        public Slider slider;
        public UpdateMode mode = UpdateMode.OnValueChanged;

        public event Action<float> OnValueChanged = delegate { };


        // Start is called before the first frame update
        void Start()
        {
            var slider = this.slider.GetComponent<Slider>();
            slider.onValueChanged.AddListener(delegate { SliderValueChangeCheck(); });

            inputField.text = slider.value.ToString();
            if(mode == UpdateMode.OnValueChanged)
                inputField.onValueChanged.AddListener(delegate { TextValueChangeCheck(); });
            else
                inputField.onEndEdit.AddListener(delegate { TextValueChangeCheck(); });
        }

        private float Round(float value)
            => (float)Math.Round(value, 2);

        public void SliderValueChangeCheck()
        {
            var slider = this.slider.GetComponent<Slider>();
            float value = Round(slider.value);

            inputField.text = value.ToString(); ;
            OnValueChanged(value);
        }

        public void TextValueChangeCheck()
        {
            var text = inputField.text;
            float.TryParse(text, out float newValue);
            newValue = Round(newValue);
            slider.value = newValue;
            inputField.text = newValue.ToString();
            OnValueChanged(newValue);
        }

        public void Initialize()
        {
            inputField.Initialize();
            slider.GetComponent<NRSlider>().Initialize();
        }

        public enum UpdateMode
        {
            OnValueChanged,
            OnEndEdit
        }
    }
}

