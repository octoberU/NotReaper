using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using NotReaper.UI.Components;

namespace NotReaper.Modifier
{
    public class LabelSetter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Slider slider;
        [SerializeField] private NRToggle toggle;
        [SerializeField] private TextMeshProUGUI hint;
        [Header("Color")]
        [SerializeField] private Slider colorSliderHueLeft;
        [SerializeField] private Slider colorSliderHueRight;
        [SerializeField] private Slider colorSliderSaturationLeft;
        [SerializeField] private Slider colorSliderSaturationRight;
        [SerializeField] private Image colorFieldLeft;
        [SerializeField] private Image colorFieldRight;

        [SerializeField] private TextMeshProUGUI colorHueLeftLabel;
        [SerializeField] private TextMeshProUGUI colorSaturationLeftLabel;
        [SerializeField] private TextMeshProUGUI colorHueRightLabel;
        [SerializeField] private TextMeshProUGUI colorSaturationRightLabel;

        [SerializeField] private TextMeshProUGUI leftColorLabel;
        [SerializeField] private TextMeshProUGUI rightColorLabel;
        [SerializeField] private bool isColorPicker = false;

        [SerializeField] private NRToggleGroup toggleGroup;

        [SerializeField] private ColorPicker colorPickerLeft;
        [SerializeField] private ColorPicker colorPickerRight;

        [SerializeField] private TextMeshProUGUI trackerText;

        public void Start()
        {
            if(inputField != null)
            {
                inputField.onSelect.AddListener(ModifierHandler.Instance.OnInputFocusChange);
                inputField.onDeselect.AddListener(ModifierHandler.Instance.OnInputFocusChange);
            }
            /*
            if(colorFieldLeft != null)
            {
                colorSliderHueLeft.value = 0f;
                colorSliderSaturationLeft.value = 0f;
            }
            if(colorFieldRight != null)
            {
                colorSliderHueRight.value = 0f;
                colorSliderSaturationRight.value = 0f;
            }
            */
        }

        private void UpdateColors()
        {
            colorPickerLeft.UpdateValues();
            colorPickerRight.UpdateValues();
        }

        public void InitializeColorFields()
        {
            colorFieldLeft.color = Color.white;
            colorFieldRight.color = Color.white;
        }

        public void SetSkyboxColor(float[] col, float saturation)
        {
            if(col.Length < 3)
            {
                col = new float[] { 0f, 0f, 0f };
            }
            colorSliderHueLeft.value = col[0];
            colorSliderSaturationLeft.value = col[1];
            colorSliderHueRight.value = col[2];
            colorSliderSaturationRight.value = saturation;
            UpdateSkyboxColor();
        }

        public void UpdateSkyboxColor()
        {
            float[] col = GetSkyboxColor();
            float saturation = GetSaturation();
            for(int i = 0; i < col.Length; i++)
            {
                col[i] *= saturation;
            }
            colorFieldLeft.color = new Color(col[0], col[1], col[2]);
        }

        public void SetSaturationRightValue(float value)
        {
            colorSliderSaturationRight.value = value;
            UpdateSkyboxColor();
        }

        public void SetLabelText(string text)
        {
            label.text = text;
        }

        public void SetMinValue(float min)
        {
            slider.minValue = min;
        }
        public void SetMaxValue(float max)
        {
            slider.maxValue = max;
        }

        public void SetSliderValue(float value)
        {
            slider.value = value;
        }

        public string GetText()
        {
            return inputField.text;
        }

        public void SetInputText(string text)
        {
            inputField.text = text;
        }

        public void SetToggleState(bool on)
        {
            toggle.selected = on;
        }

        public bool GetToggleState()
        {
            return toggle.selected;
        }

        public void SetHintText(string text)
        {
            hint.text = text;
        }

        public void SetColorSliderLeft(float[] col)
        {
            if (col.Length < 3) col = new float[] { 0f, 0f, 0f };
            Color color = new Color(col[0], col[1], col[2]);
            colorFieldLeft.color = color;
            float h, s;
            Color.RGBToHSV(color, out h, out s, out _);
            colorSliderHueLeft.value = h;
            colorSliderSaturationLeft.value = s;
            
        }

        public void SetColorSliderRight(float[] col)
        {
            if (col.Length < 3) col = new float[] { 0f, 0f, 0f };
            Color color = new Color(col[0], col[1], col[2]);
            colorFieldRight.color = color;
            float h, s;
            Color.RGBToHSV(color, out h, out s, out _);
            colorSliderHueRight.value = h;
            colorSliderSaturationRight.value = s;
        }

        public void SetMinMaxColorSliders(float min, float max)
        {
            colorSliderHueLeft.minValue = min;
            colorSliderHueRight.minValue = min;

            colorSliderSaturationLeft.minValue = min;
            colorSliderSaturationRight.minValue = min;


            colorSliderHueLeft.maxValue = max;
            colorSliderHueRight.maxValue = max;

            colorSliderSaturationLeft.maxValue = max;
            colorSliderSaturationRight.maxValue = max;           
        }

        public float[] GetLeftColor()
        {
            Color c = Color.HSVToRGB(colorSliderHueLeft.value, colorSliderSaturationLeft.value, 1f);
            return new float[] { c.r, c.g, c.b };
        }

        public float[] GetRightColor()
        {
            Color c = Color.HSVToRGB(colorSliderHueRight.value, colorSliderSaturationRight.value, 1f);
            return new float[] { c.r, c.g, c.b };
        }

        public float[] GetSkyboxColor()
        {
            Color c = new Color(colorSliderHueLeft.value, colorSliderSaturationLeft.value, colorSliderHueRight.value);
            return new float[] { c.r, c.g, c.b };
        }

        public float GetSaturation()
        {
            return colorSliderSaturationRight.value;
        }

        private void SetRightHueLabel(string label)
        {
            colorHueRightLabel.text = label;
        }
        private void SetLeftHueLabel(string label)
        {
            colorHueLeftLabel.text = label;
        }
        private void SetLeftSaturationLabel(string label)
        {
            colorSaturationLeftLabel.text = label;
        }

        private void SetRightSaturationLabel(string label)
        {
            colorSaturationRightLabel.text = label;
        }

        public void SetIsColorPicker(bool _isColorPicker)
        {
            isColorPicker = _isColorPicker;
            leftColorLabel.enabled = _isColorPicker;
            rightColorLabel.enabled = _isColorPicker;
            //colorSliderSaturationRight.transform.parent.gameObject.SetActive(_isColorPicker);
            colorFieldRight.enabled = _isColorPicker;
            if (_isColorPicker)
            {
                SetLeftHueLabel("Hue");
                SetRightHueLabel("Hue");
                SetLeftSaturationLabel("Saturation");
            }
            else
            {
                SetLeftHueLabel("Red");
                SetLeftSaturationLabel("Green");
                SetRightHueLabel("Blue");
            }
            UpdateColors();
        }

        public void EnableToggleGroup(bool enable)
        {
            toggleGroup.allowMultipleSelected = !enable;
        }

        public bool IsColorPicker()
        {
            return isColorPicker;
        }

        public void SetTrackerText(string text)
        {
            trackerText.text = text;
        }
    }
}

