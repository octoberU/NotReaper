using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using NotReaper.UI.Components;
using AudicaTools;

namespace NotReaper.MapBrowser.Recents
{
    /// <summary>
    /// Represents a recent download.
    /// </summary>
    public class RecentDownload : MonoBehaviour
    {
        private RecentType type = RecentType.Download;
        private NRButton button;
        private RecentsManager manager;
        private Audica audicaFile;
        private MapData data;

        private void Awake()
        {
            button = GetComponent<NRButton>();
        }

        /// <summary>
        /// Set the RecentsManager.
        /// </summary>
        /// <param name="manager">The RecentsManager.</param>
        public void Initialize(RecentsManager manager, RecentType type)
        {
            this.manager = manager;
            this.type = type;
        }
        /// <summary>
        /// Set the filename for this recent download.
        /// </summary>
        /// <param name="fileName">The filename of the recent download.</param>
        public void SetFilename(Audica audica)
        {
            this.audicaFile = audica;
            var color = NRSettings.config.rightColor;
            button.SetText($"{audica.desc.title} - {audica.desc.artist}\n<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{audica.desc.author}");//this.fileName.Substring(0, this.fileName.Length - 7).ToLower();
        }
        public void SetFilename(MapData data)
        {
            var color = NRSettings.config.rightColor;
            button.SetText($"{data.SongName} - {data.Artist}\n<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{data.Mapper}");
        }
        /// <summary>
        /// Set the MapData of this recent release.
        /// </summary>
        /// <param name="map">The MapData of the recent release.</param>
        public void SetMapData(MapData map)
        {
            data = map;
            SetFilename(data);
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
            if(type == RecentType.Download)
            {
                manager.LoadMap(audicaFile.fileName + ".audica");
            }
            else
            {
                button.SetText("downloading..");
                manager.DownloadAndOpenMap(data, OnDownloadDone);
            }
        }

        private void OnDownloadDone()
        {
            SetFilename(data);
        }
    }

}
