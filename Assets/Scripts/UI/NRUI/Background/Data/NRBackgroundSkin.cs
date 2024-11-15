using UnityEngine;


namespace NotReaper.UI.Components
{
    [CreateAssetMenu(menuName = "NotReaper UI/Skins/Background Skin")]
    public class NRBackgroundSkin : ScriptableObject
    {
        [Header("Background")]
        public Color backgroundColor;
        public Color timelineBackgroundColor;
    }
}
