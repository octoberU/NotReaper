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

namespace NotReaper
{
    public class Timeline : Singleton<Timeline>
    {
        #region References and Members

        [Header("UI Elements")]
        [SerializeField] private MiniTimeline miniTimeline;
        [SerializeField] private TextMeshProUGUI songTimestamp;
        [SerializeField] private TextMeshProUGUI curTick;
        [SerializeField] private TextMeshProUGUI curDiffText;

        [Header("Prefabs")]
        public TargetIcon timelineTargetIconPrefab;
        public TargetIcon gridTargetIconPrefab;
        [Space, Header("Editor Timing")]
        [SerializeField] private HorizontalSelector beatSnapSelector;
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

        //private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        public static float scaleTransform;
        public static Relative_QNT offset = new Relative_QNT(0);

        [HideInInspector] public bool hover { get; set; } = false;
        //public bool paused = true;
        private bool scrub = false;
        private ScrubParams scrubParams;
        //private bool animationsNeedStopping;
        public Button generateAudicaButton;
        public Button loadAudioFileTiming;

        //public List<TempoChange> tempoChanges { get; private set; } = new List<TempoChange>();
        private List<GameObject> bpmMarkerObjects = new List<GameObject>();

        [SerializeField] public PrecisePlayback songPlayback;

        public AudioWaveformVisualizer waveformVisualizer;
        [SerializeField]
        internal AudioWaveformVisualizer sustainVisualizer;
        //public bool areNotesSelected => selectedNotes.Count > 0;

        [SerializeField] public LineRenderer leftHandTraceLine;
        [SerializeField] public LineRenderer rightHandTraceLine;
        [SerializeField] public GameObject dualNoteTraceLinePrefab;
        [Space, SerializeField] private Transform timelineTargetCollector;
        public Transform timelineCamera;
        public Transform gridCamera;
        List<LineRenderer> dualNoteTraceLines = new List<LineRenderer>();

        [NRInject] internal Pathbuilder pathbuilder;
        [NRInject] internal RepeaterManager repeaterManager;
        [NRInject] private Preview3DManager previewManager;
        public delegate void OnAudicaLoaded(AudicaFile file);
        public static event OnAudicaLoaded onAudicaLoaded;

        private GridTargetPool gridPool;
        private TimelineTargetPool timelinePool;
        private Camera mainCam;
        private bool isBeatSnapWarningActive = false;
        #endregion

        #region Awake and Start
        protected override void Awake()
        {
            base.Awake();
            gridPool = GetComponent<GridTargetPool>();
            timelinePool = GetComponent<TimelineTargetPool>();
            EditorBeatSnap.onBeatSnapChanged += BeatSnapChanged;
            EditorScale.onScaleChanged += OnScaleChanged;
            EditorAudio.onPlaybackSpeedChanged += OnPlaybackSpeedChanged;
        }

        private void Start()
        {
            mainCam = CameraProvider.main;

            gridNotesStatic = gridTransformParent;
            timelineNotesStatic = timelineTransformParent;

            NRSettings.OnLoad(() =>
            {
                SetAudioDSP();
                HandleCache.ClearCache();
            });
            beatSnapWarningText.DOFade(0f, 0f);
        }
        #endregion

        #region UI Updates

        public void UpdateDifficultyColor() => curDiffText.color = NRSettings.config.rightColor;
        /*public void UpdateTargetColors()
        {
            foreach (var target in EditorNotes.OrderedNotes)
            {
                target.gridTargetIcon.UpdateColors();
                target.timelineTargetIcon.UpdateColors();
            }
        }*/
        /*public void UpdateTrail()
        {
            Vector3[] positions = new Vector3[gridTransformParent.childCount];
            for (int i = 0; i < gridTransformParent.transform.childCount; i++)
            {
                positions[i] = gridTransformParent.GetChild(i).localPosition;
            }
            positions = positions.OrderBy(v => v.z).ToArray();
            var liner = gridTransformParent.gameObject.GetComponentInChildren<LineRenderer>();
            liner.positionCount = gridTransformParent.childCount;
            liner.SetPositions(positions);
        }*/

        /*[NRListener]
        public void OnToolChanged(EditorTool _)
        {
            EnableNearSustainButtons();
        }*/
        
