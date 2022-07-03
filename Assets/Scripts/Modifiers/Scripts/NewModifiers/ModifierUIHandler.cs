using System.Collections.Generic;
using NotReaper.UI.Components;
using UnityEngine;
using NotReaper.Modifier;
using NotReaper.Modifiers.Processors;
using TMPro;
using UnityEngine.UI;

namespace NotReaper.Modifiers
{
    public class ModifierUIHandler : MonoBehaviour
    {
        [SerializeField] private NRTitle modifierName;
        [SerializeField] private NRInputSliderCombo amountInput;
        [SerializeField] private TextMeshProUGUI hint;
        [SerializeField] private RectTransform content;

        [Space]
        [SerializeField] private NRInputField value1Input;

        [SerializeField] private NRInputField value2Input;
        [SerializeField] private NRInputField value3Input;
        [SerializeField] private NRInputField value4Input;
        [SerializeField] private NRInputField value5Input;

        [Space]
        [SerializeField] private NRToggle option1Toggle;

        [SerializeField] private NRToggle option2Toggle;
        [SerializeField] private NRToggle option3Toggle;

        [Space]
        [SerializeField] private ColorPickerHSV colorPickerHSV;
        [SerializeField] private ColorPickerRgb colorPickerRGB;

        [Space]
        [SerializeField] private NRButton extraButton;

        public delegate void OnUIUpdate(Modifier modifier);
        public static event OnUIUpdate onBeforeUIUpdate;
        
        private Processor processor = null;
        [NRInject] private ModifierManager manager;


        private void Start()
        {
            ModifierManager.onModifierSelected += OnModifierSelected;
            ModifierManager.onSelectedModifierRemoved += HideUI;
            ModifierManager.onMultiSelect += HideUI;

            amountInput.slider.onValueChanged.AddListener(OnAmountChanged);

            value1Input.onValueChanged.AddListener(OnValue1Changed);
            value2Input.onValueChanged.AddListener(OnValue2Changed);
            value3Input.onValueChanged.AddListener(OnValue3Changed);
            value4Input.onValueChanged.AddListener(OnValue4Changed);
            value5Input.onValueChanged.AddListener(OnValue5Changed);

            option1Toggle.onSelected.AddListener(OnOption1Changed);
            option2Toggle.onSelected.AddListener(OnOption2Changed);
            option3Toggle.onSelected.AddListener(OnOption3Changed);
            
            extraButton.onClick.AddListener(OnExtraButtonPressed);

            colorPickerHSV.onLeftColorUpdated += OnLeftColorUpdated;
            colorPickerHSV.onRightColorUpdated += OnRightColorUpdated;
            colorPickerRGB.onColorUpdated += OnLeftColorUpdated;

            amountInput.gameObject.SetActive(false);
            hint.text = "";
            
            value1Input.gameObject.SetActive(false);
            value2Input.gameObject.SetActive(false);
            value3Input.gameObject.SetActive(false);
            value4Input.gameObject.SetActive(false);
            value5Input.gameObject.SetActive(false);

            option1Toggle.gameObject.SetActive(false);
            option2Toggle.gameObject.SetActive(false);
            option3Toggle.gameObject.SetActive(false);

            colorPickerHSV.gameObject.SetActive(false);
            colorPickerRGB.gameObject.SetActive(false);
            
            extraButton.gameObject.SetActive(false);
        }

        private void HideUI()
        {
            modifierName.textContainer.text = "";
            amountInput.gameObject.SetActive(false);
            value1Input.gameObject.SetActive(false);
            value2Input.gameObject.SetActive(false);
            value3Input.gameObject.SetActive(false);
            value4Input.gameObject.SetActive(false);
            value5Input.gameObject.SetActive(false);
            option1Toggle.gameObject.SetActive(false);
            option2Toggle.gameObject.SetActive(false);
            option3Toggle.gameObject.SetActive(false);
            extraButton.gameObject.SetActive(false);
            colorPickerHSV.gameObject.SetActive(false);
            colorPickerRGB.gameObject.SetActive(false);
        }

