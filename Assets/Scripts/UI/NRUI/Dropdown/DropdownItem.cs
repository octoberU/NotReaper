using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.UI.Components.Dropdown
{
    public class DropdownItem : MonoBehaviour
    {
        internal int index;
        internal NRDropdown dropdown;
        internal string text
        {
            get
            {
                return textContainer.text;
            }
            set
            {
                textContainer.text = value.ToLower();
            }
        }
        internal ColorBlock buttonColorBlock
        {
            get
            {
                return button.colors;
            }
            set
            {
                button.colors = value;
            }
        }
        internal Color textColor
        {
            get
            {
                return textContainer.color;
            }
            set
            {
                textContainer.color = value;
            }
        }
        internal float fontSize
        {
            get
            {
                return textContainer.fontSize;
            }
            set
            {
                textContainer.fontSizeMax = value;
                textContainer.fontSize = value;
            }
        }

        private TextMeshProUGUI textContainer;
        private Button button;

        private void Awake()
        {
            textContainer = GetComponentInChildren<TextMeshProUGUI>();
            button = GetComponent<Button>();
        }

        public void OnSelect()
        {
            dropdown.SelectItem(index);
            dropdown.Shrink();
        }
    }
}

