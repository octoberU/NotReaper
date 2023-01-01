using NotReaper.UI;
using NotReaper.UserInput;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI
{
    public static class ThemeableManager
    {
        private static List<NRThemeable> themeables = new List<NRThemeable>();

        public static void AddThemeable(NRThemeable themeable)
        {
            if (!themeables.Contains(themeable))
            {
                themeables.Add(themeable);
            }
        }

        public static void UpdateColors()
        {
            foreach (var themeable in themeables) themeable.UpdateColors();
            AssetContainer.UpdateTargetColors();
            EditorTargets.UpdateTargetColors();
        }
    }
}

