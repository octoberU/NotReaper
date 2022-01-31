using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace NotReaper.MapBrowser
{
    public class MapBrowserWindow : MonoBehaviour
    {

        public static MapBrowserWindow Instance = null;
        public static bool IsOpen => Instance.gameObject.activeInHierarchy;
        #region References
        [Header("References")]
        [SerializeField] private TMP_InputField inputSearch;
        [SerializeField] private Button buttonSearch;
        [SerializeField] private Button buttonDownload;
        [SerializeField] private Button buttonDownloadAll;
        [SerializeField] private Button buttonNext;
        [SerializeField] private Button buttonPrevious;
        [SerializeField] private ToggleGroup toggleGroup;
        [SerializeField] private TextMeshProUGUI pageText;
        [SerializeField] private Scrollbar rightScrollbar;
        [SerializeField] private Scrollbar leftScrollbar;
        [SerializeField] private GameObject noResults;
        [SerializeField] private Toggle beginnerToggle;
        [SerializeField] private Toggle standardToggle;
        [SerializeField] private Toggle advancedToggle;
        [SerializeField] private Toggle expertToggle;
        [SerializeField] private TextMeshProUGUI selectedMapCount;
        [SerializeField] private GameObject downloadOverlay;
        [SerializeField] private TextMeshProUGUI downloadProgress;
        [SerializeField] private Button buttonClearSelection;
        [SerializeField] private GameObject downloadComplete;
        [SerializeField] private TextMeshProUGUI failedMapsText;
        [SerializeField] private GameObject middlePanel;
        [SerializeField] private GameObject navigationPanel;
        [SerializeField] private GameObject sidePanel;
        [SerializeField] private GameObject downloadPanel;
        [SerializeField] private GameObject retryButton;
        [SerializeField] private ScrollRect selectedScrollRect;
        [SerializeField] private GameObject cancelButton;
        [SerializeField] private GameObject closeButton;
        [SerializeField] private Toggle songToggle;
        [SerializeField] private Toggle artistToggle;
        [SerializeField] private Toggle mapperToggle;
        [SerializeField] private Toggle curatedToggle;
        [SerializeField] private Toggle almostCuratedToggle;
        #endregion

        private float timeToScroll = 1f;
        private void Awake()
        {
            if(Instance != null)
            {
                Debug.Log("MapBrowserWindow already exists.");
                return;
            }
            Instance = this;           
        }

        private void Start()
        {
            //buttonDownload.interactable = false;
            buttonDownloadAll.interactable = false;
            buttonNext.interactable = false;
            buttonPrevious.interactable = false;
            //buttonClearSelection.interactable = false;
            EnableDownloadPanel(false);
        }

        #region UI Events
        /// <summary>
        /// 0 = None
        /// 1 = Semi
        /// 2 = Curated
        /// </summary>
        /// <param name="state">The state to filter for.</param>
        public void SetCurationToggleState(int state)
        {
            MapBrowser.CurationState = (CurationState)state;
        }

        /// <summary>
        /// Searches maudica.com API for input in search field.
        /// </summary>
        public void Search()
        {
            //bool success = MapBrowser.Instance.Search(inputSearch.text, GetDifficultyFilter());
            UpdateFilters();
            MapBrowser.Instance.Search(inputSearch.text, GetDifficulties());
            //buttonDownloadAll.interactable = success;
        }

        private void UpdateFilters()
        {
            MapBrowser.CurationState = curatedToggle.isOn ? CurationState.Curated : almostCuratedToggle.isOn ? CurationState.Semi : CurationState.None;
            MapBrowser.FilterState = songToggle.isOn ? FilterState.Song : artistToggle.isOn ? FilterState.Artist : mapperToggle.isOn ? FilterState.Mapper : FilterState.All;
            
        }
        /// <summary>
        /// Downloads the selected map.
        /// </summary>
        public void OnDownloadClicked()
        {
            EnableDownloadOverlay(true);
            MapBrowser.Instance.Download();
        }

        /// <summary>
        /// Downloads all currently listed maps.
        /// </summary>
        public void OnDownloadAllClicked()
        {
            MapBrowser.Instance.AddPageToSelection();
        }

        /// <summary>
        /// Advances to a new page of maps (if available). -1 is previous, 1 is next
        /// </summary>
        public void OnChangePageClicked(int direction)
        {
            MapBrowser.Instance.ChangePage(direction, GetDifficulties());
        }

        public void OnClearSelectionClicked()
        {
            MapEntrySpawnManager.Instance.ClearSelectedEntries();
            buttonClearSelection.interactable = false;
        }

        public void OnCloseOverlayClicked()
        {
            downloadOverlay.SetActive(false);
            downloadComplete.SetActive(false);
            EnableDownloadOverlay(false);
            downloadProgress.text = "0%";
            failedMapsText.text = "";
            retryButton.SetActive(false);
        }

        public void OnRetryClicked()
        {
            OnCloseOverlayClicked();
            EnableDownloadOverlay(true);
            MapBrowser.Instance.RetryDownload();
        }

        public void OnCancelClicked()
        {
            MapBrowser.Instance.StopDownloads();
            cancelButton.SetActive(false);
        }
        public void OnOpenClicked(bool open)
        {
            gameObject.SetActive(open);
        }

        public void OnClearSearchClicked()
        {
            inputSearch.text = "";
            beginnerToggle.isOn = standardToggle.isOn = advancedToggle.isOn = expertToggle.isOn = false;
            curatedToggle.isOn = almostCuratedToggle.isOn = false;
            songToggle.isOn = artistToggle.isOn = mapperToggle.isOn = false;
            Search();
        }
        #endregion
        #region UI Update
        public void EnableNoResults()
        {
            noResults.SetActive(true);
            buttonDownloadAll.interactable = false;
            buttonNext.interactable = false;
            buttonPrevious.interactable = false;
            pageText.text = "";
        }

        public void UpdateSelectedCount(int count)
        {
            
            selectedMapCount.text = count == 0 ? "no maps selected" : $"{count} {(count == 1 ? "map" : "maps")} selected";
            //buttonClearSelection.interactable = count != 0;
            EnableDownloadPanel(count != 0);
        }

        private void EnableDownloadPanel(bool enable)
        {
            downloadPanel.SetActive(enable);
            buttonDownload.gameObject.SetActive(enable);
            buttonClearSelection.gameObject.SetActive(enable);
        }

        public void UpdateDownloadButton()
        {
            buttonDownload.interactable = MapEntrySpawnManager.Instance.SelectedMapsCount > 0;
        }
        public void EnableDownloadOverlay(bool enable)
        {
            downloadOverlay.SetActive(enable);
            middlePanel.SetActive(!enable);
            sidePanel.SetActive(!enable);
            navigationPanel.SetActive(!enable);
            leftScrollbar.interactable = !enable;
            EnableSelectedEntriesInteraction(!enable);
            closeButton.SetActive(!enable);
            cancelButton.SetActive(true);
            downloadProgress.text = "0%";
        }

        private void EnableSelectedEntriesInteraction(bool enable)
        {
            var selectedMaps = MapEntrySpawnManager.Instance.GetSelectedEntries();
            foreach (var map in selectedMaps) map.SetButtonInteractable(enable);
        }

        public void UpdateDownloadProgress(int numMap, int total, RectTransform rectTransform)
        {
            float percentage = ((float)(numMap - 1) / (float)total) * 100f;
            var rounded = Mathf.Ceil(percentage);
            downloadProgress.text = $"Map {numMap}/{total}\t({rounded}%)";
            if(rectTransform) StartCoroutine(MoveScroller(GetScrollerSnapPosition(rectTransform)));
            //if (rounded == 100f)
            if(numMap > total)
            {
                downloadOverlay.SetActive(false);
                downloadComplete.SetActive(true);
            }
        }

        private IEnumerator MoveScroller(Vector3 newPosition)
        {
            Vector3 startPos = selectedScrollRect.content.localPosition;
            float time = 0f;
            while(time < 1f)
            {
                time += Time.deltaTime / timeToScroll;
                selectedScrollRect.content.localPosition = Vector3.Lerp(startPos, newPosition, time);
                yield return null;
            }
        }
        public void UpdateNavigation(bool enableNext, bool enablePrevious, int page, int total)
        {
            pageText.text = $"<color=green>{page}</color> / {Mathf.Ceil((float)total / 20f)}";
            buttonNext.interactable = enableNext;
            buttonPrevious.interactable = enablePrevious;
            buttonDownload.interactable = MapEntrySpawnManager.Instance.SelectedMapsCount > 0;
            buttonDownloadAll.interactable = true;
            rightScrollbar.value = 1f;
            noResults.SetActive(false);
        }

        public void SetFailedMaps(int count)
        {
            if (count == 0) return;
            failedMapsText.text = $"{count} maps failed to download.\nwould you like to retry?";
            retryButton.SetActive(true);
        }

        public Vector2 GetScrollerSnapPosition(RectTransform rectTransform)
        {
            
            Canvas.ForceUpdateCanvases();
            Vector2 viewportLocalPosition = selectedScrollRect.viewport.localPosition;
            Vector2 childLocalPosition = rectTransform.localPosition;
            Vector2 result = new Vector2(
                0 - (viewportLocalPosition.x + childLocalPosition.x),
                0 - (viewportLocalPosition.y + childLocalPosition.y)
            );
            return result;
        }
        #endregion
        #region Input Handling
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                Search();
            }
        }
        #endregion
        #region Helpers
        private bool[] GetDifficulties()
        {
            return new bool[] { beginnerToggle.isOn, standardToggle.isOn, advancedToggle.isOn, expertToggle.isOn };     
        }
        #endregion
    }
}

