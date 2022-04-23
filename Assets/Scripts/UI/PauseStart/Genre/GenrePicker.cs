using Newtonsoft.Json;
using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NotReaper.Genres
{
    public class GenrePicker : MonoBehaviour
    {
        [SerializeField] private TagManager tagManager;
        [SerializeField] private NRDropdown genreDrop;
        [SerializeField] private NRInputField filterInput;

        private string genreFilePath;
        private List<Genre> genres = new();

        private void Awake()
        {
            
            genreFilePath = Path.Combine(Application.dataPath, "StreamingAssets", "genres.json");
            LoadGenres();
            OnGenreChanged();
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

        public void OnGenreSearchChanged() => genreDrop.FilterItems(filterInput.text);

        public void OnGenreChanged()
        {
            if(genres.Any(g => g.name.Equals(genreDrop.valueString, System.StringComparison.InvariantCultureIgnoreCase)))
            {
                Genre genre = genres.First(g => g.name.Equals(genreDrop.valueString, System.StringComparison.InvariantCultureIgnoreCase));
                tagManager.SetSuggestions(genre.subgenres);
            }
        }

        internal string GetGenre() => genreDrop.valueString;
        internal List<string> GetTags() => tagManager.GetTags();
    }

}
