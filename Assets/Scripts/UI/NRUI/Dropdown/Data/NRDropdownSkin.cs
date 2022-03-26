using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.UI.Components
{
    [CreateAssetMenu(menuName = "NotReaper UI/Skins/Dropdown Skin")]
    public class NRDropdownSkin : ScriptableObject
    {
        [Header("Background")]
        public Color backgroundColor;
        public Color itemBackgroundColor;
        [Space, Header("Items")]
        public Color defaultItemColor;
        public Color highlightedItemColor;
        public Color pressedItemColor;
        [Space, Header("Text")]
        public Color textColor;
        [Space, Header("Icon")]
        public Color iconColor;

        internal ColorBlock GetItemColorBlock()
        {
            ColorBlock block = new();
            block.normalColor = defaultItemColor;
            block.highlightedColor = highlightedItemColor;
            block.pressedColor = pressedItemColor;
            block.disabledColor = defaultItemColor;
            block.colorMultiplier = 1f;
            block.fadeDuration = .1f;
            return block;
        }

    }
}
