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


    public class DifficultyManager : MonoBehaviour
    {

        public static DifficultyManager I;

        [HideInInspector] public int loadedIndex = -1;

        [SerializeField] private NRDiscordPresence nrDiscordPresence;

        [SerializeField] private TextMeshProUGUI curSongName;
        [SerializeField] private TextMeshProUGUI curSongDiff;
        //[SerializeField] private TextMeshProUGUI curTargetAmount;

        [NRInject] private Pathbuilder pathbuilder;
        [NRInject] private UIModeSelect modeSelect;
        private void Awake()
        {
            I = this;
        }


        public Timeline timeline;

        public string GetDifficultyText(int index = -2)
        {
            if (index == -2) index = loadedIndex;
            switch (index)
            {
                case 0:
                    return "Expert";
                case 1:
                    return "Advanced";
                case 2:
                    return "Standard";
                case 3:
                    return "Beginner";
                default:
                    return "Expert";
            }
        }

        //Use this when starting up, load highest diff in audica file
        public int LoadHighestDifficulty(bool save = false)
        {
            if (!EditorFile.IsAudicaFileLoaded) return -1;

            if (EditorFile.AudicaFile.diffs.expert.cues != null)
            {
                LoadDifficulty(0, save);

                return 0;
            }

            else if (EditorFile.AudicaFile.diffs.advanced.cues != null)
            {
                LoadDifficulty(1, save);
                return 1;
            }

            else if (EditorFile.AudicaFile.diffs.moderate.cues != null)
            {
                LoadDifficulty(2, save);
                return 2;
            }

            else if (EditorFile.AudicaFile.diffs.beginner.cues != null)
            {
                LoadDifficulty(3, save);
                return 3;
            }

            //If no difficulties exist gen a new one
            bool genned = GenerateDifficulty(0);
            if (genned) return 0;

            else return -1;
        }

        /// <summary>
        /// Overrides any difficulty with a blank cues object.
        /// </summary>
        /// <param name="index">The index of the difficulty to clear/generate.0-expert, 3-easy</param>
        /// <returns>True if succeeded</returns>
        public bool GenerateDifficulty(int index)
        {

            if (DifficultyExists(index)) return false;

            switch (index)
            {
                case 0:
                    EditorFile.AudicaFile.diffs.expert.cues = new List<Cue>();
                    break;
                case 1:
                    EditorFile.AudicaFile.diffs.advanced.cues = new List<Cue>();
                    break;
                case 2:
                    EditorFile.AudicaFile.diffs.moderate.cues = new List<Cue>();
                    break;
                case 3:
                    EditorFile.AudicaFile.diffs.beginner.cues = new List<Cue>();
                    break;

            }
            return true;
        }

        public void RemoveDifficulty(int index)
        {
            if (loadedIndex == index) EditorTargets.DeleteAllTargets();
            switch (index)
            {
                case 0:
                    EditorFile.AudicaFile.diffs.expert.cues = null;
                    break;
                case 1:
                    EditorFile.AudicaFile.diffs.advanced.cues = null;
                    break;
                case 2:
                    EditorFile.AudicaFile.diffs.moderate.cues = null;
                    break;
                case 3:
                    EditorFile.AudicaFile.diffs.beginner.cues = null;
                    break;

            }

        }

        public bool DifficultyExists(int index)
        {
            DiffsList diffs = EditorFile.AudicaFile.diffs;
            switch (index)
            {
                case 0:
                    if (diffs.expert.cues != null)
                    {
                        return true;
                    }
                    break;
                case 1:
                    if (diffs.advanced.cues != null)
                    {
                        return true;
                    }
                    break;
                case 2:
                    if (diffs.moderate.cues != null)
                    {
                        return true;
                    }
                    break;
                case 3:
                    if (diffs.beginner.cues != null)
                    {
                        return true;
                    }
                    break;
            }
            //if it fell through
            return false;
        }

        public bool CopyToOtherDifficulty(int origin, int dest)
        {

            if (!DifficultyExists(origin)) return false;

            //Save the current difficulty
            timeline.Export();

            DiffsList diffs = EditorFile.AudicaFile.diffs;

            switch (origin)
            {
                case 0:
                    ActuallyCopyToOtherDifficulty(diffs.expert.cues, dest);
                    break;
                case 1:
                    ActuallyCopyToOtherDifficulty(diffs.advanced.cues, dest);
                    break;
                case 2:
                    ActuallyCopyToOtherDifficulty(diffs.moderate.cues, dest);
                    break;
                case 3:
                    ActuallyCopyToOtherDifficulty(diffs.beginner.cues, dest);
                    break;
            }

            return true;


        }

        private void ActuallyCopyToOtherDifficulty(List<Cue> cues, int dest)
        {
            switch (dest)
            {
                case 0:
                    EditorFile.AudicaFile.diffs.expert.cues = cues;
                    break;
                case 1:
                    EditorFile.AudicaFile.diffs.advanced.cues = cues;
                    break;
                case 2:
                    EditorFile.AudicaFile.diffs.moderate.cues = cues;
                    break;
                case 3:
                    EditorFile.AudicaFile.diffs.beginner.cues = cues;
                    break;
            }
            LoadDifficulty(dest); //Load difficulty after copying to it from other difficulty
        }


        public bool LoadDifficulty(int index, bool save = true)
        {

            Debug.Log("Loading difficulty");
            if (!EditorFile.IsAudicaFileLoaded) return false;

            DiffsList diffs = EditorFile.AudicaFile.diffs;
            curSongName.text = EditorFile.SongDesc.title;
            ReviewSystem.ReviewManager.Instance.ClearContainer();
            switch (index)
            {
                case 0:
                    if (diffs.expert.cues != null)
                    {
                        curSongDiff.text = "Expert";
                        curSongDiff.color = new Color(0.74118f, 0.15686f, 1.00000f);
                        LoadTimelineDiff(diffs.expert, save);
                        loadedIndex = index;

                        nrDiscordPresence.UpdatePresenceDifficulty(0);
                        return true;
                    }
                    break;
                case 1:
                    if (diffs.advanced.cues != null)
                    {
                        curSongDiff.text = "Advanced";
                        curSongDiff.color = new Color(0.91765f, 0.65098f, 0.05490f);
                        LoadTimelineDiff(diffs.advanced, save);
                        loadedIndex = index;

                        nrDiscordPresence.UpdatePresenceDifficulty(1);
                        return true;
                    }
                    break;
                case 2:
                    if (diffs.moderate.cues != null)
                    {
                        curSongDiff.text = "Standard";
                        curSongDiff.color = new Color(0.16078f, 0.86275f, 0.93725f);
                        LoadTimelineDiff(diffs.moderate, save);
                        loadedIndex = index;

                        nrDiscordPresence.UpdatePresenceDifficulty(2);
                        return true;
                    }
                    break;
                case 3:
                    if (diffs.beginner.cues != null)
                    {
                        curSongDiff.text = "Beginner";
                        curSongDiff.color = new Color(0.28235f, 0.87059f, 0.10980f);
                        LoadTimelineDiff(diffs.beginner, save);
                        loadedIndex = index;

                        nrDiscordPresence.UpdatePresenceDifficulty(3);
                        return true;
                    }
                    break;
            }

            //Else, if it failed, return false
            return false;



        }

        private bool LoadTimelineDiff(CueFile cueFile, bool save = true)
        {
            Debug.Log("Loading timeline diff");
            if (save) timeline.Export();

            EditorTargets.DeleteAllTargets();
            timeline.repeaterManager.RemoveAllRepeaters();
            foreach (Cue cue in cueFile.cues)
            {
                EditorTargets.AddTargetFromAction(timeline.GetTargetDataForCue(cue));
            }
            if (cueFile.NRCueData != null)
            {
                if (cueFile.NRCueData.pathBuilderNoteData.Count == cueFile.NRCueData.pathBuilderNoteCues.Count)
                {
                    for (int i = 0; i < cueFile.NRCueData.pathBuilderNoteCues.Count; ++i)
                    {
                        var data = timeline.GetTargetDataForCue(cueFile.NRCueData.pathBuilderNoteCues[i]);
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
                        var data = timeline.GetTargetDataForCue(cueFile.NRCueData.newPathbuilderCues[i]);
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
            EditorState.SelectMode(EditorMode.Compose);

            return true;
        }


    }

}