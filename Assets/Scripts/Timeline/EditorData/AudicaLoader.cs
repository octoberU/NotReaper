using Ionic.Zip;
using NAudio.Midi;
using NotReaper.IO;
using NotReaper.Managers;
using NotReaper.Models;
using NotReaper.Modifier;
using NotReaper.Notifications;
using NotReaper.UI;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NotReaper.Modifiers;
using UnityEngine;

namespace NotReaper.MapIO
{
    public class AudicaLoader : MonoBehaviour
    {
        private string appPath;
        private DifficultyManager difficultyManager;
        private NRDiscordPresence discordPresence;

        private void Start()
        {    
            appPath = Application.dataPath;
            difficultyManager = DifficultyManager.Instance;
            discordPresence = NRDiscordPresence.Instance;
        }

        public void LoadMap(string filePath, Action<bool> onFinished = null, float bpm = -1, int numerator = -1, int denominator = -1)
        {
            if (EditorFile.IsAudicaFileLoaded && NRSettings.config.saveOnLoadNew)
                EditorIO.SaveMap(new System.Action(() => { StartCoroutine(DoLoadMap(filePath, onFinished, bpm, numerator, denominator)); }));
            else
                StartCoroutine(DoLoadMap(filePath, onFinished, bpm, numerator, denominator));
        }

        public void SelectMap(Action<bool> onFinished = null)
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
                onFinished?.Invoke(false);
                return;
            }

