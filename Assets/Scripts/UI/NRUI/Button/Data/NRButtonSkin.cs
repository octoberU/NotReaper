using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI.Components
{
    [CreateAssetMenu(menuName = "NotReaper UI/Skins/Button Skin")]
    public class NRButtonSkin : ScriptableObject
    {
        [Header("Button")]
        public Color defaultColor;
        public Color highlightedColor;
        public Color pressedColor;
        public Color disabledColor;
        [Space, Header("Outline")]
        public Color outlineColor;
        [Space, Header("Text")]
        public Color textColor;
        [Space, Header("Icon")]
        public Color defaultIconColor;
        public Color iconDisabledColor;

    }
}

