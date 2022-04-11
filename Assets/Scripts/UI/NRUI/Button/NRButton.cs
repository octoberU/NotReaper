using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using NotReaper.Audio;
using UnityEngine.InputSystem;
using NotReaper.Models;

namespace NotReaper.UI.Components
{
    [ExecuteAlways]
    public class NRButton : NRThemeable, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Skin")]
        [SerializeField] private NRButtonSkin skin;
        [Space, Header("Animation")]
        [SerializeField, Range(.01f, 1f)] private float animationDuration = .25f;
        [SerializeField] private bool growOnHover;
        [SerializeField] private bool growOnClick;
        [SerializeField, Range(0f, 10f)] private float growPercentage = 1f;
        [SerializeField, Range(0f, 20f)] private float moveAmount = .1f;
        [SerializeField] private AnimationMode mode;
        [Space, Header("Sound")]
        [SerializeField] private bool playSound = true;
        [Space, Header("Background")]
        [SerializeField] private bool hideBackground;
        [Space, Header("Outline")]
        [SerializeField] private bool useOutline = true;
        [Space, Header("Underline")]
        [SerializeField] private bool useUnderline = false;
        [SerializeField] private Theme underlineTheme = Theme.OutlineColor;
        [Space, Header("Icon")]
        [SerializeField] private Sprite icon;
        [SerializeField] private bool overrideIconColors = false;
        [SerializeField] private Color highlightedColor = Color.white;
        [SerializeField] private Color pressedColor = Color.white;
        [Space, Header("Text")]
        [SerializeField] private float textSize = 15f;
        [SerializeField] private bool autoSizeText;
        [SerializeField] private string text;
        [Space, Header("Tooltip")]
        [SerializeField] private string tooltipText;
        [SerializeField] private InputActionReference keybind;
        [Space, Header("Group")]
        [SerializeField] private NRButtonGroup buttonGroup;
        [Space]

        public OnClick onClick;

        [SerializeField, HideInInspector] private Image background;
        [SerializeField, HideInInspector] private Image outline;
        [SerializeField, HideInInspector] private Image underline;
        [SerializeField, HideInInspector] private TextMeshProUGUI textContainer;
        [SerializeField, HideInInspector] private GameObject iconHolder;
        [SerializeField, HideInInspector] private Image iconDisplay;

        private bool isMouseOver = false;
        private Vector2 initialPosition;
        private Vector3 initialScale;
        private float initialRotation;
        private bool initializedPosition = false;
        private bool initialized = false;
        private bool _interactable = true;
        private bool stayOnSelected = false;
        private bool isSelected = false;
        private Action<NRButton> onSelectedAction;

        private SoundEffects effects;
        private bool hasStarted = false;
        private bool hasAppliedTheme = false;

        public bool interactable
        {
            get { return _interactable; }
            set
            {
                _interactable = value;
                SetInteractable(_interactable);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (Application.isPlaying)
            {
                initialized = true;
                underline.color = underlineTheme == Theme.OutlineColor ? skin.outlineColor : underlineTheme == Theme.LeftHand ? NRSettings.config.leftColor : underlineTheme == Theme.RightHand ? NRSettings.config.rightColor : NRSettings.config.selectedHighlightColor;
                if (underlineTheme == Theme.CurrentHandColor || underlineTheme == Theme.OppositeHandColor)
                {
                    underline.color = GetHandColorForUnderline();
                }
                initialScale = transform.localScale;
            }
            if (buttonGroup != null)
            {
                buttonGroup.RegisterButton(this);
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                effects = NRDependencyInjector.Get<SoundEffects>();

                if (!hasAppliedTheme)
                    ApplyLoadedTheme();
            }
            hasStarted = true;
        }

        internal void ApplyLoadedTheme()
        {
            hasAppliedTheme = true;
            var theme = ThemeManager.GetSelectedTheme();
            if (theme == null)
                return;

            if (ThemeManager.SelectedMode == ThemeMode.Light)
                skin = ThemeManager.GetSelectedTheme().button.light;
            else
                skin = ThemeManager.GetSelectedTheme().button.dark;

            UpdateVisuals();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                SetInteractable(interactable);
            }
        }

#if UNITY_EDITOR
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (buttonGroup != null)
            {
                buttonGroup.UnregisterButton(this);
            }
        }
