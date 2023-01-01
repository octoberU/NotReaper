using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NotReaper.UI.Components
{
    [ExecuteAlways]
    public class NRIconInputField : NRThemeable, ITabbable
    {
        [Header("Skin")]
        [SerializeField] private NRIconInputFieldSkin skin;
        [Space, Header("Title")]
        [SerializeField] private string titleText = "";
        [SerializeField] private float titleSize = 12f;
        [SerializeField] private bool autoSizeTitle = false;
        [SerializeField] private bool useTitle = true;
        [Space, Header("Input")]
        [SerializeField] private string placeholderText = "";
        [SerializeField] private float textSize = 12f;
        [SerializeField] private bool usePlaceholderText = false;
        [SerializeField] private bool autoSizeInput = false;
        [SerializeField] private HorizontalAlignmentOptions horizontalAlignment = HorizontalAlignmentOptions.Left;
        [SerializeField] private VerticalAlignmentOptions verticalAlignment = VerticalAlignmentOptions.Capline;
        [SerializeField] private Vector4 margin = new Vector4(5f, 0f, 0f, 0f);
        [Space, Header("Input Method")]
        [SerializeField] private TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard;
        [Space, Header("Animation")]
        [SerializeField] private float animationDuration = .3f;
        [Space, Header("Icon")]
        [SerializeField] private Sprite icon;
        [Space, Header("Callbacks")]
        [SerializeField] public OnValueChanged onValueChanged;
        [HideInInspector, SerializeField] public TextMeshProUGUI title;
        [HideInInspector, SerializeField] public TMP_InputField inputField;
        [HideInInspector, SerializeField] public GameObject group;
        [HideInInspector, SerializeField] public TextMeshProUGUI placeholder;
        [HideInInspector, SerializeField] public TextMeshProUGUI inputText;
        [HideInInspector, SerializeField] public Image background;
        [HideInInspector, SerializeField] public Image outline;
        [HideInInspector, SerializeField] public Image circle;
        [HideInInspector, SerializeField] public Image iconDisplay;
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

        private bool initialized;
        public int Index { get; set; }
        public bool IsFocused { get; set; }

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                //UpdateVisuals();
                inputField.onValueChanged.AddListener(ValueChanged);
                inputField.onSelect.AddListener(OnSelected);
                inputField.onDeselect.AddListener(OnDeselected);
            }
        }

        public override void Initialize()
        {
            title = transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
            inputField = transform.GetChild(1).GetComponent<TMP_InputField>();
            group = inputField.transform.GetChild(0).gameObject;
            background = group.transform.GetChild(0).GetComponent<Image>();
            outline = background.transform.GetChild(0).GetComponent<Image>();
            placeholder = outline.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
            inputText = outline.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();
            circle = group.transform.GetChild(1).GetComponent<Image>();
            iconDisplay = circle.transform.GetChild(0).GetComponent<Image>();
        }

        public void Focus() => inputField.Select();

        private void SetText(string text)
        {
            inputField.SetTextWithoutNotify(text);
        }

        private void ValueChanged(string text)
        {
            onValueChanged?.Invoke(text);
        }

        private void OnSelected(string _)
        {
            IsFocused = true;
            var color = GetOutlineColor();
            outline.DOColor(color, animationDuration);
            circle.DOColor(color, animationDuration);
            title.DOColor(color, animationDuration);
        }

        private void OnDeselected(string _)
        {
            IsFocused = false;
            outline.DOColor(skin.outlineColor, animationDuration);
            circle.DOColor(skin.outlineColor, animationDuration);
            title.DOColor(skin.textColor, animationDuration);
        }

        private void OnDisable()
        {
            outline.color = skin.outlineColor;
            circle.color = skin.outlineColor;
        }

        private Color GetOutlineColor()
        {
            switch (skin.outlineColorMode)
            {
                case NRIconInputFieldSkin.OutlineColorMode.Custom:
                    return skin.selectedOutlineColor;
                case NRIconInputFieldSkin.OutlineColorMode.CurrentHand:
                    return EditorState.Hand.Current == Models.TargetHandType.Left ? NRSettings.config.leftColor : NRSettings.config.rightColor;
                case NRIconInputFieldSkin.OutlineColorMode.OppositeHand:
                    return EditorState.Hand.Current == Models.TargetHandType.Left ? NRSettings.config.rightColor : NRSettings.config.leftColor;
                case NRIconInputFieldSkin.OutlineColorMode.LeftHand:
                    return NRSettings.config.leftColor;
                case NRIconInputFieldSkin.OutlineColorMode.RightHand:
                    return NRSettings.config.rightColor;
                default:
                    return skin.selectedOutlineColor;
            }
        }

        public override void ApplyDarkTheme(ThemeData theme)
        {
            skin = theme.iconinputField.dark;
        }

        public override void ApplyLightTheme(ThemeData theme)
        {
            skin = theme.iconinputField.light;
        }

        public override void UpdateVisuals()
        {
            title.text = titleText.ToLower();
            title.color = skin.textColor;
            title.enableAutoSizing = autoSizeTitle;
            title.fontSize = titleSize;
            title.fontSizeMin = .1f;
            title.fontSizeMax = titleSize;
            title.transform.parent.gameObject.SetActive(useTitle);
            background.color = skin.backgroundColor;
            outline.color = skin.outlineColor;
            circle.color = skin.outlineColor;

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
            if (icon != null)
            {
                iconDisplay.sprite = icon;
                iconDisplay.color = skin.iconColor;
            }
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
    }
}

