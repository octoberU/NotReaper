using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NotReaper.UI.Components
{
    [ExecuteAlways]
    public class NRInputField : NRThemeable
    {
        [Header("Skin")]
        [SerializeField] private NRInputFieldSkin skin;
        [Space, Header("Title")]
        [SerializeField] private string titleText = "";
        [SerializeField] private float titleSize = 12f;
        [SerializeField] private bool autoSizeTitle = false;
        [SerializeField] private bool useTitle = true;
        [SerializeField] private bool allowOverflow = true;
        [Space, Header("Input")]
        [SerializeField] private string placeholderText = "";
        [SerializeField] private float textSize = 12f;
        [SerializeField] private bool usePlaceholderText = false;
        [SerializeField] private bool autoSizeInput = false;
        [SerializeField] private HorizontalAlignmentOptions horizontalAlignment = HorizontalAlignmentOptions.Left;
        [SerializeField] private VerticalAlignmentOptions verticalAlignment = VerticalAlignmentOptions.Capline;
        [SerializeField] private Vector4 margin = new Vector4(5f, 0f, 0f, 0f);
        [Space, Header("Input Method")]
        [SerializeField] public TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard;
        [Space, Header("Animation")]
        [SerializeField] private float animationDuration = .3f;
        [Space, Header("Callbacks")]
        public OnValueChanged onValueChanged;
        public OnEndEdit onEndEdit;
        public OnSubmit onSubmit;

        [HideInInspector, SerializeField] public TextMeshProUGUI title;
        [HideInInspector, SerializeField] public TMP_InputField inputField;
        [HideInInspector, SerializeField] public TextMeshProUGUI placeholder;
        [HideInInspector, SerializeField] public TextMeshProUGUI inputText;
        [HideInInspector, SerializeField] public Image background;
        [HideInInspector, SerializeField] public Image outline;
        [HideInInspector] public string text
        {
            get
            {
                return inputField.text;
            }
            set
            {
                SetText(value);
            }
        }

        internal int index;
        internal bool isFocused;

        private bool initialized;
        

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {

            if (Application.isPlaying)
            {
                inputField.onValueChanged.AddListener(ValueChanged);
                inputField.onEndEdit.AddListener(EndEdit);
                inputField.onSelect.AddListener(OnSelected);
                inputField.onDeselect.AddListener(OnDeselected);
                inputField.onSubmit.AddListener(OnSubmitted);
            }
            else
            {
                UpdateVisuals();
            }
        }

        public override void Initialize()
        {
            title = transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
            inputField = transform.GetChild(1).GetComponent<TMP_InputField>();
            background = inputField.transform.GetChild(0).GetComponent<Image>();
            outline = background.transform.GetChild(0).GetComponent<Image>();
            placeholder = outline.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
            inputText = outline.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();
        }

        private void SetText(string text)
        {
            inputField.SetTextWithoutNotify(text);
        }

        private void ValueChanged(string text)
            => onValueChanged?.Invoke(text);

        private void EndEdit(string text)
            => onEndEdit?.Invoke(text);

        private void OnSelected(string _)
        {
            var color = GetOutlineColor();
            outline.DOColor(color, animationDuration);
            title.DOColor(color, animationDuration);
            isFocused = true;
        }

        private void OnDeselected(string _)
        {
            outline.DOColor(skin.outlineColor, animationDuration);
            title.DOColor(skin.textColor, animationDuration);
            isFocused = false;
        }

        private void OnSubmitted(string text)
        {
            onSubmit?.Invoke(text);
        }

        private void OnDisable()
        {
            outline.color = skin.outlineColor;
        }

        private Color GetOutlineColor()
        {
            switch (skin.outlineColorMode)
            {
                case NRInputFieldSkin.OutlineColorMode.Custom:
                    return skin.selectedOutlineColor;
                case NRInputFieldSkin.OutlineColorMode.CurrentHand:
                    return EditorState.Hand.Current == Models.TargetHandType.Left ? NRSettings.config.leftColor : NRSettings.config.rightColor;
                case NRInputFieldSkin.OutlineColorMode.OppositeHand:
                    return EditorState.Hand.Current == Models.TargetHandType.Left ? NRSettings.config.rightColor : NRSettings.config.leftColor;
                case NRInputFieldSkin.OutlineColorMode.LeftHand:
                    return NRSettings.config.leftColor;
                case NRInputFieldSkin.OutlineColorMode.RightHand:
                    return NRSettings.config.rightColor;
                default:
                    return skin.selectedOutlineColor;
            }
        }

        public override void ApplyDarkTheme(ThemeData theme)
        {
            skin = theme.inputField.dark;
        }

        public override void ApplyLightTheme(ThemeData theme)
        {
            skin = theme.inputField.light;
        }

        public override void UpdateVisuals()
        {
            title.text = titleText.ToLower();
            title.color = skin.textColor;
            title.enableAutoSizing = autoSizeTitle;
            title.fontSize = titleSize;
            title.fontSizeMin = .1f;
            title.fontSizeMax = titleSize;
            title.overflowMode = allowOverflow ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
            title.enableWordWrapping = !allowOverflow;
            title.transform.parent.gameObject.SetActive(useTitle);
            background.color = skin.backgroundColor;
            outline.color = skin.outlineColor;

            inputText.color = skin.textColor;
            inputText.fontSize = textSize;
            inputText.enableAutoSizing = autoSizeInput;
            inputText.fontSizeMin = .1f;
            inputText.fontSizeMax = textSize;
            inputText.horizontalAlignment = horizontalAlignment;
            inputText.verticalAlignment = verticalAlignment;
            inputText.margin = margin;

            placeholder.gameObject.SetActive(usePlaceholderText);
            placeholder.text = placeholderText;
            placeholder.color = skin.placeholderColor;
            placeholder.fontSize = textSize;
            placeholder.enableAutoSizing = autoSizeInput;
            placeholder.fontSizeMin = .1f;
            placeholder.fontSizeMax = textSize;
            placeholder.horizontalAlignment = horizontalAlignment;
            placeholder.verticalAlignment = verticalAlignment;
            placeholder.margin = margin;

            inputField.contentType = contentType;
        }

        internal void Select()
        {
            inputField.Select();
        }

        protected override void OnValidate()
        {
            if (Application.isPlaying) return;

            if (!initialized)
            {
                Initialize();
            }

            UpdateVisuals();
        }

        public enum InputMethod
        {
            Text,
            Integer,
            Decimal
        }

        [Serializable]
        public class OnValueChanged : UnityEvent<string>
        {
            public OnValueChanged OnEvent;
        }

        [Serializable]
        public class OnEndEdit : UnityEvent<string>
        {
            public OnEndEdit OnEvent;
        }

        [Serializable]
        public class OnSubmit : UnityEvent<string>
        {
            public OnSubmit OnEvent;
        }
    }
}

