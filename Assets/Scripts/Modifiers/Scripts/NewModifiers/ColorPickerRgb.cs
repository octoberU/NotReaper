using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.UI.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ColorPickerRgb : MonoBehaviour
{
    [SerializeField]
    private NRInputSliderCombo red;

    [SerializeField]
    private NRInputSliderCombo green;

    [SerializeField]
    private NRInputSliderCombo blue;

    [SerializeField]
    private Image colorField;

    private Color color = Color.white;

    public Color Color => color;

    internal Action<float[]> onColorUpdated;

    public float[] ColorArray
    {
        get
        {
            var c = new float[3];
            c[0] = color.r;
            c[1] = color.g;
            c[2] = color.b;
            return c;
        }
    }

    private void Start()
    {
        red.OnValueChanged.AddListener(_ => UpdateColorField());
        blue.OnValueChanged.AddListener(_ => UpdateColorField());
        red.OnValueChanged.AddListener(_ => UpdateColorField());
    }

    public void SetColor(float[] color)
    {
        if (color == null || color.Length == 0) return;
        
        this.color = new Color(color[0], color[1], color[2]);
        red.SetValueWithoutNotify(color[0]);
        blue.SetValueWithoutNotify(color[1]);
        green.SetValueWithoutNotify(color[2]);
        colorField.color = this.color;
    }

    private void UpdateColorField()
    {
        var red = this.red.value;
        var blue = this.blue.value;
        var green = this.green.value;

        color = new Color(red, blue, green);
        colorField.color = color;

        onColorUpdated?.Invoke(ColorArray);
    }
}