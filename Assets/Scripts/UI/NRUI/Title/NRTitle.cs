using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.UI.Components
{
    public class NRTitle : NRThemeable
    {
        [Header("Skin")]
        [SerializeField] private NRTitleSkin skin;
        [Space, Header("Text")]
        [SerializeField] private string title = "";
        [SerializeField] private float fontSize = 20f;
        [Space, Header("Underline")]
        [SerializeField] private bool useUnderline = true;
        //[Space, Header("Animation")]
        //[SerializeField] private float animationDuration = .3f;

        [HideInInspector, SerializeField] public TextMeshProUGUI textContainer;
        [HideInInspector, SerializeField] public Image underline;

        private bool initialized = false;
        private VerticalLayoutGroup layout;
        private ContentSizeFitter fitter;

        protected override void Awake()
        {
            base.Awake();
            layout = GetComponent<VerticalLayoutGroup>();
            fitter = GetComponent<ContentSizeFitter>();
        }
        private void Start()
        {
            if (Application.isPlaying)
            {
                //fitter.enabled = false;
                //UpdateVisuals();
                StartCoroutine(UpdateLayout());
            }
        }

        private IEnumerator UpdateLayout()
        {
            layout.enabled = false;
            yield return new WaitForEndOfFrame();
            layout.enabled = true;
            //fitter.enabled = false;
        }

        /*private void OnEnable()
        {
            underline.transform.DOScaleX(1f, animationDuration).SetEase(Ease.OutBack).SetDelay(.5f);
            
        }

        private void OnDisable()
        {
            var scale = underline.transform.localScale;
            scale.x = 0f;
            underline.transform.localScale = scale;
        }*/

        public override void ApplyDarkTheme(ThemeData theme)
        {
            skin = theme.title.dark;
        }

        public override void ApplyLightTheme(ThemeData theme)
        {
            skin = theme.title.light;
        }

        public override void Initialize()
        {
            initialized = true;
            textContainer = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            underline = transform.GetChild(1).GetComponent<Image>();
        }

        public override void UpdateVisuals()
        {
            textContainer.font = skin.font;
            textContainer.text = title.ToLower();
            textContainer.fontSize = fontSize;
            textContainer.color = skin.textColor;
            underline.gameObject.SetActive(useUnderline);
            underline.color = GetUnderlineColor();
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

        private Color GetUnderlineColor()
        {
            switch (skin.underlineColorMode)
            {
                case NRTitleSkin.UnderlineColor.Custom:
                    return skin.underlineColor;
                case NRTitleSkin.UnderlineColor.CurrentHand:
                    return EditorState.Hand.Current == Models.TargetHandType.Left ? NRSettings.config.leftColor : NRSettings.config.rightColor;
                case NRTitleSkin.UnderlineColor.OppositeHand:
                    return EditorState.Hand.Current == Models.TargetHandType.Left ? NRSettings.config.rightColor : NRSettings.config.leftColor;
                case NRTitleSkin.UnderlineColor.LeftHand:
                    return NRSettings.config.leftColor;
                case NRTitleSkin.UnderlineColor.RightHand:
                    return NRSettings.config.rightColor;
                default:
                    return skin.underlineColor;
            }
        }
    }
}

