using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using NotReaper.Keybinds;
using UnityEngine.InputSystem;
using TMPro;

namespace NotReaper.Keyboard
{
    public class ShortcutKey : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string keyName;
        public bool selectableNormal = true;
        public bool selectableCtrl = false;
        public bool selectableShift = false;
        public bool selectableAlt = false;
        public bool selectableCtrlShift = false;
        public bool selectableCtrlAlt = false;
        public bool selectableShiftAlt = false;
        public bool isCtrl, isAlt, isShift = false;
        [HideInInspector] public bool selectable = true;

        public GameObject normalText;
        public GameObject ctrlText;
        public GameObject shiftText;
        public GameObject altText;
        public GameObject ctrlShiftText;
        public GameObject ctrlAltText;
        public GameObject shiftAltText;

        private GameObject textBox;
        private CanvasGroup textBoxCanvasGroup;
        private CanvasGroup transformCanvasGroup;

        private TextMeshProUGUI normalTextMesh;
        private TextMeshProUGUI ctrlTextMesh;
        private TextMeshProUGUI shiftTextMesh;
        private TextMeshProUGUI altTextMesh;
        private TextMeshProUGUI ctrlShiftTextMesh;
        private TextMeshProUGUI ctrlAltTextMesh;
        private TextMeshProUGUI shiftAltTextMesh;

        public void Awake()
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
            //transformCanvasGroup.alpha = selectable ? 1f : .3f;
            normalTextMesh = normalText.GetComponent<TextMeshProUGUI>();
            ctrlTextMesh = ctrlText.GetComponent<TextMeshProUGUI>();
            shiftTextMesh = shiftText.GetComponent<TextMeshProUGUI>();
            altTextMesh = altText.GetComponent<TextMeshProUGUI>();
            ctrlShiftTextMesh = ctrlShiftText.GetComponent<TextMeshProUGUI>();
            ctrlAltTextMesh = ctrlAltText.GetComponent<TextMeshProUGUI>();
            shiftAltTextMesh = shiftAltText.GetComponent<TextMeshProUGUI>();

            selectable = false;
            selectableAlt = false;
            selectableCtrl = false;
            selectableShift = false;
            selectableNormal = false;
            selectableCtrlAlt = false;
            selectableCtrlShift = false;
            selectableShiftAlt = false;

            normalTextMesh.text = "";
            ctrlTextMesh.text = "";
            shiftTextMesh.text = "";
            altTextMesh.text = "";

        }

        public void SetNormalText(string text, string map)
        {
            normalTextMesh.text += $"({map}) {text}\n";
            selectableNormal = true;
            selectable = true;
            normalText.SetActive(true);
            transformCanvasGroup.alpha = 1f;
        }

        public void SetShiftText(string text, string map)
        {
            shiftTextMesh.text += $"({map}) {text}\n";
            selectableShift = true;
        }

        public void SetCtrlText(string text, string map)
        {
            ctrlTextMesh.text += $"({map}) {text}\n";
            selectableCtrl = true;
        }

        public void SetCtrlShiftText(string text, string map)
        {
            ctrlShiftTextMesh.text += $"({map}) {text}\n";
            selectableCtrlShift = true;
        }

        public void SetCtrlAltText(string text, string map)
        {
            ctrlAltTextMesh.text += $"({map}) {text}\n";
            selectableCtrlAlt = true;
        }

        public void SetShiftAltText(string text, string map)
        {
            shiftAltTextMesh.text += $"({map}) {text}\n";
            selectableShiftAlt = true;
        }

        public void SetAltText(string text, string map)
        {
            altTextMesh.text += $"({map}) {text}\n";
            selectableAlt = true;
        }

        public void Enable(bool enable, KeybindManager.Global.Modifiers type)
        {
            bool isModifierKeyAndSelected = false;
            
            selectable = false;
            normalText.SetActive(false);
            ctrlText.SetActive(false);
            shiftText.SetActive(false);
            shiftAltText.SetActive(false);
            ctrlAltText.SetActive(false);
            ctrlShiftText.SetActive(false);
            shiftAltText.SetActive(false);
            altText.SetActive(false);

            switch (type)
            {
                case KeybindManager.Global.Modifiers.None:
                    if (selectableNormal)
                    {
                        normalText.SetActive(enable);
                        selectable = enable;
                    }
                    break;
                case KeybindManager.Global.Modifiers.Ctrl:
                    if (selectableCtrl)
                    {
                        ctrlText.SetActive(enable);
                        selectable = enable;
                    }
                    if (isCtrl)
                    {
                        normalText.SetActive(true);
                        selectable = enable;
                        isModifierKeyAndSelected = true;
                    }
                    break;
                case KeybindManager.Global.Modifiers.Shift:
                    if (selectableShift)
                    {
                        shiftText.SetActive(enable);
                        selectable = enable;
                    }
                    if (isShift)
                    {
                        normalText.SetActive(true);
                        selectable = enable;
                        isModifierKeyAndSelected = true;
                    }
                    break;
                case KeybindManager.Global.Modifiers.Alt:
                    if (selectableAlt)
                    {
                        altText.SetActive(enable);
                        selectable = enable;
                    }
                    if (isAlt)
                    {
                        normalText.SetActive(true);
                        selectable = enable;
                        isModifierKeyAndSelected = true;
                    }
                    break;
                case KeybindManager.Global.Modifiers.CtrlAlt:
                    if (selectableCtrlAlt)
                    {
                        ctrlAltText.SetActive(enable);
                        selectable = enable;
                    }
                    if (isCtrl || isAlt)
                    {
                        normalText.SetActive(true);
                        selectable = enable;
                        isModifierKeyAndSelected = true;
                    }
                    break;
                case KeybindManager.Global.Modifiers.CtrlShift:
                    if (selectableCtrlShift)
                    {
                        ctrlShiftText.SetActive(enable);
                        selectable = enable;
                    }
                    if (isCtrl || isShift)
                    {
                        normalText.SetActive(true);
                        selectable = enable;
                        isModifierKeyAndSelected = true;
                    }
                    break;
                case KeybindManager.Global.Modifiers.ShiftAlt:
                    if (selectableShiftAlt)
                    {
                        shiftAltText.SetActive(enable);
                        selectable = enable;
                    }
                    if (isShift || isAlt)
                    {
                        normalText.SetActive(true);
                        selectable = enable;
                        isModifierKeyAndSelected = true;
                    }
                    break;
                case KeybindManager.Global.Modifiers.All:
                    if(isShift || isAlt || isCtrl)
                    {
                        normalText.SetActive(true);
                        selectable = enable;
                        isModifierKeyAndSelected = true;
                    }
                    break;
                default:
                    break;

            }
            float targetAlpha = selectable ? 1f : .3f;
            if(isCtrl || isShift || isAlt)
            {
                if (!isModifierKeyAndSelected)
                    targetAlpha = .6f;
            }

            transformCanvasGroup.DOFade(targetAlpha, .3f);
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

