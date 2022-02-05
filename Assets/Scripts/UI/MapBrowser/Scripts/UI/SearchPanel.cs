using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace NotReaper.MapBrowser.UI
{
    /// <summary>
    /// Responsible for the Search Panel.
    /// </summary>
    public class SearchPanel : FadingPanel
    {
        #region References
        [Header("Scrollbar")]
        [SerializeField] private Scrollbar scrollbar;
        [Space, Header("Overlay")]
        [SerializeField] private FadingPanel noResults;
        [Space, Header("Text")]
        [SerializeField] private TMP_InputField inputSearch;
        #endregion

        /// <summary>
        /// Clears the search input.
        /// </summary>
        public void Clear()
        {
            inputSearch.text = "";
        }

        /// <summary>
        /// Scrolls to the top.
        /// </summary>
        public void ScrollToTop()
        {
            scrollbar.value = 1f;
        }
        /// <summary>
        /// Shows the NoResults hint.
        /// </summary>
        /// <param name="show">True if NoResults hint should be shown.</param>
        public void ShowNoResults(bool show)
        {
            noResults.Show(show);
        }
        /// <summary>
        /// Gets the input from the search field.
        /// </summary>
        /// <returns>The Input from the search field.</returns>
        public string GetSearchInput()
        {
            return inputSearch.text;
        }
    }
}

