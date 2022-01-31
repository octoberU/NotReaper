using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace NotReaper.RecentDownloads
{
    public class RecentDownload : MonoBehaviour
    {
        private TextMeshProUGUI label;
        private RecentsManager manager;
        private string fileName;

        private void Awake()
        {
            label = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Initialize(RecentsManager manager)
        {
            this.manager = manager;
        }

        public void SetFilename(string name)
        {
            fileName = name;
            label.text = fileName.Substring(0, fileName.Length - 7);
        }

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
