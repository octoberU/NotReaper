using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using NAudio.Midi;
using NotReaper.Grid;
using NotReaper.IO;
using NotReaper.Managers;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Tools;
using NotReaper.Tools.ChainBuilder;
using NotReaper.UI;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Application = UnityEngine.Application;
using NotReaper.Modifier;
using NotReaper.Timing;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using NotReaper.Notifications;
using UnityEngine.Events;
using NotReaper.Tools.PathBuilder;
using NotReaper.Repeaters;
using NotReaper.MapPreview;
using NotReaper.Utility;
using NotReaper.MapEditor.Notes;
using NotReaper.TargetEditor;

namespace NotReaper
{
    public class Timeline : Singleton<Timeline>
    {
        //#region References and Members

        [Header("UI Elements")]
        [SerializeField] private MiniTimeline miniTimeline;
        [SerializeField] private TextMeshProUGUI songTimestamp;
        [SerializeField] private TextMeshProUGUI curTick;
        [SerializeField] private TextMeshProUGUI curDiffText;

        [Header("Prefabs")]
        public TargetIcon timelineTargetIconPrefab;
        public TargetIcon gridTargetIconPrefab;
        [Space, Header("Editor Timing")]
        [SerializeField] internal HorizontalSelector beatSnapSelector;
        [SerializeField] private TextMeshProUGUI beatSnapWarningText;
        [SerializeField] private GameObject bpmMarkerPrefab;
        [Space, Header("Extras")]
        [SerializeField] private NRDiscordPresence nrDiscordPresence;
        [SerializeField] private DifficultyManager difficultyManager;
        [SerializeField] public EditorToolkit Tools;
        [SerializeField] public Transform timelineTransformParent;
        [SerializeField] private Transform gridTransformParent;
        public static Transform gridNotesStatic;
        public static Transform timelineNotesStatic;
        [SerializeField] private Renderer timelineBG;

        [SerializeField] private TextMeshProUGUI playbackSpeedText;
        public Transform introZone;

        public Slider musicVolumeSlider;
        public Slider hitSoundVolumeSlider;

        [Header("Configuration")]
        public string playbackSpeedPercentage = "Speed: 100%";

        public float previewDuration = 0.1f;

        public List<RepeaterSection> repeaterSections = new List<RepeaterSection>();

        public static bool inTimingMode = false;
        public static bool isSaving = false;

        public static float scaleTransform;
        public static Relative_QNT offset = new Relative_QNT(0);

        public Button generateAudicaButton;
        public Button loadAudioFileTiming;


        private List<GameObject> bpmMarkerObjects = new List<GameObject>();

        [SerializeField] public PrecisePlayback songPlayback;

        public AudioWaveformVisualizer waveformVisualizer;
        [SerializeField]
        internal AudioWaveformVisualizer sustainVisualizer;


        [SerializeField] public LineRenderer leftHandTraceLine;
        [SerializeField] public LineRenderer rightHandTraceLine;
        [Space, SerializeField] private Transform timelineTargetCollector;
        public Transform timelineCamera;
        public Transform gridCamera;


        [NRInject] internal Pathbuilder pathbuilder;
        [NRInject] internal RepeaterManager repeaterManager;

        private bool isBeatSnapWarningActive = false;

        #region Awake and Start
        protected override void Awake()
        {
            base.Awake();

            EditorBeatSnap.onBeatSnapChanged += BeatSnapChanged;
            EditorScale.onScaleChanged += OnScaleChanged;
            EditorAudio.onPlaybackSpeedChanged += OnPlaybackSpeedChanged;
            EditorTime.onTimeChanged += _ => MoveTimelineOnTickChange();
        }

        private void Start()
        {

            gridNotesStatic = gridTransformParent;
            timelineNotesStatic = timelineTransformParent;

            NRSettings.OnLoad(() =>
            {
                //SetAudioDSP();
                HandleCache.ClearCache();
            });
            beatSnapWarningText.DOFade(0f, 0f);
        }
        #endregion

        #region UI Updates

        public void UpdateDifficultyColor() => curDiffText.color = NRSettings.config.rightColor;
        public void ToggleWaveform()
        {
            waveformVisualizer.ToggleWaveform();
            sustainVisualizer.ToggleWaveform();
        }
        private string prevTimeText;
        private string prevTickText;
        private void UpdateTime()
        {
            float timeSeconds = EditorTime.Time.ToSeconds();

            string minutes = Mathf.Floor((int)timeSeconds / 60).ToString("00");
            string seconds = ((int)timeSeconds % 60).ToString("00");
            if (seconds != prevTimeText)
            {
                prevTimeText = seconds;
                songTimestamp.text = "<mspace=.5em>" + minutes + "</mspace>" + "<mspace=.4em>:</mspace>" + "<mspace=.5em>" + seconds + "</mspace>";
            }

            string currentTick = EditorTime.Time.ToString();
            if (currentTick != prevTickText)
            {
                prevTickText = currentTick;
                curTick.text = "<mspace=.5em>" + currentTick + "</mspace>";
            }
        }
        #endregion

