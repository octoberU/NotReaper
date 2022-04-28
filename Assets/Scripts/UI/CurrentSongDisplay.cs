using NotReaper;
using NotReaper.Managers;
using NotReaper.Models;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.UI
{
    public class CurrentSongDisplay : Singleton<CurrentSongDisplay>
    {
        [SerializeField] private TextMeshProUGUI songName;
        [SerializeField] private Sprite[] difficultySprites;
        [SerializeField] private Image difficultyDisplay;

        private string _title = "";

        private void Start()
        {
            EditorFile.onAudicaFileLoaded += (AudicaFile file) => SetSongTitle(file.desc.title);
            DifficultyManager.onDifficultyLoaded += SetDifficulty;
        }

        private void SetDifficulty(Difficulty difficulty)
            => difficultyDisplay.sprite = difficultySprites[(int)difficulty];


        private void SetSongTitle(string title)
        {
            songName.text = title;
            _title = title;
        }

        public void SetModifierSongTitle(string title)
            => songName.text = title;

        public void ResetTitle()
            => songName.text = _title;

        public string GetSongTitle()
            => _title;
    }

}
