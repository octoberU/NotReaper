using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NotReaper.UI.Components
{
    public class NRInputSliderCombo : MonoBehaviour
    {
        private float _value;
        public float value
        {
            get => _value;
            set
            {
                _value = Round(value);
                slider.value = value;
                inputField.text = value.ToString();
            }
        }

        public NRInputField inputField;
        public Slider slider;
        public UpdateMode mode = UpdateMode.OnValueChanged;

        public UnityEvent<float> OnValueChanged;

        private bool mouseDown = false;
        private bool needUpdate = false;

        public void SetValueWithoutNotify(float value)
        {
            _value = Round(value);
            slider.SetValueWithoutNotify(value);
            inputField.text = value.ToString();
        }


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

            if (mode == UpdateMode.OnEndEdit)
            {
                KeybindManager.onMouseDown += OnMouseDown;
            }
        }

        private void OnMouseDown(bool down)
        {
            mouseDown = down;

            if (!mouseDown && needUpdate)
            {
                UpdateOnEndEdit();
            }
        }

        private float Round(float value)
            => (float)Math.Round(value, 2);

        public void SliderValueChangeCheck()
        {
            var slider = this.slider.GetComponent<Slider>();
            if (mode == UpdateMode.OnEndEdit)
            {
                if (mouseDown)
                {
                    SilentUpdate(Round(slider.value));
                    return;
                }
            }
            _value = Round(slider.value);

            inputField.text = _value.ToString();
            OnValueChanged?.Invoke(value);
        }

        public void TextValueChangeCheck()
        {
            var text = inputField.text;
            float.TryParse(text, out float newValue);
            if (needUpdate)
            {
                needUpdate = false;
            }
            newValue = Round(newValue);
            slider.value = newValue;
            inputField.text = newValue.ToString();
            _value = newValue;
            OnValueChanged?.Invoke(newValue);
        }

        private void SilentUpdate(float value)
        {
            needUpdate = true;
            _value = value;
            slider.SetValueWithoutNotify(value);
            inputField.text = value.ToString();
        }
        
        private void UpdateOnEndEdit()
        {
            needUpdate = false;
            slider.SetValueWithoutNotify(_value);
            inputField.text = _value.ToString();
            OnValueChanged?.Invoke(_value);
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

