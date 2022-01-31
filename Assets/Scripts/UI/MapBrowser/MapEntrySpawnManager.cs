using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.MapBrowser
{
    public class MapEntrySpawnManager : MonoBehaviour
    {
        public static MapEntrySpawnManager Instance = null;
        public int SelectedMapsCount => activeSelectedEntries.Count;
        [Header("Map Entries")]
        [SerializeField] private MapBrowserEntry prefab;
        [SerializeField] private Transform parent;
        [Space, Header("Selected Entries")]
        [SerializeField] private SelectedMapEntry selectedPrefab;
        [SerializeField] private Transform selectedParent;
        private List<MapBrowserEntry> mapPool = new List<MapBrowserEntry>();
        private List<MapBrowserEntry> activeMapEntries = new List<MapBrowserEntry>();

        private List<SelectedMapEntry> selectedPool = new List<SelectedMapEntry>();
        private List<SelectedMapEntry> activeSelectedEntries = new List<SelectedMapEntry>();

        private void Awake()
        {
            if(Instance != null)
            {
                Debug.Log("MapEntrySpawnManager already exists.");
                return;
            }
            Instance = this;
        }

        public MapBrowserEntry SpawnEntry(MapData data)
        {
            MapBrowserEntry entry = null;
            if(mapPool.Count == 0)
            {
                entry = InstantiateEntry(data);
            }
            else
            {
                entry = mapPool[0];
                mapPool.RemoveAt(0);
                entry.gameObject.SetActive(true);
            }
            entry.SetMapData(data);
            entry.transform.SetParent(parent);
            activeMapEntries.Add(entry);
            return entry;
        }

        public SelectedMapEntry SpawnSelectedEntry(MapData data)
        {
            SelectedMapEntry entry = null;
            if(selectedPool.Count == 0)
            {
                entry = InstantiateSelectedEntry(data);
            }
            else
            {
                entry = selectedPool[0];
                selectedPool.RemoveAt(0);
                entry.gameObject.SetActive(true);
            }
            entry.SetData(data);
            entry.transform.SetParent(selectedParent);
            activeSelectedEntries.Add(entry);
            MapBrowserWindow.Instance.UpdateSelectedCount(SelectedMapsCount);
            return entry;
        }

        public void RemoveEntry(MapBrowserEntry entry)
        {
            if (entry is null || !activeMapEntries.Contains(entry)) return;
            mapPool.Add(entry);
            activeMapEntries.Remove(entry);
            //entry.SetSelected(false);
            entry.ClearData();
            entry.transform.SetParent(null);
            entry.gameObject.SetActive(false);
        }

        public void RemoveSelectedEntry(SelectedMapEntry entry)
        {
            if (entry is null || !activeSelectedEntries.Contains(entry)) return;
            selectedPool.Add(entry);
            activeSelectedEntries.Remove(entry);
            SetCorrespondingEntrySelected(entry.Data, false);
            entry.ClearData();
            entry.transform.SetParent(null);
            entry.gameObject.SetActive(false);
            MapBrowserWindow.Instance.UpdateSelectedCount(SelectedMapsCount);
        }

        public void ClearEntries()
        {
            for (int i = activeMapEntries.Count - 1; i >= 0; i--)
            {
                RemoveEntry(activeMapEntries[i]);
            }
            activeMapEntries.Clear();
        }

        public void ClearSelectedEntries()
        {
            for(int i = activeSelectedEntries.Count - 1; i >= 0; i--)
            {
                RemoveSelectedEntry(activeSelectedEntries[i]);
            }
            activeSelectedEntries.Clear();
        }

        private void SetCorrespondingEntrySelected(MapData data, bool selected)
        {
            var map = activeMapEntries.Where(m => m.Data.ID == data.ID).FirstOrDefault();
            if (map != null) map.SetSelected(selected);
        }

        private MapBrowserEntry InstantiateEntry(MapData data)
        {
           return Instantiate(prefab, parent);
        }

        private SelectedMapEntry InstantiateSelectedEntry(MapData data)
        {
            return Instantiate(selectedPrefab, selectedParent);
        }

        public List<MapData> GetSelectedMapData()
        {
            List<MapData> data = new List<MapData>();
            foreach(var map in activeSelectedEntries)
            {
                data.Add(map.Data);
            }
            return data;
        }

        public SelectedMapEntry GetSelectedMapEntry(MapData data)
        {
            return activeSelectedEntries.Where(m => m.Data.ID == data.ID).FirstOrDefault();
        }

        public MapBrowserEntry GetBrowserEntry(MapData data)
        {
            return activeMapEntries.Where(m => m.Data.ID == data.ID).FirstOrDefault();
        }

        public List<MapBrowserEntry> GetActiveEntries()
        {
            return activeMapEntries;
        }

        public List<SelectedMapEntry> GetSelectedEntries()
        {
            return activeSelectedEntries;
        }

        public void RemoveDownloadedEntriesFromSelection()
        {
            for(int i = activeSelectedEntries.Count - 1; i >= 0; i--)
            {
                var entry = activeSelectedEntries[i];
                if (entry.Data.Downloaded) RemoveSelectedEntry(entry);
                else entry.ResetAnimation();
            }
        }
    }

}
