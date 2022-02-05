using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotReaper.MapBrowser.Entries;

//Holds all data structures needed for MapBrowser.
namespace NotReaper.MapBrowser
{
    /// <summary>
    /// Represents the CurationState of a map.
    /// </summary>
    public enum CurationState
    {
        None,
        Semi,
        Curated
    }
    /// <summary>
    /// Represents FilterState selected in the UI.
    /// </summary>
    public enum FilterState
    {
        All,
        Song,
        Artist,
        Mapper
    }

    /// <summary>
    /// Used for API search response.
    /// </summary>
    [Serializable]
    public class APISongList
    {
        public Song[] maps = null;
        public bool has_more = false;
        public int count = 0;
    }

    /// <summary>
    /// Used for API search response.
    /// </summary>
    [Serializable]
    public class Song
    {
        public int id = 0;
        public string created_at = null;
        public string updated_at = null;
        public string title = null;
        public string artist = null;
        public string author = null;
        public string[] difficulties = null;
        public string description = null;
        public string embed_url = null;
        public string filename = null;
        public bool curated = false;
    }
    /// <summary>
    /// Represents a map.
    /// </summary>
    public class MapData
    {
        public int ID { get; }
        public string SongName { get; }
        public string Artist { get; }
        public string Mapper { get; }
        public bool Beginner { get; }
        public bool Standard { get; }
        public bool Advanced { get; }
        public bool Expert { get; }
        public bool Selected { get; private set; }
        public CurationState State { get; }
        public string RequestUrl { get; }
        public string Filename { get; }
        public bool Downloaded { get; private set; }
        public SearchEntry BrowserEntry => SpawnManager.Instance.GetSearchEntry(this);
        public SelectedEntry SelectedEntry => SpawnManager.Instance.GetSelectedEntry(this);
        public MapData(int id, string songName, string artist, string mapper, bool curated, string filename, string requestUrl, bool downloaded, params string[] difficulties)
        {
            this.ID = id;
            this.SongName = songName;
            this.Artist = artist;
            this.Mapper = mapper;
            this.State = curated ? CurationState.Curated : CurationState.None;
            Beginner = Standard = Advanced = Expert = false;
            this.Selected = false;
            this.RequestUrl = requestUrl;
            this.Filename = filename;
            this.Downloaded = downloaded;
            for(int i = 0; i < difficulties.Length; i++)
            {
                switch (difficulties[i])
                {
                    case "beginner":
                        Beginner = true;
                        break;
                    case "moderate":
                        Standard = true;
                        break;
                    case "advanced":
                        Advanced = true;
                        break;
                    case "expert":
                        Expert = true;
                        break;
                    default:
                        break;
                }
            }
        }
        /// <summary>
        /// Sets this map selected.
        /// </summary>
        /// <param name="selected">True if selected.</param>
        public void SetSelected(bool selected)
        {
            Selected = selected;
        }
        /// <summary>
        /// Sets this map downloaded.
        /// </summary>
        /// <param name="downloaded">True if downloaded.</param>
        public void SetDownloaded(bool downloaded)
        {
            this.Downloaded = downloaded;
        }
    } 
}
