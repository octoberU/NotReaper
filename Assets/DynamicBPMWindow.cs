using System;
using DG.Tweening;
using NotReaper;
using TMPro;
using UnityEngine;
using NotReaper.Timing;
using NotReaper.Models;
using UnityEngine.InputSystem;
using NotReaper.UI.Components;
using System.Linq;

namespace NotReaper.UI.BPM
{
    public class DynamicBPMWindow : NRMenu
    {
        public NRInputField dynamicBpmInput;
        public NRInputField timeSignatureNumerator;
        public NRInputField timeSignatureDenomerator;

        [SerializeField] private NRButton removeButton;

        [NRInject] private Timeline timeline;

        public bool isActive = false;

        public delegate void OnBPMItemAddedOrRemoved();

        public static event OnBPMItemAddedOrRemoved onBPMItemAddedOrRemoved;

        void Start()
        {
            Vector3 defaultPos = Vector3.zero;
            gameObject.GetComponent<RectTransform>().localPosition = defaultPos;
            gameObject.GetComponent<CanvasGroup>().alpha = 0.0f;
            gameObject.SetActive(false);
            gameObject.GetComponent<CanvasGroup>().alpha = 0f;
            removeButton.interactable = false;
        }

        public void ToggleWindow()
        {
            isActive = !isActive;
            if (isActive) Show();
            else Hide();
        }

        public override void Show()
        {
            isActive = true;
            OnActivated();
            if (EditorAudio.IsPlaying)
            {
                EditorAudio.TogglePlay();
            }

            gameObject.GetComponent<CanvasGroup>().DOFade(1.0f, 0.3f);
            gameObject.SetActive(true);

            TempoChange tempo = EditorTempo.GetTempoForTime(EditorTime.Time);
            dynamicBpmInput.text = Constants.DisplayBPMFromMicrosecondsPerQuaterNote(tempo.microsecondsPerQuarterNote);

            removeButton.interactable = EditorTempo.TempoChanges.Any(t => t.time == EditorTime.Time);
        }

        public override void Hide()
        {
            isActive = false;
            gameObject.GetComponent<CanvasGroup>().DOFade(0.0f, 0.3f).OnComplete(() =>
            {
                OnDeactivated();
            });
        }

        public override void ShowHelp()
        {
            NRHelp.Instance.ShowTiming();
        }

        public void AddDynamicBPM()
        {
            TimeSignature timeSignature = new TimeSignature(4, 4);
            if (Double.TryParse(dynamicBpmInput.text, out double dynamicBpm))
            {
                if (uint.TryParse(timeSignatureNumerator.text, out uint numer) && uint.TryParse(timeSignatureDenomerator.text, out uint denom))
                {
                    if (numer != 0 && denom != 0)
                    {
                        timeSignature = new TimeSignature(numer, denom);
                    }
                }

                EditorTempo.SetBPM(EditorTime.Time, Constants.MicrosecondsPerQuarterNoteFromBPM(dynamicBpm), true, timeSignature.Numerator, timeSignature.Denominator);
                onBPMItemAddedOrRemoved?.Invoke();
                Hide();
            }
        }

        public void RemoveBPM()
        {
            EditorTempo.SetBPM(EditorTime.Time, 0, true);
            onBPMItemAddedOrRemoved?.Invoke();
            Hide();
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }
    }
}

