using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.InputSystem;
using NotReaper.Timing;
using System;
using NotReaper.UI.Components;
using NotReaper.UI;

namespace NotReaper.BpmAlign
{
    public class BPMDragView : NRMenu
    {
        [Header("References")]
        [SerializeField] private BPMDragAlign dragAlign;
        [SerializeField] private GameObject loadingOverlay;
        [Space, Header("UI Elements")]
        [SerializeField] private NRInputField bpmInput;
        [SerializeField] private NRInputField nominatorInput;
        [SerializeField] private NRInputField denominatorInput;
        [SerializeField] private NRInputField beatLengthInput;
        [Space, Header("Views")]
        [SerializeField] private CanvasGroup bpmView;
        [SerializeField] private CanvasGroup trimView;
        private CanvasGroup canvas;

        private float bpm;
        private uint numerator;
        private uint denominator;
        [NRInject] private UIModeSelect modeSelect;
        [NRInject] private Timeline timeline;

        private Vector3 startPosition = new Vector3(0, -0.28f, 0);

        private NRButton menuBrowserButton;

        protected override void Awake()
        {
            base.Awake();
            canvas = GetComponent<CanvasGroup>();
            GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            loadingOverlay.SetActive(false);
        }


        private void Start()
        {
            menuBrowserButton = modeSelect.menuBrowserButton.GetComponent<NRButton>();
            gameObject.SetActive(false);
        }
        public override void Show()
        {
            OnActivated();
            menuBrowserButton.DisableWhenInitialized();
            transform.localPosition = startPosition;
            canvas.DOFade(1f, .3f);
            dragAlign.enabled = true;
            canvas.blocksRaycasts = true;
            //converting double => float is imprecise. A round value (e.g. 120) can be something like 120.0000698 in float, which then gets rounded to 120.0001... which sucks, so we hope we never need
            //precision below 4 decimal places.
            bpm = (float)TruncateDouble(EditorTempo.GetBpmFromTime(new(0)));
            var timeSig = EditorTempo.TempoChanges[0].timeSignature;
            nominatorInput.text = timeSig.Numerator.ToString();
            denominatorInput.text = timeSig.Denominator.ToString();
            if (bpm % 1 > .98f) bpm = Mathf.Round(bpm);
            bpmInput.text = bpm.ToString();
            TimelineCameraMouseHandler.allowMiniTimelineClick = true;
        }

       
        public static double TruncateDouble(double val)
            => Math.Truncate(val * 10000) / 10000;

        public override void Hide()
        {
            TimelineCameraMouseHandler.allowMiniTimelineClick = false;
            dragAlign.enabled = false;
            canvas.blocksRaycasts = false;
            canvas.DOFade(0f, .3f).OnComplete(() =>
            {
                bpmView.alpha = 1f;
                trimView.alpha = 0f;
                menuBrowserButton.ClearDisabledQueue();
                modeSelect.menuBrowserButton.SetActive(true);
                trimView.blocksRaycasts = false;
                trimView.interactable = false;
                bpmView.blocksRaycasts = true;
                bpmView.interactable = true;

                OnDeactivated();
            });
        }

        public override void ShowHelp()
        {
            NRHelp.Instance.ShowBPMAlign();
        }

        public void ApplyBPM()
        {
            float.TryParse(bpmInput.text, out bpm);
            if (bpm <= 0f) bpm = 150f;
            uint.TryParse(nominatorInput.text, out numerator);
            uint.TryParse(denominatorInput.text, out denominator);

            if (numerator <= 0) numerator = 4;
            if (denominator <= 0) denominator = 4;

            EditorTempo.SetBPM(new QNT_Timestamp(0), Constants.MicrosecondsPerQuarterNoteFromBPM(bpm), true, numerator, denominator);
        }

        private void ChangeView(CanvasGroup from, CanvasGroup to)
        {
            var animation = DOTween.Sequence();
            animation.Append(from.DOFade(0f, .3f));
            animation.Append(to.DOFade(1f, .3f));
            animation.Play();
            from.blocksRaycasts = false;
            from.interactable = false;
            to.blocksRaycasts = true;
            to.blocksRaycasts = true;
        }

        public void GoToTrimAudio()
        {
            loadingOverlay.SetActive(true);
            dragAlign.ModifyAudio(OnAudioModified);
        }

        private void OnAudioModified()
        {
            loadingOverlay.SetActive(false);
            dragAlign.enabled = false;
            ChangeView(bpmView, trimView);
        }

        public void GoToBpm()
        {
            ChangeView(trimView, bpmView);
            dragAlign.enabled = true;
        }

        public void Finish()
        {
            Hide();
        }

        public void AddSilence()
        {
            var duration = GetTimeFromLabels();
            if (duration == null)
            {
                return;
            }

            if (duration.Value.tick <= 0)
            {
                return;
            }

            EditorAudioManager.Instance.RemoveOrAddTimeToAudio(duration.Value, false);
        }

        public void RemoveSilence()
        {
            var duration = GetTimeFromLabels();
            if (duration == null)
            {
                return;
            }
            if(duration.Value.tick <= 0)
            {
                return;
            }

            EditorAudioManager.Instance.RemoveOrAddTimeToAudio(new Relative_QNT(-duration.Value.tick), false);
        }

        private Relative_QNT? GetTimeFromLabels()
        {
            float beatValue;
            if (float.TryParse(beatLengthInput.text, out beatValue))
            {
                if (beatValue > 0.0f)
                {
                    return new Relative_QNT((long)Math.Round(Constants.PulsesPerQuarterNote * beatValue));
                }
            }

            return null;
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            
        }
    }
}

