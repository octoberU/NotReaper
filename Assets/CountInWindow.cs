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

namespace NotReaper.UI.Countin
{
    public class CountInWindow : NRMenu
    {
        public NRInputField lengthInput;

        [NRInject] private Timeline timeline;
        private CanvasGroup canvas;
        public bool isActive = false;
        protected override void Awake()
        {
            base.Awake();
            canvas = GetComponent<CanvasGroup>();
        }

        void Start()
        {
            Vector3 defaultPos = Vector3.zero;
            lengthInput.text = "8";
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
            NRHelp.Instance.ShowCountin();
        }

        public void PreviewCountIn()
        {
            uint beats = 0;
            if (uint.TryParse(lengthInput.text, out beats))
            {
                timeline.PreviewCountIn(beats);
            }
        }

        public void GenerateCountIn()
        {
            uint beats = 0;
            if (uint.TryParse(lengthInput.text, out beats))
            {
                timeline.GenerateCountIn(beats);
            }
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }
    }
}