        public void UpdateDualines()
        {
            if (NRSettings.config.enableDualines)
            {
                foreach (var line in dualNoteTraceLines)
                {
                    line.enabled = false;
                }

                int index = 0;
                var backIt = new NoteEnumerator(EditorTime.Time - Relative_QNT.FromBeatTime(0.3f), EditorTime.Time + Relative_QNT.FromBeatTime(1.7f));
                Target lastTarget = null;
                foreach (Target t in backIt)
                {
                    if (lastTarget != null &&
                        t.data.behavior != TargetBehavior.ChainNode && lastTarget.data.behavior != TargetBehavior.ChainNode &&
                        t.data.handType != TargetHandType.Either && t.data.handType != TargetHandType.None &&
                        lastTarget.data.handType != TargetHandType.Either && lastTarget.data.handType != TargetHandType.None
                    )
                    {
                        TargetHandType expected = TargetHandType.Left;
                        if (lastTarget.data.handType == expected)
                        {
                            expected = TargetHandType.Right;
                        }

                        if (t.data.time == lastTarget.data.time && t.data.handType == expected)
                        {
                            var dualNoteTraceLine = GetOrCreateDualLine(index++);
                            dualNoteTraceLine.enabled = true;

                            float alphaVal = 0.0f;
                            if (EditorTime.Time > t.data.time)
                            {
                                alphaVal = 1.0f - ((EditorTime.Time - t.data.time).ToBeatTime() / 0.3f);
                            }
                            else
                            {
                                alphaVal = 1.0f - ((t.data.time - EditorTime.Time).ToBeatTime() / 1.7f);
                            }

                            Vector2 leftPos = t.data.position;
                            Vector2 rightPos = lastTarget.data.position;
                            if (t.data.handType == TargetHandType.Right)
                            {
                                Vector2 temp = rightPos;
                                rightPos = leftPos;
                                leftPos = temp;
                            }

                            Vector3[] positions = new Vector3[2];
                            positions[0] = new Vector3(leftPos.x, leftPos.y, 0.05f);
                            positions[1] = new Vector3(rightPos.x, rightPos.y, 0.05f);
                            dualNoteTraceLine.positionCount = positions.Length;
                            dualNoteTraceLine.SetPositions(positions);

                            Gradient gradient = new Gradient();
                            gradient.SetKeys(
                                new GradientColorKey[] { new GradientColorKey(NRSettings.config.leftColor, 0.0f), new GradientColorKey(NRSettings.config.rightColor, 1.0f) },
                                new GradientAlphaKey[] { new GradientAlphaKey(alphaVal, 0.0f), new GradientAlphaKey(alphaVal, 1.0f) }
                            );
                            dualNoteTraceLine.colorGradient = gradient;
                        }
                    }

                    lastTarget = t;
                }
            }
        }
        public static void ShowTimelineTargets(bool show)
        {
            foreach (var target in EditorNotes.OrderedNotes)
            {
                target.timelineTargetIcon.gameObject.SetActive(show);
            }
        }
        private LineRenderer GetOrCreateDualLine(int index)
        {
            while (dualNoteTraceLines.Count <= index)
            {
                GameObject inst = GameObject.Instantiate(dualNoteTraceLinePrefab);
                var renderer = inst.GetComponent<LineRenderer>();
                renderer.enabled = false;
                dualNoteTraceLines.Add(renderer);
            }

            return dualNoteTraceLines.ElementAt(index);
        }