        #region Conversion
        //When loading from cues, use this.

        #endregion

        #region BPM
        public List<float> DetectBPM(QNT_Timestamp start, QNT_Timestamp end)
        {
            return BPM.Detect(songPlayback.song, this, start, end);
        }
        #endregion

        #region Sustain Playback
        private void UpdateSustains()
        {
            if (!EditorAudio.IsPlaying)
                return;

            foreach (var note in EditorNotes.LoadedNotes)
            {
                if (note.data.behavior == TargetBehavior.Sustain)
                {
                    if ((note.GetRelativeBeatTime() < 0) && (note.GetRelativeBeatTime() + note.data.beatLength.ToBeatTime() > 0))
                    {
                        if (!note.isPlayingSustains)
                        {
                            float panPos = (float)(note.data.x / 7.15);
                            if (EditorFile.AudicaFile.usesLeftSustain && note.data.handType == TargetHandType.Left)
                            {
                                songPlayback.leftSustainVolume = EditorAudio.SustainVolume;
                                if (songPlayback.leftSustain != null) songPlayback.leftSustain.pan = panPos;

                            }
                            else if (EditorFile.AudicaFile.usesRightSustain && note.data.handType == TargetHandType.Right)
                            {
                                songPlayback.rightSustainVolume = EditorAudio.SustainVolume;
                                if (songPlayback.rightSustain != null) songPlayback.rightSustain.pan = panPos;
                            }
                            note.isPlayingSustains = true;
                        }
                    }
                    else
                    {
                        if (note.isPlayingSustains)
                        {
                            if (note.data.handType == TargetHandType.Left)
                            {
                                songPlayback.leftSustainVolume = 0.0f;
                            }
                            else if (note.data.handType == TargetHandType.Right)
                            {
                                songPlayback.rightSustainVolume = 0.0f;
                            }
                            note.isPlayingSustains = false;
                        }
                    }
                }
            }
        }
        #endregion

        #region IO

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
        public IEnumerator DoExport(bool autoSave = false)
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
            UpdateTimeline(EditorTime.Time);
            UpdateTime();
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

            EditorState.ResetEditor();

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
            EditorFile.SetIsAudicaLoaded(true);
            onLoaded?.Invoke(true);
            yield return null;
        }

        public void LoadTimingMode(AudioClip clip)
        {
            if (EditorFile.IsAudicaFileLoaded) return;

            songPlayback.LoadAudioClip(clip, PrecisePlayback.LoadType.MainSong);
            inTimingMode = true;
            EditorFile.SetIsAudioLoaded(true);
        }

        #endregion

        #region Scale
        int oldScale = EditorScale.DefaultScale;
        private void OnScaleChanged(int newScale)
        {
            timelineBG.material.SetTextureScale("_MainTex", new Vector2(newScale / 4f, 1));

            Vector3 timelineTransformScale = timelineTransformParent.transform.localScale;
            timelineTransformScale.x *= (float)oldScale / newScale;
            scaleTransform = timelineTransformScale.x;
            timelineTransformParent.transform.localScale = timelineTransformScale;

            //targetScale *= (float)newScale / oldScale;
            // fix scaling on all notes
            foreach (Transform note in timelineTransformParent.transform)
            {
                note.localScale = EditorScale.GetNoteScale(note.localScale);//GetNoteScale(note.localScale);
            }
            oldScale = newScale;

            foreach (Target target in EditorNotes.OrderedNotes)
            {
                target.UpdateTimelineSustainLength();
            }
            BuildIntroZone();
            UpdateTimeline(EditorTime.Time);
        }
        #endregion

        #region Playback
        private void MoveTimelineOnTickChange()
        {
            UpdateTimeline(EditorTime.Time);
        }

        private void OnPlaybackSpeedChanged(float speed)
        {
            string PlaybackText = ("Speed: " + speed.ToString("#%"));
            playbackSpeedText.text = PlaybackText;
        }
        #endregion

        #region Beat Snap and Timing

        public void ChangeBeatSnap(bool next)
        {
            if (next)
                EditorBeatSnap.NextBeatSnap();
            else
                EditorBeatSnap.PreviousBeatSnap();
        }

