using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NotReaper.Keybinds;
using DG.Tweening;

namespace NotReaper.UI
{
    public class ToolTips : MonoBehaviour
    {
        public static ToolTips I;
        public TextMeshProUGUI label;
        public GameObject keyPanel;
        public Image keybindImage;
        public Image modifier1;
        public Image modifier2;

        private List<Image> images = new();
        //private Image background;
        [NRInject] InputIcons icons;

        private Color disabledColor;
        private Color enabledColor;
        private CanvasGroup canvas;
        void Awake()
        {
            if (I == null) I = this;
            else
            {
                Debug.LogWarning("Trying to create another ToolTips instance.");
                return;
            }
            DOTween.SetTweensCapacity(500, 50);
            images.Add(keybindImage);
            images.Add(modifier1);
            images.Add(modifier2);
            label.text = "";
            canvas = GetComponent<CanvasGroup>();
            canvas.alpha = 0f;
            foreach (var image in images) image.color = new Color(1, 1, 1, 0);
            //modifier1.gameObject.SetActive(false);
            //modifier2.gameObject.SetActive(false);
            keyPanel.SetActive(true);
            //background = GetComponent<Image>();
            //enabledColor = background.color;
            disabledColor = enabledColor;
            disabledColor.a = 0f;
            //background.color = disabledColor;
        }

        public void SetText(InputActionReference keybind)
        {
            if(keybind == null)
            {
                canvas.DOFade(0f, .3f);
                FadeImages(false);
                return;
            }
            //label.text = text.Replace("[", "<color=#FDA50F>").Replace("]", "</color>");
            var data = RebindManager.GetKeybindDisplayData(keybind.action);
            label.text = data.displayName.ToLower();
            //background.color = enabledColor;
            keyPanel.SetActive(true);
            keybindImage.sprite = icons.GetIcon(data.keybind, out _, out _);
            keybindImage.preserveAspect = true;
            keybindImage.DOFade(1f, .3f);
            if (!string.IsNullOrEmpty(data.modifier1))
            {
                modifier1.gameObject.SetActive(true);
                modifier1.sprite = icons.GetIcon(data.modifier1, out _, out _);
                modifier1.preserveAspect = true;
                modifier1.DOFade(1f, .3f);
            }
            else
            {
                modifier1.color = new Color(1, 1, 1, 0);
                modifier1.gameObject.SetActive(false);
            }
            if (!string.IsNullOrEmpty(data.modifier2))
            {
                modifier2.gameObject.SetActive(true);
                modifier2.sprite = icons.GetIcon(data.modifier2, out _, out _);
                modifier2.preserveAspect = true;
                modifier2.DOFade(1f, .3f);
            }
            else
            {
                modifier2.color = new Color(1, 1, 1, 0);
                modifier2.gameObject.SetActive(false);
            }
            canvas.DOFade(1f, .3f);
        }

        public void SetText(string text)
        {
            if (keybindImage == null) return;
            string txt = text;
            if (string.IsNullOrEmpty(text))
            {
                canvas.DOFade(0f, .3f);
                return;
            }
            if (!string.IsNullOrEmpty(txt))
            {
                if (txt.Contains('['))
                {
                    txt = txt.Insert(txt.IndexOf('['), "\n");
                }
                txt = txt.Replace("[", "<color=#FDA50F>").Replace("]", "</color>").ToLower();
                //background.color = enabledColor;
            }
            else
            {
                //background.color = disabledColor;
            }
            //label.text = text.Replace("[", "<color=#FDA50F>").Replace("]", "</color>").ToLower();
            label.text = txt;
            //keyPanel.SetActive(false);
            keybindImage.color = new Color(1, 1, 1, 0);
            modifier1.color = new Color(1, 1, 1, 0);
            modifier2.color = new Color(1, 1, 1, 0);
            keyPanel.SetActive(false);
            canvas.DOFade(1f, .3f);
        }

        private void FadeImages(bool fadeIn)
        {
            foreach(var image in images)
            {
                image.DOFade(fadeIn ? 1f : 0f, .3f);
            }
        }

    }
}

