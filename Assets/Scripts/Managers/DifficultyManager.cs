using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Tools.ChainBuilder;
using TMPro;
using UnityEngine;
using NotReaper.Grid;
using NotReaper.Tools.PathBuilder;
using NotReaper.UI;

namespace NotReaper.Managers
{


    public class DifficultyManager : Singleton<DifficultyManager>
    {
        public Difficulty LoadedDifficulty { get; private set; } = Difficulty.None;
        public string DifficultyString => LoadedDifficulty.ToString();

        [SerializeField] private NRDiscordPresence nrDiscordPresence;

        [SerializeField] private TextMeshProUGUI curSongName;
        [SerializeField] private TextMeshProUGUI curSongDiff;

        [NRInject] private Pathbuilder pathbuilder;
        [NRInject] private Timeline timeline;

        public delegate void OnDifficultyLoaded(Difficulty difficulty);
        public static event OnDifficultyLoaded onDifficultyLoaded;

        public readonly Dictionary<Difficulty, Color> DifficultyColors = new Dictionary<Difficulty, Color>()
        {
            { Difficulty.None, Color.white },
            { Difficulty.Expert, new(0.74118f, 0.15686f, 1.00000f) },
            { Difficulty.Advanced, new(0.91765f, 0.65098f, 0.05490f) },
            { Difficulty.Standard,  new(0.16078f, 0.86275f, 0.93725f) },
            { Difficulty.Beginner, new(0.28235f, 0.87059f, 0.10980f) }
        };


        protected override void Awake() => base.Awake();

        //Use this when starting up, load highest diff in audica file
        public void LoadHighestDifficulty(bool save = false)
        {
            if (EditorFile.AudicaFile.diffs.expert.cues != null)
            {
                LoadDifficulty(Difficulty.Expert, save);
                return;
            }
            else if (EditorFile.AudicaFile.diffs.advanced.cues != null)
            {
                LoadDifficulty(Difficulty.Advanced, save);
                return;
            }
            else if (EditorFile.AudicaFile.diffs.moderate.cues != null)
            {
                LoadDifficulty(Difficulty.Standard, save);
                return;
            }
            else if (EditorFile.AudicaFile.diffs.beginner.cues != null)
            {
                LoadDifficulty(Difficulty.Beginner, save);
                return;
            }

            //If no difficulties exist gen a new one
            GenerateDifficulty(Difficulty.Expert);
        }

        /// <summary>
        /// Overrides any difficulty with a blank cues object.
        /// </summary>
        /// <param name="index">The index of the difficulty to clear/generate.0-expert, 3-easy</param>
        /// <returns>True if succeeded</returns>
        public bool GenerateDifficulty(Difficulty difficulty)
        {
            if (DifficultyExists(difficulty))
                return false;

            switch (difficulty)
            {
                case Difficulty.Expert:
                    EditorFile.AudicaFile.diffs.expert.cues = new List<Cue>();
                    break;
                case Difficulty.Advanced:
                    EditorFile.AudicaFile.diffs.advanced.cues = new List<Cue>();
                    break;
                case Difficulty.Standard:
                    EditorFile.AudicaFile.diffs.moderate.cues = new List<Cue>();
                    break;
                case Difficulty.Beginner:
                    EditorFile.AudicaFile.diffs.beginner.cues = new List<Cue>();
                    break;

            }
            return true;
        }

        public void RemoveDifficulty(Difficulty difficulty)
        {
            if (LoadedDifficulty == difficulty) EditorTargets.DeleteAllTargets();
            switch (difficulty)
            {
                case Difficulty.Expert:
                    EditorFile.AudicaFile.diffs.expert.cues = null;
                    break;
                case Difficulty.Advanced:
                    EditorFile.AudicaFile.diffs.advanced.cues = null;
                    break;
                case Difficulty.Standard:
                    EditorFile.AudicaFile.diffs.moderate.cues = null;
                    break;
                case Difficulty.Beginner:
                    EditorFile.AudicaFile.diffs.beginner.cues = null;
                    break;
            }
        }

