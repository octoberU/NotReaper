using Newtonsoft.Json;
using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NotReaper.Genres
{
    public class GenrePicker : MonoBehaviour
    {
        [SerializeField] private NRButton genreEntry;
        [SerializeField] private NRDropdown genreDrop;
        private string genreFilePath;

        private List<Genre> genres = new();

        private void Awake()
        {
            /*
            genreFilePath = Path.Combine(Application.dataPath, "StreamingAssets", "genres.json");
            LoadGenres();
            */
        }

        public void LoadGenres()
        {
            var json = File.ReadAllText(genreFilePath);
            genres = JsonConvert.DeserializeObject<List<Genre>>(json);

            foreach(var genre in genres)
            {
                genreDrop.AddItem(genre.name);
            }

        }

        public void OnGenreSearchChanged(string filter)
        {
            filter = filter.ToLower();

            genreDrop.FilterItems(filter);
        }
    }

}
