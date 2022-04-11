using NotReaper.Keybinds;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static NotReaper.Keybinds.RebindManager;

namespace NotReaper.Keybinds
{
    public class KeybindDisplay : MonoBehaviour
    {
        [SerializeField] private InputActionReference keybind;
        [SerializeField] private bool iconOnly = false;
        [SerializeField] private string customLabel = "";
        [NRInject] private InputIcons icons;

        private TextMeshProUGUI label;
        private Image keybindImage;
        private Image modifier1;
        private Image modifier2;

        private void Awake()
        {
            label = transform.Find("Label").GetComponent<TextMeshProUGUI>();
            keybindImage = transform.Find("Keybind").GetComponent<Image>();
            modifier1 = transform.Find("Modifier1").GetComponent<Image>();
            modifier2 = transform.Find("Modifier2").GetComponent<Image>();
        }

        internal void SetKeybind(InputActionReference keybind) => this.keybind = keybind;

        private void OnEnable() => Populate(GetKeybindDisplayData(keybind.action));

        internal void Populate(KeybindDisplayData data)
        {
            label.text = data.displayName.ToLower();
            keybindImage.sprite = icons.GetIcon(data.keybind, out _);
            keybindImage.preserveAspect = true;
            if (!string.IsNullOrEmpty(data.modifier1))
            {
                modifier1.gameObject.SetActive(true);
                modifier1.sprite = icons.GetIcon(data.modifier1, out _);
                modifier1.preserveAspect = true;
            }
            else
            {
                modifier1.gameObject.SetActive(false);
            }
            if (!string.IsNullOrEmpty(data.modifier2))
            {
                modifier2.gameObject.SetActive(true);
                modifier2.sprite = icons.GetIcon(data.modifier2, out _);
                modifier2.preserveAspect = true;
            }
            else
            {
                modifier2.gameObject.SetActive(false);
            }

            if (iconOnly)
            {
                label.gameObject.SetActive(false);
            }
            if (!string.IsNullOrEmpty(customLabel))
            {
                label.text = customLabel;
            }
        }

    }

}