        private void BeatSnapChanged(int snap, bool next)
        {
            if (next)
                beatSnapSelector.ForwardClick();
            else
                beatSnapSelector.PreviousClick();

            RegenerateBPMTimelineData(true);

            if (snap >= 32 && !isBeatSnapWarningActive)
            {
                beatSnapWarningText.DOFade(1f, 0.5f);
                isBeatSnapWarningActive = true;
            }
            else if (isBeatSnapWarningActive)
            {
                beatSnapWarningText.DOFade(0f, 0.5f);
                isBeatSnapWarningActive = false;
            }
        }

        internal bool readyToRegenerate = false;
        public void RegenerateBPMTimelineData(bool onlyRegenerateMesh = false)
        {
            if (!readyToRegenerate)
            {
                return;
            }

            foreach (var bpm in bpmMarkerObjects)
            {
                TimelineTextManager.Instance.RemoveText(bpm.GetComponent<BPMMarker>().id);
                Destroy(bpm);
            }
            bpmMarkerObjects.Clear();

            EditorScale.ReapplyScale();
            foreach (var tempo in EditorTempo.TempoChanges)
            {
                var timelineBPM = Instantiate(bpmMarkerPrefab, Timeline.timelineNotesStatic);
                var transform1 = timelineBPM.transform;
                transform1.localPosition = new Vector3(tempo.time.ToBeatTime(), -0.5f, 0);

                string bpm = Constants.DisplayBPMFromMicrosecondsPerQuaterNote(tempo.microsecondsPerQuarterNote);
                string timeSignature = tempo.timeSignature.ToString();

                //timelineBPM.GetComponentInChildren<TextMesh>().text = bpm + "\n" + timeSignature;
                int id = TimelineTextManager.Instance.AddText($"{bpm} {timeSignature}", tempo.time);
                timelineBPM.GetComponent<BPMMarker>().id = id;
                bpmMarkerObjects.Add(timelineBPM);
            }

            if (songPlayback.song == null)
            {
                return;
            }

            QNT_Timestamp endOfAudio = QNT_Timestamp.ShiftTick(songPlayback.song.Length);

            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();

            TempoChange currentTempo = EditorTempo.TempoChanges[0];

            uint barLengthIncr = 0;
            for (float t = 0; t < endOfAudio.tick;)
            {
                //ulong snap = (ulong)(beatSnap / 4);
                float snap = EditorBeatSnap.BeatSnap / 4f;
                float increment = 0f;
                if (snap != 0) increment = Constants.PulsesPerWholeNote / currentTempo.timeSignature.Denominator / snap;
                else increment = Constants.PulsesPerWholeNote / currentTempo.timeSignature.Denominator;

                int indexStart = vertices.Count;

                const float width = 0.020f;
                const float maxHeight = 0.4f;
                const float zIndex = 3;
                float start = t / (float)Constants.PulsesPerQuarterNote;
                start -= width / 2;

                float height = 0.0f;
                if (barLengthIncr == 0)
                {
                    height = maxHeight;
                }
                else
                {
                    height = maxHeight / 4;
                }

                //For 4/4 time, set the halfway heights
                if (currentTempo.timeSignature.Numerator == 4 && currentTempo.timeSignature.Denominator == 4)
                {
                    if (barLengthIncr == 2)
                    {
                        height = maxHeight / 2;
                    }
                }

                vertices.Add(new Vector3(start, -0.5f, zIndex));
                vertices.Add(new Vector3(start + width, -0.5f, zIndex));
                vertices.Add(new Vector3(start + width, -0.5f + height, zIndex));
                vertices.Add(new Vector3(start, -0.5f + height, zIndex));

                indices.Add(indexStart + 0);
                indices.Add(indexStart + 1);
                indices.Add(indexStart + 2);

                indices.Add(indexStart + 2);
                indices.Add(indexStart + 3);
                indices.Add(indexStart + 0);

                barLengthIncr++;
                barLengthIncr = barLengthIncr % currentTempo.timeSignature.Numerator;

                bool newTempo = false;
                foreach (TempoChange tempoChange in EditorTempo.TempoChanges)
                {
                    if (t < tempoChange.time.tick && t + increment >= tempoChange.time.tick)
                    {
                        barLengthIncr = 0;
                        t = tempoChange.time.tick;
                        currentTempo = tempoChange;
                        newTempo = true;
                        break;
                    }
                }

                if (!newTempo)
                {
                    t += increment;
                }
            }

            Mesh mesh = Timeline.timelineNotesStatic.gameObject.GetComponent<MeshFilter>().mesh;
            mesh.Clear();

            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();

            if (!onlyRegenerateMesh)
            {
                waveformVisualizer.GenerateWaveform(songPlayback.song, this);
                if (songPlayback.leftSustain != null)
                {
                    sustainVisualizer.GenerateWaveform(songPlayback.leftSustain, this);
                }
            }
        }
        #endregion

