using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotReaper.MapBrowser.API;
using NotReaper.MapBrowser.Entries;

namespace NotReaper.MapBrowser.UI
{
    /// <summary>
    /// Takes care of all UI events and handles the flow of all panels.
    /// </summary>
    public class UIManager : FadingPanel
    {
        public static UIManager Instance { get; private set; } = null;

        #region References
        [Header("References")]
        [SerializeField] private SearchPanel search;
        [SerializeField] private FilterPanel filter;
        [SerializeField] private NavigationPanel navigation;
        [SerializeField] private DownloadPanel download;
        [SerializeField] private DownloadOverlay overlay;
        [SerializeField] private SettingsPanel settings;
        [Space, Header("Buttons")]
        [SerializeField] private GameObject buttonClose;
        [SerializeField] private GameObject buttonSettings;
        #endregion

        #region Awake and Start
        protected override void Awake()
        {
            if(Instance != null)
            {
                Debug.Log("UIManager already exists.");
                return;
            }
            Instance = this;
            base.Awake();
        }

        private void Start()
        {
            download.Show(false);
        }
        #endregion

        #region UI Events
        /// <summary>
        /// Perform a new Search.
        /// </summary>
        public void Search()
        {
            SearchManager.Instance.ResetPage();
            SearchManager.Instance.Search(search.GetSearchInput(), filter.GetSelectedCurationState(), filter.GetSelectedFilterState(), filter.GetSelectedDifficulties());
        }
        /// <summary>
        /// Download selected maps.
        /// </summary>
        public void Download()
        {
            ShowDownloadOverlay(true);
            DownloadManager.Instance.Download();
        }
        /// <summary>
        /// Retry failed downloads.
        /// </summary>
        public void RetryFailedDownloads()
        {
            //CloseOverlay();
            overlay.HideOverlays();
            ShowDownloadOverlay(true);
            DownloadManager.Instance.RetryFailedDownloads();
        }
        /// <summary>
        /// Cancel currently active downloads.
        /// </summary>
        public void CancelDownload()
        {
            DownloadManager.Instance.RequestCancel();
            overlay.ShowCancelButton(false);
        }
        /// <summary>
        /// Add the current page to selection.
        /// </summary>
        public void AddPage()
        {           
            var page = SpawnManager.Instance.GetSearchEntries();
            //SpawnManager.Instance.ClearSelectedEntries();
            foreach (var map in page)
            {
                if (!map.Data.Downloaded) map.SetSelected(true);
            }          
        }
        /// <summary>
        /// Change the page of results.
        /// </summary>
        /// <param name="direction">1 for next page, -1 for previous page.</param>
        public void ChangePage(int direction)
        {
            if (direction != 1 && direction != -1) return;
            SearchManager.Instance.ChangePage(direction);
            search.ScrollToTop();
        }
        /// <summary>
        /// Clears all selected maps.
        /// </summary>
        public void ClearSelectedMaps()
        {
            SpawnManager.Instance.ClearSelectedEntries();
        }
        /// <summary>
        /// Clears all search input and filters.
        /// </summary>
        public void ClearSearch()
        {
            SearchManager.Instance.ResetPage();
            filter.Clear();
            search.Clear();
        }
        /// <summary>
        /// Closes the download overlay.
        /// </summary>
        public void CloseOverlay()
        {
            search.Show(true);
            filter.Show(true);
            download.Show(true);
            navigation.Show(true);
            overlay.HideOverlays();
            SpawnManager.Instance.ClearSelectedEntries();
            ShowDownloadOverlay(false);

        }
        /// <summary>
        /// Shows the ModBrowser.
        /// </summary>
        /// <param name="show">True if it should be shown.</param>
        public void ShowModBrowser(bool show)
        {
            Show(show);
        }
        /// <summary>
        /// Shows the settings panel.
        /// </summary>
        /// <param name="show">True if settings panel should be shown.</param>
        public void OnSettingsClicked()
        {
            search.Show(settings.isOpen);
            download.Show(settings.isOpen);
            navigation.Show(settings.isOpen);
            filter.Show(settings.isOpen);
            settings.ToggleShow();
        }
        #endregion

