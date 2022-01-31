using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.RecentDownloads
{
    public class RecentWindow : MonoBehaviour
    {
        [SerializeField] private RecentDownload[] recents;
        [SerializeField] private GameObject clearButton;
        private RecentsManager manager;

        private void Awake()
        {
            manager = GetComponent<RecentsManager>();
            foreach (var recent in recents) recent.Initialize(manager);
        }

        public void UpdateRecents(List<string> fileNames)
        {
            if (fileNames is null || fileNames.Count == 0)
            {
                foreach (var recent in recents) recent.gameObject.SetActive(false);
                clearButton.SetActive(false);
                return;
            }
            clearButton.SetActive(true);
            for(int i = 0; i < fileNames.Count; i++)
            {
                recents[i].gameObject.SetActive(true);
                recents[i].SetFilename(fileNames[i]);
            }
        }

        public void OnClearClicked()
        {
            UpdateRecents(null);
            manager.ClearRecents();
        }
    }
}

