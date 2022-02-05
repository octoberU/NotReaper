using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.MapBrowser.UI
{
    /// <summary>
    /// Responsible for the Navigation Panel.
    /// </summary>
    public class NavigationPanel : FadingPanel
    {
        #region References
        [Header("Buttons")]
        [SerializeField] private Button buttonNext;
        [SerializeField] private Button buttonPrevious;
        [Space, Header("Text")]
        [SerializeField] private TextMeshProUGUI pageText;
        #endregion

        /// <summary>
        /// Updates the navigation based on the supplied parameters.
        /// </summary>
        /// <param name="enableNext">Enable the next button.</param>
        /// <param name="enablePrevious">Enable the previous button.</param>
        /// <param name="page">The current page.</param>
        /// <param name="total">The total amount of pages.</param>
        public void UpdateNavigation(bool enableNext, bool enablePrevious, int page, int total)
        {
            pageText.text = $"<color=green>{page}</color> / {Mathf.Ceil((float)total / 20f)}";
            buttonNext.interactable = enableNext;
            buttonPrevious.interactable = enablePrevious;
        }
    }
}

