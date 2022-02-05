using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NotReaper.MapBrowser.Recents;
using NotReaper.MapBrowser.UI;
using NotReaper.MapBrowser.Entries;
namespace NotReaper.MapBrowser.API
{
    /// <summary>
    /// Handles download opeations.
    /// </summary>
    public class DownloadManager : MonoBehaviour
    {
        public static DownloadManager Instance { get; private set; } = null;
        #region Fields

        private int downloadedMapsCount = 0;
        private int totalMapsDownloading = 0;

        private List<MapData> failedDownloads = new List<MapData>();

        private bool cancelDownload = false;
        #endregion

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.Log("DownloadManager already exists.");
                return;
            }
            Instance = this;
        }

        #region Download
        /// <summary>
        /// Start download process of all selected maps.
        /// </summary>
        /// <param name="retry">Set to true if you want to download failed maps.</param>
        public void Download(bool retry = false)
        {
            if (retry && failedDownloads.Count == 0) return;
            StartCoroutine(DoDownload(retry));
        }
        /// <summary>
        /// Downloads all selected maps and informs UI with progress.
        /// </summary>
        /// <param name="retry">Set to true if you want to download failed maps.</param>
        /// <returns></returns>
        private IEnumerator DoDownload(bool retry = false)
        {
            //populate list with correct maps
            List<MapData> maps = retry ? failedDownloads.ToList() : SpawnManager.Instance.GetSelectedMapData();
            totalMapsDownloading = maps.Count;
            failedDownloads.Clear();
            downloadedMapsCount = 0;
            UIManager.Instance.UpdateDownloadProgress(1, maps.Count, null);
            foreach (var map in maps)
            {
                if (cancelDownload)
                {
                    OnDownloadStopped();
                    yield break;
                }
                else
                {
                    //Check if map is already downloaded. No need to spend unnecessary bandwidth :)
                    if (map.Downloaded)
                    {
                        OnDownloadComplete(map, true);
                        yield return null;
                    }
                    else
                    {
                        yield return StartCoroutine(APIHandler.DownloadMap(map, OnDownloadComplete));
                    }
                }
            }
            UIManager.Instance.UpdateFailedMapsCount(failedDownloads.Count);
        }
        /// <summary>
        /// Used as callback function for downloaded maps. Updates UI, data and adds map to the recents panel.
        /// </summary>
        /// <param name="data">The MapData of the downloaded map.</param>
        /// <param name="success">True if download was successful.</param>
        private void OnDownloadComplete(MapData data, bool success)
        {
            if (!data.Downloaded)
            {
                if (!success) failedDownloads.Add(data);
                data.SetDownloaded(success);
                if (data.SelectedEntry)
                {
                    //Play a little animation, celebrating our successful download.
                    data.SelectedEntry.OnDownloaded(success);
                }
                if (success) RecentsManager.AddRecent(data.Filename);
            }
            downloadedMapsCount++;
            UIManager.Instance.UpdateDownloadProgress(downloadedMapsCount + 1, totalMapsDownloading, data.SelectedEntry.GetComponent<RectTransform>());
        }
        #endregion

        #region Cancel and Retry
        /// <summary>
        /// Cancels ongoing downloads.
        /// </summary>
        /// <remarks>We have to let the current download finish before we cancel. Simply stopping the coroutine in the middle of a download is no good.</remarks>
        public void RequestCancel()
        {
            cancelDownload = true;
        }
        /// <summary>
        /// Gets called once the downloads stopped and closes the download overlay.
        /// </summary>
        private void OnDownloadStopped()
        {
            cancelDownload = false;
            UIManager.Instance.ShowDownloadOverlay(false);
        }
        /// <summary>
        /// Starts download procedure again with failed downloads.
        /// </summary>
        public void RetryFailedDownloads()
        {
            //remove successfully downloaded maps from the selection so we only show failed downloads in the list to avoid confusion.
            SpawnManager.Instance.RemoveDownloadedMapsFromSelection();
            Download(true);
        }
        #endregion
    }
}