        public bool DifficultyExists(Difficulty difficulty) =>
            difficulty == Difficulty.Expert && EditorFile.AudicaFile.diffs.expert != null ||
            difficulty == Difficulty.Advanced && EditorFile.AudicaFile.diffs.advanced != null ||
            difficulty == Difficulty.Standard && EditorFile.AudicaFile.diffs.moderate != null ||
            difficulty == Difficulty.Beginner && EditorFile.AudicaFile.diffs.beginner != null;

        public bool CopyToOtherDifficulty(Difficulty origin, Difficulty dest)
        {

            if (!DifficultyExists(origin)) return false;
            //Save the current difficulty
            timeline.Export();

            DiffsList diffs = EditorFile.AudicaFile.diffs;

            switch (origin)
            {
                case Difficulty.Expert:
                    ActuallyCopyToOtherDifficulty(diffs.expert.cues, dest);
                    break;
                case Difficulty.Advanced:
                    ActuallyCopyToOtherDifficulty(diffs.advanced.cues, dest);
                    break;
                case Difficulty.Standard:
                    ActuallyCopyToOtherDifficulty(diffs.moderate.cues, dest);
                    break;
                case Difficulty.Beginner:
                    ActuallyCopyToOtherDifficulty(diffs.beginner.cues, dest);
                    break;
            }

            return true;


        }

        private void ActuallyCopyToOtherDifficulty(List<Cue> cues, Difficulty otherDifficulty)
        {
            switch (otherDifficulty)
            {
                case Difficulty.Expert:
                    EditorFile.AudicaFile.diffs.expert.cues = cues;
                    break;
                case Difficulty.Advanced:
                    EditorFile.AudicaFile.diffs.advanced.cues = cues;
                    break;
                case Difficulty.Standard:
                    EditorFile.AudicaFile.diffs.moderate.cues = cues;
                    break;
                case Difficulty.Beginner:
                    EditorFile.AudicaFile.diffs.beginner.cues = cues;
                    break;
            }
            LoadDifficulty(otherDifficulty); //Load difficulty after copying to it from other difficulty
        }

        public CueFile GetCuesForDifficulty(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Expert:
                    return EditorFile.AudicaFile.diffs.expert;
                case Difficulty.Advanced:
                    return EditorFile.AudicaFile.diffs.advanced;
                case Difficulty.Standard:
                    return EditorFile.AudicaFile.diffs.moderate;
                case Difficulty.Beginner:
                    return EditorFile.AudicaFile.diffs.beginner;
                default:
                    return null;
            }
        }

        public bool LoadDifficulty(Difficulty difficulty, bool save = true)
        {

            if (!EditorFile.IsAudicaFileLoaded || !DifficultyExists(difficulty)) return false;
            if (save) timeline.Export();

            curSongName.text = EditorFile.SongDesc.title;
            ReviewSystem.ReviewManager.Instance.ClearContainer();

            curSongDiff.text = difficulty.ToString();
            curSongDiff.color = DifficultyColors[difficulty];
            LoadTimelineDiff(GetCuesForDifficulty(difficulty));
            LoadedDifficulty = difficulty;
            nrDiscordPresence.UpdatePresenceDifficulty(difficulty);
            onDifficultyLoaded?.Invoke(difficulty);
            return true;
            /*
            switch (difficulty)
            {
                case Difficulty.Expert:
                    if (diffs.expert.cues != null)
                    {
                        curSongDiff.text = "Expert";
                        curSongDiff.color = new Color(0.74118f, 0.15686f, 1.00000f);
                        LoadTimelineDiff(diffs.expert, save);
                        loadedIndex = difficulty;

                        nrDiscordPresence.UpdatePresenceDifficulty(0);
                        onDifficultyLoaded?.Invoke(0);
                        return true;
                    }
                    break;
                case Difficulty.Advanced:
                    if (diffs.advanced.cues != null)
                    {
                        curSongDiff.text = "Advanced";
                        curSongDiff.color = new Color(0.91765f, 0.65098f, 0.05490f);
                        LoadTimelineDiff(diffs.advanced, save);
                        loadedIndex = difficulty;

                        nrDiscordPresence.UpdatePresenceDifficulty(1);
                        onDifficultyLoaded?.Invoke(1);
                        return true;
                    }
                    break;
                case Difficulty.Standard:
                    if (diffs.moderate.cues != null)
                    {
                        curSongDiff.text = "Standard";
                        curSongDiff.color = new Color(0.16078f, 0.86275f, 0.93725f);
                        LoadTimelineDiff(diffs.moderate, save);
                        loadedIndex = difficulty;

                        nrDiscordPresence.UpdatePresenceDifficulty(2);
                        onDifficultyLoaded?.Invoke(2);
                        return true;
                    }
                    break;
                case Difficulty.Beginner:
                    if (diffs.beginner.cues != null)
                    {
                        curSongDiff.text = "Beginner";
                        curSongDiff.color = new Color(0.28235f, 0.87059f, 0.10980f);
                        LoadTimelineDiff(diffs.beginner, save);
                        loadedIndex = difficulty;

                        nrDiscordPresence.UpdatePresenceDifficulty(3);
                        onDifficultyLoaded?.Invoke(3);
                        return true;
                    }
                    break;
            }
            //Else, if it failed, return false
            return false;
            */
        }

