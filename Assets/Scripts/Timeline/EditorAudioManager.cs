using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using NotReaper.Timing;
using UnityEngine.Networking;
using NotReaper.UI;

namespace NotReaper
{
    public class EditorAudioManager : Singleton<EditorAudioManager>
    {
        public bool ConvertWavToOgg(string wavPath, string oggPath)
        {
            System.Diagnostics.Process ffmpeg = new System.Diagnostics.Process();

            string ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg.exe");

            if ((Application.platform == RuntimePlatform.LinuxEditor) || (Application.platform == RuntimePlatform.LinuxPlayer))
                ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg");

            if ((Application.platform == RuntimePlatform.OSXEditor) || (Application.platform == RuntimePlatform.OSXPlayer))
                ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpegOSX");

            ffmpeg.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            ffmpeg.StartInfo.FileName = ffmpegPath;
            ffmpeg.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.RedirectStandardError = true;

            ffmpeg.StartInfo.Arguments = String.Format("-y -i \"{0}\" \"{1}\"", wavPath, oggPath);
            ffmpeg.Start();
            Debug.Log(ffmpeg.StandardOutput.ReadToEnd());
            Debug.Log(ffmpeg.StandardError.ReadToEnd());
            ffmpeg.WaitForExit();

            return ffmpeg.ExitCode == 0;
        }

        public void ConvertOggToMogg(string oggPath, string moggPath)
        {
            var workFolder = Path.Combine(Application.streamingAssetsPath, "Ogg2Audica");

            System.Diagnostics.Process ogg2mogg = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;

            startInfo.FileName = Path.Combine(workFolder, "ogg2mogg.exe");

            if ((Application.platform == RuntimePlatform.LinuxEditor) || (Application.platform == RuntimePlatform.LinuxPlayer))
                startInfo.FileName = Path.Combine(workFolder, "ogg2mogg");

            if ((Application.platform == RuntimePlatform.OSXEditor) || (Application.platform == RuntimePlatform.OSXPlayer))
                startInfo.FileName = Path.Combine(workFolder, "ogg2moggOSX");

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            string args = $"\"{oggPath}\" \"{moggPath}\"";
            startInfo.Arguments = args;
            startInfo.UseShellExecute = false;

            ogg2mogg.StartInfo = startInfo;
            ogg2mogg.Start();
            Debug.Log(ogg2mogg.StandardOutput.ReadToEnd());
            Debug.Log(ogg2mogg.StandardError.ReadToEnd());
            ogg2mogg.WaitForExit();
        }
        public void ReplaceSongAudio()
        {
            ReplaceAudio(LoadType.Song, UISustainHandler.SustainTrack.None);
        }

