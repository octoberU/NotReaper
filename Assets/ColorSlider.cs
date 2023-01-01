
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ColorSlider : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] Image display;
    [SerializeField] TextMeshProUGUI text;
    public Color color;

    public UnityEvent<Color> onColorsSet;

    void Start()
    {
        slider.onValueChanged.AddListener(_ => { UpdateDisplay(); });
    }

    public void SetColor(Color newColor, bool silent = false)
    {
        color = newColor;
        display.color = color;
        Vector3 hsv;
        Color.RGBToHSV(color, out hsv.x, out hsv.y, out hsv.z);
        slider.value = hsv.x;
        
        if(!silent)
            onColorsSet?.Invoke(color);
    }

    public void UpdateDisplay()
    {
        Color newColor = Color.HSVToRGB(slider.value, 0.55f, 1f);
        SetColor(newColor);
    }
}