        #region Timing
        public void SetTimingModeStats(UInt64 microsecondsPerQuarterNote, int tickOffset)
        {
            EditorTargets.DeleteAllTargets();
            readyToRegenerate = false;
            EditorTempo.SetBPM(new QNT_Timestamp(0), microsecondsPerQuarterNote, false);
            //Timeline.Instance.SafeSetTime();
        }

        public void ExitTimingMode()
        {
            Timeline.inTimingMode = false;
            EditorTargets.DeleteAllTargets();

        }
        public void BuildIntroZone()
        {
            if (!readyToRegenerate) return;
            if (songPlayback.song == null) return;

            QNT_Timestamp endOfAudio = QNT_Timestamp.ShiftTick(songPlayback.song.Length);
            TempoChange currentTempo = EditorTempo.TempoChanges[0];

            uint barLengthIncr = 0;
            uint measurecount = 0;
            float endMeasure = 0;
            int introLength = currentTempo.microsecondsPerQuarterNote <= 500000 ? 3 : 2;
            for (float t = 0; t < endOfAudio.tick;)
            {
                float increment = Constants.PulsesPerWholeNote / currentTempo.timeSignature.Denominator;

                float start = t / (float)Constants.PulsesPerQuarterNote;
                if (barLengthIncr == 0)
                {
                    measurecount++;

                    if (measurecount == introLength)
                    {
                        endMeasure = start;

                        break;
                    }
                }

                barLengthIncr++;
                barLengthIncr %= currentTempo.timeSignature.Numerator;

                t += increment;

            }

            introZone.localPosition = new Vector3(0, -0.35f, 0); // intro red zone

            introZone.localScale = new Vector3(endMeasure, 0.3f, 1);

            Vector2 topLeft = introZone.transform.TransformPoint(0, 0, 0);
            Vector2 size = introZone.transform.TransformVector(1, 1, 1);

            Vector2 center = new Vector2(topLeft.x + size.x / 2, topLeft.y - size.y / 2);

        }

        public void UpdateTimeline(QNT_Timestamp t)
        {
            float x = t.ToBeatTime() - offset.ToBeatTime();
            Vector3 pos = timelineCamera.transform.localPosition;
            pos.x = 1f * x / EditorScale.ScaleAmount;
            timelineCamera.transform.localPosition = pos;
            pos = gridCamera.position;
            pos.z = x - 5f;
            gridCamera.position = pos;
            UpdateTime();
        }
        #endregion

        #region Count In
        public void PreviewCountIn(uint beats)
        {
            if (EditorAudio.IsPlaying)
            {
                EditorAudio.TogglePlay();
            }

            EditorTime.SetTime(0);
            //SafeSetTime();

            TempoChange first = EditorTempo.TempoChanges[0];
            QNT_Duration timeSignatureDuration = new QNT_Duration(Constants.PulsesPerWholeNote / first.timeSignature.Denominator) * beats;
            songPlayback.PlayClickTrack(new QNT_Timestamp(0) + timeSignatureDuration);
            if (!EditorAudio.IsPlaying)
            {
                EditorAudio.TogglePlay();
            }
        }
        public void GenerateCountIn(uint beats)
        {
            TempoChange first = EditorTempo.TempoChanges[0];
            QNT_Duration timeSignatureDuration = new QNT_Duration(Constants.PulsesPerWholeNote / first.timeSignature.Denominator) * beats;
            string appPath = Application.dataPath;
            string wavPath = $"{appPath}/.cache/" + "clickTrack.wav";
            string oggPath = $"{appPath}/.cache/" + "clickTrack.ogg";

            string moggName = "song_extras.mogg";
            string moggPath = $"{appPath}/.cache/" + moggName;
            SavWav.Save(wavPath, songPlayback.GenerateClickTrack(new QNT_Timestamp(0) + timeSignatureDuration));

            //Convert wav to ogg
            if (!EditorAudioManager.Instance.ConvertWavToOgg(wavPath, oggPath))
            {
                return;
            }

            //Convert ogg to mogg
            EditorAudioManager.Instance.ConvertOggToMogg(oggPath, moggPath);

            //Add extra to zip archive
            using (var archive = ZipArchive.Open(EditorFile.AudicaFile.filepath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.ToString() == moggName)
                    {
                        archive.RemoveEntry(entry);
                    }
                }
                archive.AddEntry(moggName, moggPath);
                archive.SaveTo(EditorFile.AudicaFile.filepath + ".temp", SharpCompress.Common.CompressionType.None);
                archive.Dispose();
            }
            File.Delete(EditorFile.AudicaFile.filepath);
            File.Move(EditorFile.AudicaFile.filepath + ".temp", EditorFile.AudicaFile.filepath);

            //Load the generated extra sounds
            StartCoroutine(EditorAudioManager.Instance.LoadExtraAudio($"file://{oggPath}"));
        }
        #endregion
    }
}