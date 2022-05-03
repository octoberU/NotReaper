using NotReaper.Grid;
using NotReaper.Models;
using NotReaper.Modifier;
using NotReaper.Repeaters;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools.ChainBuilder;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NotReaper.Managers;
using Newtonsoft.Json;
using NotReaper.Audio;
using NotReaper.Notifications;
using System.IO;
using System;
using SharpCompress;
using SharpCompress.Archives;
using System.Text;
using SharpCompress.Archives.Zip;
using NotReaper.IO;
using NAudio.Midi;
using System.Threading.Tasks;

namespace NotReaper.MapIO
{

    public class AudicaExporter
    {
        internal bool isSaving;
        private RepeaterManager repeaterManager;
        private DifficultyManager difficultyManager;
        public AudicaExporter(RepeaterManager repeaterManager, DifficultyManager difficultyManager)
        {
            this.repeaterManager = repeaterManager;
            this.difficultyManager = difficultyManager;
        }

        public void Save(bool autoSave = false, System.Action onSaved = null)
        {
            if (isSaving)
            {
                onSaved?.Invoke();
                return;
            }
            isSaving = true;
            try
            {
                Export(autoSave, onSaved);
            }
            catch
            {
                NotificationCenter.SendNotification("Something went wrong while saving :(", NotificationType.Error);
                isSaving = false;
                onSaved?.Invoke();
            }
        }
        private void Export(bool autoSave = false, System.Action onSaved = null)
        {
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

            CueFile export = new CueFile();
            export.cues = new List<Cue>();
            export.NRCueData = new NRCueData();
            export.NRCueData.newRepeaterSections = repeaterManager.GetSections();

            foreach (Target target in EditorNotes.OrderedNotes)
            {
                if (target.data.beatLength == 0) target.data.beatLength = Constants.SixteenthNoteDuration;

                var cue = NotePosCalc.ToCue(target, Timeline.offset);
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

            switch (difficultyManager.LoadedDifficulty)
            {
                case Difficulty.Expert:
                    EditorFile.AudicaFile.diffs.expert = export;
                    break;
                case Difficulty.Advanced:
                    EditorFile.AudicaFile.diffs.advanced = export;
                    break;
                case Difficulty.Standard:
                    EditorFile.AudicaFile.diffs.moderate = export;
                    break;
                case Difficulty.Beginner:
                    EditorFile.AudicaFile.diffs.beginner = export;
                    break;
            }

            EditorFile.SongDesc.tempoList = EditorTempo.TempoChanges;
            ExportToFile(EditorFile.AudicaFile, autoSave, onSaved);
        }

        private async void ExportToFile(AudicaFile audicaFile, bool autoSave, System.Action onSaved)
        {

            if (!File.Exists(audicaFile.filepath))
            {
                Debug.Log("Save file is gone... :(");
                isSaving = false;
                onSaved?.Invoke();
                return;
            }

            string targetPath = audicaFile.desc.testplay ? 
                Path.Combine(Path.GetDirectoryName(audicaFile.filepath), $"[WIP]{Path.GetFileName(audicaFile.filepath)}") : 
                audicaFile.filepath;

            string autoSavePath = "";
            using (var archive = ZipArchive.Open(audicaFile.filepath))
            {


                HandleCache.CheckCacheFolderValid();
                HandleCache.CheckSaveFolderValid();

                bool expert = false, advanced = false, standard = false, easy = false, modifiers = false;
                //Write the cues files to disk so we can add them to the audica file.
                if (audicaFile.diffs.expert.cues != null)
                {
                    await File.WriteAllTextAsync($"{Application.dataPath}/.cache/expert-new.cues", CuesToJson(audicaFile.diffs.expert));
                    expert = true;
                }
                if (audicaFile.diffs.advanced.cues != null)
                {
                    await File.WriteAllTextAsync($"{Application.dataPath}/.cache/advanced-new.cues", CuesToJson(audicaFile.diffs.advanced));
                    advanced = true;
                }
                if (audicaFile.diffs.moderate.cues != null)
                {
                    await File.WriteAllTextAsync($"{Application.dataPath}/.cache/moderate-new.cues", CuesToJson(audicaFile.diffs.moderate));
                    standard = true;
                }
                if (audicaFile.diffs.beginner.cues != null)
                {
                    await File.WriteAllTextAsync($"{Application.dataPath}/.cache/beginner-new.cues", CuesToJson(audicaFile.diffs.beginner));
                    easy = true;
                }
                audicaFile.modifiers = new ModifierList();
                audicaFile.modifiers.modifiers = ModifierHandler.Instance.MapToDTO();
                if (audicaFile.modifiers.modifiers.Count > 0)
                {
                    await File.WriteAllTextAsync($"{Application.dataPath}/.cache/modifiers-new.json", ModifiersToJson2(audicaFile.modifiers));
                    modifiers = true;
                }
                await File.WriteAllTextAsync($"{Application.dataPath}/.cache/{audicaFile.desc.moggSong}", audicaFile.mainMoggSong.ExportToText(false));
                await File.WriteAllTextAsync($"{Application.dataPath}/.cache/song_sustain_l.moggsong", UISustainHandler.Instance.sustainSongLeft.ExportToText(true));
                await File.WriteAllTextAsync($"{Application.dataPath}/.cache/song_sustain_r.moggsong", UISustainHandler.Instance.sustainSongRight.ExportToText(true));
                await File.WriteAllTextAsync($"{Application.dataPath}/.cache/song-new.desc", Newtonsoft.Json.JsonConvert.SerializeObject(audicaFile.desc, Formatting.Indented));

                var workFolder = Path.Combine(Application.streamingAssetsPath, "Ogg2Audica");
                MidiFile songMidi = new MidiFile(Path.Combine(workFolder, "songtemplate.mid"));

                MidiEventCollection events = new MidiEventCollection(0, (int)Constants.PulsesPerQuarterNote);
                foreach (var tempo in audicaFile.desc.tempoList)
                {
                    events.AddEvent(new TempoEvent((int)tempo.microsecondsPerQuarterNote, (long)tempo.time.tick), 0);
                    events.AddEvent(new TimeSignatureEvent((long)tempo.time.tick, (int)tempo.timeSignature.Numerator, (int)TimeSignature.GetMIDIDenominator(tempo.timeSignature.Denominator), 0, 8), 0);
                }

                events.PrepareForExport();
                MidiFile.Export(Path.Combine(workFolder, $"{Application.dataPath}/.cache/song.mid"), events);


                //Remove any files we'll be replacing
                foreach (ZipArchiveEntry entry in archive.Entries)
                {

                    if (entry.ToString() == "expert.cues")
                    {
                        archive.RemoveEntry(entry);
                    }
                    else if (entry.ToString() == "song.desc")
                    {
                        archive.RemoveEntry(entry);
                    }
                    else if (entry.ToString() == audicaFile.desc.moggSong)
                    {
                        archive.RemoveEntry(entry);
                    }
                    else if (entry.ToString() == "song.mid")
                    {
                        archive.RemoveEntry(entry);
                    }
                    else if (entry.ToString() == "song.png")
                    {
                        archive.RemoveEntry(entry);
                    }
                    else if (entry.ToString() == "advanced.cues")
                    {
                        archive.RemoveEntry(entry);
                    }
                    else if (entry.ToString() == "moderate.cues")
                    {
                        archive.RemoveEntry(entry);
                    }
                    else if (entry.ToString() == "beginner.cues")
                    {
                        archive.RemoveEntry(entry);
                    }
                    else if (entry.ToString() == "modifiers.json")
                    {
                        archive.RemoveEntry(entry);
                    }
                    else if (entry.ToString() == "song_sustain_r.moggsong")
                    {
                        archive.RemoveEntry(entry);
                    }
                    else if (entry.ToString() == "song_sustain_l.moggsong")
                    {
                        archive.RemoveEntry(entry);
                    }
                    else if (UISustainHandler.PendingDelete)
                    {
                        if (UISustainHandler.LoadedTracks == UISustainHandler.SustainTrack.Left && entry.ToString() == "song_sustain_r.mogg")
                        {
                            archive.RemoveEntry(entry);
                        }
                        else if (UISustainHandler.LoadedTracks == UISustainHandler.SustainTrack.Right && entry.ToString() == "song_sustain_l.mogg")
                        {
                            archive.RemoveEntry(entry);
                        }
                        else if (UISustainHandler.LoadedTracks == UISustainHandler.SustainTrack.None && (entry.ToString() == "song_sustain_r.mogg" || entry.ToString() == "song_sustain_l.mogg"))
                        {
                            archive.RemoveEntry(entry);
                        }
                    }


                }
                if (expert) archive.AddEntry("expert.cues", $"{Application.dataPath}/.cache/expert-new.cues");
                if (advanced) archive.AddEntry("advanced.cues", $"{Application.dataPath}/.cache/advanced-new.cues");
                if (standard) archive.AddEntry("moderate.cues", $"{Application.dataPath}/.cache/moderate-new.cues");
                if (easy) archive.AddEntry("beginner.cues", $"{Application.dataPath}/.cache/beginner-new.cues");
                if (modifiers) archive.AddEntry("modifiers.json", $"{Application.dataPath}/.cache/modifiers-new.json");



                if (autoSave)
                {
                    int pos = audicaFile.filepath.LastIndexOf(@"\") + 1;
                    string fileName = audicaFile.filepath.Substring(pos, audicaFile.filepath.Length - pos);
                    string shortName = fileName.Substring(0, fileName.LastIndexOf(@"."));
                    shortName = shortName.Replace(" ", "");
                    targetPath = $"{Application.dataPath}/autosaves/{shortName}/";
                    autoSavePath = targetPath;
                    targetPath += DateTime.Now.ToString("MM-dd_h-mm-ss_");
                    targetPath += fileName;
                    if (!Directory.Exists($"{Application.dataPath}/autosaves/")) Directory.CreateDirectory($"{Application.dataPath}/autosaves/");
                    if (!Directory.Exists($"{Application.dataPath}/autosaves/{shortName}/")) Directory.CreateDirectory($"{Application.dataPath}/autosaves/{shortName}/");
                }
                archive.AddEntry($"{audicaFile.desc.moggSong}", $"{Application.dataPath}/.cache/{audicaFile.desc.moggSong}");
                archive.AddEntry($"song_sustain_l.moggsong", $"{Application.dataPath}/.cache/song_sustain_l.moggsong");
                archive.AddEntry($"song_sustain_r.moggsong", $"{Application.dataPath}/.cache/song_sustain_r.moggsong");
                archive.AddEntry("song.desc", $"{Application.dataPath}/.cache/song-new.desc");
                archive.AddEntry("song.mid", $"{Application.dataPath}/.cache/song.mid");
                if (File.Exists($"{Application.dataPath}/.cache/song.png"))
                {
                    archive.AddEntry("song.png", $"{Application.dataPath}/.cache/song.png");
                }
                archive.SaveTo(audicaFile.filepath + ".temp", SharpCompress.Common.CompressionType.None);
                archive.Dispose();
            }
            File.Delete($"{Application.dataPath}/.cache/{audicaFile.desc.moggSong}");

            if (!autoSave)
            {
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                File.Delete(targetPath);
            }

            File.Move(audicaFile.filepath + ".temp", targetPath);

            if (autoSave) NRSettings.autosavePath = autoSavePath;
            NotificationCenter.SendNotification("Map saved!", NotificationType.Success, false);
            SoundEffects.Instance.PlaySound(SoundEffects.Sound.Save);
            isSaving = false;
            onSaved?.Invoke();
        }


        public static string CuesToJson(CueFile cueFile)
            => JsonUtility.ToJson(cueFile, true);
        

        public static string ModifiersToJson2(ModifierList modifiers)
            => JsonUtility.ToJson(modifiers, true);
    }
}
