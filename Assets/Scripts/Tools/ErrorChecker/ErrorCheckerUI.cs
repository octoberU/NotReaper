using System;
using NotReaper.Models;
using NotReaper.Overlays;
using NotReaper.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NotReaper.Tools.ErrorChecker
{
    public class ErrorCheckerUI : NROverlay, IPointerEnterHandler, IPointerExitHandler
    {
        [Space, Header("Error Checker UI")]
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI errorBody;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private OnHover listHover;
        [SerializeField] private ErrorEntry errorEntryPrefab;
        [SerializeField] private Transform contentTransform;
        [SerializeField] private ScrollRect scroller;
        [NRInject] private ErrorChecker checker;
        [NRInject] private NewPauseMenu pauseMenu;

        private List<ErrorEntry> errors = new();

        private void Awake() => listHover.onHover.AddListener(OnListHover);

        private void OnListHover(bool isHovering)
        {
            if (isHovering)
            { 
                KeybindManager.DisableKeybind("Scrub");
                KeybindManager.DisableKeybind("ScrubByTick");
            }
            else
            {
                KeybindManager.EnableKeybind("Scrub");
                KeybindManager.EnableKeybind("ScrubByTick");
            }
        }

        public override void Hide()
        {
            checker.initialized = false;
            OnListHover(false);
            EditorState.SetIsInUI(false);
            checker.Hide();
            OnDeactivated();
            KeybindManager.EnableKeybind("Pause");
            KeybindManager.Global.UnregisterEscCallback(CloseChecker);
        }

        public override void Show()
        {
            if(checker.initialized)
                OnActivated();
            else
                checker.RunErrorCheck();
            
            KeybindManager.DisableKeybind("Pause");
            KeybindManager.Global.RegisterEscCallback(CloseChecker);
        }

        private void CloseChecker(InputAction.CallbackContext callbackContext) => Hide();

        public override void ShowHelp()
        {
            NRHelp.Instance.ShowErrorChecker();
        }

        protected override void OnEditorModeChanged(EditorMode mode)
        {
            
        }

        public void OnFixedClicked()
        {
            var error = errors[checker.CurrentErrorIndex];
            errors.Remove(error);
            Destroy(error.gameObject);
            checker.MarkCurrentFixed();
        }

        public void FillErrorList(List<ErrorData> errorData)
        {
            for (int i = errors.Count - 1; i >= 0; i--)
            {
                Destroy(errors[i].gameObject);
            }
            errors.Clear();

            for (var index = 0; index < errorData.Count; index++)
            {
                var data = errorData[index];
                var error = Instantiate(errorEntryPrefab, contentTransform);
                error.onSelected += OnErrorSelected;
                data.AddEntry(error);
                error.SetData(data);
                error.Index = index;
                errors.Add(error);
            }
        }

        private void SnapToContent(int index) => scroller.content.localPosition = scroller.GetSnapToPositionToBringChildIntoView(errors.First(e => e.Index == index).rect);

        private void OnErrorSelected(int index)
        {
            checker.SelectError(index);
            SnapToContent(index);
        }
        public void Rescan() => checker.RerunErrorCheck();

        public void FixItForMe()
        {
            if (checker.CurrentErrorIndex < 0) return;
            var error = errors[checker.CurrentErrorIndex];
            error.FixItForMe();
            Rescan();
        }

        public void OnExitClicked()
        {
            EditorState.SetIsInUI(false);
            Hide();
        }

        public void OnNextClicked()
        {
            checker.NextError();
            SnapToContent(checker.CurrentErrorIndex);
        }

        public void OnPreviousClicked()
        {
            checker.PrevError();
            SnapToContent(checker.CurrentErrorIndex);
        }
        internal void SetErrorCount(int count) =>  countText.text = count.ToString();

        internal void SetErrorBody(string text, string time)
        {
            errorBody.text = text.ToLower();
            timeText.text = time;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (drag.isMouseDown) return;
            EditorState.SetIsInUI(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (drag.isMouseDown) return;
            EditorState.SetIsInUI(true);
        }
    }
}