        private bool LoadTimelineDiff(CueFile cueFile)
        {
            EditorTargets.DeleteAllTargets();
            timeline.repeaterManager.RemoveAllRepeaters();
            EditorTargets.IsLoadingTargets = true;
            foreach (Cue cue in cueFile.cues)
            {
                EditorTargets.AddTargetFromAction(cue);
            }
            if (cueFile.NRCueData != null)
            {
                if (cueFile.NRCueData.pathBuilderNoteData.Count == cueFile.NRCueData.pathBuilderNoteCues.Count)
                {
                    for (int i = 0; i < cueFile.NRCueData.pathBuilderNoteCues.Count; ++i)
                    {
                        var data = EditorTargets.ConvertCueToTargetData(cueFile.NRCueData.pathBuilderNoteCues[i]);
                        data.legacyPathbuilderData = cueFile.NRCueData.pathBuilderNoteData[i];
                        data.legacyPathbuilderData.parentNotes.Add(data);

                        //Recalculate the notes, and remove any identical enties that would have been loaded through the cues
                        ChainBuilder.CalculateChainNotes(data);
                        foreach (TargetData genData in data.legacyPathbuilderData.generatedNotes)
                        {
                            var foundData = TargetFinder.FindTargetData(genData.time, genData.behavior, genData.handType);
                            if (foundData != null)
                            {
                                EditorTargets.DeleteTargetFromAction(foundData);
                            }
                        }

                        EditorTargets.AddTargetFromAction(data);

                        //Generate the notes, so the song is complete
                        ChainBuilder.GenerateChainNotes(data);
                    }
                }
                if (cueFile.NRCueData.newPathbuilderData.Count > 0)
                {
                    for (int i = 0; i < cueFile.NRCueData.newPathbuilderCues.Count; i++)
                    {
                        var data = EditorTargets.ConvertCueToTargetData(cueFile.NRCueData.newPathbuilderCues[i]);
                        var foundData = TargetFinder.FindTargetData(data.time, data.behavior, data.handType);
                        if (foundData != null)
                        {
                            foundData.isPathbuilderTarget = true;
                            foundData.pathbuilderData = cueFile.NRCueData.newPathbuilderData[i];
                            pathbuilder.CalculateNodesOnLoad(foundData);
                            foreach (var segment in foundData.pathbuilderData.Segments)
                            {
                                foreach (var genNode in segment.generatedNodes)
                                {
                                    var foundNode = TargetFinder.FindTargetData(genNode.time, genNode.behavior, genNode.handType);
                                    if (foundNode != null)
                                    {
                                        EditorTargets.DeleteTargetFromAction(foundNode);
                                    }
                                }
                            }
                            pathbuilder.GenerateNodesOnLoad(foundData);
                        }


                    }
                }
                if (cueFile.NRCueData.newRepeaterSections.Count > 0)
                {
                    foreach (var section in cueFile.NRCueData.newRepeaterSections)
                    {
                        timeline.repeaterManager.LoadRepeater(section);
                    }
                }
            }
            EditorTargets.IsLoadingTargets = false;

            foreach (var target in EditorNotes.OrderedNotes)
            {
                var data = target.data;
                if (data.behavior.IsChainStart())
                {
                    EditorTargets.UpdateChainConnector(target);
                }
            }

            EditorState.SelectMode(EditorMode.Compose);

            return true;
        }


    }

}