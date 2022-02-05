using System.Net;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace NotReaper.MapBrowser.API
{
    /// <summary>
    /// Handles calls to the maudica API.
    /// </summary>
    public static class APIHandler
    {
        #region Fields
        private const string ApiUrl = "https://maudica.com/api/maps?per_page=20";
        private const string DownloadUrlFormat = "https://maudica.com/maps/{0}/download";
        private const string PreviewUrlFormat = "https://maudica.com/maps/{0}/preview";
        private static string DownloadFolder = Path.Combine(Application.dataPath, @"../", "downloads");
        #endregion
        /// <summary>
        /// Creates downloads directory if it doesn't exist already.
        /// </summary>
        static APIHandler()
        {
            if (!Directory.Exists(DownloadFolder)) Directory.CreateDirectory(DownloadFolder);
        }

        #region Search
        /// <summary>
        /// Builds the request URL with the supplied parameters.
        /// </summary>
        /// <param name="searchText">The text to search for.</param>
        /// <param name="curationState">The curation state to search for.</param>
        /// <param name="page">The page of results you want to receive.</param>
        /// <param name="difficulties">The difficulties the maps must contain.</param>
        /// <returns></returns>
        internal static string GetRequestUrl(string searchText, CurationState curationState, int page, bool[] difficulties)
        {
            string webSearch = searchText == null || searchText == "" ? "" : "&search=" + WebUtility.UrlEncode(searchText);
            string webPage = page == 1 ? "" : "&page=" + page.ToString();
            string webDifficulty = "";
            if (difficulties[0]) webDifficulty += "&difficulties%5B%5D=beginner";
            if (difficulties[1]) webDifficulty += "&difficulties%5B%5D=moderate";
            if (difficulties[2]) webDifficulty += "&difficulties%5B%5D=advanced";
            if (difficulties[3]) webDifficulty += "&difficulties%5B%5D=expert";
            //TODO: AlmostCurated is not accessible through API yet. Implement once that's available.
            string curated = curationState == CurationState.None ? "" : "&curated=true";
            return ApiUrl + webSearch + webPage + webDifficulty + curated;
        }
        /// <summary>
        /// Delegate function for when search is done. Supplies APISongList from API response.
        /// </summary>
        /// <param name="songlist">The APISongList received from the API.</param>
        internal delegate void OnSearchDone(APISongList songlist);

        /// <summary>
        /// Sends a search request to the API with the given requestUrl.
        /// </summary>
        /// <param name="requestUrl">The request to send to the API. Build it using <see cref="GetRequestUrl"/></see></param>
        /// <param name="callback">Callback that gets invoked after receiving a response from the API.</param>
        /// <returns></returns>
        internal static IEnumerator Search(string requestUrl, OnSearchDone callback)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(requestUrl))
            {
                www.timeout = 20;
                yield return www.SendWebRequest();
                callback.Invoke(JsonConvert.DeserializeObject<APISongList>(www.downloadHandler.text));
            }          
        }
        #endregion

        #region Download
        /// <summary>
        /// Delegate function for when a download completes. Supplies MapData of downloaded map and sets success = true if the download was successful.
        /// </summary>
        /// <param name="data">The MapData of the downloaded map.</param>
        /// <param name="success">Indicates if the download was successful.</param>
        internal delegate void OnDownloadComplete(MapData data, bool success);

        /// <summary>
        /// Downloads a map from the API using the supplied MapData.
        /// </summary>
        /// <param name="map">The map to download.</param>
        /// <param name="callback">Callback that gets invoked after the downloaded completed.</param>
        /// <returns></returns>
        internal static IEnumerator DownloadMap(MapData map, OnDownloadComplete callback)
        {
            bool success;
            string customLocation = NRSettings.config.downloadCustomSaveLocation;
            string outputFile = Path.Combine(NRSettings.config.downloadSaveLocation == 0 ? DownloadFolder : customLocation.Length >= 3 ? customLocation : DownloadFolder, map.Filename);
            //No need to download the map again if it already exists. Simply return success.
            if (File.Exists(outputFile))
            {
                callback.Invoke(map, true);
                yield break;
            }
            string downloadUrl = string.Format(DownloadUrlFormat, map.ID);
            DownloadHandlerFile handler = new DownloadHandlerFile(outputFile);
            handler.removeFileOnAbort = true;
            UnityWebRequest www = new UnityWebRequest(downloadUrl);
            www.timeout = 30;
            www.downloadHandler = handler;
            yield return www.SendWebRequest();          
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                success = false;
            }
            else
            {
                success = true;
            }
            handler.Dispose();
            www.Dispose();           
            callback.Invoke(map, success);
        }
        #endregion
    }
}

