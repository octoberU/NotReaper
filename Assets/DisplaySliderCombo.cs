using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisplaySliderCombo : MonoBehaviour
{
    public float value
    {
        get => slider.value;
        set
        {
            Initialize();
            slider.value = value;
            textObj.text = value.ToString("F0") + Extension;
        }
    }

    public bool useDbExtension = true;

    public void SetValueWithoutNotify(float value)
    {
        Initialize();
        slider.SetValueWithoutNotify(value);
        textObj.text = value.ToString() + Extension;
    }

    public GameObject displayTextObject;
    public GameObject sliderObject;

    public event Action<float> OnValueChanged = delegate { };

    private Slider slider;
    private TextMeshProUGUI textObj;

    private bool isInitialized = false;


    // Start is called before the first frame update
    private void Start() => Initialize();

    private void Initialize()
    {
        if (isInitialized) return;
        
        slider = sliderObject.GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate { SliderValueChangeCheck(); });

        textObj = displayTextObject.GetComponent<TextMeshProUGUI>();
        textObj.text = slider.value.ToString("F0") + Extension;
        
        isInitialized = true;
    }

    private string Extension => useDbExtension ? " db" : "";

    public void SliderValueChangeCheck()
    {
        float value = slider.value;

        textObj.text = value.ToString("F0") + Extension;
        OnValueChanged(value);
    }
}
