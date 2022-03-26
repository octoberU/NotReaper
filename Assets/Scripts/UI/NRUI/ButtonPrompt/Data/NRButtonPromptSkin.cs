using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.UI.Components
{
    [CreateAssetMenu(menuName = "NotReaper UI/Skins/Button Prompt Skin")]
    public class NRButtonPromptSkin : ScriptableObject
    {
        [Header("Background")]
        public Color backgroundColor;
        public Color itemBackgroundColor;
        public Color disabledColor;
        [Space, Header("Text")]
        public Color textColor;
        [Space, Header("Icon")]
        public Color iconColor;
    }
}
