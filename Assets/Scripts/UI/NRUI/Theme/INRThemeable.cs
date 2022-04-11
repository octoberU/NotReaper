using NotReaper.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.UI.Components
{
    public interface INRThemeable
    {
        /// <summary>
        /// Applies the light theme to this object.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        public void ApplyLightTheme(ThemeData theme);
        /// <summary>
        /// Applies the dark theme to this object.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        public void ApplyDarkTheme(ThemeData theme);

        /// <summary>
        /// Update this object's visuals.
        /// </summary>
        public void UpdateVisuals();
        /// <summary>
        /// Register this object to the ThemeManager.
        /// </summary>
        public void RegisterThemeable();
        /// <summary>
        /// Unregister this object from the ThemeManager.
        /// </summary>
        public void UnregisterThemeable();
        /// <summary>
        /// Gets the currently selected theme, applies it, and updates visuals.
        /// </summary>
        public void UpdateSkin();
    }

}
