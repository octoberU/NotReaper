using Newtonsoft.Json;
using NotReaper.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NotReaper.RecentDownloads
{
    public class RecentsManager : MonoBehaviour
    {
        private string downloadsFolder;
        private static string recentDownloadsPath;
        private static List<string> recentDownloads = null;
        private static RecentWindow window = null;
        private void Awake()
        {
            downloadsFolder = Path.Combine(Application.dataPath, @"../", "downloads");
            recentDownloadsPath = Path.Combine(Application.persistentDataPath, "recentDownloads.json");
            window = GetComponent<RecentWindow>();
        }

        private void Start()
        {
            LoadRecents();
            window.UpdateRecents(recentDownloads);
        }

        public static void AddRecent(string fileName)
        {
            if (recentDownloads.Contains(fileName)) recentDownloads.Remove(fileName);
            recentDownloads.Insert(0, fileName);
            if (recentDownloads.Count > 5) recentDownloads = recentDownloads.GetRange(0, 5);
            SaveRecents();
            window.UpdateRecents(recentDownloads);
        }

        private static void SaveRecents()
        {
            string text = JsonConvert.SerializeObject(recentDownloads);
            File.WriteAllText(recentDownloadsPath, text);
        }

        private void LoadRecents()
        {
            if (recentDownloads != null) return;
            if (File.Exists(recentDownloadsPath))
            {
                try
                {
                    string text = File.ReadAllText(recentDownloadsPath);
                    recentDownloads = JsonConvert.DeserializeObject<List<string>>(text);
                }
                catch (Exception e)
                {
                    Debug.Log("Error: " + e);
                    throw;
                }
            }
            else
            {
                recentDownloads = new List<string>();
            }
        }

        public void LoadMap(string filename)
        {
            string path = Path.Combine(downloadsFolder, filename);
            Timeline.instance.LoadAudicaFile(false, path);
            PauseMenu.Instance.ClosePauseMenu();
        }

        public void ClearRecents()
        {
            recentDownloads = new List<string>();
            SaveRecents();
        }
    }
}

