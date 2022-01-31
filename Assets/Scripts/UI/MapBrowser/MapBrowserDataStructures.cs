using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.MapBrowser
{
    public enum CurationState
    {
        None,
        Semi,
        Curated
    }

    public enum FilterState
    {
        All,
        Song,
        Artist,
        Mapper
    }

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
        public MapBrowserEntry BrowserEntry => MapEntrySpawnManager.Instance.GetBrowserEntry(this);
        public SelectedMapEntry SelectedEntry => MapEntrySpawnManager.Instance.GetSelectedMapEntry(this);
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

        public void SetSelected(bool selected)
        {
            Selected = selected;
        }

        public void SetDownloaded(bool downloaded)
        {
            this.Downloaded = downloaded;
        }
    } 
}
