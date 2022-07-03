using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NotReaper.Modifiers
{
    public class ColorPickerHSV : MonoBehaviour
    {
        [SerializeField] private ColorPicker colorPickerLeft;
        [SerializeField] private ColorPicker colorPickerRight;

        public Color LeftColor => colorPickerLeft.Color;
        public float[] LeftColorHSV => colorPickerLeft.ColorHSV;

        public Color RightColor => colorPickerRight.Color;
        public float[] RightColorHSV => colorPickerRight.ColorHSV;

        internal Action<float[]> onLeftColorUpdated;
        internal Action<float[]> onRightColorUpdated;

        private void Awake()
        {
            colorPickerLeft.onColorUpdated += OnLeftColorUpdated;
            colorPickerRight.onColorUpdated += OnRightColorUpdated;
        }

        public void SetColor(float[] leftColor, float[] rightColor)
        {
            if (leftColor == null || rightColor == null || leftColor.Length == 0 || rightColor.Length == 0) return;
            colorPickerLeft.SetColor(leftColor);
            colorPickerRight.SetColor(rightColor);
        }

        private void OnLeftColorUpdated() => onLeftColorUpdated?.Invoke(LeftColorHSV);
        private void OnRightColorUpdated() => onRightColorUpdated?.Invoke(RightColorHSV);
    }
}
