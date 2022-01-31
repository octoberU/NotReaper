using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using NotReaper.RecentDownloads;
using System.IO;

namespace NotReaper.MapBrowser
{
    public class MapBrowser : MonoBehaviour
    {
        public static MapBrowser Instance = null;
        public static bool CancelDownload = false;
        public static CurationState CurationState { get; set; } = CurationState.None;
        public static FilterState FilterState { get; set; } = FilterState.All;

        private APIHandler apiHandler = null;
        private MapBrowserCache cache = null;

        private string lastSearchText = "";
        private int page = 1;
        private bool hasMore = false;
        private int numMapsDownloading = 0;
        private int downloadedCount = 0;
        private APISongList songList = null;
        private List<MapData> failedMaps = new List<MapData>();
        private List<string> localMaps = new List<string>();
        private const int DeleteAfterDays = 7;
        private bool initialSearchDone = false;
        private int totalMapsDownloading = 0;
        private bool searchInProgress = false;
        private string searchText;
        private bool[] difficulties;
        private void Awake()
        {
            if (Instance != null)
            {
                Debug.Log("MapBrowser instance already exists.");
                return;
            }
            Instance = this;
            apiHandler = new APIHandler();
            cache = new MapBrowserCache();
            HandleLocalMaps();

           
            //foreach (var file in files) localMaps.Add(new FileInfo(file).Name);
        }

        private void Start()
        {
            if (!initialSearchDone)
            {
                initialSearchDone = true;
                Search("", new bool[] { false, false, false, false });
            }
        }
        private void HandleLocalMaps()
        {
            List<string> files = new List<string>();
            files = Directory.GetFiles(Path.Combine(Application.dataPath, @"../", "downloads")).ToList();
            if (files.Count == 0) return;

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (fi.CreationTime < DateTime.Now.AddDays(DeleteAfterDays * -1)) fi.Delete();
                else localMaps.Add(fi.Name);
            }
            
        }

        public void Search(string searchText, bool[] difficulties)
        {
            this.searchText = searchText;
            this.difficulties = difficulties;
            StartCoroutine(QueueSearch());
            /*
            if (searchInProgress)
            {
                StopAllCoroutines();
            }
            searchInProgress = true;
            StartCoroutine(DoSearch(searchText, difficulties));
            */
        }

        private IEnumerator QueueSearch()
        {
            while (searchInProgress)
            {
                yield return new WaitForSeconds(.1f);
            }
            StartCoroutine(DoSearch(searchText, difficulties));
        }

        private IEnumerator DoSearch(string searchText, bool[] difficulties)
        {
            if (apiHandler is null) apiHandler = new APIHandler();
            MapEntrySpawnManager.Instance.ClearEntries();
            string requestUrl = apiHandler.GetRequestUrl(searchText, CurationState, page, difficulties);
            int count;
            songList = null;
            List<MapData> maps = new List<MapData>();

            if(cache.CheckCache(requestUrl, out MapBrowserCache.CacheEntry entry))
            {
                count = entry.count;
                hasMore = entry.hasMore;
                maps = entry.maps;
            }
            else
            {
                //songList = apiHandler.APISearch(requestUrl);
                yield return StartCoroutine(apiHandler.APISearch(requestUrl, OnSearchDone));
                count = songList.count;
                hasMore = songList.has_more;
            }
            if (count == 0)
            {
                lastSearchText = "";
                MapBrowserWindow.Instance.EnableNoResults();
                yield break;
            }
            if(songList != null)
            {
                for (int i = 0; i < songList.maps.Length; i++)
                {
                    var song = songList.maps[i];
                    var data = new MapData(song.id, song.title, song.artist, song.author, song.curated, song.filename, requestUrl, IsDownloaded(song.filename), song.difficulties);
                    maps.Add(data);
                    //MapEntrySpawnManager.Instance.SpawnEntry(data);
                }
                cache.CacheQuery(requestUrl, maps, hasMore, count);
            }
            
            foreach(var data in maps)
            {
                if(PassesFilter(data, searchText))
                {
                    MapEntrySpawnManager.Instance.SpawnEntry(data);
                }
            }
            
            lastSearchText = searchText;  
            MapBrowserWindow.Instance.UpdateNavigation(hasMore, page != 1, page, count);
            searchInProgress = false;
        }

        private bool PassesFilter(MapData map, string searchText)
        {
            searchText = searchText.ToLower();
            switch (FilterState)
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

        private bool IsDownloaded(string filename)
        {
            return localMaps.Any(m => m == filename);
        }

        public int ChangePage(int direction, bool[] difficulties)
        {
            if ((direction == 1 && !hasMore) || (direction == -1 && page == 1)) return page;
            page += direction;
            Search(lastSearchText, difficulties);
            return page;
        }

        public void DeselectCachedEntry(string requestUrl, int mapId)
        {
            cache.DeselectCachedEntry(requestUrl, mapId);

        }

        public void Download(bool retryFailed = false)
        {
            StartCoroutine(DoDownload(retryFailed));
        }

        public void StopDownloads()
        {
            CancelDownload = true;
        }

        private void OnDownloadStopped()
        {
            CancelDownload = false;
            MapBrowserWindow.Instance.EnableDownloadOverlay(false);
        }

        public void RetryDownload()
        {
            MapEntrySpawnManager.Instance.RemoveDownloadedEntriesFromSelection();
            Download(true);
        }
        private IEnumerator DoDownload(bool retryFailed = false)
        {
            List<MapData> maps = retryFailed ? failedMaps.ToList() : MapEntrySpawnManager.Instance.GetSelectedMapData();
            totalMapsDownloading = maps.Count;
            failedMaps.Clear();
            downloadedCount = 0;
            numMapsDownloading = maps.Count;
            MapBrowserWindow.Instance.UpdateDownloadProgress(1, maps.Count, null);
            //bool success;
            foreach(var map in maps)
            {
                if (CancelDownload)
                {
                    OnDownloadStopped();
                    yield break;
                }               
                else
                {
                    if (map.Downloaded)
                    {
                        OnDownloadComplete(map, true);
                        yield return null;
                    }
                    else
                    {
                        yield return StartCoroutine(apiHandler.DownloadSong(map, OnDownloadComplete));
                    }
                }
            }
            MapBrowserWindow.Instance.SetFailedMaps(failedMaps.Count);
        }

        public void OnDownloadComplete(MapData data, bool success)
        {
            if (!data.Downloaded)
            {
                if (!success) failedMaps.Add(data);
                data.SetDownloaded(success);
                if (data.SelectedEntry)
                {
                    data.SelectedEntry.OnDownloaded(success);
                }
                if (success) RecentsManager.AddRecent(data.Filename);
            }        
            downloadedCount++;
            MapBrowserWindow.Instance.UpdateDownloadProgress(downloadedCount + 1, totalMapsDownloading, data.SelectedEntry.GetComponent<RectTransform>());
        }

        private void OnSearchDone(APISongList songList)
        {
            this.songList = songList;
        }

        public void AddPageToSelection()
        {
            var page = MapEntrySpawnManager.Instance.GetActiveEntries();
            MapEntrySpawnManager.Instance.ClearSelectedEntries();
            foreach(var map in page)
            {
                if(!map.Data.Downloaded) map.SetSelected(true);
            }
            //MapBrowserWindow.Instance.EnableDownloadOverlay(true);
            //StartCoroutine(DownloadSelectedMaps());
        }
    }
}

