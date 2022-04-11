using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.UI.Components
{
    [RequireComponent(typeof(CanvasGroup))]
    public class NRWindow : NRThemeable
    {
        [Header("Skin")]
        [SerializeField] private NRWindowSkin skin;
        [Space, Header("Close Button")]
        [SerializeField] private bool useCloseButton = true;
        [SerializeField] private Location closeButtonLocation = Location.TopRight;
        [SerializeField] private float margin = 2f;
        [Space, Header("Animation")]
        [SerializeField] private float fadeDuration = .3f;


        #region References
        [HideInInspector, SerializeField] public CanvasGroup canvas;
        [HideInInspector, SerializeField] public NRButton closeButton;
        [HideInInspector, SerializeField] public RectTransform buttonRect;
        [HideInInspector, SerializeField] private List<NRBlur> blurs = new();
        private bool initialized;
        #endregion

        protected override void Awake()
        {
            base.Awake();
        }
        private void Start()
        {
            if (Application.isPlaying)
            {
                foreach (var blur in blurs)
                {
                    blur.SetBlurOpactiy(0f);
                }
                UpdateVisuals();
                StartCoroutine(UpdateLayout());
            }
        }

        private IEnumerator UpdateLayout()
        {
            yield return new WaitForEndOfFrame();
            var layouts = GetComponentsInChildren<LayoutGroup>();
            var fitters = GetComponentsInChildren<ContentSizeFitter>();
            foreach (var layout in layouts)
            {
                layout.enabled = false;
                layout.enabled = true;
                layout.SetLayoutVertical();
                layout.SetLayoutHorizontal();
                LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
            }
            foreach(var fitter in fitters)
            {
                fitter.enabled = false;
                fitter.enabled = true;
                fitter.SetLayoutVertical();
                fitter.SetLayoutHorizontal();
                LayoutRebuilder.ForceRebuildLayoutImmediate(fitter.GetComponent<RectTransform>());
            }
            //LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }

        public void RegisterBlur(NRBlur blur)
        {
            if (!blurs.Contains(blur))
            {
                blurs.Add(blur);
            }
        }

        public void UnregisterBlur(NRBlur blur)
        {
            if (blurs.Contains(blur))
            {
                blurs.Remove(blur);
            }
        }

        public Sequence GetFadeInAnimationSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Append(canvas.DOFade(1f, fadeDuration));
            return sequence;
        }

        public Sequence GetFadeOutAnimationSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Append(canvas.DOFade(0f, fadeDuration));
            return sequence;
        }

        public void FadeIn()
        {
            canvas.DOFade(1f, fadeDuration);
            foreach (var blur in blurs)
            {
                blur.FadeBlur(1f, fadeDuration);
            }
        }

        public void FadeOut()
        {
            canvas.DOFade(0f, fadeDuration);
            foreach (var blur in blurs)
            {
                blur.FadeBlur(0f, fadeDuration);
            }
        }

        public override void Initialize()
        {
            initialized = true;
            canvas = GetComponent<CanvasGroup>();
            closeButton = transform.Find("Close").GetComponent<NRButton>();
            buttonRect = closeButton.GetComponent<RectTransform>();
        }
        public override void UpdateVisuals()
        {
            closeButton.gameObject.SetActive(useCloseButton);
            foreach (var blur in blurs)
            {
                blur.SetBackgroundColor(skin.backgroundColor);
                blur.SetBlurOpactiy(skin.blurOpacity);
                blur.SetBlurEnabled(skin.blurEnabled);
            }
            if (useCloseButton)
            {
                switch (closeButtonLocation)
                {
                    case Location.TopLeft:
                        buttonRect.anchorMin = new Vector2(0, 1);
                        buttonRect.anchorMax = new Vector2(0, 1);
                        buttonRect.pivot = new Vector2(0, 1);
                        buttonRect.anchoredPosition = new Vector2(margin, -margin);
                        break;
                    case Location.TopCenter:
                        buttonRect.anchorMin = new Vector2(.5f, 1f);
                        buttonRect.anchorMax = new Vector2(.5f, 1f);
                        buttonRect.pivot = new Vector2(.5f, 1f);
                        buttonRect.anchoredPosition = new Vector2(0f, -margin);
                        break;
                    case Location.TopRight:
                        buttonRect.anchorMin = new Vector2(1f, 1f);
                        buttonRect.anchorMax = new Vector2(1f, 1f);
                        buttonRect.pivot = Vector2.one;
                        buttonRect.anchoredPosition = new Vector2(-margin, -margin);
                        break;
                }
            }
            UpdateTextColor();
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
        public override void ApplyDarkTheme(ThemeData theme)
        {
            skin = theme.window.dark;
        }

        public override void ApplyLightTheme(ThemeData theme)
        {
            skin = theme.window.light;
        }

        private void UpdateTextColor()
        {
            var components = transform.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in components)
            {
                if (text.transform.parent != null)
                {
                    if (text.transform.parent.parent != null)
                    {
                        if (text.transform.parent.parent.parent != null)
                        {
                            if (text.transform.parent.parent.parent.GetComponent<NRThemeable>() != null)
                            {
                                continue;
                            }
                        }
                    }
                }
                text.color = skin.textColor;
            }
        }

        public enum Location
        {
            TopLeft,
            TopCenter,
            TopRight
        }

    }
}

