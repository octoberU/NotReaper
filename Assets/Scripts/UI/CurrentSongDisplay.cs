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
    public class CurrentSongDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI songName;
        [SerializeField] private Sprite[] difficultySprites;
        [SerializeField] private Image difficultyDisplay;

        private void Start()
        {
            EditorFile.onAudicaFileLoaded += (AudicaFile file) => SetSongTitle(file.desc.title);
            DifficultyManager.onDifficultyLoaded += SetDifficulty;
        }

        private void SetDifficulty(int difficulty)
        {
            print("Difficulty: " + difficulty);
            difficultyDisplay.sprite = difficultySprites[difficulty];
        }
            

        private void SetSongTitle(string title)
            => songName.text = title;
    }

}
