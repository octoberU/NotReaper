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
        [Space, Header("Views")]
        [SerializeField] private CanvasGroup shortcuts;
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
        [Space, Header("Buttons")]
        [SerializeField] private NRButton buttonShortcuts;
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

        internal bool isOpened = false;
        private NRWindow nrWindow;
        private CanvasGroup canvas;
        private CanvasGroup activeView;
        private List<CanvasGroup> views = new();
        public static NRHelp Instance { get; private set; } = null;
        protected override void Awake()
        {
            base.Awake();
            nrWindow = GetComponent<NRWindow>();
            if (Instance != null)
            {
                Debug.Log("ShortcutInfo already exists.");
                return;
            }
            Instance = this;
        }

        // Start is called before the first frame update
        private void Start()
        {
            canvas = GetComponent<CanvasGroup>();
            var t = transform;
            var position = t.localPosition;
            t.localPosition = new Vector3(0, position.y, position.z);

            TextMeshProUGUI versionLabel = version.GetComponent<TextMeshProUGUI>();
            var versionButton = version.GetComponent<Button>();
            versionLabel.text = "v beta_" + Application.version;
            GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.SetActive(false);
            keyboard.OnClose();
            isOpened = false;


            views.Add(shortcuts);
            views.Add(basics);
            views.Add(timing);
            views.Add(sustains);
            views.Add(selection);
            views.Add(pathbuilder);
            views.Add(legacyPathbuilder);
            views.Add(spacingSnap);
            views.Add(repeater);
            views.Add(reviews);
            views.Add(bpmAlign);
            views.Add(presets);
            views.Add(bookmarks);
            views.Add(modifyAudio);
            views.Add(modifiers);
            views.Add(countin);
            views.Add(menuBrowser);
            views.Add(statistics);
            views.Add(errorChecker);

            foreach(var view in views)
            {
                view.alpha = 0f;
                view.interactable = false;
                view.blocksRaycasts = false;
            }

            activeView = shortcuts;
            activeView.alpha = 1f;
            activeView.interactable = true;
            activeView.blocksRaycasts = true;
        }

        public override void Show()
        {
            OnActivated();
            transform.position = Vector3.zero;
            nrWindow.FadeIn();
            //readmeUnderline.color = NRSettings.config.leftColor;
            isOpened = true;
            keyboard.OnOpen();
        }

        public override void Hide()
        {
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
            {
                Show();
            }
            var oldView = activeView;           
            oldView.DOFade(0f, .3f);
            oldView.interactable = false;
            oldView.blocksRaycasts = false;

            newView.DOFade(1f, .3f);
            newView.interactable = true;
            newView.blocksRaycasts = true;

            activeView = newView;
            button.Select(false, false);
        }

        public override void ShowHelp() { }

        public void ShowShortcuts()
        {
            ChangeView(shortcuts, buttonShortcuts);
        }
        public void ShowBasics()
        {
            ChangeView(basics, buttonBasics);
        }
        public void ShowTiming()
        {
            ChangeView(timing, buttonTiming);
        }
        public void ShowSustains()
        {
            ChangeView(sustains, buttonSustains);
        }
        public void ShowSelection()
        {
            ChangeView(selection, buttonSelection);
        }
        public void ShowPathbuilder()
        {
            ChangeView(pathbuilder, buttonPathbuilder);
        }
        public void ShowLegacyPathbuilder()
        {
            ChangeView(legacyPathbuilder, buttonLegacyPathbuilder);
        }
        public void ShowSpacingSnap()
        {
            ChangeView(spacingSnap, buttonspacingSnap);
        }
        public void ShowRepeater()
        {
            ChangeView(repeater, buttonRepeater);
        }
        public void ShowReview()
        {
            ChangeView(reviews, buttonReviews);
        }
        public void ShowBPMAlign()
        {
            ChangeView(bpmAlign, buttonBpmAlign);
        }
        public void ShowPresets()
        {
            ChangeView(presets, buttonPresets);
        }
        public void ShowBookmarks()
        {
            ChangeView(bookmarks, buttonBookmarks);
        }
        public void ShowModifyAudio()
        {
            ChangeView(modifyAudio, buttonModifyAudio);
        }
        public void ShowModifiers()
        {
            ChangeView(modifiers, buttonModifiers);
        }
        public void ShowCountin()
        {
            ChangeView(countin, buttonCountin);
        }
        public void ShowStatistics()
        {
            ChangeView(statistics, buttonStatistics);
        }
        public void ShowMenuBrowser()
        {
            ChangeView(menuBrowser, buttonMenuBrowser);
        }
        public void ShowErrorChecker()
        {
            ChangeView(errorChecker, buttonErrorChecker);
        }
        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }
    }

}