        private void OnModifierSelected(Modifier modifier)
        {
            if (!ProcessorManager.TryGetProcessor(modifier, out processor)) return;
            
            onBeforeUIUpdate?.Invoke(modifier);
            
            if(processor.RefreshOnSelect) processor.RefreshFields();

            modifierName.textContainer.text = modifier.Type.ToDisplayName();

            amountInput.gameObject.SetActive(processor.Amount.Show);
            amountInput.slider.minValue = processor.AmountMinMax.x;
            amountInput.slider.maxValue = processor.AmountMinMax.y;
            amountInput.SetValueWithoutNotify(processor.Amount.Get());
            amountInput.inputField.title.text = processor.Amount.DisplayName;

            value1Input.gameObject.SetActive(processor.Value1.Show);
            value2Input.gameObject.SetActive(processor.Value2.Show);
            value3Input.gameObject.SetActive(processor.Value3.Show);
            value4Input.gameObject.SetActive(processor.Value4.Show);
            value5Input.gameObject.SetActive(processor.Value5.Show);

            
            option1Toggle.gameObject.SetActive(processor.Option1.Show);
            option2Toggle.gameObject.SetActive(processor.Option2.Show);
            option3Toggle.gameObject.SetActive(processor.Option3.Show);

            colorPickerHSV.gameObject.SetActive(processor.ColorPickerType is ColorPickerType.HSV);
            colorPickerRGB.gameObject.SetActive(processor.ColorPickerType is ColorPickerType.RGB);
            colorPickerHSV.SetColor(processor.ColorLeft.Get(), processor.ColorRight.Get());
            colorPickerRGB.SetColor(processor.ColorLeft.Get());

            extraButton.gameObject.SetActive(processor.ExtraButton.Show);
            
            hint.text = processor.Hint;

            value1Input.text = processor.Value1.Get();
            value1Input.title.text = processor.Value1.DisplayName;
            value1Input.contentType = processor.Value1.ContentType;

            value2Input.text = processor.Value2.Get();
            value2Input.title.text = processor.Value2.DisplayName;

            value3Input.text = processor.Value3.Get();
            value3Input.title.text = processor.Value3.DisplayName;

            value4Input.text = processor.Value4.Get();
            value4Input.title.text = processor.Value4.DisplayName;

            value5Input.text = processor.Value5.Get();
            value5Input.title.text = processor.Value5.DisplayName;

            option1Toggle.selected = processor.Option1.Get();
            option1Toggle.textContainer.text = processor.Option1.DisplayName;

            option2Toggle.selected = processor.Option2.Get();
            option2Toggle.textContainer.text = processor.Option2.DisplayName;

            option3Toggle.selected = processor.Option3.Get();
            option3Toggle.textContainer.text = processor.Option3.DisplayName;

            modifier.ResetDuration();
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }


        
        public void RefreshProcessor() => OnModifierSelected(manager.CurrentModifier);
        private void OnAmountChanged(float amount) => processor?.Amount.Set(amount);
        private void OnValue1Changed(string value) => processor?.Value1.Set(value);
        private void OnValue2Changed(string value) => processor?.Value2.Set(value);
        private void OnValue3Changed(string value) => processor?.Value3.Set(value);
        private void OnValue4Changed(string value) => processor?.Value4.Set(value);
        private void OnValue5Changed(string value) => processor?.Value5.Set(value);
        private void OnExtraButtonPressed() =>  processor?.OnExtraButtonPressed();
        private void OnOption3Changed(bool option) => processor?.Option3.Set(option);
        private void OnRightColorUpdated(float[] color) => processor?.ColorRight.Set(color);
        private void OnLeftColorUpdated(float[] color) =>  processor?.ColorLeft.Set(color);
        
        private void OnOption1Changed(bool option)
        {
            if (processor == null) return;
            
            processor.Option1.Set(option);
            processor.OnOption1Changed();
        }

        private void OnOption2Changed(bool option)
        {
            if (processor == null) return;
            
            processor.Option2.Set(option);
            processor.OnOption2Changed();
        }
    }
}