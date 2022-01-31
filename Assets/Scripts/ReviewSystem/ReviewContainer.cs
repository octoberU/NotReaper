using Newtonsoft.Json;
using NotReaper.Models;
using NotReaper.Targets;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NotReaper.Managers;
namespace NotReaper.ReviewSystem
{
    [System.Serializable]
    public class ReviewContainer
    {
        public string songID;
        public List<ReviewComment> comments = new List<ReviewComment>();
        public string reviewAuthor;
        public int difficulty = -1;

        public ReviewContainer(string songID)
        {
            this.songID = songID;
        }

        public ReviewContainer()
        {
            //this.songID = Timeline.desc.songID;
        }

        public static ReviewContainer Read(string path)
        {
            if (File.Exists(path)) return JsonConvert.DeserializeObject<ReviewContainer>(File.ReadAllText(path));
            else return default;
        }

        public void Export()
        {
            songID = Timeline.desc.songID;
            difficulty = DifficultyManager.I.loadedIndex;
            string difficultyText = DifficultyManager.I.GetDifficultyText();
            string dataDirectory = Application.dataPath;
            string exportFolder = Path.Combine(Directory.GetParent(dataDirectory).ToString(), "reviews");
            if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
            string reviewText = JsonConvert.SerializeObject(this);
            string exportPath = Path.Combine(exportFolder, $"{songID}_{difficultyText}_{reviewAuthor}.review");
            File.WriteAllText(exportPath, reviewText);
        }
    }

    [System.Serializable]
    public class ReviewComment
    {
        public Cue[] selectedCues;
        public Cue[] suggestionCues;
        public string description;
        public CommentType type;
        public bool isChecked;
        [System.NonSerialized, JsonIgnore] public CommentEntry entry;
        [JsonIgnore] public bool HasSuggestion => suggestionCues != null && suggestionCues.Length > 0;
        [JsonIgnore] public bool HasSelectedCues => selectedCues != null && selectedCues.Length > 0;
        [JsonConstructor]
        public ReviewComment(Cue[] selectedCues, string description, CommentType type, Cue[] suggestionCues = null, bool isChecked = false, CommentEntry entry = null)
        {
            this.selectedCues = selectedCues;
            this.description = description;
            this.type = type;
            this.isChecked = isChecked;
            this.entry = entry;
            this.suggestionCues = suggestionCues;
        }
        public ReviewComment()
        {
        }
    }
    
    [System.Serializable]
    public enum CommentType
    {
        Negative,
        Positive,
        Suggestion,
    }
}