using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;
using NotReaper.UI.Components.Dropdown;
using System.Linq;
using UnityEngine.EventSystems;
using System;

namespace NotReaper.UI.Components
{
    [ExecuteAlways]
    public class NRButtonPrompt : NRThemeable, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Skin")]
        [SerializeField] private NRButtonPromptSkin skin;
        [Space, Header("Text")]
        [SerializeField] private string buttonText = "button";
        [SerializeField] private string promptText = "are you sure?";
        [SerializeField] private float fontSize = 15f;
        [Space, Header("Background")]
        [SerializeField] private bool hideBackground = false;
        [Space, Header("Icon Scale")]
        [SerializeField] private Sprite icon;
        [SerializeField] private float iconScale = 1f;
        [Space, Header("Animation")]
        [SerializeField] private float animationDuration = .3f;

        [SerializeField, HideInInspector] public Image contentBackground;
        [SerializeField, HideInInspector] public GameObject triggerObject;
        [SerializeField, HideInInspector] public TextMeshProUGUI buttonTextContainer;
        [SerializeField, HideInInspector] public Transform itemParent;
        [SerializeField, HideInInspector] public RectTransform contentRect;
        [SerializeField, HideInInspector] public CanvasGroup selectedItemCanvas;
        [SerializeField, HideInInspector] public CanvasGroup scrollerCanvas;
        [SerializeField, HideInInspector] public RectTransform scrollerRect;
        [SerializeField, HideInInspector] public RectTransform scrollerContentRect;
        [SerializeField, HideInInspector] public GameObject iconHolder;
        [SerializeField, HideInInspector] public Image iconDisplay;
        [SerializeField, HideInInspector] public TextMeshProUGUI promptTextContainer;

        [Space(10)]
        public OnClick onPromptOpened;
        public OnClick onConfirm;
        public OnClick onCancel;

