using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.MapBrowser
{
    public class MapBrowserCache
    {
        private Dictionary<string, CacheEntry> cache;

        public MapBrowserCache()
        {
            cache = new Dictionary<string, CacheEntry>();
        }

        public void CacheQuery(string requestUrl, List<MapData> maps, bool hasMore, int count)
        {
            if (cache.ContainsKey(requestUrl)) return;
            cache.Add(requestUrl, new CacheEntry(maps, hasMore, count));
        }

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

        public void DeselectCachedEntry(string requestUrl, int mapId)
        {
            if (cache.ContainsKey(requestUrl))
            {
                cache[requestUrl].maps.Where(m => m.ID == mapId).First().SetSelected(false);
            }
        }

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

