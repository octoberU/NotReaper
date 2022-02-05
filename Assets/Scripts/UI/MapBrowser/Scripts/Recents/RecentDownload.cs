using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace NotReaper.MapBrowser.Recents
{
    /// <summary>
    /// Represents a recent download.
    /// </summary>
    public class RecentDownload : MonoBehaviour
    {
        private TextMeshProUGUI label;
        private RecentsManager manager;
        private string fileName;


        private void Awake()
        {
            label = GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <summary>
        /// Set the RecentsManager.
        /// </summary>
        /// <param name="manager">The RecentsManager.</param>
        public void Initialize(RecentsManager manager)
        {
            this.manager = manager;
        }
        /// <summary>
        /// Set the filename for this recent download.
        /// </summary>
        /// <param name="fileName">The filename of the recent download.</param>
        public void SetFilename(string fileName)
        {
            this.fileName = fileName;
            label.text = this.fileName.Substring(0, this.fileName.Length - 7);
        }

        /// <summary>
        /// Called through UI when this RecentDownload is clicked.
        /// </summary>
        public void OnClick()
        {
            if (manager is null)
            {
                Debug.LogWarning("RecentDownload hasn't been initialized!");
                return;
            }
            manager.LoadMap(fileName);
        }
    }

}
