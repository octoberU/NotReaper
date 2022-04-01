using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NotReaper;
using TMPro;
using UnityEngine;
using NotReaper.UserInput;
using UnityEngine.EventSystems;
using NotReaper.Timing;
using UnityEngine.InputSystem;
using NotReaper.UI.Components;

namespace NotReaper.UI.ModifyAudio
{
    public class AddOrTrimAudioWindow : NRMenu
    {
        public NRInputField timeLengthInput;
        public NRInputField beatLengthInput;

        [NRInject] private Timeline timeline;
        private CanvasGroup canvas;

        public bool isActive = false;

        void Start()
        {
            canvas = GetComponent<CanvasGroup>();
            Vector3 defaultPos = Vector3.zero;
            gameObject.GetComponent<RectTransform>().localPosition = defaultPos;
            canvas.alpha = 0.0f;
            gameObject.SetActive(false);
        }

        public override void Show()
        {
            isActive = true;
            OnActivated();
            canvas.DOFade(1.0f, 0.3f);
            gameObject.SetActive(true);


        }

        public override void Hide()
        {
            isActive = false;
            canvas.DOFade(0.0f, 0.3f).OnComplete(() =>
            {
                OnDeactivated();
            });
        }

        public override void ShowHelp()
        {
            NRHelp.Instance.ShowModifyAudio();
        }

        Relative_QNT? GetTimeFromLabels()
        {
            float beatValue = 0;
            if (float.TryParse(beatLengthInput.text, out beatValue))
            {
                if (beatValue > 0.0f)
                {
                    return new Relative_QNT((long)Math.Round(Constants.PulsesPerQuarterNote * beatValue));
                }
            }

            float timeValue = 0.0f;
            if (float.TryParse(timeLengthInput.text, out timeValue))
            {
                if (timeValue > 0.0f)
                {
                    return Conversion.ToQNT(timeValue, EditorTempo.TempoChanges[0].microsecondsPerQuarterNote);
                }
            }

            return null;
        }

        public void AddSilence()
        {
            var duration = GetTimeFromLabels();
            if (duration == null)
            {
                return;
            }

            if (duration.Value.tick < 0)
            {
                return;
            }

            EditorAudioManager.Instance.RemoveOrAddTimeToAudio(duration.Value);
            Hide();
        }

        public void TrimAudio()
        {
            var duration = GetTimeFromLabels();
            if (duration == null)
            {
                return;
            }

            EditorAudioManager.Instance.RemoveOrAddTimeToAudio(new Relative_QNT(-duration.Value.tick));
            Hide();
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }
    }

}