        #region UI Updates

        #region Search
        /// <summary>
        /// Show the NoResultsFound hint.
        /// </summary>
        /// <param name="show">True if it should be shown.</param>
        public void ShowNoResultsFound(bool show)
        {
            search.ShowNoResults(show);
        }
        #endregion

        #region Overlay
        /// <summary>
        /// Show the download overlay. Also hides other panels.
        /// </summary>
        /// <param name="show">True if it should be shown.</param>
        public void ShowDownloadOverlay(bool show)
        {
            download.SetScrollbarInteractable(!show);
            download.ShowClearButton(!show);
            download.EnableDownloadButton(!show);
            overlay.ShowDownloadingOverlay(show);
            search.Show(!show);
            filter.Show(!show);
            navigation.Show(!show);
            download.SetScrollbarInteractable(!show);
            EnableSelectedEntriesInteraction(!show);
            buttonClose.SetActive(!show);
            buttonSettings.SetActive(!show);
            overlay.ShowCancelButton(show);
        }
        /// <summary>
        /// Updates the download progress.
        /// </summary>
        /// <param name="numMap">The current map downloading.</param>
        /// <param name="total">The total amount of maps downloading.</param>
        /// <param name="rect">The RectTransform of the currently downloading map.</param>
        public void UpdateDownloadProgress(int numMap, int total, RectTransform rect)
        {
            float percentage = ((float)(numMap - 1) / (float)total) * 100f;
            var rounded = Mathf.Ceil(percentage);
            string text = $"Map {numMap}/{total}\t({rounded}%)";
            overlay.SetProgressText(text);
            download.MoveScroller(rect);
            //switch to the DownloadDone overlay once all maps have been downloaded.
            if (numMap > total)
            {
                overlay.ShowDownloadingOverlay(false);
                overlay.PlayDownloadDoneAnimation();
            }
        }
        /// <summary>
        /// Updates the count of failed maps.
        /// </summary>
        /// <param name="count">The amount of failed maps.</param>
        public void UpdateFailedMapsCount(int count)
        {
            overlay.SetFailedMapsCount(count);
        }
        /// <summary>
        /// Enables interaction with SelectedEntries.
        /// </summary>
        /// <param name="enable">True if interaction should be enabled.</param>
        /// <remarks>Used during download procedure to prevent the user from deselecting an entry.</remarks>
        private void EnableSelectedEntriesInteraction(bool enable)
        {
            var selectedMaps = SpawnManager.Instance.GetSelectedEntries();
            foreach (var map in selectedMaps) map.SetButtonInteractable(enable);
        }
        #endregion

        #region Navigation
        /// <summary>
        /// Updates the navigation panel.
        /// </summary>
        /// <param name="enableNext">Enables next button.</param>
        /// <param name="enablePrevious">Enables previous button.</param>
        /// <param name="page">The current page.</param>
        /// <param name="total">The total amount of pages.</param>
        public void UpdateNavigation(bool enableNext, bool enablePrevious, int page, int total)
        {
            navigation.UpdateNavigation(enableNext, enablePrevious, page, total);
            ShowNoResultsFound(false);
        }
        #endregion

        #region Download
        /// <summary>
        /// Sets the download button active if at least one map has been selected. Disables it if no map is selected.
        /// </summary>
        public void UpdateDownloadButton()
        {
            download.EnableDownloadButton(SpawnManager.Instance.SelectedMapsCount > 0);
        }
        /// <summary>
        /// Updates the count of selected maps.
        /// </summary>
        /// <param name="count">The amount of selected maps.</param>

        public void UpdateSelectedCount(int count)
        {
            download.UpdateSelectedMapCount(count);
        }
        #endregion

        #endregion
    }
}