            PlayerPrefs.SetString("recentDir", Path.GetDirectoryName(paths[0]));
            PlayerPrefs.SetString("recentFile", paths[0]);
            //StartCoroutine(DoLoadMap(paths[0], onFinished));
            LoadMap(paths[0], onFinished);
        }

        private IEnumerator DoLoadMap(string filePath, Action<bool> onFinished = null, float bpm = -1, int numerator = -1, int denominator = -1)
        {

            EditorFile.SetIsLoading(true);
            
            if (EditorFile.IsAudicaFileLoaded && NRSettings.config.saveOnLoadNew)
                EditorIO.SaveMap();

            while (EditorIO.IsSaving)
                yield return null;


            var file = LoadAudicaFile(filePath, out bool hasLeftSustain, out bool hasRightSustain);
            if(file == null)
            {
                onFinished?.Invoke(false);
                yield break;
            }

            RecentAudicaFiles.AddRecentDir(filePath);
            UISustainHandler.Instance.LoadVolume(hasLeftSustain, hasRightSustain);

            EditorState.ResetEditor();
            EditorFile.UnloadAudicaFile();
            EditorFile.SetAudicaFile(file);
            EditorTempo.LoadFromFile(EditorFile.AudicaFile.song_mid, bpm, EditorFile.SongDesc.tempo, numerator, denominator);
            //Update our discord presence
            discordPresence.UpdatePresenceSongName(EditorFile.SongDesc.title);


            //Loads all the sounds.
            yield return StartCoroutine(EditorAudioManager.Instance.GetAudioClip($"file://{appPath}/.cache/{EditorFile.AudicaFile.desc.cachedMainSong}.ogg"));
            if (EditorFile.AudicaFile.desc.sustainSongLeft != "") yield return StartCoroutine(EditorAudioManager.Instance.LoadLeftSustain($"file://{appPath}/.cache/{EditorFile.AudicaFile.desc.cachedSustainSongLeft}.ogg"));
            if (EditorFile.AudicaFile.desc.sustainSongRight != "") yield return StartCoroutine(EditorAudioManager.Instance.LoadRightSustain($"file://{appPath}/.cache/{EditorFile.AudicaFile.desc.cachedSustainSongRight}.ogg"));
            yield return StartCoroutine(EditorAudioManager.Instance.LoadExtraAudio($"file://{appPath}/.cache/{EditorFile.AudicaFile.desc.cachedFxSong}.ogg"));

            difficultyManager.LoadHighestDifficulty();


            //Load bookmarks
            MiniTimeline.Instance.LoadBookmarks();
            //Load modifiers
            if (EditorFile.AudicaFile.modifiers != null)
            {
                if (EditorFile.AudicaFile.modifiers.modifiers.Count > 0)
                {
                    //ModifierHandler.isLoading = true;
                    //yield return StartCoroutine(ModifierHandler.Instance.LoadModifiers(EditorFile.AudicaFile.modifiers.modifiers, true));
                    yield return StartCoroutine(ModifierIO.LoadModifiers(EditorFile.AudicaFile.modifiers.modifiers));
                }

            }

            //Restart autosave
            StopCoroutine(NRSettings.Autosave());
            StartCoroutine(NRSettings.Autosave());
            EditorFile.SetIsAudicaLoaded(true);
            EditorFile.SetIsLoading(false);
            onFinished?.Invoke(true);
            yield return null;

        }

        private AudicaFile LoadAudicaFile(string path, out bool hasLeftSustain, out bool hasRightSustain)
        {
            AudicaFile audicaFile = new AudicaFile();
            ZipFile audicaZip;
            hasLeftSustain = hasRightSustain = false;
            try
            {
                audicaZip = ZipFile.Read(path);
            }
            catch (IOException)
            {
                NotificationCenter.SendNotification("Audica file not found.", NotificationType.Error);
                return null;
            }

            bool easy = false, standard = false, advanced = false, expert = false, modifiers = false;

            HandleCache.ClearCache();
            HandleCache.CheckCacheFolderValid();
            HandleCache.ClearCueCache();
            //Figure out what files we need to extract by getting the song.desc.
            foreach (ZipEntry entry in audicaZip.Entries)
            {
                if (entry.FileName == "song.desc")
                {
                    MemoryStream ms = new MemoryStream();
                    entry.Extract(ms);
                    string tempDesc = Encoding.UTF8.GetString(ms.ToArray());
                    JsonUtility.FromJsonOverwrite(tempDesc, audicaFile.desc);
                    ms.Dispose();
                    continue;
                }
                //Extract the cues files.
                else if (entry.FileName == "expert.cues")
                {
                    entry.Extract($"{appPath}/.cache");
                    expert = true;

                }
                else if (entry.FileName == "advanced.cues")
                {
                    entry.Extract($"{appPath}/.cache");
                    advanced = true;

                }
                else if (entry.FileName == "moderate.cues")
                {
                    entry.Extract($"{appPath}/.cache");
                    standard = true;

                }
                else if (entry.FileName == "beginner.cues")
                {
                    entry.Extract($"{appPath}/.cache");
                    easy = true;
                }
                else if (entry.FileName == "modifiers.json")
                {
                    entry.Extract($"{appPath}/.cache");
                    modifiers = true;
                }
            }

            //Load moggsongg, has to be done after desc is loaded
            if (audicaZip.ContainsEntry(audicaFile.desc.moggSong))
            {
                if (File.Exists($"{appPath}/.cache/{audicaFile.desc.moggSong}"))
                {
                    File.Delete($"{appPath}/.cache/{audicaFile.desc.moggSong}");
                }
                MemoryStream ms = new MemoryStream();
                audicaZip[audicaFile.desc.moggSong].Extract(ms);
                audicaFile.mainMoggSong = new MoggSong(ms);
                audicaZip[audicaFile.desc.moggSong].Extract($"{appPath}/.cache");
            }
            else Debug.Log("Moggsong not found");
            string l = "song_sustain_l.moggsong";
            if (audicaZip.ContainsEntry(l))
            {
                MemoryStream ms = new MemoryStream();
                audicaZip[l].Extract(ms);
                UISustainHandler.Instance.sustainSongLeft = new MoggSong(ms, true);
                audicaZip[l].Extract($"{appPath}/.cache", ExtractExistingFileAction.OverwriteSilently);
            }
            else
            {
                Debug.Log("Sustain Song Left not found");
            }
            string r = "song_sustain_r.moggsong";
            if (audicaZip.ContainsEntry(r))
            {
                MemoryStream ms = new MemoryStream();
                audicaZip[r].Extract(ms);
                UISustainHandler.Instance.sustainSongRight = new MoggSong(ms, true);
                audicaZip[r].Extract($"{appPath}/.cache", ExtractExistingFileAction.OverwriteSilently);
            }
            else
            {
                Debug.Log("Sustain Song Right not found");
            }
            hasLeftSustain = audicaZip.ContainsEntry("song_sustain_l.mogg");
            hasRightSustain = audicaZip.ContainsEntry("song_sustain_r.mogg");
            //Now we fill the audicaFile var with all the things it needs.
            //Remember, all props in audicaFile.desc refer to either moggsong or the name of the mogg.
            //Real clips are stored in main audicaFile object.

            //Load the cues files.
            if (expert)
            {
                audicaFile.diffs.expert = JsonUtility.FromJson<CueFile>(File.ReadAllText($"{appPath}/.cache/expert.cues"));
            }
            if (advanced)
            {
                audicaFile.diffs.advanced = JsonUtility.FromJson<CueFile>(File.ReadAllText($"{appPath}/.cache/advanced.cues"));
            }
            if (standard)
            {
                audicaFile.diffs.moderate = JsonUtility.FromJson<CueFile>(File.ReadAllText($"{appPath}/.cache/moderate.cues"));
            }
            if (easy)
            {
                audicaFile.diffs.beginner = JsonUtility.FromJson<CueFile>(File.ReadAllText($"{appPath}/.cache/beginner.cues"));
            }
            if (modifiers)
            {
                audicaFile.modifiers = JsonUtility.FromJson<ModifierList>(File.ReadAllText($"{appPath}/.cache/modifiers.json"));
            }
            MemoryStream temp = new MemoryStream();

            //Load the names of the moggs
            foreach (ZipEntry entry in audicaZip.Entries)
            {

                if (entry.FileName == audicaFile.desc.moggSong)
                {
                    entry.Extract(temp);
                    audicaFile.desc.moggMainSong = MoggSongParser.parse_metadata(Encoding.UTF8.GetString(temp.ToArray()))[0];

                }
                else if (entry.FileName == audicaFile.desc.fxSong)
                {
                    entry.Extract(temp);
                    audicaFile.desc.moggFxSong = MoggSongParser.parse_metadata(Encoding.UTF8.GetString(temp.ToArray()))[0];

                }
                else if (entry.FileName == "song.mid" || entry.FileName == audicaFile.desc.midiFile)
                {
                    string midiFiileName = $"{appPath}/.cache/song.mid";

                    entry.Extract($"{appPath}/.cache", ExtractExistingFileAction.OverwriteSilently);

                    if (entry.FileName != "song.mid")
                    {
                        File.Delete(midiFiileName);
                        File.Move($"{appPath}/.cache/" + audicaFile.desc.midiFile, midiFiileName);

                        //Sometimes these midi files get marked with strange attributes. Reset them to normal so we don't have problems deleting them
                        File.SetAttributes(midiFiileName, FileAttributes.Normal);
                    }

                    audicaFile.song_mid = new MidiFile(midiFiileName);

                    //Album art, just gonna shove it here
                }
                else if (entry.FileName == "song.png" || entry.FileName == audicaFile.desc.albumArt)
                {
                    string albumArtName = $"{appPath}/.cache/song.png";
                    entry.Extract($"{appPath}/.cache", ExtractExistingFileAction.OverwriteSilently);

                    if (entry.FileName != "song.png")
                    {
                        File.Delete(albumArtName);
                        File.Move($"{appPath}/.cache/" + audicaFile.desc.albumArt, albumArtName);

                    }
                }
                temp.SetLength(0);
            }

            bool mainSongCached = false, sustainRightCached = false, sustainLeftCached = false, extraSongCached = false;

            if (File.Exists($"{appPath}/.cache/{audicaFile.desc.cachedMainSong}.ogg"))
                mainSongCached = true;

            if (File.Exists($"{appPath}/.cache/{audicaFile.desc.cachedSustainSongRight}.ogg"))
                sustainRightCached = true;

            if (File.Exists($"{appPath}/.cache/{audicaFile.desc.cachedSustainSongLeft}.ogg"))
                sustainLeftCached = true;

            if (File.Exists($"{appPath}/.cache/{audicaFile.desc.cachedFxSong}.ogg"))
                extraSongCached = true;


            //If all the songs were already cached, skip this and go to the finish.
            if (mainSongCached && sustainRightCached && sustainLeftCached)
            {
                Debug.Log("Audio files were already cached and will be loaded.");
                goto Finish;
            }
            //If the files weren't cached, we now need to cache them manually then load them.
            MemoryStream tempMogg = new MemoryStream();

            foreach (ZipEntry entry in audicaZip.Entries)
            {

                if (!mainSongCached && entry.FileName == audicaFile.desc.moggMainSong)
                {
                    entry.Extract(tempMogg);
                    MoggToOgg(tempMogg.ToArray(), audicaFile.desc.cachedMainSong);

                }
                else if (!sustainRightCached && entry.FileName == audicaFile.desc.moggSustainSongRight)
                {
                    entry.Extract(tempMogg);
                    MoggToOgg(tempMogg.ToArray(), audicaFile.desc.cachedSustainSongRight);

                }
                else if (!sustainLeftCached && entry.FileName == audicaFile.desc.moggSustainSongLeft)
                {
                    entry.Extract(tempMogg);
                    MoggToOgg(tempMogg.ToArray(), audicaFile.desc.cachedSustainSongLeft);

                }
                else if (!extraSongCached && entry.FileName == audicaFile.desc.moggFxSong)
                {
                    entry.Extract(tempMogg);
                    MoggToOgg(tempMogg.ToArray(), audicaFile.desc.cachedFxSong);
                }

                tempMogg.SetLength(0);

            }

        Finish:

            audicaFile.filepath = path;
            audicaZip.Dispose();
            return audicaFile;
        }

        public static void MoggToOgg(byte[] bytes, string name)
        {
            byte[] oggStartLocation = new byte[4];

            oggStartLocation[0] = bytes[4];
            oggStartLocation[1] = bytes[5];
            oggStartLocation[2] = bytes[6];
            oggStartLocation[3] = bytes[7];

            int start = BitConverter.ToInt32(oggStartLocation, 0);

            byte[] dst = new byte[bytes.Length - start];
            Array.Copy(bytes, start, dst, 0, dst.Length);
            File.WriteAllBytes($"{Application.dataPath}/.cache/{name}.ogg", dst);

        }

    }
}
