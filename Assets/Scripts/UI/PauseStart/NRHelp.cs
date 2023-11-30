using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;
using NotReaper.Keyboard;
using NotReaper.UI.Components;
namespace NotReaper.UI
{
    public class NRHelp : NRMenu
    {
        [Header("References")]
        [SerializeField] private GameObject version;
        [SerializeField] private GameObject readme;
        [SerializeField] private ShortcutKeyboardHandler keyboard;

        #region Views
        [Space, Header("Views")]
        [SerializeField] private CanvasGroup shortcuts;
        [SerializeField] private CanvasGroup musicTheory;
        [SerializeField] private CanvasGroup basics;
        [SerializeField] private CanvasGroup timing;
        [SerializeField] private CanvasGroup sustains;
        [SerializeField] private CanvasGroup selection;
        [SerializeField] private CanvasGroup pathbuilder;
        [SerializeField] private CanvasGroup legacyPathbuilder;
        [SerializeField] private CanvasGroup spacingSnap;
        [SerializeField] private CanvasGroup repeater;
        [SerializeField] private CanvasGroup reviews;
        [SerializeField] private CanvasGroup bpmAlign;
        [SerializeField] private CanvasGroup presets;
        [SerializeField] private CanvasGroup bookmarks;
        [SerializeField] private CanvasGroup modifyAudio;
        [SerializeField] private CanvasGroup modifiers;
        [SerializeField] private CanvasGroup countin;
        [SerializeField] private CanvasGroup menuBrowser;
        [SerializeField] private CanvasGroup statistics;
        [SerializeField] private CanvasGroup errorChecker;
        [SerializeField] private CanvasGroup previewer;
        [SerializeField] private CanvasGroup hitsoundTimeline;
        [SerializeField] private CanvasGroup gridSize;
        #endregion

        #region Buttons
        [Space, Header("Buttons")]
        [SerializeField] private NRButton buttonShortcuts;
        [SerializeField] private NRButton buttonMusicTheory;
        [SerializeField] private NRButton buttonBasics;
        [SerializeField] private NRButton buttonTiming;
        [SerializeField] private NRButton buttonSustains;
        [SerializeField] private NRButton buttonSelection;
        [SerializeField] private NRButton buttonPathbuilder;
        [SerializeField] private NRButton buttonLegacyPathbuilder;
        [SerializeField] private NRButton buttonspacingSnap;
        [SerializeField] private NRButton buttonRepeater;
        [SerializeField] private NRButton buttonReviews;
        [SerializeField] private NRButton buttonBpmAlign;
        [SerializeField] private NRButton buttonPresets;
        [SerializeField] private NRButton buttonBookmarks;
        [SerializeField] private NRButton buttonModifyAudio;
        [SerializeField] private NRButton buttonModifiers;
        [SerializeField] private NRButton buttonCountin;
        [SerializeField] private NRButton buttonMenuBrowser;
        [SerializeField] private NRButton buttonStatistics;
        [SerializeField] private NRButton buttonErrorChecker;
        [SerializeField] private NRButton buttonPreviewer;
        [SerializeField] private NRButton buttonHitsoundTimeline;
        [SerializeField] private NRButton buttonGridSize;
        #endregion

        internal bool isOpened = false;
        private NRWindow nrWindow;
        public static NRHelp Instance { get; private set; } = null;

        private TabView tabs = new();

        protected override void Awake()
        {
            base.Awake();
            nrWindow = GetComponent<NRWindow>();
            if (Instance != null)
            {
                Debug.Log("NRHelp already exists.");
                return;
            }
            Instance = this;
        }

