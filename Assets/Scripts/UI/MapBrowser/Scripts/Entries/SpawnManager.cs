using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NotReaper.MapBrowser.UI;

namespace NotReaper.MapBrowser.Entries
{
    /// <summary>
    /// Handles spawning of search- and selected entries with object pooling.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        #region References
        [Header("Search Entries")]
        [SerializeField] private SearchEntry searchEntryPrefab;
        [SerializeField] private Transform searchEntryParent;
        [Space, Header("Selected Entries")]
        [SerializeField] private SelectedEntry selectedEntryPrefab;
        [SerializeField] private Transform selectedEntryParent;
        #endregion

        public static SpawnManager Instance { get; private set; } = null;
        public int SelectedMapsCount => activeSelectedEntries.Count;

        #region Fields
        private List<SearchEntry> searchPool = new List<SearchEntry>();
        private List<SearchEntry> activeSearchEntries = new List<SearchEntry>();

        private List<SelectedEntry> selectedPool = new List<SelectedEntry>();
        private List<SelectedEntry> activeSelectedEntries = new List<SelectedEntry>();
        #endregion

        private void Awake()
        {
            if(Instance != null)
            {
                Debug.Log("SpawnManager already exists.");
                return;
            }
            Instance = this;
        }

        #region Object Pooling

        #region SearchEntry
        /// <summary>
        /// Spawns a search entry.
        /// </summary>
        /// <param name="data">The MapData to assing to the entry.</param>
        /// <returns>The spawned SearchEntry.</returns>
        public SearchEntry SpawnSearchEntry(MapData data)
        {
            SearchEntry entry = null;
            //Instantiate a new search entry if we have none available in our pool.
            if (searchPool.Count == 0)
            {
                entry = InstantiateSearchEntry();
            }
            //else get one from the pool
            else
            {
                entry = searchPool[0];
                searchPool.RemoveAt(0);
                entry.gameObject.SetActive(true);
            }
            entry.SetMapData(data);
            entry.transform.SetParent(searchEntryParent);
            activeSearchEntries.Add(entry);
            return entry;
        }
        /// <summary>
        /// Instantiates a new SearchEntry.
        /// </summary>
        /// <returns>The instantiated SearchEntry.</returns>
        private SearchEntry InstantiateSearchEntry()
        {
            return Instantiate(searchEntryPrefab, searchEntryParent);
        }
        /// <summary>
        /// Remove a SearchEntry, clear it's data and return it to the pool.
        /// </summary>
        /// <param name="entry">The SearchEntry to remove.</param>
        public void RemoveSearchEntry(SearchEntry entry)
        {
            if (entry is null || !activeSearchEntries.Contains(entry)) return;
            searchPool.Add(entry);
            activeSearchEntries.Remove(entry);
            entry.ClearData();
            entry.gameObject.SetActive(false);
        }
        #endregion

        #region SelectedEntry
        /// <summary>
        /// Spawns a SelectedEntry.
        /// </summary>
        /// <param name="data">The MapData to assing to the entry.</param>
        /// <returns>The spawned SelecteddEntry.</returns>
        public SelectedEntry SpawnSelectedEntry(MapData data)
        {
            SelectedEntry entry = null;
            //Instantiate a new search entry if we have none available in our pool.
            if (selectedPool.Count == 0)
            {
                entry = InstantiateSelectedEntry();
            }
            //else get one from the pool
            else
            {
                entry = selectedPool[0];
                selectedPool.RemoveAt(0);
                entry.gameObject.SetActive(true);
            }
            entry.SetData(data);
            entry.transform.SetParent(selectedEntryParent);
            activeSelectedEntries.Add(entry);
            UIManager.Instance.UpdateSelectedCount(SelectedMapsCount);
            return entry;
        }
   
        /// <summary>
        /// Instantiate a new SelectedEntry.
        /// </summary>
        /// <returns>The instantiated SelectedEntry.</returns>
        private SelectedEntry InstantiateSelectedEntry()
        {
            return Instantiate(selectedEntryPrefab, selectedEntryParent);
        }

