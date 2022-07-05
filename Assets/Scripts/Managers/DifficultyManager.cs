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
using NotReaper.TargetEditor;
using System.Linq;

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
            difficulty switch
            {
                Difficulty.Expert => EditorFile.AudicaFile.diffs.expert.cues != null,
                Difficulty.Advanced => EditorFile.AudicaFile.diffs.advanced.cues != null,
                Difficulty.Standard => EditorFile.AudicaFile.diffs.moderate.cues != null,
                Difficulty.Beginner => EditorFile.AudicaFile.diffs.beginner.cues != null,
                _ => false
            };

        public void CopyToOtherDifficulty(Difficulty origin, Difficulty dest)
        {
            if (!DifficultyExists(origin)) return;
            EditorIO.SaveMap(new Action(() => { OnSaveDone(origin, dest); }));
        }

        private void OnSaveDone(Difficulty origin, Difficulty dest)
        {
            DiffsList diffs = EditorFile.AudicaFile.diffs;

            switch (origin)
            {
                case Difficulty.Expert:
                    ActuallyCopyToOtherDifficulty(diffs.expert, dest);
                    break;
                case Difficulty.Advanced:
                    ActuallyCopyToOtherDifficulty(diffs.advanced, dest);
                    break;
                case Difficulty.Standard:
                    ActuallyCopyToOtherDifficulty(diffs.moderate, dest);
                    break;
                case Difficulty.Beginner:
                    ActuallyCopyToOtherDifficulty(diffs.beginner, dest);
                    break;
            }
        }

        private void ActuallyCopyToOtherDifficulty(CueFile cueFile, Difficulty otherDifficulty)
        {
            switch (otherDifficulty)
            {
                case Difficulty.Expert:
                    EditorFile.AudicaFile.diffs.expert = cueFile;
                    break;
                case Difficulty.Advanced:
                    EditorFile.AudicaFile.diffs.advanced = cueFile;
                    break;
                case Difficulty.Standard:
                    EditorFile.AudicaFile.diffs.moderate = cueFile;
                    break;
                case Difficulty.Beginner:
                    EditorFile.AudicaFile.diffs.beginner = cueFile;
                    break;
            }
            LoadDifficulty(otherDifficulty); //Load difficulty after copying to it from other difficulty
        }

        public void TriggerLoaded(Difficulty diff) => onDifficultyLoaded?.Invoke(diff);

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

        public void LoadDifficulty(Difficulty difficulty, bool save = false)
        {

            if (!EditorFile.IsAudicaFileLoaded || !DifficultyExists(difficulty)) return;

            if (save) 
                EditorIO.SaveMap(new Action(() => { DoLoad(difficulty); }));
            else
                DoLoad(difficulty);
        }

        private void DoLoad(Difficulty difficulty)
        {
            curSongName.text = EditorFile.SongDesc.title;
            ReviewSystem.ReviewManager.Instance.ClearContainer();

            curSongDiff.text = difficulty.ToString();
            curSongDiff.color = DifficultyColors[difficulty];
            LoadTimelineDiff(GetCuesForDifficulty(difficulty));
            LoadedDifficulty = difficulty;
            nrDiscordPresence.UpdatePresenceDifficulty(difficulty);
            onDifficultyLoaded?.Invoke(difficulty);
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
                    for (int i = 0; i < cueFile.NRCueData.pathBuilderNoteCues.Count; i++)
                    {
                        var data = EditorTargets.ConvertCueToTargetData(cueFile.NRCueData.pathBuilderNoteCues[i]);
                        var legacyData = cueFile.NRCueData.pathBuilderNoteData[i];
                        data.behavior = legacyData.behavior;

                        var pbData = new PathbuilderData();
                        pbData.Mode = PathbuilderMode.Simple;
                        pbData.Segments.Add(new PathbuilderData.Segment
                        {
                            startPoint = data.position,
                            generatedNodes = legacyData.generatedNotes,
                            interval = new (1, legacyData.interval),
                            beatLength = data.beatLength
                        });

                        var simpleData = new PathbuilderData.SimpleModeData
                        {
                            angle = legacyData.angle,
                            angleIncrement = legacyData.angleIncrement,
                            beatLength = data.beatLength,
                            initialAngle = legacyData.initialAngle,
                            interval = legacyData.interval,
                            stepDistance = legacyData.stepDistance,
                            stepIncrement = legacyData.stepIncrement
                        };
                        pbData.SimpleData = simpleData;
                        data.pathbuilderData = pbData;
                        data.legacyPathbuilderData = null;
                        cueFile.NRCueData.newPathbuilderData.Add(pbData);
                        cueFile.NRCueData.newPathbuilderCues.Add(NotePosCalc.ToCue(data, new(0)));
                    }
                    
                    cueFile.NRCueData.pathBuilderNoteCues.Clear();
                    cueFile.NRCueData.pathBuilderNoteData.Clear();
                    
                    /*for (int i = 0; i < cueFile.NRCueData.pathBuilderNoteCues.Count; ++i)
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
                    }*/
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