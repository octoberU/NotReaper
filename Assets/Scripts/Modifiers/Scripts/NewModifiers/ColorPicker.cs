using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using NotReaper.UI.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NotReaper.Modifiers
{
    public class ColorPicker : MonoBehaviour
    {
        [SerializeField]
        private NRInputSliderCombo hue;
        [SerializeField]
        private NRInputSliderCombo saturation;
        [SerializeField]
        private Image colorField;

        public Color Color => color;

        internal Action onColorUpdated;

        public float[] ColorHSV
        {
            get
            {
                var c = new float[3];
                c[0] = hue.value;
                c[1] = saturation.value;
                c[2] = 1f;
                return c;
            }
        }

        private Color color = Color.white;
        
        private void Start()
        {
            hue.OnValueChanged.AddListener(_ => UpdateColorField());
            saturation.OnValueChanged.AddListener(_ => UpdateColorField());
        }

        public void SetColor(float[] color)
        {
            this.color = Color.HSVToRGB(color[0], color[1], 1f);
            colorField.color = this.color;
            
            hue.SetValueWithoutNotify(color[0]);
            saturation.SetValueWithoutNotify(color[1]);
        }

        private void UpdateColorField()
        {
            var hue = this.hue.value;
            var saturation = this.saturation.value;

            color = Color.HSVToRGB(hue, saturation, 1f);
            colorField.color = color;
            
            onColorUpdated?.Invoke();
        }
    }
}
