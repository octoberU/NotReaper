using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using NotReaper.IO;
using NotReaper.Managers;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NotReaper.Timing;
using NotReaper.Tools.PathBuilder;
using NotReaper.Repeaters;
using NotReaper.Notifications;

namespace NotReaper
{
    public class Timeline : Singleton<Timeline>
    {
        [Header("UI Elements")]
        [SerializeField] private MiniTimeline miniTimeline;
        [SerializeField] private TextMeshProUGUI songTimestamp;
        [SerializeField] private TextMeshProUGUI curTick;
        [SerializeField] private TextMeshProUGUI curDiffText;

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

        [Header("Configuration")]
        public string playbackSpeedPercentage = "Speed: 100%";

        public static bool inTimingMode = false;
        public static bool isSaving = false;

        public static float scaleTransform;
        public static Relative_QNT offset = new Relative_QNT(0);

        private List<GameObject> bpmMarkerObjects = new List<GameObject>();

        [SerializeField] public PrecisePlayback songPlayback;

        public AudioWaveformVisualizer waveformVisualizer;
        [SerializeField] private AudioWaveformVisualizer miniWaveformVisualizer;
        [SerializeField]
        internal AudioWaveformVisualizer sustainVisualizer;

        public Transform timelineCamera;
        public Transform gridCamera;

        [NRInject] internal Pathbuilder pathbuilder;
        [NRInject] internal RepeaterManager repeaterManager;

        #region Awake and Start
        protected override void Awake()
        {
            base.Awake();

            EditorBeatSnap.onBeatSnapChanged += BeatSnapChanged;
            EditorScale.onScaleChanged += OnScaleChanged;
            EditorAudio.onPlaybackSpeedChanged += OnPlaybackSpeedChanged;
            EditorTime.onTimeChanged += _ => MoveTimelineOnTickChange();
            EditorFile.onLoaded += () => RegenerateBPMTimelineData();
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
            NRSettings.config.showWaveform = !NRSettings.config.showWaveform;
            waveformVisualizer.UpdateWaveformVisibility();
            sustainVisualizer.UpdateWaveformVisibility();
            NRSettings.SaveSettingsJson();
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

        #region Scale
        private int oldScale = EditorScale.DefaultScale;
        private void OnScaleChanged(int newScale)
        {
            timelineBG.material.SetTextureScale("_MainTex", new Vector2(newScale / 4f, 1));

            Vector3 timelineTransformScale = timelineTransformParent.transform.localScale;
            timelineTransformScale.x *= (float)oldScale / newScale;
            scaleTransform = timelineTransformScale.x;
            timelineTransformParent.transform.localScale = timelineTransformScale;

            // fix scaling on all notes
            foreach (Transform note in timelineTransformParent.transform)
            {
                note.localScale = EditorScale.GetNoteScale(note.localScale);
            }
            oldScale = newScale;

            foreach (Target target in EditorNotes.OrderedNotes)
            {
                target.UpdateTimelineSustainLength();
            }
            BuildIntroZone();
            UpdateTimeline(EditorTime.Time, true);
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

        public float timelineMoveSpeed = 1f;
        private bool isAnimatingTimeline = false;
        private Vector3 targetPos;
        private void AnimateTimeline()
        {
            if (isAnimatingTimeline)
                return;

            isAnimatingTimeline = true;
            StartCoroutine(DoAnimate());
        }

        private IEnumerator DoAnimate()
        {
            while (EditorAudio.IsPlaying)
            {
                timelineCamera.transform.localPosition = Vector3.Lerp(timelineCamera.transform.localPosition, targetPos, Time.deltaTime * timelineMoveSpeed);
                yield return null;
            }
            isAnimatingTimeline = false;
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

            if (EditorFile.IsLoading) return;
            
            EditorScale.ReapplyScale();
            foreach (var tempo in EditorTempo.TempoChanges)
            {
                var timelineBPM = Instantiate(bpmMarkerPrefab, Timeline.timelineNotesStatic);
                var transform1 = timelineBPM.transform;
                transform1.localPosition = new Vector3(tempo.time.ToBeatTime(), -0.5f, 0);

                string bpm = Constants.DisplayBPMFromMicrosecondsPerQuaterNote(tempo.microsecondsPerQuarterNote);
                string timeSignature = tempo.timeSignature.ToString();

                int id = TimelineTextManager.Instance.AddText($"{bpm} {timeSignature}", tempo.time);
                timelineBPM.GetComponent<BPMMarker>().id = id;
                bpmMarkerObjects.Add(timelineBPM);
            }

            if (songPlayback.song == null)
            {
                return;
            }

            DrawTimingBars(timelineNotesStatic.gameObject.GetComponent<MeshFilter>().mesh, .02f, .4f, 3f);

            if (!onlyRegenerateMesh)
            {
                waveformVisualizer.GenerateWaveform(songPlayback.song, this);
                miniWaveformVisualizer.GenerateWaveform(songPlayback.song, this);
                if (songPlayback.leftSustain != null)
                {
                    sustainVisualizer.GenerateWaveform(songPlayback.leftSustain, this);
                }
            }

            repeaterManager.UpdateMiniIndicatorPositions();
        }

        public void DrawTimingBars(Mesh mesh, float width, float maxHeight, float zIndex)
        {
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

                //const float width = 0.020f;
                //const float maxHeight = 0.4f;
                //const float zIndex = 3;
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
            mesh.Clear();

            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
        }
        #endregion

        #region Timing
        public void LoadTimingMode(AudioClip clip)
        {
            if (EditorFile.IsAudicaFileLoaded) return;

            songPlayback.LoadAudioClip(clip, PrecisePlayback.LoadType.MainSong);
            inTimingMode = true;
            EditorFile.SetIsAudioLoaded(true);
        }
        public void SetTimingModeStats(UInt64 microsecondsPerQuarterNote, int tickOffset)
        {
            EditorTargets.DeleteAllTargets();
            readyToRegenerate = false;
            EditorTempo.SetBPM(new QNT_Timestamp(0), microsecondsPerQuarterNote, false);
        }

        public void ExitTimingMode()
        {
            inTimingMode = false;
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

        public void UpdateTimeline(QNT_Timestamp t, bool resetPosition = false)
        {
            float x = t.ToBeatTime() - offset.ToBeatTime();
            Vector3 pos = timelineCamera.transform.localPosition;
            pos.x = 1f * x / EditorScale.ScaleAmount;
            targetPos = pos;
            if (EditorAudio.IsPlaying && !resetPosition)
            {
                AnimateTimeline();
            }
            else
            {
                timelineCamera.transform.localPosition = pos;
            }
            pos = gridCamera.position;
            pos.z = x - 5f;
            gridCamera.position = pos;
            UpdateTime();
        }
        #endregion
    }
}