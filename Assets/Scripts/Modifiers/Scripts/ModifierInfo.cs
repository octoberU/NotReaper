using System.Collections;
using System.Collections.Generic;
using NotReaper;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;
using NotReaper.UI.Components;
namespace NotReaper.UI
{
    public class ModifierInfo : NRMenu
    {
        public static bool isOpened = false;
        public static ModifierInfo Instance { get; private set; } = null;
        private CanvasGroup canvas;

        [Header("Views")]
        [SerializeField] private CanvasGroup generalView;
        [SerializeField] private CanvasGroup trackEditingView;
        [SerializeField] private CanvasGroup aimAssistView;
        [SerializeField] private CanvasGroup arenaChangeView;
        [SerializeField] private CanvasGroup colorView;
        [SerializeField] private CanvasGroup hiddenTelegraphsView;
        [SerializeField] private CanvasGroup invisibleGunsView;
        [SerializeField] private CanvasGroup overlaySetterView;
        [SerializeField] private CanvasGroup particlesView;
        [SerializeField] private CanvasGroup psychedeliaView;
        [SerializeField] private CanvasGroup autoLightView;
        [SerializeField] private CanvasGroup skyboxColorView;
        [SerializeField] private CanvasGroup skyboxBrightnessView;
        [SerializeField] private CanvasGroup skyboxFaderView;
        [SerializeField] private CanvasGroup skyboxLimiterView;
        [SerializeField] private CanvasGroup skyboxRotationView;
        [SerializeField] private CanvasGroup speedView;
        [SerializeField] private CanvasGroup textPopupView;
        [SerializeField] private CanvasGroup zOffsetView;
        [Space, Header("Buttons")]
        [SerializeField] private NRButton generalButton;
        [SerializeField] private NRButton trackEditingButton;
        [SerializeField] private NRButton aimAssistButton;
        [SerializeField] private NRButton arenaChangeButton;
        [SerializeField] private NRButton colorButton;
        [SerializeField] private NRButton hiddenTelegraphsButton;
        [SerializeField] private NRButton invisibleGunsButton;
        [SerializeField] private NRButton overlaySetterButton;
        [SerializeField] private NRButton particlesButton;
        [SerializeField] private NRButton psychedeliaButton;
        [SerializeField] private NRButton autoLightButton;
        [SerializeField] private NRButton skyboxColorButton;
        [SerializeField] private NRButton skyboxBrightnessButton;
        [SerializeField] private NRButton skyboxFaderButton;
        [SerializeField] private NRButton skyboxLimiterButton;
        [SerializeField] private NRButton skyboxRotationButton;
        [SerializeField] private NRButton speedButton;
        [SerializeField] private NRButton textPopupButton;
        [SerializeField] private NRButton zOffsetButton;

        private TabView tabs = new();
        protected override void Awake()
        {
            base.Awake();
            if (Instance is null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Trying to create second ModiferInfo Instance.");
                return;
            }
        }

        private void Start()
        {
            var t = transform;
            var position = t.localPosition;
            t.localPosition = new Vector3(0, position.y, position.z);
            canvas = GetComponent<CanvasGroup>();
            canvas.alpha = 0f;

            tabs.AddView(generalView, generalButton);
            tabs.AddView(trackEditingView, trackEditingButton);
            tabs.AddView(aimAssistView, aimAssistButton);
            tabs.AddView(arenaChangeView, arenaChangeButton);
            tabs.AddView(colorView, colorButton);
            tabs.AddView(hiddenTelegraphsView, hiddenTelegraphsButton);
            tabs.AddView(invisibleGunsView, invisibleGunsButton);
            tabs.AddView(overlaySetterView, overlaySetterButton);
            tabs.AddView(particlesView, particlesButton);
            tabs.AddView(psychedeliaView, psychedeliaButton);
            tabs.AddView(autoLightView, autoLightButton);
            tabs.AddView(skyboxColorView, skyboxColorButton);
            tabs.AddView(skyboxBrightnessView, skyboxBrightnessButton);
            tabs.AddView(skyboxFaderView, skyboxFaderButton);
            tabs.AddView(skyboxLimiterView, skyboxLimiterButton);
            tabs.AddView(skyboxRotationView, skyboxRotationButton);
            tabs.AddView(speedView, speedButton);
            tabs.AddView(textPopupView, textPopupButton);
            tabs.AddView(zOffsetView, zOffsetButton);

            tabs.HideAllViews();
            tabs.SetDefaultView(generalView, generalButton);

            gameObject.SetActive(false);
        }

        public override void Show()
        {
            OnActivated();
            canvas.DOFade(1f, .3f);
            transform.position = Vector3.zero;
            isOpened = true;
        }

        public override void Hide()
        {
            isOpened = false;

            canvas.DOFade(0f, .3f).OnComplete(() =>
            {
                OnDeactivated();
            });
        }

        private void ChangeView(CanvasGroup view, NRButton button)
        {
            if (!isOpened)
                Show();

            tabs.ChangeView(view, button);
        }

        public override void ShowHelp() { }

        public void ShowGeneral() => ChangeView(generalView, generalButton);
        public void ShowTrackEditing() => ChangeView(trackEditingView, trackEditingButton);
        public void ShowAimAssist() => ChangeView(aimAssistView, aimAssistButton);
        public void ShowArenaChange() => ChangeView(arenaChangeView, arenaChangeButton);
        public void ShowColor() => ChangeView(colorView, colorButton);
        public void ShowHiddenTelegraphs() => ChangeView(hiddenTelegraphsView, hiddenTelegraphsButton);
        public void ShowInvisibleGuns() => ChangeView(invisibleGunsView, invisibleGunsButton);
        public void ShowOverlaySetter() => ChangeView(overlaySetterView, overlaySetterButton);
        public void ShowParticles() => ChangeView(particlesView, particlesButton);
        public void ShowPsychedelia() => ChangeView(psychedeliaView, psychedeliaButton);
        public void ShowAutoLight() => ChangeView(autoLightView, autoLightButton);
        public void ShowSkyboxColor() => ChangeView(skyboxColorView, skyboxColorButton);
        public void ShowSkyboxBrightness() => ChangeView(skyboxBrightnessView, skyboxBrightnessButton);
        public void ShowFader() => ChangeView(skyboxFaderView, skyboxFaderButton);
        public void ShowLimiter() => ChangeView(skyboxLimiterView, skyboxLimiterButton);
        public void ShowSkyboxRotation() => ChangeView(skyboxRotationView, skyboxRotationButton);
        public void ShowSpeed() => ChangeView(speedView, speedButton);
        public void ShowTextPopup() => ChangeView(textPopupView, textPopupButton);
        public void ShowZOffset() => ChangeView(zOffsetView, zOffsetButton);

        protected override void OnEscPressed(InputAction.CallbackContext context) => Hide();
    }

}