#endif

        public override void Initialize()
        {
            initialized = true;
            background = transform.GetChild(0).GetComponent<Image>();
            underline = background.transform.GetChild(1).GetComponent<Image>();
            outline = background.transform.GetChild(0).GetComponent<Image>();
            textContainer = outline.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            iconHolder = outline.transform.GetChild(1).gameObject;
            iconDisplay = iconHolder.transform.GetChild(0).GetComponent<Image>();
        }

        protected override void OnValidate()
        {
            if (Application.isPlaying) return;

            if (!initialized)
            {
                Initialize();
            }

            if (buttonGroup != null)
            {
                buttonGroup.RegisterButton(this);
            }

            UpdateVisuals();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            if (initialized)
            {
                if (!stayOnSelected)
                {
                    background.transform.DOKill();
                    background.DOKill();
                    isMouseOver = false;
                    isSelected = false;
                }
                background.transform.localPosition = initialPosition;
                background.transform.localScale = initialScale;
                background.color = skin.defaultColor;
                if (hideBackground)
                {
                    Color col = background.color;
                    col.a = 0;
                    background.color = col;
                }
                var scale = underline.transform.localScale;
                scale.x = .3f;
                underline.transform.localScale = scale;

                iconDisplay.transform.localRotation = Quaternion.Euler(0f, 0f, initialRotation);

            }
            ToolTips.I.SetText("");
        }

        private void SetInteractable(bool interactable)
        {
            if (!interactable)
            {
                background.color = skin.disabledColor;
                if (hasStarted)
                {
                    DoIconColorTransition(skin.iconDisabledColor);
                    DoTextColorTransition(skin.iconDisabledColor);
                }
                else 
                {
                    iconDisplay.color = skin.iconDisabledColor;
                    textContainer.color = skin.iconDisabledColor;
                }
            }
            else
            {
                if (hasStarted)
                {
                    DoIconColorTransition(skin.defaultIconColor);
                    DoTextColorTransition(skin.textColor);
                }
                else
                {
                    iconDisplay.color = skin.defaultIconColor;
                    textContainer.color = skin.textColor;
                }

                if (isMouseOver)
                {
                    background.color = skin.highlightedColor;
                }
                else if (isSelected)
                {
                    background.color = skin.pressedColor;
                }
                else
                {
                    background.color = skin.defaultColor;
                }
            }

            if (hideBackground)
            {
                var color = skin.defaultColor;
                color.a = 0;
                background.color = color;
            }
        }

        public override void UpdateVisuals()
        {
            if (buttonGroup != null)
            {
                skin = buttonGroup.skin;
                animationDuration = buttonGroup.animationDuration;
                growOnHover = buttonGroup.growOnHover;
                growOnClick = buttonGroup.growOnClick;
                growPercentage = buttonGroup.growPercentage;
                moveAmount = buttonGroup.moveAmount;
                mode = buttonGroup.mode;
                useOutline = buttonGroup.useOutline;
                useUnderline = buttonGroup.useUnderline;
                underlineTheme = buttonGroup.underlineTheme;
                autoSizeText = buttonGroup.autoSizeText;
                textSize = buttonGroup.textSize;
                hideBackground = buttonGroup.hideBackground;
                highlightedColor = buttonGroup.highlightedColor;
                pressedColor = buttonGroup.pressedColor;
                stayOnSelected = buttonGroup.stayOnSelected;
                overrideIconColors = buttonGroup.overrideIconColors;
            }
            background.color = skin.defaultColor;
            if (hideBackground)
            {
                background.enabled = true;
                var color = background.color;
                color.a = 0f;
                background.color = color;
            }
            //background.enabled = !hideBackground;

            outline.enabled = useOutline;
            outline.color = skin.outlineColor;

            underline.gameObject.SetActive(useUnderline);
            underline.enabled = useUnderline;
            underline.color = underlineTheme == Theme.OutlineColor ? skin.outlineColor : underlineTheme == Theme.LeftHand ? NRSettings.config.leftColor : underlineTheme == Theme.RightHand ? NRSettings.config.rightColor : NRSettings.config.selectedHighlightColor;
            if (underlineTheme == Theme.CurrentHandColor || underlineTheme == Theme.OppositeHandColor)
            {
                underline.color = GetHandColorForUnderline();
            }
            if (icon == null)
            {
                iconHolder.SetActive(false);
                textContainer.gameObject.SetActive(true);
                textContainer.text = text.ToLower();
                textContainer.color = skin.textColor;
                textContainer.enableAutoSizing = autoSizeText;
                textContainer.fontSizeMax = textSize;
                textContainer.fontSizeMin = 0.1f;
                textContainer.fontSize = textSize;


            }
            else
            {
                iconHolder.SetActive(true);
                iconDisplay.sprite = icon;
                iconDisplay.color = skin.defaultIconColor;
                textContainer.gameObject.SetActive(false);
            }

        }

        public void SetText(string text)
        {
            this.text = text;
            textContainer.text = text.ToLower();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {

            if (keybind != null)
            {
                ToolTips.I.SetText(keybind);
            }
            else if (!string.IsNullOrEmpty(tooltipText))
            {
                ToolTips.I.SetText(tooltipText);
            }
            if (!interactable || isSelected) return;
            isMouseOver = true;
            DoBackgroundColorTransition(skin.highlightedColor);
            DoIconColorTransition(overrideIconColors ? highlightedColor : skin.defaultIconColor);
            DoUnderlineTransition(.9f);
            DoMove(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {

            ToolTips.I.SetText("");
            if (!interactable || isSelected) return;

            isMouseOver = false;
            DoBackgroundColorTransition(skin.defaultColor);
            DoIconColorTransition(skin.defaultIconColor);
            DoUnderlineTransition(.3f);
            DoMove(false);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!interactable || isSelected) return;

            DoBackgroundColorTransition(isMouseOver ? skin.highlightedColor : skin.defaultColor);
            DoIconColorTransition(isMouseOver ? (overrideIconColors ? highlightedColor : skin.defaultIconColor) : skin.defaultIconColor);
            if (growOnClick)
            {
                GrowOnClick(false);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Select();
        }

        public void Select(bool playSound = true, bool notify = true)
        {
            if (!interactable || isSelected) return;

            DoBackgroundColorTransition(skin.pressedColor);
            DoIconColorTransition(overrideIconColors ? pressedColor : skin.defaultIconColor);
            if (growOnClick)
            {
                GrowOnClick(true);
            }
            if (stayOnSelected)
            {
                if (buttonGroup != null)
                {
                    buttonGroup.SetSelectedButton(this);
                }
                else
                {
                    onSelectedAction?.Invoke(this);
                }
                isSelected = true;
            }
            if (playSound && this.playSound)
            {
                effects.PlaySound(SoundEffects.Sound.Click);
            }
            if (notify)
            {
                onClick.Invoke();
            }
        }

        public void OverrideStayOnSelected(bool stay, Action<NRButton> onSelectedAction)
        {
            stayOnSelected = stay;
            this.onSelectedAction = onSelectedAction;
        }

        public void SetDefaultSelected()
        {
            if (buttonGroup != null)
            {
                buttonGroup.SetSelectedButton(this);
            }
            else
            {
                onSelectedAction?.Invoke(this);
            }
            isSelected = true;
            DoBackgroundColorTransition(skin.pressedColor, true);
        }

        public void Deselect()
        {
            isSelected = false;
            OnPointerExit(null);
        }

        private void DoUnderlineTransition(float scale)
        {
            underline.transform.DOKill();
            underline.transform.DOScaleX(scale, animationDuration);
        }

        private void DoBackgroundColorTransition(Color newColor, bool immediate = false)
        {
            if (hideBackground) return;

            if (immediate)
            {
                background.color = newColor;
            }
            else
            {
                background.DOKill();
                background.DOColor(newColor, animationDuration);
            }
        }

        private void DoTextColorTransition(Color color)
        {
            textContainer.DOKill();
            textContainer.DOColor(color, animationDuration);
        }

        private void DoIconColorTransition(Color newColor)
        {
            if (icon != null)
            {
                iconDisplay.DOKill();
                iconDisplay.DOColor(newColor, animationDuration);
            }
        }

        private void DoMove(bool hover, bool immediate = false)
        {
            background.transform.DOKill();
            if (!initializedPosition)
            {
                initializedPosition = true;
                initialScale = background.transform.localScale;
                initialPosition = background.transform.localPosition;
                initialRotation = iconDisplay.transform.localRotation.z;
            }
            if (growOnHover)
            {
                float amount = Mathf.Abs(initialScale.x * (growPercentage * .01f));
                Vector3 growAmount = new Vector3(Mathf.Sign(initialScale.x) * amount, Mathf.Sign(initialScale.y) * amount, Mathf.Sign(initialScale.z) * amount);
                if (immediate)
                {
                    background.transform.localScale = hover ? initialScale : initialScale + growAmount;
                }
                else
                {
                    background.transform.DOScale(hover ? initialScale + growAmount : initialScale, animationDuration);
                }
            }

            if (mode == AnimationMode.Spin && icon != null)
            {
                iconDisplay.transform.DOKill();
                iconDisplay.transform.DOLocalRotate(new Vector3(0f, 0f, hover ? 90f : initialRotation), animationDuration, RotateMode.Fast);
            }
            else if (mode != AnimationMode.None)
            {

                Vector2 position = initialPosition;
                if (hover)
                {
                    position.x += mode == AnimationMode.MoveLeft ? -moveAmount : mode == AnimationMode.MoveRight ? moveAmount : 0f;
                    position.y += mode == AnimationMode.MoveUp ? moveAmount : mode == AnimationMode.MoveDown ? -moveAmount : 0f;
                }
                if (immediate)
                {
                    background.transform.localPosition = position;
                }
                else
                {
                    background.transform.DOLocalMove(position, animationDuration);
                }
            }
        }

        private void GrowOnClick(bool clickDown, bool immediate = false)
        {
            background.transform.DOKill();
            Vector3 target = GetGrowOnClickAmount(clickDown);
            if (immediate)
            {
                background.transform.localScale = target;
            }
            else
            {
                background.transform.DOScale(target, animationDuration);
            }
        }

        private Vector3 GetGrowOnClickAmount(bool clickDown)
        {
            float amount = Mathf.Abs(initialScale.x * (growPercentage * .01f));
            Vector3 growAmount = new Vector3(Mathf.Sign(initialScale.x) * amount, Mathf.Sign(initialScale.y) * amount, Mathf.Sign(initialScale.z) * amount);
            return clickDown ? growOnHover ? initialScale + (growAmount * 2f) : initialScale + growAmount : growOnHover ? initialScale + growAmount : initialScale;
        }

        [NRListener]
        private void OnHandChanged(TargetHandType _)
        {
            if (useUnderline && (underlineTheme == Theme.CurrentHandColor || underlineTheme == Theme.OppositeHandColor))
            {
                underline.DOColor(GetHandColorForUnderline(), animationDuration);
            }
        }

        private Color GetHandColorForUnderline()
        {
            if (underlineTheme == Theme.OppositeHandColor)
            {
                return EditorState.Hand.Current == TargetHandType.Left ? NRSettings.config.rightColor : NRSettings.config.leftColor;
            }
            else
            {
                return EditorState.Hand.Current == TargetHandType.Left ? NRSettings.config.leftColor : NRSettings.config.rightColor;
            }
        }

        public override void ApplyLightTheme(ThemeData theme)
        {
            if (buttonGroup != null)
            {
                buttonGroup.skin = theme.button.light;
            }

            skin = theme.button.light;
            hasAppliedTheme = true;
        }

        public override void ApplyDarkTheme(ThemeData theme)
        {
            if (buttonGroup != null)
            {
                buttonGroup.skin = theme.button.dark;
            }

            skin = theme.button.dark;
            hasAppliedTheme = true;
        }
    }

    public enum AnimationMode
    {
        None,
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        Spin
    }

    public enum Theme
    {
        OutlineColor,
        LeftHand,
        RightHand,
        Selected,
        CurrentHandColor,
        OppositeHandColor
    }

    [Serializable]
    public class OnClick : UnityEvent
    {
        public OnClick OnEvent;
    }
}