        public bool ReplaceAudio(LoadType type, UISustainHandler.SustainTrack track)
        {
            string lastPath = type == LoadType.Song ? PlayerPrefs.GetString("lastSong") : PlayerPrefs.GetString("lastSustain");
            var compatible = new[] { type == LoadType.Song ? new ExtensionFilter("Compatible Audio Types", "mp3", "ogg") : new ExtensionFilter("Compatible Audio Types", "ogg") };
            //string[] paths = StandaloneFileBrowser.OpenFilePanel ("Select music track", Path.Combine (Application.persistentDataPath), compatible, false);
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select music track", lastPath, compatible, false);
            if (paths is null || paths.Length == 0) return false;
            var filePath = paths[0];

            if (filePath == null) return false;

            string appPath = Application.dataPath;
            string _p = "";
            string moggName = "";
            switch (type)
            {
                case LoadType.Song:
                    _p = EditorFile.AudicaFile.desc.cachedMainSong;
                    moggName = "song.mogg";
                    PlayerPrefs.SetString("lastSong", Path.GetDirectoryName(filePath));
                    break;
                case LoadType.Sustain:
                    _p = EditorFile.AudicaFile.desc.cachedSustainSongLeft;
                    moggName = "song_sustain_l.mogg";
                    PlayerPrefs.SetString("lastSustain", Path.GetDirectoryName(filePath));
                    /*if(track == UISustainHandler.SustainTrack.Left)
                    {
                        _p = EditorData.AudicaFile.desc.cachedSustainSongLeft;
                        moggName = "song_sustain_l.mogg";
                    }
                    else if(track == UISustainHandler.SustainTrack.Right)
                    {
                        _p = EditorData.AudicaFile.desc.cachedSustainSongRight;
                        moggName = "song_sustain_r.mogg";
                    }*/
                    break;
            }
            string mainSongPathBase = $"{appPath}/.cache/";
            string mainSongPath = mainSongPathBase + $"{_p}.ogg";
            string moggPathBase = $"{appPath}/.cache/";
            string moggPath = moggPathBase + moggName;

            var ffmpeg = new System.Diagnostics.Process();

            if (filePath != null)
            {
                if (paths[0].EndsWith(".mp3"))
                {
                    UnityEngine.Debug.Log(String.Format("-y -i \"{0}\" -map 0:a \"{1}\"", paths[0], "converted.ogg"));
                    ffmpeg.StartInfo.Arguments =
                        String.Format("-y -i \"{0}\" -map 0:a \"{1}\"", paths[0], "converted.ogg");
                    ffmpeg.Start();
                    ffmpeg.WaitForExit();
                    filePath = $"file://" + Path.Combine(Application.streamingAssetsPath, "FFMPEG", filePath);
                    if (type == LoadType.Song) StartCoroutine(GetAudioClip(filePath));
                }
                else
                {
                    if (type == LoadType.Song) StartCoroutine(GetAudioClip(filePath));
                }
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            File.Delete(mainSongPath);
            File.Copy(filePath, mainSongPath);
            if (type == LoadType.Sustain)
            {
                string sustainR = mainSongPathBase + $"{EditorFile.AudicaFile.desc.cachedSustainSongRight}.ogg";
                File.Delete(sustainR);
                File.Copy(filePath, sustainR);
            }

            ConvertOggToMogg(filePath, moggPath);
            if (type == LoadType.Sustain)
            {
                string sustainR = moggPathBase + "song_sustain_r.mogg";
                File.Delete(sustainR);
                File.Copy(moggPath, sustainR);
            }
            using (var archive = ZipArchive.Open(EditorFile.AudicaFile.filepath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (type == LoadType.Song)
                    {
                        if (entry.ToString() == moggName)
                        {
                            archive.RemoveEntry(entry);
                        }
                    }
                    else
                    {
                        if (track == UISustainHandler.SustainTrack.Left)
                        {
                            if (entry.ToString() == "song_sustain_l.mogg") archive.RemoveEntry(entry);
                        }
                        else if (track == UISustainHandler.SustainTrack.Right)
                        {
                            if (entry.ToString() == "song_sustain_r.mogg") archive.RemoveEntry(entry);
                        }
                    }
                }
                if (type == LoadType.Song) archive.AddEntry(moggName, moggPath);
                else
                {
                    if (track == UISustainHandler.SustainTrack.Left)
                    {
                        string sustainL = "song_sustain_l.mogg";
                        archive.AddEntry(sustainL, moggPathBase + sustainL);

                    }
                    else if (track == UISustainHandler.SustainTrack.Right)
                    {
                        string sustainR = "song_sustain_r.mogg";
                        archive.AddEntry(sustainR, moggPathBase + sustainR);
                    }

                }
                archive.SaveTo(EditorFile.AudicaFile.filepath + ".temp", SharpCompress.Common.CompressionType.None);
                archive.Dispose();
            }
            File.Delete(EditorFile.AudicaFile.filepath);
            File.Move(EditorFile.AudicaFile.filepath + ".temp", EditorFile.AudicaFile.filepath);
            string file = $"file://{filePath}";
            if (type == LoadType.Song) StartCoroutine(LoadNewAudioClip(file));
            else
            {
                if (track == UISustainHandler.SustainTrack.Left)
                {
                    if (EditorFile.AudicaFile.desc.sustainSongLeft != "") StartCoroutine(LoadLeftSustain(file));
                }
                else if (track == UISustainHandler.SustainTrack.Right)
                {
                    if (EditorFile.AudicaFile.desc.sustainSongRight != "") StartCoroutine(LoadRightSustain(file));
                }
            }
            return true;
        }

        public enum LoadType
        {
            Song,
            Sustain
        }
        public void RemoveOrAddTimeToAudio(Relative_QNT timeChange)
        {
            string appPath = Application.dataPath;
            string mainSongPath = $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.cachedMainSong}";
            string leftSustatinPath = $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.cachedSustainSongLeft}";
            string rightSustatinPath = $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.cachedSustainSongRight}";
            string extraSongPath = $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.cachedFxSong}";

            double beatTimeChange = Conversion.FromQNT(timeChange, EditorTempo.TempoChanges[0].microsecondsPerQuarterNote);
            Func<ClipData, string, bool> modifyAudio = (ClipData data, string basePath) =>
            {
                if (data == null || data.samples.Length == 0)
                {
                    return false;
                }

                SavWav.WavModificationOptions options = new SavWav.WavModificationOptions();
                int samples = (int)Math.Round(beatTimeChange * data.frequency * data.channels);
                if (samples > 0)
                {
                    options.silenceSamples = (uint)samples;
                }
                else
                {
                    options.trimSamples = (uint)-samples;
                }

                SavWav.AudioClipData audioData = new SavWav.AudioClipData();
                audioData.samples = data.samples;
                audioData.frequency = (uint)data.frequency;
                audioData.channels = (ushort)data.channels;

                SavWav.Save(basePath + ".wav", audioData, options);

                if (ConvertWavToOgg(basePath + ".wav", basePath + ".ogg"))
                {
                    File.Delete(basePath + ".wav");
                    return true;
                }

                return false;
            };

            bool modificationSucceeded = modifyAudio(Timeline.Instance.songPlayback.song, mainSongPath);
            bool leftSustainSucceeded = modifyAudio(Timeline.Instance.songPlayback.leftSustain, leftSustatinPath);
            bool rightSustainSucceeded = modifyAudio(Timeline.Instance.songPlayback.rightSustain, rightSustatinPath);
            bool extraSongSucceeded = modifyAudio(Timeline.Instance.songPlayback.songExtra, extraSongPath);
            //If success, Shift, then reload audio
            if (modificationSucceeded)
            {
                //Convert ogg to mogg
                ConvertOggToMogg(mainSongPath + ".ogg", $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggMainSong}");

                HashSet<string> entriesToUpdate = new HashSet<string>();
                entriesToUpdate.Add(EditorFile.AudicaFile.desc.moggMainSong);

                if (leftSustainSucceeded)
                {
                    ConvertOggToMogg(leftSustatinPath + ".ogg", $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggSustainSongLeft}");
                    entriesToUpdate.Add(EditorFile.AudicaFile.desc.moggSustainSongLeft);
                }

                if (rightSustainSucceeded)
                {
                    ConvertOggToMogg(rightSustatinPath + ".ogg", $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggSustainSongRight}");
                    entriesToUpdate.Add(EditorFile.AudicaFile.desc.moggSustainSongRight);
                }

                if (extraSongSucceeded)
                {
                    ConvertOggToMogg(extraSongPath + ".ogg", $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggFxSong}");
                    entriesToUpdate.Add(EditorFile.AudicaFile.desc.moggFxSong);
                }

                //Add extra to zip archive
                using (var archive = ZipArchive.Open(EditorFile.AudicaFile.filepath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entriesToUpdate.Contains(entry.ToString()))
                        {
                            archive.RemoveEntry(entry);
                        }
                    }

                    archive.AddEntry(EditorFile.AudicaFile.desc.moggMainSong, $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggMainSong}");

                    if (leftSustainSucceeded)
                    {
                        archive.AddEntry(EditorFile.AudicaFile.desc.moggSustainSongLeft, $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggSustainSongLeft}");
                    }

                    if (rightSustainSucceeded)
                    {
                        archive.AddEntry(EditorFile.AudicaFile.desc.moggSustainSongRight, $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggSustainSongRight}");
                    }

                    if (extraSongSucceeded)
                    {
                        archive.AddEntry(EditorFile.AudicaFile.desc.moggFxSong, $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggFxSong}");
                    }

                    archive.SaveTo(EditorFile.AudicaFile.filepath + ".temp", SharpCompress.Common.CompressionType.None);
                    archive.Dispose();
                }
                File.Delete(EditorFile.AudicaFile.filepath);
                File.Move(EditorFile.AudicaFile.filepath + ".temp", EditorFile.AudicaFile.filepath);

                //After we have the new audica file, move the notes and load the new audio
                EditorTempo.ShiftEverythingByTime(timeChange);
                EditorFile.SetIsAudicaLoaded(false);
                EditorFile.SetIsAudioLoaded(false);
                StartCoroutine(GetAudioClip($"file://{Application.dataPath}/.cache/{EditorFile.AudicaFile.desc.cachedMainSong}.ogg"));

                if (leftSustainSucceeded)
                {
                    StartCoroutine(LoadLeftSustain($"file://{leftSustatinPath}.ogg"));
                }

                if (rightSustainSucceeded)
                {
                    StartCoroutine(LoadRightSustain($"file://{rightSustatinPath}.ogg"));
                }

                if (extraSongSucceeded)
                {
                    StartCoroutine(LoadExtraAudio($"file://{extraSongPath}.ogg"));
                }
            }
        }

        public IEnumerator GetAudioClip(string uri)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    Timeline.Instance.songPlayback.LoadAudioClip(myClip, PrecisePlayback.LoadType.MainSong);
                    //EditorFile.SetIsAudicaLoaded(true);
                    EditorFile.SetIsAudioLoaded(true);

                    //Load the preview start point
                    MiniTimeline.Instance.SetPreviewStartPoint(QNT_Timestamp.ShiftTick(0, EditorFile.SongDesc.previewStartSeconds));

                    Timeline.Instance.readyToRegenerate = true;
                    Timeline.Instance.RegenerateBPMTimelineData();
                    Timeline.Instance.BuildIntroZone();
                }
            }
        }

        public IEnumerator LoadNewAudioClip(string uri)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    Timeline.Instance.songPlayback.LoadAudioClip(myClip, PrecisePlayback.LoadType.MainSong);

                    Timeline.Instance.readyToRegenerate = true;
                    Timeline.Instance.RegenerateBPMTimelineData();
                    Timeline.Instance.BuildIntroZone();
                }
            }
        }

        public IEnumerator LoadLeftSustain(string uri)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    EditorFile.AudicaFile.usesLeftSustain = true;
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    Timeline.Instance.songPlayback.LoadAudioClip(myClip, PrecisePlayback.LoadType.LeftSustain);
                    Timeline.Instance.sustainVisualizer.GenerateWaveform(Timeline.Instance.songPlayback.leftSustain, Timeline.Instance);
                }
            }
        }
        public IEnumerator LoadRightSustain(string uri)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    EditorFile.AudicaFile.usesRightSustain = true;
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    Timeline.Instance.songPlayback.LoadAudioClip(myClip, PrecisePlayback.LoadType.RightSustain);
                }
            }
        }

        public IEnumerator LoadExtraAudio(string uri)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    Timeline.Instance.songPlayback.LoadAudioClip(myClip, PrecisePlayback.LoadType.Extra);
                }
            }
        }
    }

}
