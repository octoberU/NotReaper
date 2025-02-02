using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NotReaper.Targets;
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
        
        public IEnumerator ConvertWavToOggAsync(string wavPath, string oggPath, Action<bool> onComplete = null)
        {
            System.Diagnostics.Process ffmpeg = new System.Diagnostics.Process();
            bool ffmpegFinished = false;
            var waitItem = new WaitUntil(() => ffmpegFinished);
            string ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg.exe");

            if ((Application.platform == RuntimePlatform.LinuxEditor) || (Application.platform == RuntimePlatform.LinuxPlayer))
                ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg");

            if ((Application.platform == RuntimePlatform.OSXEditor) || (Application.platform == RuntimePlatform.OSXPlayer))
                ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpegOSX");

            ffmpeg.StartInfo.Arguments = String.Format("-y -i \"{0}\" \"{1}\"", wavPath, oggPath);
            ffmpeg.EnableRaisingEvents = true;
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            ffmpeg.StartInfo.FileName = ffmpegPath;
            ffmpeg.Exited += (obj, args) => ffmpegFinished = true;
            ffmpeg.Start();
            yield return waitItem;
            onComplete?.Invoke(ffmpeg.ExitCode == 0);
            ffmpeg.Close();
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
        
        public IEnumerator ConvertOggToMoggAsync(string oggPath, string moggPath)
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

            bool ogg2moggFinished = false;
            var waitItem = new WaitUntil(() => ogg2moggFinished);
            string args = $"\"{oggPath}\" \"{moggPath}\"";
            startInfo.Arguments = args;
            ogg2mogg.StartInfo = startInfo;
            ogg2mogg.EnableRaisingEvents = true;
            ogg2mogg.StartInfo.CreateNoWindow = true;
            ogg2mogg.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            ogg2mogg.Exited += (obj, args) => ogg2moggFinished = true;
            ogg2mogg.Start();
            yield return waitItem;
            ogg2mogg.Close();
        }
        
        public void ReplaceSongAudio()
        {
            ReplaceAudio(LoadType.Song, UISustainHandler.SustainTrack.None);
        }

        public bool ReplaceAudio(LoadType type, UISustainHandler.SustainTrack track)
        {
            string lastPath = type == LoadType.Song ? PlayerPrefs.GetString("lastSong") : PlayerPrefs.GetString("lastSustain");
            var compatible = new[] { type == LoadType.Song ? new ExtensionFilter("Compatible Audio Types", "mp3", "ogg", "flac") : new ExtensionFilter("Compatible Audio Types", "ogg") };
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
                    break;
            }
            string mainSongPathBase = $"{appPath}/.cache/";
            string mainSongPath = mainSongPathBase + $"{_p}.ogg";
            string moggPathBase = $"{appPath}/.cache/";
            string moggPath = moggPathBase + moggName;

            var ffmpeg = new System.Diagnostics.Process();

            if (filePath != null)
            {
                if (paths[0].EndsWith(".mp3") || paths[0].EndsWith(".flac"))
                {
                    UnityEngine.Debug.Log(String.Format("-y -i \"{0}\" -map 0:a \"{1}\"", paths[0], Path.Combine(Application.streamingAssetsPath, "FFMPEG", "converted.ogg")));
                    ffmpeg.StartInfo.Arguments =
                        String.Format("-y -i \"{0}\" -map 0:a \"{1}\"", paths[0], Path.Combine(Application.streamingAssetsPath, "FFMPEG", "converted.ogg"));
                    ffmpeg.StartInfo.FileName = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg.exe");
                    ffmpeg.Start();
                    ffmpeg.WaitForExit();
                    Debug.Log("Filepath: " + filePath);
                    //filePath = $"file://" + Path.Combine(Application.streamingAssetsPath, "FFMPEG", "converted.ogg");
                    filePath = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "converted.ogg");
                    Debug.Log("Path: " + filePath);
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
            bool hasSplitAudio = false;
            string leftSustainPath = "";
            string rightSustainPath = "";
            string leftSustainMoggPath = "";
            string rightSustainMoggPath = "";
            if (type == LoadType.Sustain)
            {
                string sustainR = mainSongPathBase + $"{EditorFile.AudicaFile.desc.cachedSustainSongRight}.ogg";
                File.Delete(sustainR);
                File.Copy(filePath, sustainR);

                string errorout = "";
                ffmpeg = new();
                ffmpeg.StartInfo.Arguments = $"-i {sustainR} -af astats -f null -";
                ffmpeg.StartInfo.FileName = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg.exe");
                ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.CreateNoWindow = true;
                ffmpeg.Start();
                errorout = ffmpeg.StandardError.ReadToEnd();
                ffmpeg.WaitForExit();
                ffmpeg.Close();
                if (errorout.Contains("stereo"))
                {
                    SplitStereoSustainToMono(mainSongPathBase, sustainR, out leftSustainPath, out rightSustainPath);
                    hasSplitAudio = true;
                    UISustainHandler.Instance.SetBothTracksLoaded();
                }
            }
            if (hasSplitAudio)
            {
                leftSustainMoggPath = Path.Combine(moggPathBase, Path.GetFileNameWithoutExtension(leftSustainPath) + ".mogg");
                rightSustainMoggPath = Path.Combine(moggPathBase, Path.GetFileNameWithoutExtension(rightSustainPath) + ".mogg");
                ConvertOggToMogg(leftSustainPath, leftSustainMoggPath);
                ConvertOggToMogg(rightSustainPath, rightSustainMoggPath);
            }
            else
            {
                ConvertOggToMogg(filePath, moggPath);
            }
            if (type == LoadType.Sustain)
            {
                if (hasSplitAudio)
                {
                    string sustainL = moggPathBase + "song_sustain_l.mogg";
                    File.Delete(sustainL);
                    File.Copy(leftSustainMoggPath, sustainL);
                    string sustainR = moggPathBase + "song_sustain_r.mogg";
                    File.Delete(sustainR);
                    File.Copy(rightSustainMoggPath, sustainR);
                }
                else
                {
                    string sustainR = moggPathBase + "song_sustain_r.mogg";
                    File.Delete(sustainR);
                    File.Copy(moggPath, sustainR);
                }
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
                        if (track == UISustainHandler.SustainTrack.Left || hasSplitAudio)
                        {
                            if (entry.ToString() == "song_sustain_l.mogg") archive.RemoveEntry(entry);
                        }

                        if (track == UISustainHandler.SustainTrack.Right || hasSplitAudio)
                        {
                            if (entry.ToString() == "song_sustain_r.mogg") archive.RemoveEntry(entry);
                        }
                    }
                }
                if (type == LoadType.Song) archive.AddEntry(moggName, moggPath);
                else
                {
                    if (track == UISustainHandler.SustainTrack.Left || hasSplitAudio)
                    {
                        string sustainL = "song_sustain_l.mogg";
                        archive.AddEntry(sustainL, moggPathBase + sustainL);

                    }
                    if (track == UISustainHandler.SustainTrack.Right || hasSplitAudio)
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
                if (track == UISustainHandler.SustainTrack.Left || hasSplitAudio)
                {
                    if (EditorFile.AudicaFile.desc.sustainSongLeft != "") StartCoroutine(LoadLeftSustain(hasSplitAudio ? leftSustainPath : file));
                }
                if (track == UISustainHandler.SustainTrack.Right || hasSplitAudio)
                {
                    if (EditorFile.AudicaFile.desc.sustainSongRight != "") StartCoroutine(LoadRightSustain(hasSplitAudio ? rightSustainPath : file));
                }
            }

            return true;
        }

        private void SplitStereoSustainToMono(string pathBase, string path, out string leftSus, out string rightSus)
        {

            var tempFlac = EditorFile.AudicaFile.desc.cachedSustainSongRight + "_temp_converted.flac";
            ConvertToFlac(path, tempFlac);
            leftSus = Path.Combine(pathBase, EditorFile.AudicaFile.desc.cachedSustainSongLeft);
            rightSus = Path.Combine(pathBase, EditorFile.AudicaFile.desc.cachedSustainSongRight);
            string tempLeft = leftSus + "_temp.ogg";
            string tempRight = rightSus + "_temp.ogg";
            leftSus += ".ogg";
            rightSus += ".ogg";
            var ffmpeg = new System.Diagnostics.Process();
            ffmpeg.StartInfo.Arguments = $"-i {tempFlac} -filter_complex \"[0:a]channelsplit = channel_layout = stereo[left][right]\" -map \"[left]\" {tempLeft} -map \"[right]\" {tempRight}";
            ffmpeg.StartInfo.FileName = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg.exe");
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.Start();
            ffmpeg.WaitForExit();
            ffmpeg.Close();

            if (File.Exists(leftSus))
                File.Delete(leftSus);

            if (File.Exists(rightSus))
                File.Delete(rightSus);
            
            if(File.Exists(tempFlac))
                File.Delete(tempFlac);

            File.Copy(tempLeft, leftSus);
            File.Copy(tempRight, rightSus);
            File.Delete(tempLeft);
            File.Delete(tempRight);
        }

        private void ConvertToFlac(string filePath, string newFile)
        {
            var ffmpeg = new System.Diagnostics.Process();
            ffmpeg.StartInfo.Arguments = $"-i {filePath} {newFile}";
            ffmpeg.StartInfo.FileName = Path.Combine(Application.streamingAssetsPath, "FFMPEG", "ffmpeg.exe");
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.Start();
            ffmpeg.WaitForExit();
            ffmpeg.Close();
        }

        public enum LoadType
        {
            Song,
            Sustain
        }

        public void RemoveOrAddTimeToAudio(Relative_QNT timeChange, bool modifyEnd, Action onComplete = null)
            => StartCoroutine(ModifyAudioLength(timeChange, modifyEnd, onComplete));

        private IEnumerator ModifyAudio(ClipData data, string basePath, double beatTimeChange, bool modifyEnd, Action<bool> onComplete = null)
        {
            if (data == null || data.samples.Length == 0)
            {
                onComplete?.Invoke(false);
                yield break;
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

            options.fromEnd = modifyEnd;

            SavWav.AudioClipData audioData = new SavWav.AudioClipData();
            audioData.samples = data.samples;
            audioData.frequency = (uint)data.frequency;
            audioData.channels = (ushort)data.channels;

            SavWav.Save(basePath + ".wav", audioData, options);
            bool success = false;
            yield return ConvertWavToOggAsync(basePath + ".wav", basePath + ".ogg", (bool s) => success = s);
            if (success)
            {
                File.Delete(basePath + ".wav");
                onComplete?.Invoke(true);
            }
            else
            {
                onComplete?.Invoke(false);
            }
        }
        
        private IEnumerator ModifyAudioLength(Relative_QNT timeChange, bool modifyEnd, Action onComplete = null)
        {
            string appPath = Application.dataPath;
            string mainSongPath = $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.cachedMainSong}";
            string leftSustatinPath = $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.cachedSustainSongLeft}";
            string rightSustatinPath = $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.cachedSustainSongRight}";
            string extraSongPath = $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.cachedFxSong}";

            double beatTimeChange = Conversion.FromQNT(timeChange, EditorTempo.TempoChanges[0].microsecondsPerQuarterNote);

            bool modificationSucceeded = false;
            bool leftSustainSucceeded = false;
            bool rightSustainSucceeded = false;
            bool extraSongSucceeded = false;

            yield return ModifyAudio(Timeline.Instance.songPlayback.song,
                mainSongPath, beatTimeChange, modifyEnd, (bool s) => modificationSucceeded = s);

            yield return ModifyAudio(Timeline.Instance.songPlayback.leftSustain, leftSustatinPath, beatTimeChange, modifyEnd,
                (bool s) => leftSustainSucceeded = s);

            yield return ModifyAudio(Timeline.Instance.songPlayback.rightSustain, rightSustatinPath, beatTimeChange, modifyEnd,
                (bool s) => rightSustainSucceeded = s);

            yield return ModifyAudio(Timeline.Instance.songPlayback.songExtra, extraSongPath, beatTimeChange, modifyEnd,
                (bool s) => extraSongSucceeded = s);
            
            //If success, Shift, then reload audio
            if (modificationSucceeded)
            {
                //Convert ogg to mogg
                yield return ConvertOggToMoggAsync(mainSongPath + ".ogg", $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggMainSong}");

                HashSet<string> entriesToUpdate = new HashSet<string>();
                entriesToUpdate.Add(EditorFile.AudicaFile.desc.moggMainSong);

                if (leftSustainSucceeded)
                {
                    yield return ConvertOggToMoggAsync(leftSustatinPath + ".ogg", $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggSustainSongLeft}");
                    entriesToUpdate.Add(EditorFile.AudicaFile.desc.moggSustainSongLeft);
                }

                if (rightSustainSucceeded)
                {
                    yield return ConvertOggToMoggAsync(rightSustatinPath + ".ogg", $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggSustainSongRight}");
                    entriesToUpdate.Add(EditorFile.AudicaFile.desc.moggSustainSongRight);
                }

                if (extraSongSucceeded)
                {
                    yield return ConvertOggToMoggAsync(extraSongPath + ".ogg", $"{appPath}/.cache/" + $"{EditorFile.AudicaFile.desc.moggFxSong}");
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
                GC.Collect();
                GC.WaitForPendingFinalizers();
                File.Delete(EditorFile.AudicaFile.filepath);
                File.Move(EditorFile.AudicaFile.filepath + ".temp", EditorFile.AudicaFile.filepath);

                //After we have the new audica file, move the notes and load the new audio, but only if we're modifying the start of the song!
                if(!modifyEnd)
                    EditorTempo.ShiftEverythingByTime(timeChange);
                //EditorFile.SetIsAudicaLoaded(false);
                //EditorFile.SetIsAudioLoaded(false);
                yield return StartCoroutine(GetAudioClip($"file://{Application.dataPath}/.cache/{EditorFile.AudicaFile.desc.cachedMainSong}.ogg"));

                if (leftSustainSucceeded)
                {
                    yield return StartCoroutine(LoadLeftSustain($"file://{leftSustatinPath}.ogg"));
                }

                if (rightSustainSucceeded)
                {
                    yield return StartCoroutine(LoadRightSustain($"file://{rightSustatinPath}.ogg"));
                }

                if (extraSongSucceeded)
                {
                    yield return StartCoroutine(LoadExtraAudio($"file://{extraSongPath}.ogg"));
                }
            }
            onComplete?.Invoke();
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

        public IEnumerator LoadExtraAudio(string uri, Action<bool> onLoaded = null)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                    onLoaded?.Invoke(false);
                }
                else
                {
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    Timeline.Instance.songPlayback.LoadAudioClip(myClip, PrecisePlayback.LoadType.Extra);
                    onLoaded?.Invoke(true);
                }
            }
        }
    }

}