        // Start is called before the first frame update
        private void Start()
        {
            var t = transform;
            var position = t.localPosition;
            t.localPosition = new Vector3(0, position.y, position.z);

            TextMeshProUGUI versionLabel = version.GetComponent<TextMeshProUGUI>();
            versionLabel.text = "v beta_" + Application.version;
            GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.SetActive(false);
            keyboard.OnClose();
            isOpened = false;

            tabs.AddView(shortcuts, buttonShortcuts);
            tabs.AddView(musicTheory, buttonMusicTheory);
            tabs.AddView(basics, buttonBasics);
            tabs.AddView(timing, buttonTiming);
            tabs.AddView(sustains, buttonSustains);
            tabs.AddView(selection, buttonSelection);
            tabs.AddView(pathbuilder, buttonPathbuilder);
            tabs.AddView(legacyPathbuilder, buttonLegacyPathbuilder);
            tabs.AddView(spacingSnap, buttonspacingSnap);
            tabs.AddView(repeater, buttonRepeater);
            tabs.AddView(reviews, buttonReviews);
            tabs.AddView(bpmAlign, buttonBpmAlign);
            tabs.AddView(presets, buttonPresets);
            tabs.AddView(bookmarks, buttonBookmarks);
            tabs.AddView(modifyAudio, buttonModifyAudio);
            tabs.AddView(modifiers, buttonModifiers);
            tabs.AddView(countin, buttonCountin);
            tabs.AddView(menuBrowser, buttonMenuBrowser);
            tabs.AddView(statistics, buttonStatistics);
            tabs.AddView(errorChecker, buttonErrorChecker);
            tabs.AddView(previewer, buttonPreviewer);
            tabs.AddView(hitsoundTimeline, buttonHitsoundTimeline);
            tabs.AddView(gridSize, buttonGridSize);

            tabs.HideAllViews();
            tabs.SetDefaultView(shortcuts, buttonShortcuts);
        }

        public override void Show()
        {
            OnActivated();
            transform.position = Vector3.zero;
            nrWindow.FadeIn();
            isOpened = true;
            keyboard.OnOpen();
            CameraProvider.grid.enabled = false;
        }

        public override void Hide()
        {
            CameraProvider.grid.enabled = true;
            keyboard.OnClose();
            isOpened = false;
            var sequence = nrWindow.GetFadeOutAnimationSequence();
            sequence.OnComplete(() =>
            {
                OnDeactivated();
            });
            sequence.Play();
        }

        private void ChangeView(CanvasGroup newView, NRButton button)
        {
            if (!isOpened)
                Show();
            
            tabs.ChangeView(newView, button);
        }

        public override void ShowHelp() => ShowBasics();
        public void ShowShortcuts() => ChangeView(shortcuts, buttonShortcuts);
        public void ShowMusicTheory() => ChangeView(musicTheory, buttonMusicTheory);
        public void ShowBasics() => ChangeView(basics, buttonBasics);
        public void ShowTiming() => ChangeView(timing, buttonTiming);
        public void ShowSustains() => ChangeView(sustains, buttonSustains);
        public void ShowSelection() => ChangeView(selection, buttonSelection);
        public void ShowPathbuilder() => ChangeView(pathbuilder, buttonPathbuilder);
        public void ShowLegacyPathbuilder() => ChangeView(legacyPathbuilder, buttonLegacyPathbuilder);
        public void ShowSpacingSnap() => ChangeView(spacingSnap, buttonspacingSnap);
        public void ShowRepeater() => ChangeView(repeater, buttonRepeater);
        public void ShowReview() => ChangeView(reviews, buttonReviews);
        public void ShowBPMAlign() => ChangeView(bpmAlign, buttonBpmAlign);
        public void ShowPresets() => ChangeView(presets, buttonPresets);
        public void ShowBookmarks() => ChangeView(bookmarks, buttonBookmarks);
        public void ShowModifyAudio() => ChangeView(modifyAudio, buttonModifyAudio);
        public void ShowModifiers() => ChangeView(modifiers, buttonModifiers);
        public void ShowCountin() => ChangeView(countin, buttonCountin);
        public void ShowStatistics() => ChangeView(statistics, buttonStatistics);
        public void ShowMenuBrowser()=> ChangeView(menuBrowser, buttonMenuBrowser);
        public void ShowErrorChecker()=> ChangeView(errorChecker, buttonErrorChecker);
        public void ShowPreviewer() => ChangeView(previewer, buttonPreviewer);
        public void ShowHitsoundTimeline() => ChangeView(hitsoundTimeline, buttonHitsoundTimeline);
        public void ShowGridSize() => ChangeView(gridSize, buttonGridSize);
        protected override void OnEscPressed(InputAction.CallbackContext context) => Hide();

        public void OpenMappingGuidelines() => Application.OpenURL("https://docs.google.com/document/d/1y4cXmhvu3gOtsiHwvieQPBEQjXQtao1nTxaP5afTHPM");
    }

}
