using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace NotReaper.Keyboard
{
    public class ShortcutKey : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool selectableNormal = true;
        public bool selectableCtrl = false;
        public bool selectableShift = false;
        public bool isCtrlKey = false;
        public bool isShiftKey = false;
        [HideInInspector] public bool selectable = true;

        public GameObject normalText;
        public GameObject ctrlText;
        public GameObject shiftText;

        private GameObject textBox;
        private CanvasGroup textBoxCanvasGroup;
        private CanvasGroup transformCanvasGroup;
        public void Start()
        {
            textBox = transform.GetChild(1).gameObject;
            textBoxCanvasGroup = textBox.GetComponent<CanvasGroup>();
            transformCanvasGroup = transform.GetComponent<CanvasGroup>();
            textBox.SetActive(true);
            textBoxCanvasGroup.alpha = 0f;
            selectable = selectableNormal;
            normalText.SetActive(selectableNormal);
            ctrlText.SetActive(false);
            shiftText.SetActive(false);
            transformCanvasGroup.alpha = selectable ? 1f : .3f;
        }

        public void Enable(bool enable, OfType type)
        {
            transformCanvasGroup.DOFade(enable ? 1f : .3f, .3f);
            selectable = enable;
            normalText.SetActive(false);
            ctrlText.SetActive(false);
            shiftText.SetActive(false);
            switch (type)
            {
                case OfType.Normal:
                    normalText.SetActive(enable);
                    break;
                case OfType.Ctrl:
                    ctrlText.SetActive(enable);
                    break;
                case OfType.Shift:
                    shiftText.SetActive(enable);
                    break;
                default:                   
                    break;
            }
            if (!enable) FadeTextBox(false);
        }

        private void FadeTextBox(bool fadeIn)
        {
            textBoxCanvasGroup.DOFade(fadeIn ? 1f : 0f, .3f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!selectable) return;
            transform.parent.SetAsLastSibling();
            FadeTextBox(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!selectable) return;
            FadeTextBox(false);
        }
    }
}

