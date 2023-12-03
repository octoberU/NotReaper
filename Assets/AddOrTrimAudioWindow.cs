using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NotReaper;
using NotReaper.Repeaters;
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
        public NRToggle addToEndToggle;
        [SerializeField] private GameObject loadingScreen;
        [NRInject] private Timeline timeline;
        [NRInject] private RepeaterManager repeaterManager;
        private CanvasGroup canvas;

        public bool isActive = false;

        private bool isModifying => loadingScreen.activeInHierarchy;

        private Relative_QNT _lastModifiedAmount;
        
        void Start()
        {
            canvas = GetComponent<CanvasGroup>();
            Vector3 defaultPos = Vector3.zero;
            gameObject.GetComponent<RectTransform>().localPosition = defaultPos;
            canvas.alpha = 0.0f;
            loadingScreen.SetActive(false);
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
            if (isModifying)
                return;
            
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
            if (isModifying)
                return;
            
            var duration = GetTimeFromLabels();
            if (duration == null)
            {
                return;
            }

            if (duration.Value.tick < 0)
            {
                return;
            }

            DoModify(duration.Value);
        }

        public void TrimAudio()
        {
            if (isModifying)
                return;
            
            var duration = GetTimeFromLabels();
            if (duration == null)
            {
                return;
            }
            DoModify(new Relative_QNT(-duration.Value.tick));
        }

        private void DoModify(Relative_QNT amount)
        {
            _lastModifiedAmount = amount;
            loadingScreen.SetActive(true);
            EditorAudio.ForceJumpToPercent(0);
            EditorAudioManager.Instance.RemoveOrAddTimeToAudio(amount, addToEndToggle.selected, OnModifyComplete);
        }
        
        private void OnModifyComplete()
        {
            if (!addToEndToggle.selected)
            {
                MiniTimeline.Instance.ShiftBookmarksByTime(_lastModifiedAmount);
                MiniTimeline.Instance.SetPreviewStartPoint(QNT_Timestamp.ShiftTick(EditorFile.SongDesc.previewStartSeconds));
                repeaterManager.ShiftAllRepeatersByAmount(_lastModifiedAmount);
                EditorTargets.UpdateChainConnectors();
            }
            
            loadingScreen.SetActive(false);
            Hide();
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            if (isModifying)
                return;
            
            Hide();
        }
    }

}
