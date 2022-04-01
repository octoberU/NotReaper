using System;
using DG.Tweening;
using NotReaper;
using TMPro;
using UnityEngine;
using NotReaper.Timing;
using NotReaper.Models;
using UnityEngine.InputSystem;
using NotReaper.UI.Components;

namespace NotReaper.UI.BPM
{
    public class DynamicBPMWindow : NRMenu
    {
        public NRInputField dynamicBpmInput;
        public NRInputField timeSignatureNumerator;
        public NRInputField timeSignatureDenomerator;

        [NRInject] private Timeline timeline;

        public bool isActive = false;

        void Start()
        {
            Vector3 defaultPos = Vector3.zero;
            gameObject.GetComponent<RectTransform>().localPosition = defaultPos;
            gameObject.GetComponent<CanvasGroup>().alpha = 0.0f;
            gameObject.SetActive(false);
            gameObject.GetComponent<CanvasGroup>().alpha = 0f;
        }

        public void ToggleWindow()
        {
            isActive = !isActive;
            if (isActive) Show();
            else Hide();
        }

        public override void Show()
        {
            OnActivated();
            if (!EditorState.IsPaused)
            {
                timeline.TogglePlayback();
            }

            gameObject.GetComponent<CanvasGroup>().DOFade(1.0f, 0.3f);
            gameObject.SetActive(true);

            TempoChange tempo = EditorTempo.GetTempoForTime(EditorTime.Time);
            dynamicBpmInput.text = Constants.DisplayBPMFromMicrosecondsPerQuaterNote(tempo.microsecondsPerQuarterNote);
        }

        public override void Hide()
        {
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
            double dynamicBpm = 0.0f;
            TimeSignature timeSignature = new TimeSignature(4, 4);
            if (Double.TryParse(dynamicBpmInput.text, out dynamicBpm))
            {

                uint numer = 4;
                uint denom = 4;
                if (uint.TryParse(timeSignatureNumerator.text, out numer) && uint.TryParse(timeSignatureDenomerator.text, out denom))
                {
                    if (numer != 0 && denom != 0)
                    {
                        timeSignature = new TimeSignature(numer, denom);
                    }
                }

                EditorTempo.SetBPM(EditorTime.Time, Constants.MicrosecondsPerQuarterNoteFromBPM(dynamicBpm), true, timeSignature.Numerator, timeSignature.Denominator);
                Hide();
            }
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }
    }
}

