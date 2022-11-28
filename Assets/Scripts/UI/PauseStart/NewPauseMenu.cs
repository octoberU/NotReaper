using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;
using NotReaper;
using TMPro;
using UnityEngine.UI;
using NotReaper.Maudica;
using NotReaper.Audio;
using NotReaper.UI.Components;
using NotReaper.UI.Volume;

namespace NotReaper.UI
{
    public class NewPauseMenu : NRMenu
    {

        [Header("References")]
        [SerializeField] private GameObject volumePanel;
        [SerializeField] private GameObject maudicaMenuButton;
        [SerializeField] private Image nrStartOverlay;
        [SerializeField] private TextMeshProUGUI title;
        [Space, Header("Views")]
        [SerializeField] private View defaultView;
        [SerializeField] private View maudicaView;
        [SerializeField] private View recentsView;
        [SerializeField] private View newView;
        [SerializeField] private View browserView;
        [SerializeField] private View settingsView;
        [Space, Header("BG")]
        [SerializeField] private GameObject bg;
        [SerializeField] private GameObject pulseBG;
        [SerializeField] private GameObject logo;
        [Space, Header("Groups")]
        [SerializeField] private NRButtonGroup menuGroup;
        
        [Space, Header("Volume")]
        [SerializeField] private ScrollSlider musicSlider;
        [SerializeField] private ScrollSlider hitsoundSlider;
        [SerializeField] private ScrollSlider sustainSlider;
        [SerializeField] private ScrollSlider sfxSlider;
        [SerializeField] private HoverTextFader musicFader;
        [SerializeField] private HoverTextFader hitsoundFader;
        [SerializeField] private HoverTextFader sustainFader;
        [SerializeField] private HoverTextFader sfxFader;


        [NRInject] private Timeline timeline;
        private CanvasGroup canvas;
        private View activeView;
        private View previousView;
        private bool isInStartScreen = true;
        private bool isActive;

        protected override void Awake()
        {
            base.Awake();

            canvas = GetComponent<CanvasGroup>();
            canvas.alpha = 0f;
            defaultView.Initialize();
            recentsView.Initialize();
            newView.Initialize();
            browserView.Initialize();
            settingsView.Initialize();
            maudicaView.Initialize();
            Reset();
            transform.localPosition = Vector3.zero;
            Color c = nrStartOverlay.color;
            c.a = 1f;
            nrStartOverlay.color = c;
            //cam.enabled = false;
            Show();
        }

        private void Start()
        {
            GetComponent<Canvas>().worldCamera = CameraProvider.menu;
            pulseBG.GetComponent<Canvas>().worldCamera = CameraProvider.menu;
            bg.GetComponent<Canvas>().worldCamera = CameraProvider.menu;
            
            StartCoroutine(OnStart());
        }

        private IEnumerator OnStart()
        {
            yield return new WaitForSeconds(.5f);
            SoundEffects.Instance.PlaySound(SoundEffects.Sound.Startup);
            nrStartOverlay.DOFade(0f, 1f).OnComplete(() =>
            {
                nrStartOverlay.gameObject.SetActive(false);
            });
        }

        private void Reset()
        {
            activeView = defaultView;
            previousView = null;
            maudicaMenuButton.SetActive(false);
            defaultView.gameObject.SetActive(true);
            maudicaView.gameObject.SetActive(true);
            recentsView.gameObject.SetActive(true);
            newView.gameObject.SetActive(true);
            browserView.gameObject.SetActive(true);
            settingsView.gameObject.SetActive(true);
            volumePanel.SetActive(false);
            SetViewEnabled(defaultView, true);
            SetViewEnabled(recentsView, false);
            SetViewEnabled(newView, false);
            SetViewEnabled(browserView, false);
            SetViewEnabled(settingsView, false);
            SetViewEnabled(maudicaView, false);
            menuGroup.SetSelectedButtonToDefault();
        }

        private void OnIsInUIChanged(bool inUI)
        {
            if(isActive && !inUI)
            {
                EditorState.SetIsInUI(true);
            }
        }

        private void SetViewEnabled(View menu, bool enabled)
        {
            menu.canvas.blocksRaycasts = enabled;
            menu.canvas.alpha = enabled ? 1f : 0f;
            if (enabled) menu.canvas.interactable = true;
        }

        public override void Show()
        {
            isActive = true;
            EditorState.IsInUIChanged += OnIsInUIChanged;
            OnActivated();
            //cam.enabled = true;
            if (!isInStartScreen)
            {
                volumePanel.SetActive(true);
                maudicaMenuButton.SetActive(true);
            }
            canvas.interactable = true;
            canvas.blocksRaycasts = true;
            canvas.alpha = 1f;
            bg.SetActive(true);
            pulseBG.SetActive(true);
            logo.SetActive(true);
        }

        public override void Hide()
        {
            EditorState.IsInUIChanged -= OnIsInUIChanged;
            isActive = false;
            canvas.alpha = 0f;
            //cam.enabled = false;
            if (isInStartScreen)
            {
                isInStartScreen = false;
            }
            Reset();
            OnDeactivated();
            bg.SetActive(false);
            pulseBG.SetActive(false);
            logo.SetActive(false);
            canvas.interactable = false;
            canvas.blocksRaycasts = false;
            
            musicSlider.OnPointerExit(null);
            hitsoundSlider.OnPointerExit(null);
            sustainSlider.OnPointerExit(null);
            sfxSlider.OnPointerExit(null);
            musicFader.OnPointerExit(null);
            hitsoundFader.OnPointerExit(null);
            sustainFader.OnPointerExit(null);
            sfxFader.OnPointerExit(null);
        }

        public override void ShowHelp() { }

        public void OnOpenFile()
        {
            //StartCoroutine(timeline.LoadAudicaFile(false, null, -1, OnLoaded));
            EditorIO.SelectAudicaFile(OnLoaded);
        }

        private void OnLoaded(bool success)
        {
            if (success)
            {
                Hide();
            }
        }

        public void OnOpenPressed()
        {
            ChangeView(defaultView);
        }

        public void OnNewPressed()
        {
            ChangeView(newView);
        }

        public void OnRecentsPressed()
        {
            ChangeView(recentsView);
        }

        public void OnMaudicaPressed()
        {
            ChangeView(maudicaView);
        }

        public void OnBrowserPressed()
        {
            ChangeView(browserView);
        }

        public void OnSettingsPressed()
        {
            if(activeView == settingsView)
            {
                ChangeView(previousView);
            }
            else
            {
                menuGroup.DeselectActiveButton();
                ChangeView(settingsView);
            }
        }

        private Sequence fadeOutAnimation;
        private Sequence fadeInAnimation;
        private void ChangeView(View newView) 
        {
            if (newView == activeView) return;
            previousView = activeView;
            if (fadeOutAnimation != null)
            {
                fadeOutAnimation.Kill(true);
            }
            if (fadeInAnimation != null)
            {
                fadeInAnimation.Kill(true);
            }
            var currentView = activeView;
            activeView = newView;
            Sequence animation = DOTween.Sequence();
            animation.Append(currentView.canvas.DOFade(0f, .3f));
            animation.Join(newView.canvas.DOFade(1f, .3f));
            animation.OnComplete(() =>
            {
                currentView.Hide();
                newView.Show();
                SetViewEnabled(currentView, false);
                SetViewEnabled(newView, true);
            });
            animation.Play();
            fadeInAnimation = animation;
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            if (EditorFile.IsAudicaFileLoaded)
            {
                Hide();
            }
        }
    }
}

