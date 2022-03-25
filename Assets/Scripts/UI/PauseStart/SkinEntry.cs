using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI.Customization
{
    public class SkinEntry : MonoBehaviour
    {
        private ThemeData _skin;
        internal ThemeData skin
        {
            get
            {
                return _skin;
            }
            set
            {
                _skin = value;
                GetComponent<NRButton>().SetText(_skin.skinName);
            }
        }
        internal CustomizationPanel panel;

        public void SelectTheme()
        {
            panel.PreviewTheme(skin);
        }
    }
}

