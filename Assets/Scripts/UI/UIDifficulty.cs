using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NotReaper.Managers;
using NotReaper.Models;

namespace NotReaper.UI
{



    public class UIDifficulty : MonoBehaviour
    {
        [NRInject] private DifficultyManager difficultyManager;
        public Components.NRButton expert;
        public Components.NRButton advanced;
        public Components.NRButton standard;
        public Components.NRButton easy;

        public GameObject warningWindow;
        public TextMeshProUGUI warningText;

        private Difficulty ogDiff = Difficulty.None;
        private Difficulty newDiff = Difficulty.None;



        public void ApplyDifficultyToOther()
            => difficultyManager.CopyToOtherDifficulty(ogDiff, newDiff);


        public void DifficultyComingFrom(Difficulty difficulty)
        {
            ogDiff = difficulty;

            expert.interactable = difficulty == Difficulty.Expert;
            advanced.interactable = difficulty == Difficulty.Advanced;
            standard.interactable = difficulty == Difficulty.Standard;
            easy.interactable = difficulty == Difficulty.Beginner;
        }

        public void Confirm(string newDifficulty)
        {
            newDiff = ConvertStringToDifficulty(newDifficulty);
            warningText.SetText("WARNING: This will replace all cues in " + newDiff + " with the " + ogDiff + " cues.");
            warningWindow.SetActive(true);
        }

        private Difficulty ConvertStringToDifficulty(string diff) =>
            diff switch
            {
                "expert" => Difficulty.Expert,
                "advanced" => Difficulty.Advanced,
                "standard" => Difficulty.Standard,
                "easy" => Difficulty.Beginner,
                _ => Difficulty.None
            };

        public void WarningCancel() => warningWindow.SetActive(false);
        public void Cancel() => gameObject.SetActive(false);
    }

}