        internal bool isExpanded;
        private bool initialized;
        private bool initializedPosition;
        private Vector2 initialSize;
        private Button button;
        internal bool interactable
        {
            get
            {
                return button.interactable;
            }
            set
            {
                button.interactable = value;
                UpdateBackground();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (Application.isPlaying)
            {
                button = GetComponent<Button>();
            }
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            Initialize();
            triggerObject.SetActive(false);
            StartCoroutine(DisableLayoutGroup());
        }

        private IEnumerator DisableLayoutGroup()
        {
            yield return new WaitForEndOfFrame();
            //disabling just doesn't work. Unity simply reenables it again -_-
            //Destroy(GetComponent<VerticalLayoutGroup>());
            GetComponent<VerticalLayoutGroup>().enabled = false;
        }

        public void ToggleState()
        {
            if (isAnimating) return;
            if (isExpanded)
            {
                Shrink();
                onCancel?.Invoke();
            }
            else Expand();
        }
        private bool isAnimating = false;
        public void Shrink()
        {
            isAnimating = true;
            isExpanded = false;
            triggerObject.SetActive(false);

            var animation = DOTween.Sequence();
            animation.Append(contentRect.DOSizeDelta(initialSize, animationDuration));
            animation.Join(scrollerCanvas.DOFade(0f, animationDuration * .5f));
            animation.Join(selectedItemCanvas.DOFade(1f, animationDuration * .5f).SetDelay(animationDuration * .5f));
            animation.Join(contentBackground.DOColor(hideBackground ? GetFadedColor(skin.backgroundColor) : skin.backgroundColor, animationDuration));
            animation.SetEase(Ease.InSine);
            animation.OnComplete(() =>
            {
                scrollerCanvas.interactable = false;
                scrollerCanvas.blocksRaycasts = false;
                selectedItemCanvas.interactable = true;
                selectedItemCanvas.blocksRaycasts = true;
                isAnimating = false;
            });
        }

        public void Expand()
        {
            isAnimating = true;
            if (!initializedPosition)
            {
                initializedPosition = true;
                initialSize = contentRect.sizeDelta;
            }
            isExpanded = true;
            GetComponent<VerticalLayoutGroup>().enabled = false;
            var animation = DOTween.Sequence();
            animation.Append(contentRect.DOSizeDelta(scrollerContentRect.sizeDelta, animationDuration));
            animation.Join(selectedItemCanvas.DOFade(0f, animationDuration * .5f));
            animation.Join(scrollerCanvas.DOFade(1f, animationDuration * .5f).SetDelay(animationDuration * .5f));
            animation.Join(contentBackground.DOColor(skin.itemBackgroundColor, animationDuration));
            animation.SetEase(Ease.OutSine);
            animation.OnComplete(() => isAnimating = false);
            triggerObject.SetActive(true);
            scrollerCanvas.interactable = true;
            scrollerCanvas.blocksRaycasts = true;
            selectedItemCanvas.interactable = false;
            selectedItemCanvas.blocksRaycasts = false;
            onPromptOpened?.Invoke();
        }

        public override void Initialize()
        {
            initialized = true;
            triggerObject = transform.GetChild(0).gameObject;
            var button = transform.GetChild(1).GetChild(0);
            buttonTextContainer = button.GetChild(0).GetComponent<TextMeshProUGUI>();
            iconHolder = button.GetChild(1).gameObject;
            iconDisplay = iconHolder.GetComponentInChildren<Image>();
            selectedItemCanvas = button.GetComponent<CanvasGroup>();
            contentRect = transform.GetChild(1).GetComponent<RectTransform>();
            contentBackground = contentRect.GetComponent<Image>();
            scrollerCanvas = contentRect.GetChild(1).GetComponent<CanvasGroup>();
            scrollerRect = scrollerCanvas.GetComponent<RectTransform>();
            scrollerContentRect = scrollerRect.GetChild(0).GetComponent<RectTransform>();
            itemParent = contentRect.GetChild(1).GetChild(0);
            promptTextContainer = scrollerContentRect.GetChild(0).GetComponent<TextMeshProUGUI>();
        }

        public override void UpdateVisuals()
        {
            Initialize();
            buttonTextContainer.color = skin.textColor;
            buttonTextContainer.fontSizeMax = fontSize;
            buttonTextContainer.fontSize = fontSize;
            buttonTextContainer.text = buttonText.ToLower();
            contentBackground.color = skin.backgroundColor;
            iconHolder.transform.localScale = Vector3.one * iconScale;
            if (hideBackground)
            {
                contentBackground.color = GetFadedColor(skin.backgroundColor);
            }
            if(icon == null)
            {
                buttonTextContainer.gameObject.SetActive(true);
                iconHolder.SetActive(false);
            }
            else
            {
                buttonTextContainer.gameObject.SetActive(false);
                iconHolder.SetActive(true);
                iconDisplay.sprite = icon;
                iconDisplay.SetNativeSize();
            }
            promptTextContainer.text = promptText.ToLower();
        }

        internal void SetPromptText(string text)
        {
            promptTextContainer.text = text.ToLower();
        }

        private Color GetFadedColor(Color color)
        {
            color.a = 0f;
            return color;
        }

        private void UpdateBackground()
        {
            if (hideBackground)
            {
                buttonTextContainer.color = interactable ? skin.textColor : skin.disabledColor;
                iconDisplay.color = interactable ? skin.iconColor : skin.disabledColor;
            }
            else
            {
                contentBackground.color = interactable ? skin.backgroundColor : skin.disabledColor;
            }
        }

        public void OnConfirm()
        {
            Shrink();
            onConfirm?.Invoke();
        }

        public void OnCancel()
        {
            Shrink();
            onCancel?.Invoke();
        }

        public override void ApplyLightTheme(ThemeData theme)
        {
            skin = theme.buttonPrompt.light;
        }

        public override void ApplyDarkTheme(ThemeData theme)
        {
            skin = theme.buttonPrompt.dark;
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactable) return;
            var scale = Vector3.one * 1.1f;
            iconHolder.transform.DOScale(scale, animationDuration);
            buttonTextContainer.transform.DOScale(scale, animationDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!interactable) return;
            var scale = Vector3.one;
            iconHolder.transform.DOScale(scale, animationDuration);
            buttonTextContainer.transform.DOScale(scale, animationDuration);
        }
    }
}