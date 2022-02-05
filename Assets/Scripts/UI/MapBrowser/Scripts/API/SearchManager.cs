using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using NotReaper.MapBrowser.UI;
using NotReaper.MapBrowser.Entries;
using NotReaper.MapBrowser.API.Cache;
namespace NotReaper.MapBrowser.API
{
    /// <summary>
    /// Handles search operations.
    /// </summary>
    public class SearchManager : MonoBehaviour
    {
        public static SearchManager Instance { get; private set; } = null;

        #region Fields
        private APISongList songList;
        private MapBrowserCache cache;

        //Stored search parameters
        private string searchText;
        private CurationState curationState;
        private FilterState filterState;
        private bool[] difficulties;
        private int page = 1;
        private bool hasMore = false;

        private bool searchInProgress = false;
        private bool initialSearchDone = false;

        private List<string> localMaps = new List<string>();
        private List<string> localMapsCustomLocation = new List<string>();
        #endregion

        #region Initialization
        private void Awake()
        {
            if (Instance != null)
            {
                Debug.Log("SearchManager already exists.");
                return;
            }
            Instance = this;

            cache = new MapBrowserCache();
            //HandleLocalMaps should only be called once settings are loaded.
            NRSettings.OnLoad(() => HandleLocalMaps());
        }

        private void Start()
        {
            //Perform a search when MapBrowser gets opened for the first time to present the user with the newest results right away.
            if (!initialSearchDone)
            {
                initialSearchDone = true;
                Search("", CurationState.None, FilterState.All, new bool[] { false, false, false, false });
            }
        }
        /// <summary>
        /// Deletes downloads that are older than the days specified in settings, then stores all of the downloaded files in a list.
        /// </summary>
        /// <remarks>The localMaps list is used for later reference, so we can check if a map is downloaded.</remarks>
        private void HandleLocalMaps()
        {
            //handles maps in downloads folder
            List<string> files = Directory.GetFiles(Path.Combine(Application.dataPath, @"../", "downloads")).ToList();
            if (files.Count > 0)
            {
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    int days = NRSettings.config.downloadDeleteAfterDays;
                    if (days != 0 && fi.CreationTime < DateTime.Now.AddDays(days * -1)) fi.Delete();
                    else localMaps.Add(fi.Name);
                }
            }
            //handles maps in custom folder
            if(NRSettings.config.downloadCustomSaveLocation.Length >= 3)
            {
                files.Clear();
                files = Directory.GetFiles(NRSettings.config.downloadCustomSaveLocation).ToList();
                if(files.Count > 0)
                {
                    foreach(string file in files)
                    {
                        localMapsCustomLocation.Add(new FileInfo(file).Name);
                    }
                }
            }

        }
        #endregion
  
