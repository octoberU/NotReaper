using NotReaper.Grid;
using NotReaper.IO;
using NotReaper.Managers;
using NotReaper.Models;
using NotReaper.Modifier;
using NotReaper.Notifications;
using NotReaper.Repeaters;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools.ChainBuilder;
using NotReaper.UI;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NotReaper
{
    /*
    public class EditorIO : MonoBehaviour
    {
        public static bool inTimingMode = false;
        public static bool isSaving = false;
        internal bool readyToRegenerate = false;
        public static Relative_QNT offset = new Relative_QNT(0);
        [NRInject] private RepeaterManager repeaterManager;
        [SerializeField] private MiniTimeline miniTimeline;
        [SerializeField] private DifficultyManager difficultyManager;
        [SerializeField] private NRDiscordPresence nrDiscordPresence;

        /* IO stuff
        public void Export(bool autoSave = false)
        {
            if (isSaving) return;
            try
            {
                StartCoroutine(DoExport(autoSave));
            }
            catch
            {
                NotificationCenter.SendNotification("Something went wrong while saving.", NotificationType.Error);
            }
        }
        AudicaExporter exporter = new();
        private IEnumerator DoExport(bool autoSave = false)
        {
            //if (isSaving) return;

            isSaving = true;
            //Debug.Log ("Saving: " + EditorData.AudicaFile.desc.title);

            //Ensure all chains are generated
            List<TargetData> nonGeneratedNotes = new List<TargetData>();

            foreach (Target note in EditorNotes.Notes)
            {
                if (note.data.behavior == TargetBehavior.Legacy_Pathbuilder && note.data.legacyPathbuilderData.createdNotes == false)
                {
                    nonGeneratedNotes.Add(note.data);
                }
            }

            foreach (var data in nonGeneratedNotes)
            {
                ChainBuilder.GenerateChainNotes(data);
            }

            //Export map
            string dirpath = Application.persistentDataPath;

            CueFile export = new CueFile();
            export.cues = new List<Cue>();
            export.NRCueData = new NRCueData();
            export.NRCueData.newRepeaterSections = repeaterManager.GetSections();

            foreach (Target target in EditorNotes.OrderedNotes)
            {
                if (target.data.beatLength == 0) target.data.beatLength = Constants.SixteenthNoteDuration;

                var cue = NotePosCalc.ToCue(target, offset);
                if (target.data.behavior == TargetBehavior.Legacy_Pathbuilder)
                {
                    export.NRCueData.pathBuilderNoteCues.Add(cue);
                    export.NRCueData.pathBuilderNoteData.Add(target.data.legacyPathbuilderData);
                    continue;
                }
                else if (target.data.isPathbuilderTarget)
                {
                    export.NRCueData.newPathbuilderData.Add(target.data.pathbuilderData);
                    export.NRCueData.newPathbuilderCues.Add(cue);
                }

                export.cues.Add(cue);
            }
            if (EditorFile.AudicaFile.desc.bakedzOffset)
            {
                export.cues = ZOffsetBaker.Instance.Bake(export.cues.ToList());
            }

            //export.NRCueData.repeaterSections = repeaterSections.GetRange(0, repeaterSections.Count);

            switch (difficultyManager.loadedIndex)
            {
                case 0:
                    EditorFile.AudicaFile.diffs.expert = export;
                    break;
                case 1:
                    EditorFile.AudicaFile.diffs.advanced = export;
                    break;
                case 2:
                    EditorFile.AudicaFile.diffs.moderate = export;
                    break;
                case 3:
                    EditorFile.AudicaFile.diffs.beginner = export;
                    break;
            }

            //EditorData.AudicaFile.desc = desc;

            EditorFile.SongDesc.tempoList = EditorTempo.TempoChanges;

            //AudicaExporter.ExportToAudicaFile(EditorData.AudicaFile, autoSave);

            yield return StartCoroutine(exporter.ExportToAudicaFile(EditorFile.AudicaFile, autoSave));

            isSaving = false;

        }

        public void ExportAndPlay()
        {
            Export();
            string songFolder = PathLogic.GetSongFolder();
            File.Delete(Path.Combine(songFolder, EditorFile.AudicaFile.desc.songID + ".audica"));
            File.Copy(EditorFile.AudicaFile.filepath, Path.Combine(songFolder, EditorFile.AudicaFile.desc.songID + ".audica"));

            string newPath = Path.GetFullPath(Path.Combine(songFolder, @"..\..\..\..\"));
            System.Diagnostics.Process.Start(Path.Combine(newPath, "Audica.exe"));
        }

     
        public IEnumerator LoadAudicaFile(bool loadRecent = false, string filePath = null, float bpm = -1, Action<bool> onLoaded = null)
        {
            readyToRegenerate = false;
            inTimingMode = false;
            EditorTime.SetTime(0);
            SetBeatTime(EditorTime.Time);
            SetOffset(new Relative_QNT(0));
            SafeSetTime();
            SetCurrentTick();
            SetCurrentTime();
            if (EditorFile.IsAudicaFileLoaded && NRSettings.config.saveOnLoadNew)
            {
                yield return StartCoroutine(DoExport());
                //Export();
            }
            if (EditorFile.IsAudicaFileLoaded)
            {
                miniTimeline.ClearBookmarks(false);
            }
            HandleCache.ClearCache();
            if (loadRecent)
            {
                //EditorData.AudicaFile = null;
                EditorFile.UnloadAudicaFile();
                EditorFile.SetAudicaFile(AudicaHandler.LoadAudicaFile(PlayerPrefs.GetString("recentFile", null)));
                if (EditorFile.AudicaFile == null)
                {
                    onLoaded?.Invoke(false);
                    yield break;
                }

            }
            else if (filePath != null)
            {
                EditorFile.UnloadAudicaFile();
                EditorFile.SetAudicaFile(AudicaHandler.LoadAudicaFile(filePath));
                if (EditorFile.AudicaFile == null)
                {
                    onLoaded?.Invoke(false);
                    yield break;
                }
                PlayerPrefs.SetString("recentFile", EditorFile.AudicaFile.filepath);
                RecentAudicaFiles.AddRecentDir(EditorFile.AudicaFile.filepath);

            }
            else
            {

                string prevDir = PlayerPrefs.GetString("recentDir", "");

                string[] paths;

                if (prevDir != "")
                {
                    paths = StandaloneFileBrowser.OpenFilePanel("Audica File (Not OST)", prevDir, "audica", false);

                }
                else
                {
                    paths = StandaloneFileBrowser.OpenFilePanel("Audica File (Not OST)", Application.dataPath, "audica", false);
                }

                if (paths.Length == 0)
                {
                    onLoaded?.Invoke(false);
                    yield break;
                }

                PlayerPrefs.SetString("recentDir", Path.GetDirectoryName(paths[0]));

                EditorFile.UnloadAudicaFile();

                EditorFile.SetAudicaFile(AudicaHandler.LoadAudicaFile(paths[0]));
                if (EditorFile.AudicaFile == null)
                {
                    onLoaded?.Invoke(false);
                    yield break;
                }
                PlayerPrefs.SetString("recentFile", paths[0]);
                RecentAudicaFiles.AddRecentDir(EditorFile.AudicaFile.filepath);
            }

            ResetTimeline();

            //desc = EditorData.AudicaFile.desc;
            // Get song BPM
            EditorTempo.LoadFromFile(EditorFile.AudicaFile.song_mid, bpm, EditorFile.SongDesc.tempo);
            //Update our discord presence
            nrDiscordPresence.UpdatePresenceSongName(EditorFile.SongDesc.title);

            //Loads all the sounds.
            yield return StartCoroutine(EditorAudioManager.Instance.GetAudioClip($"file://{Application.dataPath}/.cache/{EditorFile.AudicaFile.desc.cachedMainSong}.ogg"));
            if (EditorFile.AudicaFile.desc.sustainSongLeft != "") StartCoroutine(EditorAudioManager.Instance.LoadLeftSustain($"file://{Application.dataPath}/.cache/{EditorFile.AudicaFile.desc.cachedSustainSongLeft}.ogg"));
            if (EditorFile.AudicaFile.desc.sustainSongRight != "") StartCoroutine(EditorAudioManager.Instance.LoadRightSustain($"file://{Application.dataPath}/.cache/{EditorFile.AudicaFile.desc.cachedSustainSongRight}.ogg"));
            yield return StartCoroutine(EditorAudioManager.Instance.LoadExtraAudio($"file://{Application.dataPath}/.cache/{EditorFile.AudicaFile.desc.cachedFxSong}.ogg"));
            //foreach (Cue cue in EditorData.AudicaFile.diffs.expert.cues) {
            //AddTarget(cue);
            //}
            //Difficulty manager loads stuff now
            //EditorFile.SetIsAudicaLoaded(true);
            difficultyManager.LoadHighestDifficulty();

            //Disable timing window buttons so users don't mess stuff up.
            //generateAudicaButton.interactable = false;
            //loadAudioFileTiming.interactable = false;

            //Load bookmarks
            if (EditorFile.AudicaFile.desc.bookmarks != null)
            {
                foreach (BookmarkData data in EditorFile.AudicaFile.desc.bookmarks)
                {
                    if (data.r == 0 && data.g == 0 && data.b == 0)
                    {
                        Color c = BookmarkColorPicker.Instance.GetUIColor((BookmarkUIColor)data.uiColor);
                        data.r = c.r;
                        data.g = c.g;
                        data.b = c.b;
                        miniTimeline.SetBookmark(data.xPosMini, data.xPosTop, new QNT_Timestamp(0), data.type, data.text, c, (BookmarkUIColor)data.uiColor, true, true);
                    }

                    miniTimeline.SetBookmark(data.xPosMini, data.xPosTop, new QNT_Timestamp(0), data.type, data.text, new Color(data.r, data.g, data.b), (BookmarkUIColor)data.uiColor, true, true);

                }
            }

            //Load metadata
            if (EditorFile.AudicaFile.desc != null)
            {
                //UIMetadata.Instance.UpdateUIValues();
            }

            if (EditorFile.AudicaFile.modifiers != null)
            {
                if (EditorFile.AudicaFile.modifiers.modifiers.Count > 0)
                {
                    ModifierHandler.isLoading = true;
                    StartCoroutine(ModifierHandler.Instance.LoadModifiers(EditorFile.AudicaFile.modifiers.modifiers, true));
                }

            }

            //Loaded successfully

            //NotificationCenter.SendNotification (new NRNotification ("Map loaded successfully!"));
            NotificationCenter.SendNotification("Press F1 to view shortcuts", NotificationType.Info);
            StopCoroutine(NRSettings.Autosave());
            StartCoroutine(NRSettings.Autosave());
            UpdateState();
            EditorFile.SetIsAudicaLoaded(true);
            onLoaded?.Invoke(true);
            yield return null;
        }
        
    }
*/
}