        /// <summary>
        /// Remove a SelectedEntry, clear it's data and return it to the pool.
        /// </summary>
        /// <param name="entry">The SelectedEntry to remove.</param>
        public void RemoveSelectedEntry(SelectedEntry entry)
        {
            if (entry is null || !activeSelectedEntries.Contains(entry)) return;
            selectedPool.Add(entry);
            activeSelectedEntries.Remove(entry);
            SetCorrespondingSearchEntrySelected(entry.Data, false);
            entry.ClearData();
            entry.transform.SetParent(null);
            entry.gameObject.SetActive(false);
            UIManager.Instance.UpdateSelectedCount(SelectedMapsCount);
        }
        #endregion

        #endregion

        #region Selection
        /// <summary>
        /// Removes all active SearchEntries and returns them to the pool.
        /// </summary>
        public void ClearSearchEntries()
        {
            for (int i = activeSearchEntries.Count - 1; i >= 0; i--)
            {
                RemoveSearchEntry(activeSearchEntries[i]);
            }
            activeSearchEntries.Clear();
        }
        /// <summary>
        /// Removes all active SelectedEntries and returns them to the pool.
        /// </summary>
        public void ClearSelectedEntries()
        {
            for(int i = activeSelectedEntries.Count - 1; i >= 0; i--)
            {
                RemoveSelectedEntry(activeSelectedEntries[i]);
            }
            activeSelectedEntries.Clear();
        }

        /// <summary>
        /// Sets the SearchEntry belonging to a SelectedEntry to selected.
        /// </summary>
        /// <param name="data">The MapData of the SelectedEntry.</param>
        /// <param name="selected">True if selected.</param>
        private void SetCorrespondingSearchEntrySelected(MapData data, bool selected)
        {
            var map = activeSearchEntries.Where(m => m.Data.ID == data.ID).FirstOrDefault();
            if (map != null) map.SetSelected(selected);
        }
        /// <summary>
        /// Removes downloaded maps from active SelectedEntries.
        /// </summary>
        public void RemoveDownloadedMapsFromSelection()
        {
            for(int i = activeSelectedEntries.Count - 1; i >= 0; i--)
            {
                var entry = activeSelectedEntries[i];
                if (entry.Data.Downloaded) RemoveSelectedEntry(entry);
                else entry.ResetAnimations();
            }
        }
        #endregion

        #region Getter
        /// <summary>
        /// Get MapData of all selected maps.
        /// </summary>
        /// <returns>A List of MapData from selected entries.</returns>
        public List<MapData> GetSelectedMapData()
        {
            List<MapData> data = new List<MapData>();
            foreach(var map in activeSelectedEntries)
            {
                data.Add(map.Data);
            }
            return data;
        }
        /// <summary>
        /// Get a SelectedEntry.
        /// </summary>
        /// <param name="data">The MapData of the entry you want.</param>
        /// <returns>The SelectedEntry if found, null if no match found.</returns>
        public SelectedEntry GetSelectedEntry(MapData data)
        {
            return activeSelectedEntries.Where(m => m.Data.ID == data.ID).FirstOrDefault();
        }
        /// <summary>
        /// Get a SearchEntry.
        /// </summary>
        /// <param name="data">The MapData of the entry you want.</param>
        /// <returns>The SearchEntry if found, null if no match found.</returns>
        public SearchEntry GetSearchEntry(MapData data)
        {
            return activeSearchEntries.Where(m => m.Data.ID == data.ID).FirstOrDefault();
        }
        /// <summary>
        /// Returns all active SearchEntries.
        /// </summary>
        /// <returns>List of all active SearchEntries.</returns>
        public List<SearchEntry> GetSearchEntries()
        {
            return activeSearchEntries;
        }
        /// <summary>
        /// Returns all active SelectedEntries.
        /// </summary>
        /// <returns>List of all active SelectedEntries.</returns>
        public List<SelectedEntry> GetSelectedEntries()
        {
            return activeSelectedEntries;
        }
        #endregion

    }

}