        #region Search
        /// <summary>
        /// Updates search parameters and queues up a request.
        /// </summary>
        /// <param name="searchText">The text to search for.</param>
        /// <param name="curationState">The curation state of maps.</param>
        /// <param name="filterState">What searchText gets compared against (search for song/artist/mapper only).</param>
        /// <param name="difficulties">The difficulties a map has to contain.</param>
        public void Search(string searchText, CurationState curationState, FilterState filterState, bool[] difficulties, int page = 1)
        {
            this.searchText = searchText;
            this.curationState = curationState;
            this.filterState = filterState;
            this.difficulties = difficulties;
            this.page = page;
            StartCoroutine(QueueSearch());
        }
        /// <summary>
        /// Queues up a search and waits if a search is already ongoing.
        /// </summary>
        /// <remarks>Since the input field triggers a search on every change, we make sure to never send multiple search requests at once and instead simply update the search paramters through <see cref="Search(string, CurationState, FilterState, bool[])"/></remarks>
        private IEnumerator QueueSearch()
        {
            while (searchInProgress)
            {
                yield return new WaitForSeconds(.1f);
            }
            searchInProgress = true;
            StartCoroutine(DoSearch(searchText, curationState, filterState, difficulties));
        }
        /// <summary>
        /// Performs a search request with the most up-to-date search parameters.
        /// </summary>
        /// <param name="searchText">The text to search for.</param>
        /// <param name="curationState">The curation state of maps.</param>
        /// <param name="filterState">What searchText should be compared against (search for song/artist/mapper only).</param>
        /// <param name="difficulties">Which difficulties a map must contain.</param>
        /// <returns></returns>
        private IEnumerator DoSearch(string searchText, CurationState curationState, FilterState filterState, bool[] difficulties)
        {
            SpawnManager.Instance.ClearSearchEntries();
            string requestUrl = APIHandler.GetRequestUrl(searchText, curationState, page, difficulties);
            int count;
            songList = null;
            List<MapData> maps = new List<MapData>();
            //check if we already performed the same request earlier and retreive it from cache.
            if (cache.CheckCache(requestUrl, out MapBrowserCache.CacheEntry entry))
            {
                count = entry.count;
                hasMore = entry.hasMore;
                maps = entry.maps;
            }
            else
            {
                yield return StartCoroutine(APIHandler.Search(requestUrl, OnSearchDone));
                count = songList.count;
                hasMore = songList.has_more;
            }

            if (count == 0)
            {
                UIManager.Instance.ShowNoResultsFound(true);
                searchInProgress = false;
                yield break;
            }
            //songList is only null if we retreived data from cache.
            if (songList != null)
            {
                for (int i = 0; i < songList.maps.Length; i++)
                {
                    var song = songList.maps[i];
                    var data = new MapData(song.id, song.title, song.artist, song.author, song.curated, song.filename, requestUrl, IsDownloaded(song.filename), song.difficulties);
                    maps.Add(data);
                }
                //chache the current request and response.
                cache.CacheQuery(requestUrl, maps, hasMore, count);
            }

            foreach (var data in maps)
            {
                //only show results that pass the filter.
                if (PassesFilter(data, searchText, filterState))
                {
                    SpawnManager.Instance.SpawnSearchEntry(data);
                }
            }
            UIManager.Instance.UpdateNavigation(hasMore, page != 1, page, count);
            searchInProgress = false;
        }
        /// <summary>
        /// Callback function for when search finishes. 
        /// </summary>
        /// <param name="songList">The API response as APISongList.</param>
        private void OnSearchDone(APISongList songList)
        {
            this.songList = songList;
        }
        /// <summary>
        /// Advances or returns one page of results.
        /// </summary>
        /// <param name="direction">1 for next page, -1 for previous page.</param>
        /// <returns>The new page.</returns>
        public int ChangePage(int direction)
        {
            if ((direction == 1 && !hasMore) || (direction == -1 && page == 1)) return page;
            page += direction;
            Search(searchText, curationState, filterState, difficulties, page);
            return page;
        }
        #endregion

        #region Helper Functions
        /// <summary>
        /// Checks if a search result passes the currently selected filter.
        /// </summary>
        /// <param name="map">The map to compare against.</param>
        /// <param name="searchText">The searchText to compare with.</param>
        /// <param name="filterState">The current filter state.</param>
        /// <returns>True if the map passes the filter.</returns>
        private bool PassesFilter(MapData map, string searchText, FilterState filterState)
        {
            searchText = searchText.ToLower();
            switch (filterState)
            {
                case FilterState.All:
                    return true;
                case FilterState.Song:
                    return map.SongName.ToLowerInvariant().Contains(searchText);
                case FilterState.Artist:
                    return map.Artist.ToLowerInvariant().Contains(searchText);
                case FilterState.Mapper:
                    return map.Mapper.ToLowerInvariant().Contains(searchText);
                default:
                    return true;
            }
        }
        /// <summary>
        /// Checks if a map is already available locally.
        /// </summary>
        /// <param name="filename">The filename to look for.</param>
        /// <returns>True if map is available locally.</returns>
        private bool IsDownloaded(string filename)
        {
            return NRSettings.config.downloadSaveLocation == 0 ? localMaps.Any(m => m == filename) : localMapsCustomLocation.Any(m => m == filename);
        }

        /// <summary>
        /// Checks each map again if it is downloaded. Use this after switching save location.
        /// </summary>
        public void RescanDownloadedMaps()
        {
            var entries = SpawnManager.Instance.GetSearchEntries();
            foreach (var entry in entries)
            {
                entry.Data.SetDownloaded(IsDownloaded(entry.Data.Filename));
                entry.UpdateDownloadedIcon();
            }
            cache.ClearCache();
        }
        #endregion

        #region Misc
        /// <summary>
        /// Reset the page of results back to 1.
        /// </summary>
        public void ResetPage()
        {
            page = 1;
        }
        /// <summary>
        /// Deselects a cached entry.
        /// </summary>
        /// <param name="requestUrl">The requestUrl of the cached map.</param>
        /// <param name="mapId">The ID of the cached map.</param>
        public void DeselectCachedMap(string requestUrl, int mapId)
        {
            cache.DeselectCachedSearchEntry(requestUrl, mapId);
        }
        #endregion


    }
}

