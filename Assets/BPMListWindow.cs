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
using NotReaper.Notifications;

namespace NotReaper.UI.BPM
{
    public class BPMListWindow : NRMenu
    {
        public TMP_Text bpmTextList;
        public bool isActive;
        void Start() {
            Vector3 defaultPos = Vector3.zero;
            gameObject.GetComponent<RectTransform>().localPosition = defaultPos;
            gameObject.GetComponent<CanvasGroup>().alpha = 0.0f;
            gameObject.SetActive(false);
        }

        public override void Show()
        {
            NotificationCenter.SendNotification("No BPM Start point set.", NotificationType.Warning);
        }

        public void Show(List<float> bpmList) {
            isActive = true;
            OnActivated();
            gameObject.GetComponent<CanvasGroup>().DOFade(1.0f, 0.3f);
            gameObject.SetActive(true);

            bpmTextList.text = "";
            foreach(float f in bpmList) {
                bpmTextList.text += f.ToString() + "\n";
            }
        }

        public override void Hide() {
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

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }
    }
}
