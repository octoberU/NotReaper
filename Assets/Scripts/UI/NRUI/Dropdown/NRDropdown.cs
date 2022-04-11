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
using NotReaper.Audio;

namespace NotReaper.UI.Components
{
    [ExecuteAlways]
    public class NRDropdown : NRThemeable, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Skin")]
        [SerializeField] private NRDropdownSkin skin;
        [Space, Header("Text")]
        [SerializeField] private float fontSize = 15f;
        [SerializeField] private float itemFontSize = 12f;
        [Space, Header("Icon Scale")]
        [SerializeField] private float iconScale = 1f;
        [Space, Header("Animation")]
        [SerializeField] private bool limitWidth;
        [SerializeField] private Vector2 maxExpandSize = new Vector2(120f, 150f);
        [SerializeField] private Vector2 expandPositionOffset = Vector2.zero;
        [SerializeField] private float animationDuration = .3f;
        [Space, Header("Index")]
        public int startIndex = 0;
        public int value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                SelectItem(_value);
            }
        }

        [SerializeField, HideInInspector] public Image contentBackground;
        [SerializeField, HideInInspector] public GameObject triggerObject;
        [SerializeField, HideInInspector] public TextMeshProUGUI selectedText;
        [SerializeField, HideInInspector] public Image[] dropdownIcons = new Image[2];
        [SerializeField, HideInInspector] public Transform itemParent;
        [SerializeField, HideInInspector] public RectTransform contentRect;
        [SerializeField, HideInInspector] public CanvasGroup selectedItemCanvas;
        [SerializeField, HideInInspector] public CanvasGroup scrollerCanvas;
        [SerializeField, HideInInspector] public RectTransform scrollerRect;
        [SerializeField, HideInInspector] public RectTransform scrollerContentRect;
        [SerializeField, HideInInspector] public Transform iconParent;

        public string valueString => items[value];

        [Space(10)]
        [SerializeField]
        public List<string> items = new List<string>();
        [Space(10)]
        public OnValueChanged onValueChanged;

        internal bool isExpanded { get; private set; }
        private int _value;
        private bool initialized;
        private bool initializedPosition;
        private Vector2 initialSize;
        private Vector2 initialPosition;
        private DropdownItem dropdownItemPrefab;
        private RectTransform dropdownRect;
        private List<DropdownItem> dropdownItems = new();
        private SoundEffects sounds;
        private Canvas canvas;

        protected override void Awake()
        {
            base.Awake();
            if (Application.isPlaying)
            {
                canvas = GetComponent<Canvas>();
                OverrideSorting(false);
            }
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            sounds = NRDependencyInjector.Get<SoundEffects>();
            dropdownRect = GetComponent<RectTransform>();
            Initialize();
            triggerObject.transform.localScale = Vector3.one * 10f;
            triggerObject.SetActive(false);
            for (int i = 0; i < items.Count; ++i)
            {
                DropdownItem item = Instantiate(dropdownItemPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                item.transform.SetParent(itemParent, false);
                item.text = items[i];
                item.dropdown = this;
                item.index = i;
                item.buttonColorBlock = skin.GetItemColorBlock();
                item.textColor = skin.textColor;
                item.buttonColorBlock = skin.GetItemColorBlock();
                item.textColor = skin.textColor;
                item.fontSize = itemFontSize;
                dropdownItems.Add(item);
            }

            selectedText.text = items[startIndex];
            StartCoroutine(DisableLayoutGroup());
        }

        private void OverrideSorting(bool enabled)
        {
            canvas.overrideSorting = enabled;
            if (enabled)
            {
                canvas.sortingOrder = 500;
            }
        }

        private IEnumerator DisableLayoutGroup()
        {
            yield return new WaitForEndOfFrame();
            //disabling just doesn't work. Unity simply reenables it again -_-
            //Destroy(GetComponent<VerticalLayoutGroup>());
            GetComponent<VerticalLayoutGroup>().enabled = false;
        }

        internal void SetValueWithoutNotify(int value)
        {
            SelectItem(value, false);
        }

        public void ToggleState()
        {
            if (isExpanded) Shrink();
            else Expand();
        }

        public void Shrink()
        {
            isExpanded = false;
            triggerObject.SetActive(false);
            var animation = DOTween.Sequence();
            animation.Append(contentRect.DOSizeDelta(initialSize, animationDuration).SetEase(Ease.OutSine));
            animation.Join(scrollerCanvas.DOFade(0f, animationDuration * .5f).SetEase(Ease.OutSine));
            animation.Join(selectedItemCanvas.DOFade(1f, animationDuration * .5f).SetEase(Ease.OutSine).SetDelay(animationDuration * .5f));
            animation.Join(contentBackground.DOColor(skin.backgroundColor, animationDuration).SetEase(Ease.OutSine));
            if (expandPositionOffset != Vector2.zero)
            {
                animation.Join(contentRect.DOLocalMove(initialPosition, animationDuration).SetEase(Ease.InBack));
            }
            animation.OnComplete(() =>
            {
                scrollerCanvas.interactable = false;
                scrollerCanvas.blocksRaycasts = false;
                selectedItemCanvas.interactable = true;
                selectedItemCanvas.blocksRaycasts = true;
                OverrideSorting(false);
            });
            animation.Play();
            sounds.PlaySound(SoundEffects.Sound.Close);
        }

        public void Expand()
        {
            if (!initializedPosition)
            {
                initializedPosition = true;
                initialSize = contentRect.sizeDelta;
                initialPosition = (Vector2)contentRect.localPosition;
            }
            OverrideSorting(true);
            isExpanded = true;
            GetComponent<VerticalLayoutGroup>().enabled = false;
            var size = maxExpandSize;
            size.x = dropdownRect.sizeDelta.x + 10f;
            if (scrollerContentRect.sizeDelta.y < size.y) size.y = scrollerContentRect.sizeDelta.y;
            if (limitWidth && size.x > maxExpandSize.x) size.x = maxExpandSize.x;
            var targetPos = initialPosition + expandPositionOffset;
            var animation = DOTween.Sequence();
            if(expandPositionOffset != Vector2.zero)
            {
                animation.Append(contentRect.DOLocalMove(targetPos, animationDuration * .5f).SetEase(Ease.OutSine));
            }
            animation.Append(contentRect.DOSizeDelta(size, animationDuration).SetEase(Ease.InSine));
            animation.Join(selectedItemCanvas.DOFade(0f, animationDuration).SetEase(Ease.InSine));
            animation.Join(scrollerCanvas.DOFade(1f, animationDuration).SetEase(Ease.InSine));
            animation.Join(contentBackground.DOColor(skin.itemBackgroundColor, animationDuration).SetEase(Ease.InSine));
            animation.Play();
            triggerObject.SetActive(true);
            scrollerCanvas.interactable = true;
            scrollerCanvas.blocksRaycasts = true;
            selectedItemCanvas.interactable = false;
            selectedItemCanvas.blocksRaycasts = false;
            sounds.PlaySound(SoundEffects.Sound.Open);
        }

        public void SelectItem(int itemIndex, bool notify = true)
        {
            selectedText.text = items[itemIndex];
            _value = itemIndex;
            if (notify)
            {
                onValueChanged?.Invoke(value);
            }
            // dropdownItems[itemIndex].OnItemSelection.Invoke();
        }

        public override void Initialize()
        {
            initialized = true;
            dropdownItemPrefab = Resources.Load<DropdownItem>("NRDropdownItem");
            triggerObject = transform.GetChild(0).gameObject;
            var selected = transform.GetChild(1).GetChild(0);
            selectedText = selected.GetChild(0).GetComponent<TextMeshProUGUI>();
            iconParent = selected.GetChild(1);
            dropdownIcons[0] = iconParent.GetChild(0).GetComponent<Image>();
            dropdownIcons[1] = iconParent.GetChild(1).GetComponent<Image>();
            selectedItemCanvas = selected.GetComponent<CanvasGroup>();
            contentRect = transform.GetChild(1).GetComponent<RectTransform>();
            contentBackground = contentRect.GetComponent<Image>();
            scrollerCanvas = contentRect.GetChild(1).GetComponent<CanvasGroup>();
            scrollerRect = scrollerCanvas.GetComponent<RectTransform>();
            scrollerContentRect = scrollerRect.GetChild(0).GetComponent<RectTransform>();
            itemParent = contentRect.GetChild(1).GetChild(0);
        }

        public override void UpdateVisuals()
        {
            selectedText.color = skin.textColor;
            selectedText.fontSizeMax = fontSize;
            selectedText.fontSize = fontSize;
            contentBackground.color = skin.backgroundColor;
            iconParent.localScale = Vector3.one * iconScale;
            foreach(var icon in dropdownIcons)
            {
                icon.color = skin.iconColor;
            }
            foreach(var item in dropdownItems)
            {
                item.buttonColorBlock = skin.GetItemColorBlock();
            }
        }

        public override void ApplyLightTheme(ThemeData theme)
        {
            skin = theme.dropdown.light;
        }

        public override void ApplyDarkTheme(ThemeData theme)
        {
            skin = theme.dropdown.dark;
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
            if (isExpanded) return;
            for (int i = 0; i < dropdownIcons.Length; i++)
            {
                var scale = new Vector3(1f, i == 0 ? 1f : -1f, 1f);
                scale *= 1.1f;
                dropdownIcons[i].transform.DOScale(scale, animationDuration);
            }
            selectedText.transform.DOScale(Vector3.one * 1.1f, animationDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isExpanded) return;
            for (int i = 0; i < dropdownIcons.Length; i++)
            {
                var scale = new Vector3(1f, i == 0 ? 1f : -1f, 1f);
                dropdownIcons[i].transform.DOScale(scale, animationDuration);
            }
            selectedText.transform.DOScale(Vector3.one, animationDuration);
        }
    }

    [Serializable]
    public class OnValueChanged : UnityEvent<int>
    {
        public OnValueChanged OnEvent;
    }
}