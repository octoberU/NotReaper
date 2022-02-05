using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.MapBrowser.API.Cache
{
    /// <summary>
    /// Handles caching of search queries and responses.
    /// </summary>
    public class MapBrowserCache
    {
        private Dictionary<string, CacheEntry> cache;

        /// <summary>
        /// Initializes cache dictionary.
        /// </summary>
        public MapBrowserCache()
        {
            cache = new Dictionary<string, CacheEntry>();
        }
        /// <summary>
        /// Caches a request and it's response.
        /// </summary>
        /// <param name="requestUrl">The request to cache.</param>
        /// <param name="maps">The reponse for that request as MapData list.</param>
        /// <param name="hasMore">hasMore of response.</param>
        /// <param name="count">count of response.</param>
        public void CacheQuery(string requestUrl, List<MapData> maps, bool hasMore, int count)
        {
            if (cache.ContainsKey(requestUrl)) return;
            cache.Add(requestUrl, new CacheEntry(maps, hasMore, count));
        }
        /// <summary>
        /// Checks if a request has been chached.
        /// </summary>
        /// <param name="requestUrl">The request to check for.</param>
        /// <param name="cachedEntry">Populates cachedEntry if the request has been found in cache, else sets it to null.</param>
        /// <returns>True if the request has been cached.</returns>
        public bool CheckCache(string requestUrl, out CacheEntry cachedEntry)
        {
            if (cache.ContainsKey(requestUrl))
            {
                cachedEntry = cache[requestUrl];
                return true;
            }

            cachedEntry = null;
            return false;
        }
        /// <summary>
        /// Deselects a cached SearchEntry.
        /// </summary>
        /// <param name="requestUrl">The request of that cached entry.</param>
        /// <param name="mapId">The ID of the cached map.</param>
        public void DeselectCachedSearchEntry(string requestUrl, int mapId)
        {
            if (cache.ContainsKey(requestUrl))
            {
                cache[requestUrl].maps.Where(m => m.ID == mapId).First().SetSelected(false);
            }
        }
        /// <summary>
        /// Empties the cache.
        /// </summary>
        public void ClearCache()
        {
            cache.Clear();
        }
        /// <summary>
        /// Holds necessary data that's needed for caching.
        /// </summary>
        public class CacheEntry
        {
            public List<MapData> maps;
            public bool hasMore;
            public int count;
            public CacheEntry(List<MapData> maps, bool hasMore, int count)
            {
                this.maps = maps;
                this.hasMore = hasMore;
                this.count = count;
            }
        }
    }
}

