using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI.Components
{
    [CreateAssetMenu(menuName = "NotReaper UI/Skins/Slider Skin")]
    public class NRSliderSkin : ScriptableObject
    {
        [Header("Slider colors")]
        public Color sliderBGColor;
        public Color sliderFillColor;
    }
}

