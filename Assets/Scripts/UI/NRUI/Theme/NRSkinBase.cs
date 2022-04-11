using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI.Components
{
    [CreateAssetMenu(menuName = "NotReaper UI/Data/Skin Base")]
    public class NRSkinBase : ScriptableObject
    {
        public Color defaultBackgroundColor = Color.black;
        public Color primaryColor = Color.black;
        public Color accentColor = Color.black;
    }
}