        private void UpdateTraceLine(LineRenderer renderer, TargetHandType handType, Color color)
        {
            float TraceAheadTime = 1f;
            renderer.enabled = false;
            if (EditorState.IsPaused) { return; }

            var startTime = EditorTime.Time + Relative_QNT.FromBeatTime(TraceAheadTime);

            Target nextTarget = null;
            foreach (Target t in new NoteEnumerator(startTime, startTime + Relative_QNT.FromBeatTime(1)))
            {
                if (t.data.behavior == TargetBehavior.Melee || t.data.behavior == TargetBehavior.ChainNode) continue;

                if (t.data.handType == handType)
                {
                    nextTarget = t;
                    break;
                }
            }

            if (nextTarget == null)
            {
                return;
            }

            var backIt = new NoteEnumerator(nextTarget.data.time - Relative_QNT.FromBeatTime(2), nextTarget.data.time);
            backIt.reverse = true;

            Target closest = null;
            Target startTarget = null;
            foreach (Target t in backIt)
            {
                if (t == nextTarget || t.data.behavior == TargetBehavior.Melee || t.data.behavior == TargetBehavior.ChainNode) continue;

                if (t.data.handType == handType)
                {
                    startTarget = t;
                    break;
                }

                if (closest == null)
                {
                    closest = t;
                }
            }

            if (startTarget == null)
            {
                if (closest == null)
                {
                    return;
                }

                startTarget = closest;
            }

            if (nextTarget != null)
            {
                float NoteFadeInTime = 1.0f;
                float dist = (nextTarget.data.time - startTarget.data.time).ToBeatTime();
                float travelTime = (nextTarget.data.time - startTime).ToBeatTime();
                if (dist <= 2f && travelTime < dist && travelTime > 0.0 && travelTime < NoteFadeInTime)
                {
                    float totalTime = Math.Min(NoteFadeInTime, dist);
                    float percent = 1.0f - ((travelTime) / totalTime);

                    renderer.enabled = true;

                    Vector2 start = startTarget.data.position + (nextTarget.data.position - startTarget.data.position) * Easings.Linear(percent);
                    Vector2 end = startTarget.data.position + (nextTarget.data.position - startTarget.data.position);

                    Vector3[] positions = new Vector3[2];
                    positions[0] = new Vector3(start.x, start.y, 0.0f);
                    positions[1] = new Vector3(end.x, end.y, 0.0f);
                    renderer.positionCount = positions.Length;
                    renderer.SetPositions(positions);

                    Gradient gradient = new Gradient();
                    gradient.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 0.25f), new GradientColorKey(color, 1.0f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(0.1f, 0.0f), new GradientAlphaKey(0.25f, 0.25f), new GradientAlphaKey(0.5f, 1.0f) }
                    );
                    renderer.colorGradient = gradient;
                }
            }
        }
        public void ToggleWaveform()
        {
            waveformVisualizer.ToggleWaveform();
            sustainVisualizer.ToggleWaveform();
        }
        string prevTimeText;
        private void SetCurrentTime()
        {
            float timeSeconds = EditorTime.Time.ToSeconds();

            string minutes = Mathf.Floor((int)timeSeconds / 60).ToString("00");
            string seconds = ((int)timeSeconds % 60).ToString("00");
            if (seconds != prevTimeText)
            {
                prevTimeText = seconds;
                songTimestamp.text = "<mspace=.5em>" + minutes + "</mspace>" + "<mspace=.4em>:</mspace>" + "<mspace=.5em>" + seconds + "</mspace>";
            }

        }

        private string prevTickText;

        private void SetCurrentTick()
        {
            string currentTick = EditorTime.Time.ToString();
            if (currentTick != prevTickText)
            {
                prevTickText = currentTick;
                curTick.text = "<mspace=.5em>" + currentTick + "</mspace>";
            }
        }
        #endregion

        #region Loaded Notes
        /*public void SortOrderedList()
        {
            orderedNotes.Sort((left, right) => left.data.time.CompareTo(right.data.time));
        }*/
        public void UpdateLoadedNotes()
        {
            UpdateDualines();
        }
        /*public static void AddLoadedNote(Target target)
        {
            loadedNotes.Add(target);
        }
        public static void RemoveLoadedNote(Target target)
        {
            loadedNotes.Remove(target);
        }*/
        #endregion

        #region Conversion

        //When loading from cues, use this.
        public TargetData GetTargetDataForCue(Cue cue)
        {
            TargetData data = new TargetData(cue);
            if (data.time.tick == 0) data.SetTimeFromAction(new QNT_Timestamp(120));
            return data;
        }
        #endregion

        #region Target Manipulation
        //Use when adding a singular target to the project (from the user)
        public void AddTarget(float x, float y)
        {
            if (!EditorFile.IsAudicaFileLoaded || EditorState.IsInUI || !EditorState.IsOverGrid) 
            {
                return;
            }

            TargetData data = new TargetData();
            data.x = x;
            data.y = y;
            data.handType = EditorState.Hand.Current;
            data.behavior = EditorState.Behavior.Current;

            QNT_Timestamp tempTime = EditorTime.SnappedTime;
            TempoChange currentTempo = EditorTempo.TempoChanges[0];
            int leftHandMeleeCount = 0;
            int rightHandMeleeCount = 0;
            int meleeCount = 0;
            //int eitherHandCount = 0;
            int targetCount = 0;
            foreach (Target target in EditorNotes.LoadedNotes)
            {
                if (target.data.time == tempTime)
                {
                    if (target.data.behavior != TargetBehavior.Melee && target.data.behavior != TargetBehavior.Mine)
                    {
                        targetCount++;
                    }
                    if (target.data.handType == TargetHandType.Either && target.data.behavior != TargetBehavior.Melee && target.data.behavior != TargetBehavior.Mine)
                    {
                        if (targetCount == 2) return;
                    }
                    if (target.data.handType == EditorState.Hand.Current && EditorState.Behavior.Current != TargetBehavior.Melee)
                    {
                        if (EditorState.Behavior.Current != TargetBehavior.Mine && target.data.handType != TargetHandType.Either) return;
                    }
                    else if (EditorState.Behavior.Current == TargetBehavior.Melee)
                    {
                        if (target.data.x == data.x && target.data.y == data.y) return;

                        if (target.data.behavior == TargetBehavior.Melee)
                        {
                            if (target.data.handType == TargetHandType.Left) leftHandMeleeCount++;
                            else if (target.data.handType == TargetHandType.Right) rightHandMeleeCount++;
                            else meleeCount++;
                        }
                        if (leftHandMeleeCount == 1 && data.handType == TargetHandType.Left && data.behavior == TargetBehavior.Melee)
                        {
                            return;
                        }
                        else if (rightHandMeleeCount == 1 && data.handType == TargetHandType.Right && data.behavior == TargetBehavior.Melee)
                        {
                            return;
                        }
                        else if (meleeCount + rightHandMeleeCount + leftHandMeleeCount == 2 && data.behavior == TargetBehavior.Melee)
                        {
                            return;
                        }
                    }
                }

            }
            if (data.handType == TargetHandType.Either && data.behavior != TargetBehavior.Melee && data.behavior != TargetBehavior.Mine)
            {
                if (targetCount == 2) return;
            }

            if (tempTime.tick < (currentTempo.timeSignature.Numerator * 2) * Constants.QuarterNoteDuration.tick) // deny if in intro redzone
            {
                NotificationCenter.SendNotification("Can't place target in intro zone. Targets before the 2 second mark don't properly work in-game.", NotificationType.Info);
                return;
            }

            data.SetTimeFromAction(EditorTime.SnappedTime);

            //Default sustains length should be more than 0.
            if (data.supportsBeatLength)
            {
                data.beatLength = Constants.QuarterNoteDuration;
            }
            else
            {
                data.beatLength = Constants.SixteenthNoteDuration;
            }

            switch (EditorState.Hitsound.Current)
            {
                case TargetHitsound.Standard:
                    data.velocity = InternalTargetVelocity.Kick;
                    break;
                case TargetHitsound.Snare:
                    data.velocity = InternalTargetVelocity.Snare;
                    break;
                case TargetHitsound.Percussion:
                    data.velocity = InternalTargetVelocity.Percussion;
                    break;
                case TargetHitsound.ChainStart:
                    data.velocity = InternalTargetVelocity.ChainStart;
                    break;
                case TargetHitsound.ChainNode:
                    data.velocity = InternalTargetVelocity.Chain;
                    break;
                case TargetHitsound.Melee:
                    data.velocity = InternalTargetVelocity.Melee;
                    break;
                case TargetHitsound.Mine:
                    data.velocity = InternalTargetVelocity.Mine;
                    break;
                case TargetHitsound.Silent:
                    data.velocity = InternalTargetVelocity.Silent;
                    break;
                default:
                    data.velocity = InternalTargetVelocity.Kick;
                    break;
            }

            var action = new NRActionAddNote { targetData = data };
            Tools.undoRedoManager.AddAction(action);

            songPlayback.PlayHitsound(EditorTime.Time);
            EditorScale.ReapplyScale();
            UpdateLoadedNotes();
        }

        //Adds a target directly to the timeline. targetData is kept as a reference NOT copied
        public Target AddTargetFromAction(TargetData targetData, bool transient = false)
        {

            //var timelineTargetIcon = Instantiate(timelineTargetIconPrefab, timelineTransformParent);
            var timelineTargetIcon = timelinePool.Spawn();
            timelineTargetIcon.location = TargetIconLocation.Timeline;
            var transform1 = timelineTargetIcon.transform;
            transform1.localPosition = new Vector3(targetData.time.ToBeatTime(), 0, 0);

            Vector3 noteScale = transform1.localScale;
            noteScale.x = EditorScale.NoteScale; // + (1f - NRSettings.config);
            transform1.localScale = noteScale;

            //var gridTargetIcon = Instantiate(gridTargetIconPrefab, gridTransformParent);
            var gridTargetIcon = gridPool.Spawn();
            gridTargetIcon.transform.localPosition = new Vector3(targetData.x, targetData.y, targetData.time.ToBeatTime());

            gridTargetIcon.transform.localScale = new Vector3(NRSettings.config.noteScale, NRSettings.config.noteScale, 1f);
            gridTargetIcon.location = TargetIconLocation.Grid;

            Target target = new Target(targetData, timelineTargetIcon, gridTargetIcon, transient, gridCamera);

            EditorNotes.AddNote(target);
            //notes.Add(target);
            //orderedNotes = notes.OrderBy(v => v.data.time.tick).ToList();

            //Subscribe to the delete note event so we can delete it if the user wants. And other events.
            target.DeleteNoteEvent += DeleteTarget;

            Debug.Log("Disabled AddLoadedNote and RemoveLoadedNote events. Re-enable if buggy");
            //target.TargetEnterLoadedNotesEvent += AddLoadedNote;
            //target.TargetExitLoadedNotesEvent += RemoveLoadedNote;

            target.TargetSelectEvent += EditorNotes.SelectTarget;
            target.TargetDeselectEvent += EditorNotes.DeselectTarget;

            target.MakeTimelineUpdateSustainLengthEvent += EditorNotes.UpdateSustainLength;

            //Trigger all callbacks on the note
            targetData.Copy(targetData);
            //Also generate chains if needed
            //this might be the culprit of chain nodes staying on grid + hitsounds not working.
            if (targetData.behavior == TargetBehavior.Legacy_Pathbuilder)
            {
                ChainBuilder.GenerateChainNotes(targetData);
            }
            UpdateLoadedNotes();
            return target;
        }

        /*public void SelectTarget(Target target)
        {
            if (!selectedNotes.Contains(target))
            {
                selectedNotes.Add(target);
                target.Select();
                OnSelectedNoteCountChanged.Invoke(selectedNotes.Count);
            }
        }*/

        /*public void SelectAllTargets()
        {
            //Camera.main.farClipPlane = 1000;
            foreach (Target target in orderedNotes)
            {
                target.MakeTimelineSelectTarget();
            }
            OnSelectedNoteCountChanged.Invoke(selectedNotes.Count);
        }*/
        /*
        public void DeselectTarget(Target target, bool resettingAll = false)
        {
            if (selectedNotes.Contains(target))
            {

                target.Deselect();

                if (!resettingAll)
                {
                    selectedNotes.Remove(target);
                }
                OnSelectedNoteCountChanged.Invoke(selectedNotes.Count);
            }

        }
        */
        /*public void DeselectAllTargets()
        {
            if (!EditorData.IsAudicaFileLoaded) return;

            mainCam.farClipPlane = 50;

            foreach (Target target in selectedNotes)
            {
                DeselectTarget(target, true);
            }

            selectedNotes = new List<Target>();
            OnSelectedNoteCountChanged.Invoke(0);
        }*/
        public void MoveGridTargets(List<TargetGridMoveIntent> intents)
        {
            var action = new NRActionGridMoveNotes();
            action.targetGridMoveIntents = intents.Select(intent => new TargetGridMoveIntent(intent)).ToList();
            Tools.undoRedoManager.AddAction(action);
        }

        public NRActionTimelineMoveNotes GenerateMoveTimelineAction(List<TargetTimelineMoveIntent> intents)
        {
            //SortOrderedList();
            var action = new NRActionTimelineMoveNotes();

            action.targetTimelineMoveIntents = intents.Select(oldIntent =>
            {
                var intent = new TargetTimelineMoveIntent(oldIntent);
                QNT_Timestamp startTime = intent.startTick;
                QNT_Timestamp endTime = intent.intendedTick;
                Relative_QNT delta = endTime - startTime;


                //Did we start in a repeater section?
                RepeaterSection startSection = null;
                repeaterSections.ForEach(section =>
                {
                    if (section.Contains(startTime))
                    {
                        startSection = section;
                        return;
                    }
                });

                //We did start in a section!
                if (startSection != null)
                {
                    //No matter what, we need to find our siblings in the other sections
                    List<TargetData> siblings = new List<TargetData>();
                    List<RepeaterSection> siblingSections = new List<RepeaterSection>();
                    List<RepeaterSection> otherSiblingSections = new List<RepeaterSection>();

                    repeaterSections.ForEach(section =>
                    {
                        //Same section id, but not the one we've already found
                        if (section.ID == startSection.ID && section != startSection)
                        {
                            Relative_QNT offset = (section.startTime - startSection.startTime);

                            if (section.Contains(startTime + offset))
                            {
                                var data = TargetFinder.FindTargetData(startTime + offset, intent.targetData.behavior, intent.targetData.handType);
                                if (data == null)
                                {
                                    Debug.LogError("Expected to find a note for a sibling repeater section but none was found! This means that repeaters failed to replicate at some point!");
                                }
                                else
                                {
                                    siblings.Add(data);
                                    siblingSections.Add(section);
                                }
                            }
                            else
                            {
                                otherSiblingSections.Add(section);
                            }
                        }
                    });

                    //If we've moved outside our section, then all of our siblings will be destroyed
                    if (!startSection.Contains(endTime))
                    {
                        intent.startSiblingsToBeDestroyed = siblings;
                    }
                    else
                    {
                        //We need to check our siblings to see if they will survive the move
                        List<TargetData> moved = new List<TargetData>();
                        List<TargetData> destroyed = new List<TargetData>();
                        for (int i = 0; i < siblings.Count; ++i)
                        {
                            var sibling = siblings[i];
                            var siblingSection = siblingSections[i];

                            if (siblingSection.Contains(sibling.time + delta))
                            {
                                moved.Add(sibling);
                            }
                            else
                            {
                                destroyed.Add(sibling);
                            }
                        }
                        intent.startSiblingsToBeMoved = moved;
                        intent.startSiblingsToBeDestroyed = destroyed;

                        //We also need to check if any new siblings need to be created from the move (since we can move from a section ouside the bounds of a sibling section to inside their bounds)
                        List<TargetData> created = new List<TargetData>();
                        otherSiblingSections.ForEach(section =>
                        {
                            Relative_QNT offset = (section.startTime - startSection.startTime);
                            if (section.Contains(endTime + offset))
                            {
                                created.Add(new TargetData(intent.targetData, endTime + offset));
                            }
                        });
                        intent.endRepeaterSiblingsToBeCreated = created;
                    }
                }

                //Did we end in a repeater section that wasn't our start section?
                RepeaterSection endSection = null;
                repeaterSections.ForEach(section =>
                {
                    if (section.Contains(endTime) && startSection != section)
                    {
                        endSection = section;
                        return;
                    }
                });

                if (endSection != null)
                {
                    //Gather all the other sections with the same ID
                    List<RepeaterSection> otherSections = new List<RepeaterSection>();
                    repeaterSections.ForEach(section =>
                    {
                        if (section.ID == endSection.ID && section != endSection)
                        {
                            otherSections.Add(section);
                        }
                    });

                    //Create a target in each other section that will contain it
                    List<TargetData> endTargetsToCreate = new List<TargetData>();
                    otherSections.ForEach(section =>
                    {
                        Relative_QNT offset = (section.startTime - endSection.startTime);
                        if (section.Contains(endTime + offset))
                        {
                            endTargetsToCreate.Add(new TargetData(intent.targetData, endTime + offset));
                        }
                    });
                    intent.endRepeaterSiblingsToBeCreated = endTargetsToCreate;
                }
                return intent;

            }).ToList();

            return action;
        }

        public void MoveTimelineTargets(List<TargetTimelineMoveIntent> intents)
        {
            Tools.undoRedoManager.AddAction(GenerateMoveTimelineAction(intents));
        }

        public void PasteCues(List<TargetData> cues, QNT_Timestamp pasteBeatTime)
        {
            // paste new targets in the original locations
            var targetDataList = cues.Select(copyData =>
            {
                var data = new TargetData();
                data.Copy(copyData);
                if (data.behavior == TargetBehavior.Legacy_Pathbuilder)
                {
                    data.legacyPathbuilderData = new LegacyPathbuilderData();
                    data.legacyPathbuilderData.Copy(copyData.legacyPathbuilderData);
                }
                else if (data.isPathbuilderTarget)
                {
                    data.pathbuilderData = new PathbuilderData();
                    data.pathbuilderData.Copy(copyData.pathbuilderData);
                }

                return data;
            }).ToList();

            // find the soonest target in the selection
            QNT_Timestamp earliestTargetBeatTime = new QNT_Timestamp(long.MaxValue);
            foreach (TargetData data in targetDataList)
            {
                QNT_Timestamp time = data.time;
                if (time < earliestTargetBeatTime)
                {
                    earliestTargetBeatTime = time;
                }
            }

            // shift all by the amount needed to move the earliest note to now
            Relative_QNT diff = pasteBeatTime - earliestTargetBeatTime;
            foreach (TargetData data in targetDataList)
            {
                data.SetTimeFromAction(data.time + diff);
            }

            var action = new NRActionMultiAddNote();
            action.affectedTargets = targetDataList;
            Tools.undoRedoManager.AddAction(action);

            //DeselectAllTargets();
            EditorNotes.DeselectAllTargets();
            //TargetFinder.FindNotes(targetDataList).ForEach(target => SelectTarget(target));
            EditorNotes.SelectTargets(TargetFinder.FindNotes(targetDataList));
        }

        // Invert the selected targets' colour
        public void SwapTargets(List<Target> targets)
        {
            var action = new NRActionSwapNoteColors();
            action.affectedTargets = targets.Select(target => target.data).ToList();
            Tools.undoRedoManager.AddAction(action);
        }

        // Flip the selected targets on the grid about the X
        public void FlipSelectedTargetsHorizontal()
        {
            var action = new NRActionHFlipNotes();
            action.affectedTargets = EditorNotes.SelectedNotes.Select(target => target.data).ToList();//selectedNotes.Select(target => target.data).ToList();
            Tools.undoRedoManager.AddAction(action);
        }

        public void ScaleSelectedTargets(Vector2 scale)
        {
            var action = new NRActionScale();
            action.affectedTargets = EditorNotes.SelectedNotes.Select(target => target.data).ToList();//selectedNotes.Select(target => target.data).ToList();
            action.scale = scale;
            Tools.undoRedoManager.AddAction(action);
        }

        public void Rotate(List<Target> targets, float angle, Vector2? center = null)
        {
            var action = new NRActionRotate();
            if (center == null) action.rotateCenter = Vector2.zero;
            else action.rotateCenter = (Vector2)center;
            action.rotateAngle = angle;
            action.affectedTargets = targets.Select(target => target.data).ToList();
            Tools.undoRedoManager.AddAction(action);
        }

        public void Reverse(List<Target> targets)
        {
            var action = new NRActionReverse();
            action.affectedTargets = targets.Select(target => target.data).ToList();
            Tools.undoRedoManager.AddAction(action);
        }

        // Flip the selected targets on the grid about the Y
        public void FlipSelectedTargetsVertical()
        {
            var action = new NRActionVFlipNotes();
            action.affectedTargets = EditorNotes.SelectedNotes.Select(target => target.data).ToList();//selectedNotes.Select(target => target.data).ToList();
            Tools.undoRedoManager.AddAction(action);
        }

        public void SetTargetHitsounds(List<TargetSetHitsoundIntent> intents)
        {
            var action = new NRActionSetTargetHitsound();
            action.targetSetHitsoundIntents = intents.Select(intent => new TargetSetHitsoundIntent(intent)).ToList();
            Tools.undoRedoManager.AddAction(action);
        }

        public void SetTargetBehaviors(NRActionSetTargetBehavior action)
        {
            Tools.undoRedoManager.AddAction(action);
        }

        public void DeleteTarget(Target target)
        {
            var action = new NRActionRemoveNote();
            action.targetData = target.data;
            Tools.undoRedoManager.AddAction(action);
        }

        public void DeselectBehavior(TargetBehavior behavior)
        {
            var action = new NRActionDeselectBehavior();
            action.behaviorToDeselect = behavior;
            Tools.undoRedoManager.AddAction(action);
        }

        public void DeselectHand(TargetHandType handType)
        {
            var action = new NRActionDeselectHand();
            action.handToDeselect = handType;
            Tools.undoRedoManager.AddAction(action);
        }

        public void DeleteTargetFromAction(TargetData targetData)
        {
            Target target = TargetFinder.FindNote(targetData);
            if (target == null) return;

            EditorNotes.RemoveNote(target);
            /*notes.Remove(target);
            orderedNotes.Remove(target);
            loadedNotes.Remove(target);
            selectedNotes.Remove(target);*/

            target.Destroy(this);
            timelinePool.Return(target.timelineTargetIcon);
            gridPool.Return(target.gridTargetIcon);
            target = null;
            UpdateLoadedNotes();
        }

        public void DeleteTargets(List<Target> targets)
        {
            var action = new NRActionMultiRemoveNote();
            action.affectedTargets = targets.Select(target => target.data).ToList();
            Tools.undoRedoManager.AddAction(action);
            UpdateLoadedNotes();
        }

        public void DeleteAllTargets()
        {
            var notesTemp = EditorNotes.Notes.ToList();
            foreach (Target target in notesTemp)
            {
                target.Destroy(this);
            }
            foreach(Target target in notesTemp)
            {
                target.Reset();
                timelinePool.Return(target.timelineTargetIcon);
                gridPool.Return(target.gridTargetIcon);
            }
            EditorNotes.ClearAllNotes();
        }
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
            //return; return again if performance is too shit
            foreach (var note in EditorNotes.LoadedNotes)
            {
                if (note.data.behavior == TargetBehavior.Sustain)
                {
                    if ((note.GetRelativeBeatTime() < 0) && (note.GetRelativeBeatTime() + note.data.beatLength.ToBeatTime() > 0) && !EditorState.IsPaused)
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

        #region Utility
        public void ResetTimeline()
        {
            DeleteAllTargets();
            Tools.undoRedoManager.ClearActions();
            EditorTempo.ClearTempi();
            TimelineTextManager.Instance.ClearTimelineTexts();
            ModifierHandler.Instance.CleanUp();
            miniTimeline.ClearBookmarks();
            sustainVisualizer.ClearWaveform();
        }
        public void CopyTimestampToClipboard()
        {
            string timestamp = songTimestamp.text;
            GUIUtility.systemCopyBuffer = "**" + EditorTime.Time.ToString() + "**" + " - ";
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

        public void LoadTimingMode(AudioClip clip)
        {
            if (EditorFile.IsAudicaFileLoaded) return;

            songPlayback.LoadAudioClip(clip, PrecisePlayback.LoadType.MainSong);
            inTimingMode = true;
            EditorFile.SetIsAudioLoaded(true);
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
            EditorFile.SetIsAudicaLoaded(true);
            difficultyManager.LoadHighestDifficulty();

            //Disable timing window buttons so users don't mess stuff up.
            generateAudicaButton.interactable = false;
            loadAudioFileTiming.interactable = false;

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
            onAudicaLoaded?.Invoke(EditorFile.AudicaFile);
            NotificationCenter.SendNotification("Press F1 to view shortcuts", NotificationType.Info);
            StopCoroutine(NRSettings.Autosave());
            StartCoroutine(NRSettings.Autosave());
            UpdateState();
           
            onLoaded?.Invoke(true);
            yield return null;
        }


        #endregion

        private void SetAudioDSP()
        {
            //Pull DSP setting from config
            var configuration = AudioSettings.GetConfiguration();
            configuration.dspBufferSize = NRSettings.config.audioDSP;
            AudioSettings.Reset(configuration);
        }

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
            SetBeatTime(EditorTime.Time);
        }

        #region Playback
        private struct ScrubParams
        {
            public bool forward;
            public bool byTick;

            public ScrubParams(bool forward, bool byTick)
            {
                this.forward = forward;
                this.byTick = byTick;
            }
        }
        public void ScrubTimeline(bool forward, bool byTick)
        {
            scrubParams = new ScrubParams(forward, byTick);
            scrub = true;
            if (EditorState.IsPaused)
            {
                StopCoroutine(MoveTimeline());
                StartCoroutine(MoveTimeline());
            }

            onTimelineScrub?.Invoke();
        }
        public delegate void OnTimelineScrub();
        public static event OnTimelineScrub onTimelineScrub;
        private IEnumerator MoveTimeline()
        {
            while(!EditorState.IsPaused || scrub)
            {
                var startTime = EditorTime.Time;

                if (!EditorState.IsPaused)
                {
                    EditorTime.SetTime(QNT_Timestamp.ShiftTick(songPlayback.GetTime()));
                }

                if (scrub)
                {
                    Relative_QNT jumpDuration = new Relative_QNT(scrubParams.byTick ? 1 : (long)EditorBeatSnap.Duration.tick);
                    jumpDuration.tick *= scrubParams.forward ? 1 : -1;

                    EditorTime.SetTime(scrubParams.byTick ? EditorTime.Time + jumpDuration : EditorTime.GetSnappedTime(EditorTime.Time + jumpDuration, EditorBeatSnap.BeatSnap));
                    if ((float)EditorTime.Time.tick - bpmDragOffset.tick < 0)
                    {
                        EditorTime.SetTime(bpmDragOffset);
                    }
                    SafeSetTime();
                    if (EditorState.IsPaused)
                    {
                        songPlayback.PlayPreview(EditorTime.Time, jumpDuration);
                    }
                    else
                    {
                        songPlayback.Play(EditorTime.Time);
                    }

                    StopCoroutine(AnimateSetTime(new QNT_Timestamp(0)));
                    if (!EditorState.IsPaused && ModifierPreviewer.Instance.isPlaying) ModifierPreviewer.Instance.UpdateModifierList(EditorTime.Time.tick);
                    if (ModifierHandler.activated && ModifierHandler.Instance.isEditingManipulation) ModifierHandler.Instance.UpdateManipulationValues();
                }

                if (startTime != EditorTime.Time)
                {
                    QNT_Timestamp start = startTime;
                    QNT_Timestamp end = EditorTime.Time;

                    if (start > end)
                    {
                        QNT_Timestamp temp = start;
                        start = end;
                        end = temp;
                    }

                    foreach (Target t in new NoteEnumerator(start, end))
                    {
                        t.OnNoteHit();
                    }
                }

                var songEndTime = QNT_Timestamp.ShiftTick(0, songPlayback.song.Length);
                if (EditorTime.Time >= songEndTime)
                {
                    if (!EditorState.IsPaused)
                    {
                        EditorState.SetPaused(true);
                    }
                    EditorTime.SetTime(songEndTime);
                }
                SafeSetTime();
                SetBeatTime(EditorTime.Time);
                SetCurrentTime();
                SetCurrentTick();
                scrub = false;
                yield return null;
            }
            
        }
        private void OnPlaybackSpeedChanged(float speed)
        {
            string PlaybackText = ("Speed: " + speed.ToString("#%"));
            playbackSpeedText.text = PlaybackText;
        }

        public double GetPercentPlayedFromSeconds(double seconds)
            => songPlayback == null || songPlayback.song == null ? 0f :
            seconds / songPlayback.song.Length;

        public void JumpToPercent(float percent, bool forceJump = false)
        {
            if (!EditorFile.IsAudioLoaded) return;
            if ((EditorState.Mode.Current != EditorMode.Compose || EditorState.IsInUI) && !forceJump) return;
            EditorTime.SetTime(QNT_Timestamp.ShiftTick(songPlayback.song.Length * percent));

            SafeSetTime();
            SetCurrentTime();
            SetCurrentTick();

            SetBeatTime(EditorTime.Time);
            songPlayback.PlayPreview(EditorTime.Time, new((long)EditorBeatSnap.Duration.tick));
            UpdateState();
        }
        public void JumpToX(float x)
        {
            if (ModifierHandler.activated || EditorState.Mode.Current != EditorMode.Compose || EditorState.IsInUI) return;
            StopCoroutine(AnimateSetTime(new QNT_Timestamp(0)));
            bool isPlaying = !EditorState.IsPaused;
            if (isPlaying) TogglePlayback();
            //float posX = Math.Abs(timelineCamera.position.x) + x;
            float posX = x;
            QNT_Timestamp newTime = new QNT_Timestamp(0) + QNT_Duration.FromBeatTime(posX * EditorScale.ScaleAmount);
            newTime = EditorTime.GetSnappedTime(newTime, EditorBeatSnap.BeatSnap);
            SafeSetTime();
            OnAnimateSetTimeDone callback = isPlaying ? new OnAnimateSetTimeDone(TogglePlayback) : null;
            StartCoroutine(AnimateSetTime(newTime, callback));
        }
        public void TogglePlayback() => TogglePlayback(false);
        public void TogglePlayback(bool metronome = false)
        {
            if (!EditorFile.IsAudioLoaded) return;
            if (EditorState.IsPaused)
            {
                if (metronome)
                {
                    songPlayback.StartMetronome();
                }

                songPlayback.Play(EditorTime.Time);
                EditorState.SetPaused(false);
                StartCoroutine(MoveTimeline());
            }
            else
            {
                ModifierPreviewer.Instance.Stop();
                songPlayback.Stop();
                EditorState.SetPaused(true);
                StopCoroutine(MoveTimeline());
                //Snap to the beat snap when we pause
                EditorTime.SetTime(EditorTime.SnappedTime);
                float currentTimeSeconds = EditorTime.Seconds;
                if (currentTimeSeconds > songPlayback.song.Length)
                {
                    EditorTime.SetTime(QNT_Timestamp.ShiftTick(songPlayback.song.Length));
                }

                SetBeatTime(EditorTime.Time);
                SafeSetTime();
                SetCurrentTick();
                SetCurrentTime();
                UpdateState();
            }
            /*if (paused)
            {
                onPaused?.Invoke();
            }
            else
            {
                onPlay?.Invoke();
            }*/
        }

        /*public delegate void OnPaused();
        public static event OnPaused onPaused;

        public delegate void OnPlay();
        public static event OnPlay onPlay;*/
        public float GetPercentagePlayed() => songPlayback == null || songPlayback.song == null ? 0f : (EditorTime.Seconds / songPlayback.song.Length);
        public float GetPercentagePlayed(QNT_Timestamp tick) => songPlayback == null || songPlayback.song == null ? 0f : (tick.ToSeconds() / songPlayback.song.Length);

        #endregion

        #region State Management
        public void UpdateState()
        {
            UpdateSustains();
            UpdateDualines();
            miniTimeline.SetPercentagePlayed(GetPercentagePlayed());
            EditorNotes.EnableNearSustainButtons();
            if (NRSettings.config.enableTraceLines)
            {
                UpdateTraceLine(leftHandTraceLine, TargetHandType.Left, NRSettings.config.leftColor);
                UpdateTraceLine(rightHandTraceLine, TargetHandType.Right, NRSettings.config.rightColor);
            }
            //songPlayback.volume = NRSettings.config.mainVol;
            //songPlayback.hitSoundVolume = NRSettings.config.noteVol;
        }
        public void SafeSetTime()
        {
            if (EditorTime.Time.tick < 0) EditorTime.SetTime(0);
            if (!EditorFile.IsAudioLoaded) return;

            float currentTimeSeconds = EditorTime.Seconds;

            if (currentTimeSeconds > songPlayback.song.Length)
            {
                EditorTime.SetTime(QNT_Timestamp.ShiftTick(songPlayback.song.Length));
            }
        }
        public delegate void OnAnimateSetTimeDone();
        public IEnumerator AnimateSetTime(QNT_Timestamp newTime, OnAnimateSetTimeDone callback = null)
        {
            if (!EditorFile.IsAudioLoaded) yield break;

            if (newTime.ToSeconds() > songPlayback.song.Length)
            {
                newTime = QNT_Timestamp.ShiftTick(songPlayback.song.Length);
            }

            //DOTween.Play
            DOTween.To(t => SetBeatTime(new QNT_Timestamp((UInt64)Math.Round(t))), EditorTime.Time.tick, newTime.tick, 0.2f).SetEase(Ease.InOutCubic);

            yield return new WaitForSeconds(0.2f);

            EditorTime.SetTime(newTime);

            SafeSetTime();
            SetBeatTime(EditorTime.Time);

            SetCurrentTime();
            SetCurrentTick();
            songPlayback.PlayPreview(EditorTime.Time, new((long)EditorBeatSnap.Duration.tick));
            callback?.Invoke();
            yield break;

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
            Timeline.Instance.DeleteAllTargets();
            readyToRegenerate = false;
            EditorTempo.SetBPM(new QNT_Timestamp(0), microsecondsPerQuarterNote, false);
            Timeline.Instance.SafeSetTime();
        }

        public void ExitTimingMode()
        {
            Timeline.inTimingMode = false;
            Timeline.Instance.DeleteAllTargets();

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

            for (float t = 0; t < endOfAudio.tick;)
            {
                float increment = Constants.PulsesPerWholeNote / currentTempo.timeSignature.Denominator;

                float start = t / (float)Constants.PulsesPerQuarterNote;

                if (barLengthIncr == 0)
                {
                    measurecount++;

                    if (measurecount == 3)
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
        public void ShiftEverythingByTime(Relative_QNT shift_amount)
        {
            //Shift tempo markers
            var tempoChanges = EditorTempo.TempoChanges;
            for (int i = 0; i < tempoChanges.Count; ++i)
            {
                TempoChange newChange = tempoChanges[i];
                if (newChange.time.tick != 0)
                {
                    newChange.time += shift_amount;
                }

                tempoChanges[i] = newChange;
            }

            //Shift notes
            foreach (Target note in EditorNotes.OrderedNotes)
            {
                note.data.SetTimeFromAction(note.data.time + shift_amount);
            }
        }
        public void SetOffset(Relative_QNT newOffset)
        {
            StopCoroutine(AnimateSetTime(new QNT_Timestamp(0)));
            Relative_QNT diff = offset - newOffset;
            offset = newOffset;

            QNT_Timestamp newTime = EditorTime.Time + diff;
            if (newTime != EditorTime.Time)
            {
                StartCoroutine(AnimateSetTime(newTime));
            }
        }

        public void SetBeatTime(QNT_Timestamp t)
        {
            if (t.tick - bpmDragOffset.tick < 0) t = new QNT_Timestamp(0);
            else t = new QNT_Timestamp(t.tick - bpmDragOffset.tick);
            float x = t.ToBeatTime() - offset.ToBeatTime();
            Vector3 pos = timelineCamera.transform.localPosition;
            pos.x = 1f * x / EditorScale.ScaleAmount;
            timelineCamera.transform.localPosition = pos;
            pos = gridCamera.position;
            pos.z = x - 5f;
            gridCamera.position = pos;
            UpdateState();
        }

        internal QNT_Timestamp bpmDragOffset;
        public void SetBPMDragOffset(QNT_Timestamp offset)
        {
            bpmDragOffset = offset;
        }
        public bool hasBpmDragOffset => bpmDragOffset.tick != 0;
        #endregion

        #region Update
        public void Update()
        {
            //if (paused && !scrub) return;
            //MoveTimeline();
            //if (scrub) scrub = false;
        }
        #endregion

        #region Count In
        public void PreviewCountIn(uint beats)
        {
            if (!EditorState.IsPaused)
            {
                TogglePlayback();
            }

            EditorTime.SetTime(0);
            SafeSetTime();

            TempoChange first = EditorTempo.TempoChanges[0];
            QNT_Duration timeSignatureDuration = new QNT_Duration(Constants.PulsesPerWholeNote / first.timeSignature.Denominator) * beats;
            songPlayback.PlayClickTrack(new QNT_Timestamp(0) + timeSignatureDuration);
            if (EditorState.IsPaused)
            {
                TogglePlayback();
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