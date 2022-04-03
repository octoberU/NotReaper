using System;
using System.Collections.Generic;
using AudicaTools;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace DifficultyCalculation
{
    public class DifficultyCalculator
    {
        public string songID;

        public CalculatedDifficulty expert;
        public CalculatedDifficulty advanced;
        public CalculatedDifficulty standard;
        public CalculatedDifficulty beginner;

        public DifficultyCalculator(Audica audica)
        {
            EvaluateDifficulties(audica);
        }

        public static float GetRating(Audica audica, string difficulty)
        {
            var calc = new DifficultyCalculator(audica);
            var diffLower = difficulty.ToLower();
            switch (diffLower)
            {
                case "easy":
                    if (calc.beginner != null) return calc.beginner.difficultyRating;
                    else return 0f;
                case "normal":
                    if (calc.standard != null) return calc.standard.difficultyRating;
                    else return 0f;
                case "hard":
                    if (calc.advanced != null) return calc.advanced.difficultyRating;
                    else return 0f;
                case "expert":
                    if (calc.expert != null) return calc.expert.difficultyRating;
                    else return 0f;
                default:
                    return 0f;
            }
        }

        public static float GetRating(Audica audica, int difficulty)
        {
            string diff;
            switch (difficulty)
            {
                case 1:
                    diff = "hard";
                    break;
                case 2:
                    diff = "normal";
                    break;
                case 3:
                    diff = "easy";
                    break;
                default:
                    diff = "expert";
                    break;
            }
            return GetRating(audica, diff);
        }

        private void EvaluateDifficulties(Audica audica)
        {
            try
            {
                if (audica.expert != null && audica.expert.cues.Count > 0 && audica.expert.cues != null)
                    this.expert = new CalculatedDifficulty(audica.expert.cues, audica.tempoData);
                if (audica.advanced != null && audica.advanced.cues != null && audica.advanced.cues.Count > 0 && audica.advanced.cues != null)
                    this.advanced = new CalculatedDifficulty(audica.advanced.cues, audica.tempoData);
                if (audica.moderate != null && audica.moderate.cues.Count > 0 && audica.moderate.cues != null)
                    this.standard = new CalculatedDifficulty(audica.moderate.cues, audica.tempoData);
                if (audica.beginner != null && audica.beginner.cues.Count > 0 && audica.beginner.cues != null)
                    this.beginner = new CalculatedDifficulty(audica.beginner.cues, audica.tempoData);
            }
            catch (Exception e)
            {
                Debug.Log(audica == null);
                Debug.Log(audica.expert == null);
                Debug.Log(audica.expert.cues == null);
                Debug.Log(audica.expert.cues.Count == 0);
                Debug.Log(audica.advanced == null);
                Debug.Log(audica.advanced.cues == null);
                Debug.Log(audica.advanced.cues.Count == 0);
                Debug.Log(audica.moderate == null);
                Debug.Log(audica.moderate.cues == null);
                Debug.Log(audica.moderate.cues.Count == 0);
                Debug.Log(audica.beginner == null);
                Debug.Log(audica.beginner.cues == null);
                Debug.Log(audica.beginner.cues.Count == 0);
                Debug.Log(e);
                throw;
            }
        }
    }
    public class CalculatedDifficulty
    {
        public static float spacingMultiplier = 1f;
        public static float lengthMultiplier = 0.7f;
        public static float densityMultiplier = 1f;
        public static float readabilityMultiplier = 1.2f;

        public float difficultyRating;
        public float spacing;
        public float density;
        public float readability;

        float length;

        public CalculatedDifficulty(List<Cue> cues, List<TempoData> tempoData)
        {
            EvaluateCues(cues, tempoData);
        }
        public CalculatedDifficulty(Difficulty difficulty, List<TempoData> tempoData)
        {
            EvaluateCues(difficulty.cues, tempoData);
        }

        public static Dictionary<Cue.Behavior, float> objectDifficultyModifier = new Dictionary<Cue.Behavior, float>()
    {
        { Cue.Behavior.Standard, 1f },
        { Cue.Behavior.Vertical, 1.2f },
        { Cue.Behavior.Horizontal, 1.3f },
        { Cue.Behavior.Hold, 1f },
        { Cue.Behavior.ChainStart, 1.2f },
        { Cue.Behavior.Chain, 0.2f },
        { Cue.Behavior.Melee, 0.6f }
    };

        List<Cue> leftHandCues = new List<Cue>();
        List<Cue> rightHandCues = new List<Cue>();
        List<Cue> eitherHandCues = new List<Cue>();
        List<Cue> allCues = new List<Cue>();

        public void EvaluateCues(List<Cue> cues, List<TempoData> tempoData)
        {
            this.length = TempoData.TickToMilliseconds(cues[cues.Count - 1].tick, tempoData) - TempoData.TickToMilliseconds(cues[0].tick, tempoData);
            Debug.Log("Length: " + length);
            Debug.Log("Cuecount: " + cues.Count);
            if (cues.Count >= 15 && this.length > 30000f)
            {
                SplitCues(cues);
                CalculateSpacing();
                CalculateDensity();
                CalculateReadability();
                difficultyRating = ((spacing + readability) / length) * 500f + (length / 100000f * lengthMultiplier);
            }
            else difficultyRating = 0f;
        }

        void CalculateReadability()
        {
            for (int i = 0; i < allCues.Count; i++)
            {
                float modifierValue = 0f;
                objectDifficultyModifier.TryGetValue(allCues[i].behavior, out modifierValue);
                readability += modifierValue * readabilityMultiplier;
            }
            //readability /= length;
        }

        void CalculateSpacing()
        {
            GetSpacingPerHand(leftHandCues);
            GetSpacingPerHand(rightHandCues);
            //spacing /= length;
        }

        void CalculateDensity()
        {
            density = (float)allCues.Count / length;
        }

        private void GetSpacingPerHand(List<Cue> cues)
        {
            for (int i = 1; i < cues.Count; i++)
            {
                float dist = Vector2.Distance(GetTrueCoordinates(cues[i]), GetTrueCoordinates(cues[i - 1]));
                float distMultiplied = cues[i].behavior == Cue.Behavior.Melee ? float.Epsilon :
                    dist * spacingMultiplier;
                spacing += distMultiplied;
            }
        }

        Vector2 GetTrueCoordinates(Cue cue)
        {
            float x = cue.pitch % 12;
            float y = (int)(cue.pitch / 12);
            x += cue.gridOffset.x;
            y += cue.gridOffset.y;
            return new Vector2(x, y);
        }

        void SplitCues(List<Cue> cues)
        {
            for (int i = 0; i < cues.Count; i++)
            {
                allCues.Add(cues[i]);
                switch (cues[i].handType)
                {
                    case Cue.HandType.Left:
                        leftHandCues.Add(cues[i]);
                        break;
                    case Cue.HandType.Right:
                        rightHandCues.Add(cues[i]);
                        break;
                    case Cue.HandType.Either:
                        eitherHandCues.Add(cues[i]);
                        break;
                    default:
                        break;
                }
            }
        }
    }

}