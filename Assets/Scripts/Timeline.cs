using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using NotReaper.UserInput;
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
using NotReaper.ReviewSystem;
using NotReaper.Notifications;
using UnityEngine.Events;
using NotReaper.Tools.PathBuilder;
using UnityEngine.Profiling;
using NotReaper.Repeaters;
using UnityEngine.EventSystems;
using NotReaper.MapPreview;

namespace NotReaper
{

    public class NoteEnumerator : IEnumerable<Target>
    {
        public NoteEnumerator(QNT_Timestamp start, QNT_Timestamp end)
        {
            this.start = start;
            this.end = end;
        }

        public QNT_Timestamp start;
        public QNT_Timestamp end;

        public bool startInclusive = true;
        public bool endInclusive = true;
        public bool reverse = false;

        public IEnumerator<Target> GetEnumerator()
        {
            if (Timeline.orderedNotes.Count == 0)
            {
                yield break;
            }

            var result = Timeline.BinarySearchOrderedNotes(start);
            int index = result.index;

            //Invalid index? No iteration
            if (index >= Timeline.orderedNotes.Count)
            {
                yield break;
            }

            //We didn't find an exact result, so search for the nearest
            if (!result.found)
            {

                //Go back until we find a note with a time less then the start
                while (index >= 0)
                {
                    QNT_Timestamp time = Timeline.orderedNotes[index].data.time;
                    if (time < start)
                    {
                        break;
                    }

                    --index;
                }

                if (index < 0)
                {
                    index = 0;
                }

                //Increment up to and including start
                while (Timeline.orderedNotes[index].data.time < start)
                {
                    ++index;
                }
            }

            //Invalid index? No iteration
            if (index >= Timeline.orderedNotes.Count)
            {
                yield break;
            }

            //If we are not inclusive to starting time, then move up until we get a time after the start
            if (!startInclusive)
            {
                while (Timeline.orderedNotes[index].data.time <= start)
                {
                    ++index;
                }
            }

            //Iterate over the valid notes
            if (!reverse)
            {
                for (int i = index; i < Timeline.orderedNotes.Count; ++i)
                {
                    if (Timeline.orderedNotes[i].data.time > end)
                    {
                        yield break;
                    }

                    if (!endInclusive && Timeline.orderedNotes[i].data.time == end)
                    {
                        yield break;
                    }

                    yield return Timeline.orderedNotes[i];
                }
            }
            else
            {
                int endIndex = index;

                while (endIndex < Timeline.orderedNotes.Count && Timeline.orderedNotes[endIndex].data.time < end)
                {
                    ++endIndex;
                }

                if (endInclusive)
                {
                    while (endIndex < Timeline.orderedNotes.Count && Timeline.orderedNotes[endIndex].data.time <= end)
                    {
                        ++endIndex;
                    }

                    --endIndex;
                }

                if (endIndex >= Timeline.orderedNotes.Count)
                {
                    endIndex = Timeline.orderedNotes.Count - 1;
                }

                for (int i = endIndex; i >= index; --i)
                {
                    yield return Timeline.orderedNotes[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

    }

    [Serializable]
    public class RepeaterSection
    {
        public RepeaterSection(uint id, QNT_Timestamp start, QNT_Timestamp end)
        {
            ID = id;
            startTime = start;
            endTime = end;
        }

        public bool Contains(QNT_Timestamp time)
        {
            return time >= startTime && time <= endTime;
        }

        public static bool operator ==(RepeaterSection lhs, RepeaterSection rhs)
        {
            if (ReferenceEquals(lhs, null) && !ReferenceEquals(rhs, null)) { return false; }
            if (!ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null)) { return false; }
            if (ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null)) { return true; }

            return lhs.startTime == rhs.startTime;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator !=(RepeaterSection lhs, RepeaterSection rhs)
        {
            if (ReferenceEquals(lhs, null) && !ReferenceEquals(rhs, null)) { return true; }
            if (!ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null)) { return true; }
            if (ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null)) { return false; }

            return lhs.startTime != rhs.startTime;
        }

        [SerializeField]
        public uint ID;

        [SerializeField]
        public QNT_Timestamp startTime; //Time when this section starts (inclusive)

        [SerializeField]
        public QNT_Timestamp endTime; //Time when this section ends (inclusive)

        [NonSerialized]
        public GameObject timelineSectionObj;

        [NonSerialized]
        public GameObject miniTimelineSectionObj;
    }

    public class Timeline : MonoBehaviour
    {

        public static Timeline instance;
        //Hidden public values
        [HideInInspector] public static AudicaFile audicaFile;

        [HideInInspector] public static SongDesc desc;

        [Header("Audio Stuff")]
        [SerializeField] private Transform spectrogram;

        [Header("UI Elements")]
        [SerializeField] private MiniTimeline miniTimeline;
        [SerializeField] private TextMeshProUGUI songTimestamp;
        [SerializeField] private TextMeshProUGUI curTick;
        [SerializeField] private TextMeshProUGUI curDiffText;
        public Camera menuCamera;

        [SerializeField] private HorizontalSelector beatSnapSelector;

        [Header("Prefabs")]
        public TargetIcon timelineTargetIconPrefab;
        public TargetIcon gridTargetIconPrefab;
        public GameObject BPM_MarkerPrefab;
        public GameObject repeaterSectionPrefab;

        [Header("Extras")]
        [SerializeField] private NRDiscordPresence nrDiscordPresence;
        [SerializeField] private DifficultyManager difficultyManager;
        [SerializeField] public EditorToolkit Tools;
        [SerializeField] public Transform timelineTransformParent;
        [SerializeField] private Transform gridTransformParent;
        public static Transform gridNotesStatic;
        public static Transform timelineNotesStatic;
        [SerializeField] private Renderer timelineBG;
        [SerializeField] private TextMeshProUGUI beatSnapWarningText;
        [SerializeField] private TextMeshProUGUI playbackSpeedText;
        public Transform introZone;

        public Slider musicVolumeSlider;
        public Slider hitSoundVolumeSlider;

        [Header("Configuration")]
        public float playbackSpeed = 1f;
        public string playbackSpeedPercentage = "Speed: 100%";

        public float musicVolume = 0.5f;
        public float hitsoundVolume = .5f;
        public float sustainVolume = 1.0f;
        public float previewDuration = 0.1f;

        //Target Lists
        public List<Target> notes;
        public static List<Target> orderedNotes;
        public List<Target> selectedNotes;
        public UnityEvent<int> OnSelectedNoteCountChanged;

        public List<RepeaterSection> repeaterSections = new List<RepeaterSection>();

        public static List<Target> loadedNotes = new List<Target>();

        public static bool inTimingMode = false;
        public static bool audioLoaded = false;
        public static bool audicaLoaded = false;
        public static bool isSaving = false;

        private Color leftColor;
        private Color rightColor;
        private Color bothColor;
        private Color neitherColor;

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        /// <summary>
        /// The current time in the song
        /// </summary>
        /// <value></value>
        public static QNT_Timestamp time { get; set; }

        public int beatSnap { get; private set; } = 4;

        [HideInInspector] public static int scale = 20;
        public static float scaleTransform;
        private float targetScale = 0.7f;
        private float scaleOffset = 0;
        public static Relative_QNT offset = new Relative_QNT(0);

        /// <summary>
        /// If the timeline is currently being moved by an animation.
        /// </summary>
        private bool animatingTimeline = false;

        [HideInInspector] public bool hover { get; set; } = false;
        public bool paused = true;
        private bool scrub = false;
        private ScrubParams scrubParams;
        //private bool animationsNeedStopping;
        public Button generateAudicaButton;
        public Button loadAudioFileTiming;

        public List<TempoChange> tempoChanges = new List<TempoChange>();
        private List<GameObject> bpmMarkerObjects = new List<GameObject>();

        [SerializeField] public PrecisePlayback songPlayback;

        public AudioWaveformVisualizer waveformVisualizer;
        [SerializeField]
        private AudioWaveformVisualizer sustainVisualizer;
        public bool areNotesSelected => selectedNotes.Count > 0;

        [SerializeField] public LineRenderer leftHandTraceLine;
        [SerializeField] public LineRenderer rightHandTraceLine;
        [SerializeField] public GameObject dualNoteTraceLinePrefab;
        [Space, SerializeField] private Transform timelineTargetCollector;
        public Transform timelineCamera;
        public Transform gridCamera;
        List<LineRenderer> dualNoteTraceLines = new List<LineRenderer>();

        [NRInject] internal Pathbuilder pathbuilder;
        [NRInject] internal RepeaterManager repeaterManager;
        [NRInject] private PreviewManager previewManager;

        public delegate void OnAudicaLoaded(AudicaFile file);
        public static event OnAudicaLoaded onAudicaLoaded;

        private GridTargetPool gridPool;
        private TimelineTargetPool timelinePool;
        private Camera mainCam;

        private void Awake()
        {
            gridPool = GetComponent<GridTargetPool>();
            timelinePool = GetComponent<TimelineTargetPool>();
        }

        //Tools
        private void Start()
        {

            instance = this;
            mainCam = CameraProvider.main;
            //Load the config file
            NRSettings.LoadSettingsJson();
            RecentAudicaFiles.LoadRecents();

            //Initialize autoupdating:
            HandleAutoupdater();

            notes = new List<Target>();
            orderedNotes = new List<Target>();
            loadedNotes = new List<Target>();
            selectedNotes = new List<Target>();

            gridNotesStatic = gridTransformParent;
            timelineNotesStatic = timelineTransformParent;

            Physics.autoSyncTransforms = false;

            //ChainBuilder.timeline = this;

            NRSettings.OnLoad(() =>
            {
                //sustainVolume = NRSettings.config.sustainVol;
                musicVolume = NRSettings.config.mainVol;
                hitsoundVolume = NRSettings.config.noteVol;
                sustainVolume = NRSettings.config.sustainVol;
                songPlayback.volume = NRSettings.config.mainVol;
                songPlayback.hitSoundVolume = NRSettings.config.noteVol;
                SetAudioDSP();

                //if (NRSettings.config.clearCacheOnStartup)
                //{
                    HandleCache.ClearCache();
                //}
            });

            beatSnapWarningText.DOFade(0f, 0f);
        }

        private void HandleAutoupdater()
        {

        }

        public void UpdateUIColors()
        {
            curDiffText.color = NRSettings.config.rightColor;
            leftColor = NRSettings.config.leftColor;
            rightColor = NRSettings.config.rightColor;
            bothColor = UserPrefsManager.bothColor;
            neitherColor = UserPrefsManager.neitherColor;
        }

        public void UpdateTargetColors()
        {
            foreach (var target in orderedNotes)
            {
                target.gridTargetIcon.UpdateColors();
                target.timelineTargetIcon.UpdateColors();
            }
        }

        void OnApplicationQuit()
        {
            //DirectoryInfo dir = new DirectoryInfo(Application.persistentDataPath + "\\temp\\");
            //dir.Delete(true);
        }

        public void SortOrderedList()
        {
            orderedNotes.Sort((left, right) => left.data.time.CompareTo(right.data.time));
        }

        public struct BinarySearchResult
        {
            public bool found;
            public int index;
        }

        public static BinarySearchResult BinarySearchOrderedNotes(QNT_Timestamp cueTime)
        {
            BinarySearchResult result;

            int min = 0;
            int max = orderedNotes.Count - 1;
            while (min <= max)
            {
                int mid = (min + max) / 2;
                QNT_Timestamp midCueTime = orderedNotes[mid].data.time;
                if (cueTime == midCueTime)
                {
                    while (mid != 0 && orderedNotes[mid - 1].data.time == cueTime)
                    {
                        --mid;
                    }

                    result.index = mid;
                    result.found = true;
                    return result;
                }
                else if (cueTime < midCueTime)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            result.index = min;
            result.found = false;
            return result;
        }

        public TargetData FindTargetData(QNT_Timestamp time, TargetBehavior behavior, TargetHandType handType)
        {
            BinarySearchResult res = BinarySearchOrderedNotes(time);
            if (res.found == false)
            {
                Debug.LogWarning("Couldn't find note with time " + time);
                return null;
            }

            for (int i = res.index; i < orderedNotes.Count; ++i)
            {
                Target t = orderedNotes[i];
                if (t.data.time == time &&
                    t.data.behavior == behavior &&
                    t.data.handType == handType)
                {
                    return t.data;
                }
            }

            Debug.LogWarning("Couldn't find note with time " + time + " and index " + res.index);
            return null;
        }

        public Target FindNote(TargetData data)
        {
            BinarySearchResult res = BinarySearchOrderedNotes(data.time);
            if (res.found == false)
            {
                //Debug.LogWarning("Couldn't find note with time " + data.time);
                return null;
            }

            for (int i = res.index; i < orderedNotes.Count; ++i)
            {
                Target t = orderedNotes[i];
                if (t.data.ID == data.ID)
                {
                    return t;
                }
            }

            Debug.LogWarning("Couldn't find note with time " + data.time + " and index " + res.index);
            return null;
        }

        public List<Target> FindNotes(List<TargetData> targetDataList)
        {
            List<Target> foundNotes = new List<Target>();
            foreach (TargetData data in targetDataList)
            {
                foundNotes.Add(FindNote(data));
            }
            return foundNotes;
        }

        public TargetData FindChainStart(Target chain)
        {
            return FindChainStart(chain.data);
        }

        public TargetData FindChainStart(TargetData chain)
        {
            if (chain.behavior == TargetBehavior.ChainStart)
            {
                return chain;
            }
            else if(chain.behavior == TargetBehavior.ChainNode)
            {
                NoteEnumerator notes = new NoteEnumerator(new QNT_Timestamp(0), chain.time);
                notes.reverse = true;
                foreach (var note in notes)  //find the first chainstart of the same handtype
                {
                    if (note.data.behavior != TargetBehavior.ChainStart) continue;

                    if (note.data.handType == chain.handType)
                    {
                        return note.data;
                    }
                }
            }

            return null;
        }

        //When loading from cues, use this.
        public TargetData GetTargetDataForCue(Cue cue)
        {
            TargetData data = new TargetData(cue);
            if (data.time.tick == 0) data.SetTimeFromAction(new QNT_Timestamp(120));
            return data;
        }

        //Use when adding a singular target to the project (from the user)
        public void AddTarget(float x, float y)
        {
            if (audicaLoaded == false)
            {
                return;
            }

            if (EditorState.IsInUI || !EditorState.IsOverGrid)
            {
                return;
            }

            TargetData data = new TargetData();
            data.x = x;
            data.y = y;
            data.handType = EditorState.Hand.Current;
            data.behavior = EditorState.Behavior.Current;

            QNT_Timestamp tempTime = GetClosestBeatSnapped(time, (uint)beatSnap);
            TempoChange currentTempo = tempoChanges[0];
            int leftHandMeleeCount = 0;
            int rightHandMeleeCount = 0;
            int meleeCount = 0;
            //int eitherHandCount = 0;
            int targetCount = 0;
            foreach (Target target in loadedNotes)
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


            data.SetTimeFromAction(GetClosestBeatSnapped(time, (uint)beatSnap));

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

            /*if (repeaterManager.IsTargetInRepeaterZone(data.time))
            {
                repeaterManager.CreateRepeaterTarget(data);

               
                //data.repeaterData.targetID = data.repeaterData.Section.GetCurrentTargetIndexID();
            }*/

            var action = new NRActionAddNote { targetData = data };
            Tools.undoRedoManager.AddAction(action);

            songPlayback.PlayHitsound(time);
            SetScale(scale);
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
            noteScale.x = targetScale; // + (1f - NRSettings.config);
            transform1.localScale = noteScale;

            //var gridTargetIcon = Instantiate(gridTargetIconPrefab, gridTransformParent);
            var gridTargetIcon = gridPool.Spawn();
            gridTargetIcon.transform.localPosition = new Vector3(targetData.x, targetData.y, targetData.time.ToBeatTime());

            gridTargetIcon.transform.localScale = new Vector3(NRSettings.config.noteScale, NRSettings.config.noteScale, 1f);
            gridTargetIcon.location = TargetIconLocation.Grid;

            Target target = new Target(targetData, timelineTargetIcon, gridTargetIcon, transient, gridCamera);

            notes.Add(target);
            orderedNotes = notes.OrderBy(v => v.data.time.tick).ToList();

            //Subscribe to the delete note event so we can delete it if the user wants. And other events.
            target.DeleteNoteEvent += DeleteTarget;

            target.TargetEnterLoadedNotesEvent += AddLoadedNote;
            target.TargetExitLoadedNotesEvent += RemoveLoadedNote;

            target.TargetSelectEvent += SelectTarget;
            target.TargetDeselectEvent += DeselectTarget;

            target.MakeTimelineUpdateSustainLengthEvent += UpdateSustainLength;

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

        public List<float> DetectBPM(QNT_Timestamp start, QNT_Timestamp end)
        {
            return BPM.Detect(songPlayback.song, this, start, end);
        }

        public void AddRepeaterSection(RepeaterSection newSection)
        {
            //Ensure there are no overlaps
            bool validSpot = true;
            repeaterSections.ForEach(section =>
            {
                validSpot &= !(
                    //Start is within new section
                    (section.startTime >= newSection.startTime && section.startTime <= newSection.endTime) ||
                    //End is within new section
                    (section.endTime >= newSection.startTime && section.endTime <= newSection.endTime) ||
                    //New Start is within this section
                    (newSection.startTime >= section.startTime && newSection.startTime <= section.endTime) ||
                    //New End is within this section
                    (newSection.endTime >= section.startTime && newSection.endTime <= section.endTime)
                );
            });

            if (!validSpot)
            {
                return;
            }

            var action = new NRActionAddRepeaterSection();

            //Next, gather all other repeaters with the same id
            List<RepeaterSection> siblingSections = repeaterSections.Where(section => { return section.ID == newSection.ID; }).ToList();
            if (siblingSections.Count != 0)
            {
                //Find the section with the largest length
                int index = -1;
                QNT_Duration maxLength = new QNT_Duration(0);

                for (int i = 0; i < siblingSections.Count; ++i)
                {
                    RepeaterSection section = siblingSections[i];
                    QNT_Duration length = (section.endTime - section.startTime).ToDuration();
                    if (length > maxLength)
                    {
                        index = i;
                        maxLength = length;
                    }
                }

                //We found another repeater, add all of their notes to us
                if (index != -1)
                {
                    List<TargetData> sectionNotes = new List<TargetData>();
                    RepeaterSection masterSection = siblingSections[index];
                    foreach (Target t in new NoteEnumerator(masterSection.startTime, masterSection.endTime))
                    {
                        sectionNotes.Add(t.data);
                    }

                    Relative_QNT offset = (newSection.startTime - masterSection.startTime);
                    sectionNotes.ForEach(data =>
                    {
                        action.addTargets.affectedTargets.Add(new TargetData(data, data.time + offset));
                    });
                }
            }

            //If there are targets to add from another repeater section, then we need to clear out the notes in our section
            if (action.addTargets.affectedTargets.Count != 0)
            {
                foreach (Target t in new NoteEnumerator(newSection.startTime, newSection.endTime))
                {
                    action.removeTargets.affectedTargets.Add(t.data);
                }
            }

            action.section = newSection;
            Tools.undoRedoManager.AddAction(action);
        }

        public void AddRepeaterSectionFromAction(RepeaterSection newSection)
        {
            var sectionObject = Instantiate(repeaterSectionPrefab, new Vector3(0, 0, 0), Quaternion.identity, timelineNotesStatic);
            sectionObject.transform.localPosition = new Vector3(newSection.startTime.ToBeatTime(), -0.4f, 1.0f);
            var lineRenderer = sectionObject.GetComponent<LineRenderer>();
            lineRenderer.startWidth = lineRenderer.endWidth = 0.2f;
            lineRenderer.SetPosition(1, new Vector3((newSection.endTime - newSection.startTime).ToBeatTime(), 0, 0));

            newSection.timelineSectionObj = sectionObject;

            repeaterSections.Add(newSection);
            repeaterSections.OrderBy(section => section.startTime);

            miniTimeline.AddRepeaterSection(newSection);
        }

        public void RemoveRepeaterSection(RepeaterSection section)
        {
            var action = new NRActionRemoveRepeaterSection();
            action.section = section;
            Tools.undoRedoManager.AddAction(action);
        }

        public void RemoveRepeaterSectionFromAction(RepeaterSection section)
        {
            GameObject.Destroy(section.timelineSectionObj);
            miniTimeline.RemoveRepeaterSection(section);
            repeaterSections.Remove(section);
        }

        public void RemoveAllRepeaters()
        {
            List<RepeaterSection> sections = repeaterSections.GetRange(0, repeaterSections.Count);
            foreach (RepeaterSection section in sections)
            {
                RemoveRepeaterSectionFromAction(section);
            }
        }

        public RepeaterSection FindRepeaterForTime(QNT_Timestamp time)
        {
            RepeaterSection targetSection = null;
            repeaterSections.ForEach(section =>
            {
                if (section.Contains(time))
                {
                    targetSection = section;
                    return;
                }
            });

            return targetSection;
        }

        public RepeaterSection FindRepeaterForNote(TargetData data)
        {
            return FindRepeaterForTime(data.time);
        }

        public List<TargetData> GenerateRepeaterTargets(TargetData data)
        {
            List<TargetData> newTargets = new List<TargetData>();

            RepeaterSection targetSection = FindRepeaterForNote(data);
            if (targetSection != null)
            {
                for (int i = 0; i < repeaterSections.Count; ++i)
                {
                    var section = repeaterSections[i];

                    if (section.ID == targetSection?.ID && section.startTime != targetSection?.startTime)
                    {
                        Relative_QNT offset = (section.startTime - targetSection.startTime);
                        QNT_Timestamp newTime = data.time + offset;
                        if (newTime >= section.startTime && newTime <= section.endTime)
                        {
                            newTargets.Add(new TargetData(data, data.time + offset));
                        }
                    }
                }
            }

            return newTargets;
        }

        public List<TargetData> FindRepeaterTargets(TargetData data)
        {
            List<TargetData> foundTargets = new List<TargetData>();

            RepeaterSection targetSection = FindRepeaterForNote(data);
            if (targetSection != null)
            {
                for (int i = 0; i < repeaterSections.Count; ++i)
                {
                    var section = repeaterSections[i];

                    if (section.ID == targetSection?.ID && section.startTime != targetSection?.startTime)
                    {
                        //Find the note in the sibling section by offsetting our time
                        Relative_QNT offset = (section.startTime - targetSection.startTime);
                        QNT_Timestamp searchTime = data.time + offset;
                        var result = BinarySearchOrderedNotes(searchTime);
                        if (result.found)
                        {
                            int idx = result.index;
                            for (int noteIdx = idx; noteIdx < orderedNotes.Count; ++noteIdx)
                            {
                                Target t = orderedNotes[noteIdx];
                                if (t.data.time > searchTime)
                                {
                                    break;
                                }
                                if (t.data.data.InternalId == data.data.InternalId)
                                {
                                    foundTargets.Add(t.data);
                                }
                            }
                        }
                    }
                }
            }

            return foundTargets;
        }

        private void UpdateSustains()
        {
            //return; return again if performance is too shit
            foreach (var note in loadedNotes)
            {
                if (note.data.behavior == TargetBehavior.Sustain)
                {
                    if ((note.GetRelativeBeatTime() < 0) && (note.GetRelativeBeatTime() + note.data.beatLength.ToBeatTime() > 0) && !paused)
                    {

                        /*var particles = note.GetHoldParticles ();
						if (!particles.isEmitting) {
							particles.Play ();

							float panPos = (float) (note.data.x / 7.15);
							if (audicaFile.usesLeftSustain && note.data.handType == TargetHandType.Left) {
								songPlayback.leftSustainVolume = sustainVolume;
								songPlayback.leftSustain.pan = panPos;
							} else if (audicaFile.usesRightSustain && note.data.handType == TargetHandType.Right) {
								songPlayback.rightSustainVolume = sustainVolume;
								songPlayback.rightSustain.pan = panPos;
							}

							var main = particles.main;
							main.startColor = note.data.handType == TargetHandType.Left ? new Color (leftColor.r, leftColor.g, leftColor.b, 1) : new Color (rightColor.r, rightColor.g, rightColor.b, 1);
						}*/
                        if (!note.isPlayingSustains)
                        {
                            float panPos = (float)(note.data.x / 7.15);
                            if (audicaFile.usesLeftSustain && note.data.handType == TargetHandType.Left)
                            {
                                songPlayback.leftSustainVolume = sustainVolume;
                                if (songPlayback.leftSustain != null) songPlayback.leftSustain.pan = panPos;

                            }
                            else if (audicaFile.usesRightSustain && note.data.handType == TargetHandType.Right)
                            {
                                songPlayback.rightSustainVolume = sustainVolume;
                                if (songPlayback.rightSustain != null) songPlayback.rightSustain.pan = panPos;
                            }
                            note.isPlayingSustains = true;
                        }
                        /*ParticleSystem.Particle[] parts = new ParticleSystem.Particle[particles.particleCount];
						particles.GetParticles (parts);

						for (int i = 0; i < particles.particleCount; ++i) {
							parts[i].position = new Vector3 (parts[i].position.x, parts[i].position.y, 0);
						}

						particles.SetParticles (parts, particles.particleCount);
						*/

                    }
                    else
                    {
                        /*var particles = note.GetHoldParticles ();
						if (particles.isEmitting) {
							particles.Stop ();
							if (note.data.handType == TargetHandType.Left) {
								songPlayback.leftSustainVolume = 0.0f;
							} else if (note.data.handType == TargetHandType.Right) {
								songPlayback.rightSustainVolume = 0.0f;
							}
						}
						if (paused && animationsNeedStopping) {
							//note.gridTargetIcon.transform.DOKill(true);
							//note.gridTargetIcon.transform.doscale(1f, 0.1f);
						}
						*/
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
            //if (paused) animationsNeedStopping = false;
        }

        public static void AddLoadedNote(Target target)
        {
            loadedNotes.Add(target);
        }

        public static void RemoveLoadedNote(Target target)
        {
            loadedNotes.Remove(target);
        }

        public void SelectTarget(Target target)
        {
            if (!selectedNotes.Contains(target))
            {
                selectedNotes.Add(target);
                target.Select();
                OnSelectedNoteCountChanged.Invoke(selectedNotes.Count);
            }
        }

        public void SelectAllTargets()
        {
            //Camera.main.farClipPlane = 1000;
            foreach (Target target in orderedNotes)
            {
                target.MakeTimelineSelectTarget();
            }
            OnSelectedNoteCountChanged.Invoke(selectedNotes.Count);
        }

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

        public void DeselectAllTargets()
        {
            if (!audicaLoaded) return;

            mainCam.farClipPlane = 50;

            foreach (Target target in selectedNotes)
            {
                DeselectTarget(target, true);
            }

            selectedNotes = new List<Target>();
            OnSelectedNoteCountChanged.Invoke(0);
        }

        /// <summary>
        /// Updates a sustain length from the buttons next to sustains.
        /// </summary>
        /// <param name="target">The target to affect</param>
        /// <param name="increase">If true, increase by one beat snap, if false, the opposite.</param>
        public void UpdateSustainLength(Target target, bool increase)
        {
            if (!target.data.supportsBeatLength) return;
            QNT_Duration increment = Constants.DurationFromBeatSnap((uint)beatSnap);
            QNT_Duration targetLength = target.data.beatLength;
            if (increase)
            {
                if (targetLength < increment)
                {
                    targetLength = new QNT_Duration(0);
                }
                targetLength += increment;
            }
            else
            {
                targetLength -= increment;
            }
            target.data.beatLength = targetLength;
            target.UpdatePath();
        }

        public void UpdateChainConnector(TargetData data)
        {
            var notes = new NoteEnumerator(new(0), data.time); //get all targets from start up until the supplied target
            notes.reverse = true;   //reverse selection to find chainstart

            List<Target> chain = new();
            Target chainStart = null;

            foreach(var note in notes)  //find the first chainstart of the same handtype
            {
                if (note.data.behavior != TargetBehavior.ChainStart) continue;

                if(note.data.handType == data.handType)
                {
                    chainStart = note;
                    chain.Add(chainStart);
                    break;
                }
            }
            if(chainStart != null) //if we found chainstart..
            {
                notes = new NoteEnumerator(chainStart.data.time, orderedNotes.Last().data.time); //..we get all notes from chain start until the last target
                foreach(var note in notes)
                {
                    if (note.data.time == chainStart.data.time) continue; //skip our own target (chainstart)
                    if (note.data.handType != chainStart.data.handType) continue; //skip if it's not the same hand
                    if (note.data.behavior == TargetBehavior.Melee || note.data.behavior == TargetBehavior.Mine) continue; //skip if it's a mine or melee
                    if (note.data.behavior != TargetBehavior.ChainNode) break; //finally, break if it's not a node, since that means the chain has ended by now
                    chain.Add(note); //add the found node to the chain
                }

                if (chain.Count <= 1)
                {
                    //return because chain only has a chainstart
                    return;
                }

                chain.Last().gridTargetIcon.DisableChainConnector(); //disable connector on the last node in case it still had a line connecting to something
                for (int i = chain.Count - 2; i >= 0; i--)
                {
                    chain[i].gridTargetIcon.ConnectChain(chain[i + 1], chainStart); //hook up the chain
                }
            }
        }

        public void MoveGridTargets(List<TargetGridMoveIntent> intents)
        {
            var action = new NRActionGridMoveNotes();
            action.targetGridMoveIntents = intents.Select(intent => new TargetGridMoveIntent(intent)).ToList();
            Tools.undoRedoManager.AddAction(action);
        }

        public NRActionTimelineMoveNotes GenerateMoveTimelineAction(List<TargetTimelineMoveIntent> intents)
        {
            SortOrderedList();
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
                                var data = FindTargetData(startTime + offset, intent.targetData.behavior, intent.targetData.handType);
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

            DeselectAllTargets();
            FindNotes(targetDataList).ForEach(target => SelectTarget(target));
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
            action.affectedTargets = selectedNotes.Select(target => target.data).ToList();
            Tools.undoRedoManager.AddAction(action);
        }

        public void ScaleSelectedTargets(Vector2 scale)
        {
            var action = new NRActionScale();
            action.affectedTargets = selectedNotes.Select(target => target.data).ToList();
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
            action.affectedTargets = selectedNotes.Select(target => target.data).ToList();
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
            Target target = FindNote(targetData);
            if (target == null) return;

            notes.Remove(target);
            orderedNotes.Remove(target);
            loadedNotes.Remove(target);
            selectedNotes.Remove(target);

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
            var notesTemp = notes.ToList();
            foreach (Target target in notesTemp)
            {
                target.Destroy(this);
                timelinePool.Return(target.timelineTargetIcon);
                gridPool.Return(target.gridTargetIcon);
            }

            notes = new List<Target>();
            orderedNotes = new List<Target>();
            loadedNotes = new List<Target>();
            selectedNotes = new List<Target>();
        }

        public void ResetTimeline()
        {
            DeleteAllTargets();
            Tools.undoRedoManager.ClearActions();
            tempoChanges.Clear();
            RemoveAllRepeaters();
            TimelineTextManager.Instance.ClearTimelineTexts();
            ModifierHandler.Instance.CleanUp();
            miniTimeline.ClearBookmarks();
            sustainVisualizer.ClearWaveform();
        }

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
            //Debug.Log ("Saving: " + audicaFile.desc.title);

            //Ensure all chains are generated
            List<TargetData> nonGeneratedNotes = new List<TargetData>();

            foreach (Target note in notes)
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

            foreach (Target target in orderedNotes)
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
            if (audicaFile.desc.bakedzOffset)
            {
                export.cues = ZOffsetBaker.Instance.Bake(export.cues.ToList());
            }

            export.NRCueData.repeaterSections = repeaterSections.GetRange(0, repeaterSections.Count);

            switch (difficultyManager.loadedIndex)
            {
                case 0:
                    audicaFile.diffs.expert = export;
                    break;
                case 1:
                    audicaFile.diffs.advanced = export;
                    break;
                case 2:
                    audicaFile.diffs.moderate = export;
                    break;
                case 3:
                    audicaFile.diffs.beginner = export;
                    break;
            }

            audicaFile.desc = desc;

            desc.tempoList = tempoChanges;

            //AudicaExporter.ExportToAudicaFile(audicaFile, autoSave);
            
            yield return StartCoroutine(exporter.ExportToAudicaFile(audicaFile, autoSave));

            isSaving = false;

        }

        public void ExportAndPlay()
        {
            Export();
            string songFolder = PathLogic.GetSongFolder();
            File.Delete(Path.Combine(songFolder, audicaFile.desc.songID + ".audica"));
            File.Copy(audicaFile.filepath, Path.Combine(songFolder, audicaFile.desc.songID + ".audica"));

            string newPath = Path.GetFullPath(Path.Combine(songFolder, @"..\..\..\..\"));
            System.Diagnostics.Process.Start(Path.Combine(newPath, "Audica.exe"));
        }

        public void LoadTimingMode(AudioClip clip)
        {
            if (audicaLoaded) return;

            songPlayback.LoadAudioClip(clip, PrecisePlayback.LoadType.MainSong);
            inTimingMode = true;
            audioLoaded = true;
        }

        public void CopyTimestampToClipboard()
        {
            string timestamp = songTimestamp.text;
            GUIUtility.systemCopyBuffer = "**" + time.tick.ToString() + "**" + " - ";
        }

        public void SetTimingModeStats(UInt64 microsecondsPerQuarterNote, int tickOffset)
        {
            DeleteAllTargets();
            readyToRegenerate = false;
            SetBPM(new QNT_Timestamp(0), microsecondsPerQuarterNote, false);

            SafeSetTime();
        }

        public void ExitTimingMode()
        {

            inTimingMode = false;
            DeleteAllTargets();

        }

        public void UnloadAudicaFile()
        {
            if (audicaLoaded) Export(false);
            //ModifierHandler.Instance.CleanUp();
            ResetTimeline();
            audicaFile = null;

        }

        public IEnumerator LoadAudicaFile(bool loadRecent = false, string filePath = null, float bpm = -1, Action<bool> onLoaded = null)
        {
            readyToRegenerate = false;
            inTimingMode = false;
            time = new QNT_Timestamp(0);
            SetBeatTime(time);
            SetOffset(new Relative_QNT(0));
            SafeSetTime();
            SetCurrentTick();
            SetCurrentTime();
            if (audicaLoaded && NRSettings.config.saveOnLoadNew)
            {
                yield return StartCoroutine(DoExport());
                //Export();
            }
            if (audicaLoaded)
            {
                miniTimeline.ClearBookmarks(false);
            }
            HandleCache.ClearCache();
            if (loadRecent)
            {
                audicaFile = null;
                audicaFile = AudicaHandler.LoadAudicaFile(PlayerPrefs.GetString("recentFile", null));
                if (audicaFile == null)
                {
                    onLoaded?.Invoke(false);
                    yield break;
                }

            }
            else if (filePath != null)
            {
                audicaFile = null;
                audicaFile = AudicaHandler.LoadAudicaFile(filePath);
                if (audicaFile == null)
                {
                    onLoaded?.Invoke(false);
                    yield break;
                }
                PlayerPrefs.SetString("recentFile", audicaFile.filepath);
                RecentAudicaFiles.AddRecentDir(audicaFile.filepath);

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

                audicaFile = null;

                audicaFile = AudicaHandler.LoadAudicaFile(paths[0]);
                if (audicaFile == null)
                {
                    onLoaded?.Invoke(false);
                    yield break;
                }
                PlayerPrefs.SetString("recentFile", paths[0]);
                RecentAudicaFiles.AddRecentDir(audicaFile.filepath);
            }

            ResetTimeline();

            desc = audicaFile.desc;
            // Get song BPM
            if (audicaFile.song_mid != null)
            {
                foreach (var eventList in audicaFile.song_mid.Events)
                {
                    foreach (var e in eventList)
                    {
                        if (e is TempoEvent)
                        {
                            TempoEvent tempo = (e as TempoEvent);
                            QNT_Timestamp time = new QNT_Timestamp((UInt64)tempo.AbsoluteTime);
                            SetBPM(time, (UInt64)tempo.MicrosecondsPerQuarterNote, false);
                        }
                    }
                }

                //Now, try to match up time signatures with existing tempo markers
                foreach (var eventList in audicaFile.song_mid.Events)
                {
                    foreach (MidiEvent e in eventList)
                    {
                        if (e is TimeSignatureEvent)
                        {
                            TimeSignatureEvent timeSignatureEvent = (e as TimeSignatureEvent);
                            QNT_Timestamp time = new QNT_Timestamp((UInt64)timeSignatureEvent.AbsoluteTime);
                            TimeSignature signature = new TimeSignature((uint)timeSignatureEvent.Numerator, (uint)(1 << timeSignatureEvent.Denominator));

                            bool found = false;
                            for (int i = 0; i < tempoChanges.Count; ++i)
                            {
                                if (tempoChanges[i].time == time)
                                {
                                    TempoChange change = tempoChanges[i];
                                    change.timeSignature = signature;
                                    change.ExplicitSignature = true;
                                    tempoChanges[i] = change;
                                    found = true;
                                    break;
                                }
                            }

                            //If there is no tempo change with this time signature, add whatever the current tempo was at that point
                            if (!found)
                            {
                                SetBPM(time, GetTempoForTime(time).microsecondsPerQuarterNote, false, signature.Numerator, signature.Denominator);
                            }
                        }

                        //go back over the tempo changes and apply previous time signature if one is not explicitly specified
                        else if (e is TempoEvent)
                        {
                            TimeSignature previousTS = new TimeSignature(4, 4);
                            for (int i = 0; i < tempoChanges.Count; ++i)
                            {
                                if (tempoChanges[i].ExplicitSignature)
                                {
                                    previousTS = tempoChanges[i].timeSignature;
                                }
                                else
                                {
                                    TempoChange foundTempo = tempoChanges[i];
                                    foundTempo.timeSignature = previousTS;
                                    tempoChanges[i] = foundTempo;
                                }
                            }

                        }
                    }
                }
            }

            //If we didn't load any bpm, set it from the song desc
            int zeroBPMIndex = GetCurrentBPMIndex(new QNT_Timestamp(0));
            if (zeroBPMIndex == -1)
            {
                SetBPM(new QNT_Timestamp(0), Constants.MicrosecondsPerQuarterNoteFromBPM(desc.tempo), false);
            }

            if (bpm > 0f)
            {
                SetBPM(new QNT_Timestamp(0), Constants.MicrosecondsPerQuarterNoteFromBPM(bpm), true, 4, 4);
            }

            //Update our discord presence
            nrDiscordPresence.UpdatePresenceSongName(desc.title);

            //Loads all the sounds.
            yield return StartCoroutine(GetAudioClip($"file://{Application.dataPath}/.cache/{audicaFile.desc.cachedMainSong}.ogg"));
            if (audicaFile.desc.sustainSongLeft != "") StartCoroutine(LoadLeftSustain($"file://{Application.dataPath}/.cache/{audicaFile.desc.cachedSustainSongLeft}.ogg"));
            if (audicaFile.desc.sustainSongRight != "") StartCoroutine(LoadRightSustain($"file://{Application.dataPath}/.cache/{audicaFile.desc.cachedSustainSongRight}.ogg"));
            yield return StartCoroutine(LoadExtraAudio($"file://{Application.dataPath}/.cache/{audicaFile.desc.cachedFxSong}.ogg"));
            //foreach (Cue cue in audicaFile.diffs.expert.cues) {
            //AddTarget(cue);
            //}
            //Difficulty manager loads stuff now
            audicaLoaded = true;
            difficultyManager.LoadHighestDifficulty();

            //Disable timing window buttons so users don't mess stuff up.
            generateAudicaButton.interactable = false;
            loadAudioFileTiming.interactable = false;

            //Load bookmarks
            if (audicaFile.desc.bookmarks != null)
            {
                foreach (BookmarkData data in audicaFile.desc.bookmarks)
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
            if (audicaFile.desc != null)
            {
                //UIMetadata.Instance.UpdateUIValues();
            }

            if (audicaFile.modifiers != null)
            {
                if (audicaFile.modifiers.modifiers.Count > 0)
                {
                    ModifierHandler.isLoading = true;
                    StartCoroutine(ModifierHandler.Instance.LoadModifiers(audicaFile.modifiers.modifiers, true));
                }

            }

            //Loaded successfully

            //NotificationCenter.SendNotification (new NRNotification ("Map loaded successfully!"));
            onAudicaLoaded?.Invoke(audicaFile);
            NotificationCenter.SendNotification("Press F1 to view shortcuts", NotificationType.Info);
            StopCoroutine(NRSettings.Autosave());
            StartCoroutine(NRSettings.Autosave());
            UpdateState();
           
            onLoaded?.Invoke(true);
            yield return null;
        }

        public List<RepeaterSection> loadRepeaterSectionAfterAudio;
        void PostAudioLoad()
        {
            if (loadRepeaterSectionAfterAudio != null)
            {
                foreach (var section in loadRepeaterSectionAfterAudio)
                {
                    AddRepeaterSectionFromAction(section);
                }
                loadRepeaterSectionAfterAudio = null;
            }
        }

        IEnumerator GetAudioClip(string uri)
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
                    songPlayback.LoadAudioClip(myClip, PrecisePlayback.LoadType.MainSong);

                    audioLoaded = true;
                    audicaLoaded = true;

                    //Load the preview start point
                    miniTimeline.SetPreviewStartPoint(ShiftTick(new QNT_Timestamp(0), (float)desc.previewStartSeconds));

                    readyToRegenerate = true;
                    RegenerateBPMTimelineData();
                    BuildIntroZone();

                    PostAudioLoad();
                }
            }
        }

        IEnumerator LoadNewAudioClip(string uri)
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
                    songPlayback.LoadAudioClip(myClip, PrecisePlayback.LoadType.MainSong);

                    readyToRegenerate = true;
                    RegenerateBPMTimelineData();
                    BuildIntroZone();
                }
            }
        }

        IEnumerator LoadLeftSustain(string uri)
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
                    audicaFile.usesLeftSustain = true;
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    songPlayback.LoadAudioClip(myClip, PrecisePlayback.LoadType.LeftSustain);
                    sustainVisualizer.GenerateWaveform(songPlayback.leftSustain, this);
                }
            }
        }
        IEnumerator LoadRightSustain(string uri)
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
                    audicaFile.usesRightSustain = true;
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    songPlayback.LoadAudioClip(myClip, PrecisePlayback.LoadType.RightSustain);
                }
            }
        }

        IEnumerator LoadExtraAudio(string uri)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if ( www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    songPlayback.LoadAudioClip(myClip, PrecisePlayback.LoadType.Extra);
                }
            }
        }

        public void SetPlaybackSpeed(float speed)
        {
            if (!audioLoaded) return;

            playbackSpeed = speed;
            songPlayback.speed = speed;
            string PlaybackText = ("Speed: " + speed.ToString("#%"));
            playbackSpeedText.text = PlaybackText;
        }

        public void SetPlaybackSpeedFromSlider(Slider slider)
        {
            if (!audioLoaded) return;

            SetPlaybackSpeed(slider.value);
        }

        struct UpdateTiming
        {
            public TargetData data;
            public QNT_Timestamp newTime;
        }

        struct TempoFixup
        {
            public int tempoId;
            public float time;
        }

        //When we shift a previous tempo, we still need to keep the other tempo changes
        // at the same point in time
        List<TempoFixup> GatherTempoFixups(int tempoIndex)
        {
            List<TempoFixup> tempoFixes = new List<TempoFixup>();
            for (int i = tempoIndex + 1; i < tempoChanges.Count; ++i)
            {
                TempoFixup fixup;
                fixup.tempoId = i;
                fixup.time = TimestampToSeconds(tempoChanges[i].time);
                tempoFixes.Add(fixup);
            }

            return tempoFixes;
        }

        //Go through each future tempo, adjust it so that it is still at the same point in time, 
        // then adjust all of the notes in that tempo so that they are still aligned
        void FixupTempoTimings(List<TempoFixup> tempoFixes, List<UpdateTiming> updateTimings)
        {
            foreach (TempoFixup fixup in tempoFixes)
            {
                QNT_Timestamp newTime = ShiftTick(new QNT_Timestamp(0), fixup.time, false);
                TempoChange change = tempoChanges[fixup.tempoId];
                Relative_QNT changeOffset = newTime - change.time;

                QNT_Timestamp endTime = new QNT_Timestamp(UInt64.MaxValue);
                if (fixup.tempoId + 1 < tempoChanges.Count)
                {
                    endTime = tempoChanges[fixup.tempoId + 1].time;
                }

                var enumerator = new NoteEnumerator(change.time, endTime);
                enumerator.endInclusive = false;

                foreach (Target target in enumerator)
                {
                    UpdateTiming t;
                    t.data = target.data;
                    t.newTime = t.data.time + changeOffset;
                    updateTimings.Add(t);
                }

                change.time = newTime;
                tempoChanges[fixup.tempoId] = change;
            }

            tempoChanges = tempoChanges.OrderBy(tempo => tempo.time.tick).ToList();

            //Fixup secondsFromStart
            for (int i = 0; i < tempoChanges.Count; ++i)
            {
                TempoChange c = tempoChanges[i];
                c.secondsFromStart = TimestampToSeconds(c.time);
                tempoChanges[i] = c;
            }
        }

        void ShiftNotesByBPM(UInt64 prevMicrosecondPerQuarterNote, QNT_Timestamp time, List<TempoFixup> tempoFixes)
        {
            int tempoIndex = GetCurrentBPMIndex(time);
            var newTempo = tempoChanges[tempoIndex];

            UInt64 newMicrosecondPerQuarterNote = newTempo.microsecondsPerQuarterNote;

            if (prevMicrosecondPerQuarterNote == 0)
            {
                if (tempoIndex > 0)
                {
                    prevMicrosecondPerQuarterNote = tempoChanges[tempoIndex - 1].microsecondsPerQuarterNote;
                }
                //We are at the beginning, but no previous bpm. We can't shift anything
                else
                {
                    return;
                }
            }

            float p = (float)prevMicrosecondPerQuarterNote / newMicrosecondPerQuarterNote;
            List<UpdateTiming> updateTimings = new List<UpdateTiming>();

            QNT_Timestamp recalcStart = time;
            QNT_Timestamp recalcEnd = new QNT_Timestamp(UInt64.MaxValue);
            if (tempoIndex + 1 < tempoChanges.Count)
            {
                recalcEnd = tempoChanges[tempoIndex + 1].time;
            }

            //Recalc all notes in the zone
            var enumerator = new NoteEnumerator(recalcStart, recalcEnd);
            enumerator.endInclusive = false;

            foreach (Target target in enumerator)
            {
                QNT_Duration tempoTimeDifference = new QNT_Duration(target.data.time.tick - recalcStart.tick);
                QNT_Duration duration_from_start = new QNT_Duration((UInt64)(tempoTimeDifference.tick * p));

                UpdateTiming t;
                t.data = target.data;
                t.newTime = recalcStart + duration_from_start;
                updateTimings.Add(t);
            }

            FixupTempoTimings(tempoFixes, updateTimings);

            //Update all notes
            foreach (var t in updateTimings)
            {
                t.data.SetTimeFromAction(t.newTime);
            }
        }

        public void ShiftNearestBPMToCurrentTime()
        {
            int tempoIndex = GetCurrentBPMIndex(time);
            if (tempoIndex == -1)
            {
                return;
            }

            Relative_QNT offset = time - tempoChanges[tempoIndex].time;
            if (tempoIndex + 1 < tempoChanges.Count)
            {
                Relative_QNT next = time - tempoChanges[tempoIndex + 1].time;
                if (Math.Abs(next.tick) < Math.Abs(offset.tick))
                {
                    tempoIndex += 1;
                    offset = next;
                }
            }

            //If we try to shift the first tempo marker, don't do that
            if (tempoIndex == 0)
            {
                NotificationCenter.SendNotification("Cannot shift first bpm marker!", NotificationType.Error);
                return;
            }

            List<UpdateTiming> updateTimings = new List<UpdateTiming>();

            var nextTempo = tempoChanges[tempoIndex];
            QNT_Timestamp recalcStart = nextTempo.time;
            QNT_Timestamp recalcEnd = new QNT_Timestamp(UInt64.MaxValue);
            if (tempoIndex + 1 < tempoChanges.Count)
            {
                TempoChange nextChange = tempoChanges[tempoIndex + 1];
                recalcEnd = nextChange.time;
            }

            //Update the notes in the recalc area
            var enumerator = new NoteEnumerator(recalcStart, recalcEnd);
            enumerator.endInclusive = false;

            foreach (Target target in enumerator)
            {
                UpdateTiming t;
                t.data = target.data;
                t.newTime = t.data.time + offset;
                updateTimings.Add(t);
            }

            List<TempoFixup> tempoFixes = GatherTempoFixups(tempoIndex);

            //Change the tempo
            nextTempo.time = time;
            tempoChanges[tempoIndex] = nextTempo;

            FixupTempoTimings(tempoFixes, updateTimings);

            //Update all notes
            foreach (var t in updateTimings)
            {
                t.data.SetTimeFromAction(t.newTime);
            }

            RegenerateBPMTimelineData();
        }

        public void SetBPM(QNT_Timestamp time, UInt64 microsecondsPerQuarterNote, bool shiftFutureEvents, uint Numerator = 0, uint Denominator = 0)
        {

            TimeSignature signature = new TimeSignature(Numerator, Denominator);



            TempoChange c = new TempoChange();
            c.time = time;
            c.microsecondsPerQuarterNote = microsecondsPerQuarterNote;
            c.timeSignature = signature;
            c.ExplicitSignature = false;
            c.secondsFromStart = TimestampToSeconds(time);

            UInt64 prevMicrosecondPerQuarterNote = 0;

            int foundIndex = -1;
            for (int i = 0; i < tempoChanges.Count; ++i)
            {
                if (tempoChanges[i].time == time)
                {
                    foundIndex = i;
                    break;
                }
            }

            //Never attempt to remove the first bpm marker
            if (foundIndex == 0 && microsecondsPerQuarterNote == 0)
            {
                NotificationCenter.SendNotification("Cannot remove initial bpm!", NotificationType.Error);
                return;
            }

            if (foundIndex == -1 && microsecondsPerQuarterNote == 0)
            {
                NotificationCenter.SendNotification("Cannot add 0 bpm!", NotificationType.Error);
                return;
            }

            List<TempoFixup> tempoFixes = new List<TempoFixup>();

            //Found a bpm, set it to the new value
            if (foundIndex != -1)
            {
                prevMicrosecondPerQuarterNote = tempoChanges[foundIndex].microsecondsPerQuarterNote;

                //Remove marker
                if (microsecondsPerQuarterNote == 0)
                {
                    tempoFixes = GatherTempoFixups(foundIndex);
                    tempoChanges.RemoveAt(foundIndex);

                    //Fixup the indices, since they're off by 1 now
                    for (int i = 0; i < tempoFixes.Count; ++i)
                    {
                        TempoFixup newFixup = tempoFixes[i];
                        newFixup.tempoId -= 1;
                        tempoFixes[i] = newFixup;
                    }
                }
                //Set to new tempo
                else
                {
                    tempoFixes = GatherTempoFixups(foundIndex);
                    tempoChanges[foundIndex] = c;
                }
            }
            else
            {
                int index = GetCurrentBPMIndex(time);
                tempoFixes = GatherTempoFixups(index);
                tempoChanges.Add(c);

                //Fixup the indices, since they're off by 1 now
                for (int i = 0; i < tempoFixes.Count; ++i)
                {
                    TempoFixup newFixup = tempoFixes[i];
                    newFixup.tempoId += 1;
                    tempoFixes[i] = newFixup;
                }
            }

            tempoChanges = tempoChanges.OrderBy(tempo => tempo.time.tick).ToList();

            //Move all future targets back
            if (shiftFutureEvents)
            {
                //ShiftNotesByBPM(prevMicrosecondPerQuarterNote, time, tempoFixes);
                List<UpdateTiming> updateTimings = new List<UpdateTiming>();
                FixupTempoTimings(tempoFixes, updateTimings);

                //Update all notes
                foreach (var t in updateTimings)
                {
                    t.data.SetTimeFromAction(t.newTime);
                }
            }

            RegenerateBPMTimelineData();

        }

        bool readyToRegenerate = false;
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

            SetScale(scale);
            foreach (var tempo in tempoChanges)
            {
                var timelineBPM = Instantiate(BPM_MarkerPrefab, timelineTransformParent);
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

            QNT_Timestamp endOfAudio = ShiftTick(new QNT_Timestamp(0), songPlayback.song.Length);

            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();

            TempoChange currentTempo = tempoChanges[0];

            uint barLengthIncr = 0;
            for (float t = 0; t < endOfAudio.tick;)
            {
                //ulong snap = (ulong)(beatSnap / 4);
                float snap = beatSnap / 4f;
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
                foreach (TempoChange tempoChange in tempoChanges)
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

            Mesh mesh = timelineNotesStatic.gameObject.GetComponent<MeshFilter>().mesh;
            mesh.Clear();

            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();

            if (!onlyRegenerateMesh)
            {
                waveformVisualizer.GenerateWaveform(songPlayback.song, this);
                if(songPlayback.leftSustain != null)
                {
                    sustainVisualizer.GenerateWaveform(songPlayback.leftSustain, this);
                }
            }
        }

        public void BuildIntroZone()
        {
            if (!readyToRegenerate) return;
            if (songPlayback.song == null) return;

            QNT_Timestamp endOfAudio = ShiftTick(new QNT_Timestamp(0), songPlayback.song.Length);
            TempoChange currentTempo = tempoChanges[0];

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


        public void SetOffset(Relative_QNT newOffset)
        {
            StopCoroutine(AnimateSetTime(new QNT_Timestamp(0)));
            Relative_QNT diff = offset - newOffset;
            offset = newOffset;

            QNT_Timestamp newTime = time + diff;
            if (newTime != time)
            {
                StartCoroutine(AnimateSetTime(newTime));
            }
        }

        public void SetSnap(int newSnap)
        {
            beatSnap = newSnap;
        }

        private bool isBeatSnapWarningActive = false;
        public void BeatSnapChanged()
        {
            string temp = beatSnapSelector.elements[beatSnapSelector.index];
            int snap = 4;
            int.TryParse(temp.Substring(2), out snap);
            beatSnap = snap;

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

        public BinarySearchResult BinarySearchBPMIndex(float seconds)
        {
            BinarySearchResult result;

            int min = 0;
            int max = tempoChanges.Count - 1;
            while (min <= max)
            {
                int mid = (min + max) / 2;
                float midCueTime = tempoChanges[mid].secondsFromStart;
                if (seconds == midCueTime)
                {
                    while (mid != 0 && tempoChanges[mid - 1].secondsFromStart == seconds)
                    {
                        --mid;
                    }

                    result.index = mid;
                    result.found = true;
                    return result;
                }
                else if (seconds < midCueTime)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            result.index = Math.Min(Math.Max(min, 0), max);
            result.found = false;
            return result;
        }

        public BinarySearchResult BinarySearchBPMIndex(QNT_Timestamp cueTime)
        {
            BinarySearchResult result;

            int min = 0;
            int max = tempoChanges.Count - 1;
            while (min <= max)
            {
                int mid = (min + max) / 2;
                QNT_Timestamp midCueTime = tempoChanges[mid].time;
                if (cueTime == midCueTime)
                {
                    while (mid != 0 && tempoChanges[mid - 1].time == cueTime)
                    {
                        --mid;
                    }

                    result.index = mid;
                    result.found = true;
                    return result;
                }
                else if (cueTime < midCueTime)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            result.index = Math.Min(Math.Max(min, 0), max);
            result.found = false;
            return result;
        }

        public int GetCurrentBPMIndex(QNT_Timestamp t)
        {
            var res = BinarySearchBPMIndex(t);

            if (res.index > 0 && tempoChanges[0].time > t)
            {
                return res.index - 1;
            }

            return res.index;
        }

        public TempoChange GetTempoForTime(QNT_Timestamp t)
        {
            int idx = GetCurrentBPMIndex(t);
            if (idx == -1)
            {
                TempoChange change;
                change.time = t;
                change.microsecondsPerQuarterNote = Constants.OneMinuteInMicroseconds / 60;
                change.ExplicitSignature = false;
                change.timeSignature = new TimeSignature(4, 4);
                change.secondsFromStart = TimestampToSeconds(t);
                return change;
            }

            return tempoChanges[idx];
        }

        public double GetBpmFromTime(QNT_Timestamp t)
        {
            int idx = GetCurrentBPMIndex(t);
            if (idx != -1)
            {
                return Constants.GetBPMFromMicrosecondsPerQuaterNote(tempoChanges[idx].microsecondsPerQuarterNote);
            }
            else
            {
                return 120.0;
            }
        }

        public void SetBeatTime(QNT_Timestamp t)
        {
            if (t.tick - bpmDragOffset.tick < 0) t = new QNT_Timestamp(0);
            else t = new QNT_Timestamp(t.tick - bpmDragOffset.tick);
            float x = t.ToBeatTime() - offset.ToBeatTime();
            //timelineBG.material.SetTextureOffset(MainTex, new Vector2((x / 4f + scaleOffset), 1));

            //timelineTransformParent.transform.localPosition = Vector3.left * x / (scale / 20f);
            Vector3 pos = timelineCamera.transform.localPosition;
            pos.x = 1f * x / (scale / 20f);
            timelineCamera.transform.localPosition = pos;
            //gridTransformParent.transform.localPosition = Vector3.back * x;
            pos = gridCamera.position;
            pos.z = x - 5f;
            gridCamera.position = pos;
            UpdateState();
            //OptimizeInvisibleTargets ();
        }

        private QNT_Timestamp bpmDragOffset;

        public void SetBPMDragOffset(QNT_Timestamp offset)
        {
            bpmDragOffset = offset;
        }
        public bool hasBpmDragOffset => bpmDragOffset.tick != 0;
        public QNT_Timestamp GetBPMDragOffset()
        {
            return bpmDragOffset;
        }

        public void ReapplyScale()
        {
            SetScale(scale);
        }

        public Vector3 GetNoteScale(Vector3 scale)
        {
            scale.x = targetScale / 1.75f;
            return scale;
        }

        public void SetScale(int newScale)
        {

            if (newScale < 5 || newScale > 100) return;
            timelineBG.material.SetTextureScale("_MainTex", new Vector2(newScale / 4f, 1));
            scaleOffset = -newScale % 8 / 8f;

            Vector3 timelineTransformScale = timelineTransformParent.transform.localScale;
            timelineTransformScale.x *= (float)scale / newScale;
            scaleTransform = timelineTransformScale.x;
            timelineTransformParent.transform.localScale = timelineTransformScale;

            targetScale *= (float)newScale / scale;
            // fix scaling on all notes
            foreach (Transform note in timelineTransformParent.transform)
            {
                /*
				Vector3 noteScale = note.localScale;
				noteScale.x = targetScale;
				//noteScale.x /= 1.32f; // If you change this you also have to change UpdateTimelineSustainLength. NR is a mess.
				noteScale.x /= 1.75f;   // above value is the original
                */

                //Debug.Log(targetScale / noteScale.y);

                //noteScale.x *= NRSettings.config.noteTimelineScale;
                //noteScale.y = NRSettings.config.noteTimelineScale;

                //note.localScale = noteScale;
                note.localScale = GetNoteScale(note.localScale);
            }
            ModifierHandler.Instance.Scale((float)newScale / scale);
            BookmarkMenu.Instance.Scale();
            TimelineTextManager.Instance.Scale();
            scale = newScale;
            repeaterManager.UpdateRepeaterScale();

            foreach (Target target in orderedNotes)
            {
                target.UpdateTimelineSustainLength();
            }
            BuildIntroZone();
        }

        public void UpdateTrail()
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
        }

        [NRListener]
        public void OnToolChanged(EditorTool _)
        {
            EnableNearSustainButtons();
        }

        public void EnableNearSustainButtons()
        {
            foreach (Target target in loadedNotes)
            {
                if (!target.data.supportsBeatLength || target.data.isPathbuilderTarget) continue;
                bool shouldDisplayTimeline;
                bool shouldDisplayGrid = paused; //Need to be paused
                                                 //Be in drag select, or be a path builder note in path builder mode
                shouldDisplayGrid &= EditorState.Tool.Current == EditorTool.DragSelect || (target.data.behavior == TargetBehavior.Legacy_Pathbuilder && EditorState.Tool.Current == EditorTool.ChainBuilder);
                shouldDisplayTimeline = shouldDisplayGrid;
                shouldDisplayGrid &= target.GetRelativeBeatTime() < 2 && target.GetRelativeBeatTime() > -2; //Target needs to be "near"

                shouldDisplayTimeline &= target.data.time > (time - Relative_QNT.FromBeatTime(20f)) && target.data.time < (time + Relative_QNT.FromBeatTime(20f));
                target.DisplaySustainButtons(shouldDisplayGrid, shouldDisplayTimeline);
                /*if (shouldDisplayGrid) {
					target.EnableSustainButtons ();
				} else {
					target.DisableSustainButtons ();
				}*/
            }
        }

        public void ChangeBeatSnap(bool next)
        {
            if (next) beatSnapSelector.ForwardClick();
            else beatSnapSelector.PreviousClick();
        }

        public void ZoomTimeline(bool zoomIn)
        {
            if (hover)
            {
                SetScale(scale + (zoomIn ? 1 : -1));
                SetBeatTime(time);
            }
        }

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
            onTimelineScrub?.Invoke();
        }

        public delegate void OnTimelineScrub();
        public static event OnTimelineScrub onTimelineScrub;

        private void MoveTimeline()
        {
            var startTime = time;
            //UpdateSustains();
            if (!paused)
            {
                time = ShiftTick(new QNT_Timestamp(0), (float)songPlayback.GetTime());
            }

            if (scrub)
            {
                Relative_QNT jumpDuration = new Relative_QNT(scrubParams.byTick ? 1 : (long)Constants.DurationFromBeatSnap((uint)beatSnap).tick);
                jumpDuration.tick *= scrubParams.forward ? 1 : -1;

                time = scrubParams.byTick ? time + jumpDuration : GetClosestBeatSnapped(time + jumpDuration, (uint)beatSnap);
                if ((float)time.tick - bpmDragOffset.tick < 0)
                {
                    time = bpmDragOffset;
                }
                //UpdateSustains();
                SafeSetTime();
                if (paused)
                {
                    songPlayback.PlayPreview(time, jumpDuration);
                    //checkForNearSustainsOnThisFrame = true;
                }
                else
                {
                    songPlayback.Play(time);
                }

                //SetBeatTime(time);

                StopCoroutine(AnimateSetTime(new QNT_Timestamp(0)));
                if (!paused && ModifierPreviewer.Instance.isPlaying) ModifierPreviewer.Instance.UpdateModifierList(time.tick);
                if (ModifierHandler.activated && ModifierHandler.Instance.isEditingManipulation) ModifierHandler.Instance.UpdateManipulationValues();
            }

            /*if (!paused && !animatingTimeline)
            {
                SetBeatTime(time);
            }*/


            //miniTimeline.SetPercentagePlayed(GetPercentagePlayed());

            //EnableNearSustainButtons();

            //UpdateLoadedNotes();

            if (startTime != time)
            {
                QNT_Timestamp start = startTime;
                QNT_Timestamp end = time;

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

            //Update trace lines
            if (NRSettings.config.enableTraceLines)
            {
                //UpdateTraceLine(leftHandTraceLine, TargetHandType.Left, NRSettings.config.leftColor);
                //UpdateTraceLine(rightHandTraceLine, TargetHandType.Right, NRSettings.config.rightColor);
            }

            //UpdateDualines();

            //songPlayback.volume = NRSettings.config.mainVol;
            //songPlayback.hitSoundVolume = NRSettings.config.noteVol;
            var songEndTime = ShiftTick(new QNT_Timestamp(0), songPlayback.song.Length);
            if (time >= songEndTime)
            {
                if (!paused)
                {
                    paused = true;
                }
                time = songEndTime;
            }
            SafeSetTime();
            SetBeatTime(time);
            SetCurrentTime();
            SetCurrentTick();
        }

        public void UpdateState()
        {
            UpdateSustains();
            UpdateDualines();
            UpdateLoadedNotes();
            miniTimeline.SetPercentagePlayed(GetPercentagePlayed());
            EnableNearSustainButtons();
            if (NRSettings.config.enableTraceLines)
            {
                UpdateTraceLine(leftHandTraceLine, TargetHandType.Left, NRSettings.config.leftColor);
                UpdateTraceLine(rightHandTraceLine, TargetHandType.Right, NRSettings.config.rightColor);
            }
            songPlayback.volume = NRSettings.config.mainVol;
            songPlayback.hitSoundVolume = NRSettings.config.noteVol;
        }

        private void UpdateDualines()
        {
            if (NRSettings.config.enableDualines)
            {
                foreach (var line in dualNoteTraceLines)
                {
                    line.enabled = false;
                }

                int index = 0;
                var backIt = new NoteEnumerator(Timeline.time - Relative_QNT.FromBeatTime(0.3f), Timeline.time + Relative_QNT.FromBeatTime(1.7f));
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
                            if (Timeline.time > t.data.time)
                            {
                                alphaVal = 1.0f - ((Timeline.time - t.data.time).ToBeatTime() / 0.3f);
                            }
                            else
                            {
                                alphaVal = 1.0f - ((t.data.time - Timeline.time).ToBeatTime() / 1.7f);
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

        public void UpdateLoadedNotes()
        {
            List<Target> newLoadedNotes = new List<Target>();
            QNT_Timestamp loadStart = Timeline.time - Relative_QNT.FromBeatTime(10.0f);
            QNT_Timestamp loadEnd = Timeline.time + Relative_QNT.FromBeatTime(10.0f);

            foreach (Target t in new NoteEnumerator(loadStart, loadEnd))
            {

                newLoadedNotes.Add(t);
                if (loadedNotes.Contains(t)) continue;
                t.gridTargetIcon.IconEnterLoadedNotes();
            }
            loadedNotes = newLoadedNotes;
            UpdateDualines();
        }


        //bool checkForNearSustainsOnThisFrame = false;
        public void Update()
        {
            if (paused && !scrub) return;
            MoveTimeline();
            if (scrub) scrub = false;
        }

        public static void ShowTimelineTargets(bool show)
        {
            foreach (var target in orderedNotes)
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
            if (paused) { return; }

            var startTime = Timeline.time + Relative_QNT.FromBeatTime(TraceAheadTime);

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

        public double GetPercentPlayedFromSeconds(double seconds)
        {
            if (songPlayback == null || songPlayback.song == null) return 0f;
            return seconds / songPlayback.song.Length;
        }

        public void JumpToPercent(float percent, bool forceJump = false)
        {
            if (!audioLoaded) return;
            if ((EditorState.Mode.Current != EditorMode.Compose || EditorState.IsInUI) && !forceJump) return;
            time = ShiftTick(new QNT_Timestamp(0), songPlayback.song.Length * percent);

            SafeSetTime();
            SetCurrentTime();
            SetCurrentTick();

            SetBeatTime(time);
            //songPlayback.volume = NRSettings.config.mainVol;
            //songPlayback.hitSoundVolume = NRSettings.config.noteVol;
            //hitSoundVolumeSlider.value = NRSettings.config.noteVol;
            songPlayback.PlayPreview(time, new Relative_QNT((long)Constants.DurationFromBeatSnap((uint)beatSnap).tick));
            UpdateState();
        }

        public void JumpToX(float x)
        {
            if (ModifierHandler.activated || EditorState.Mode.Current != EditorMode.Compose || EditorState.IsInUI) return;
            StopCoroutine(AnimateSetTime(new QNT_Timestamp(0)));
            bool isPlaying = !paused;
            if (isPlaying) TogglePlayback();
            //float posX = Math.Abs(timelineCamera.position.x) + x;
            float posX = x;
            QNT_Timestamp newTime = new QNT_Timestamp(0) + QNT_Duration.FromBeatTime(posX * (scale / 20f));
            newTime = GetClosestBeatSnapped(newTime, (uint)beatSnap);
            SafeSetTime();
            OnAnimateSetTimeDone callback = isPlaying ? new OnAnimateSetTimeDone(TogglePlayback) : null;
            StartCoroutine(AnimateSetTime(newTime, callback));
        }

        public void ToggleWaveform()
        {
            waveformVisualizer.ToggleWaveform();
            sustainVisualizer.ToggleWaveform();
        }

        public void TogglePlayback()
        {
            TogglePlayback(false);
        }

        public void TogglePlayback(bool metronome = false)
        {
            if (!audioLoaded) return;
            //bool isCtrlDown = Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl);
            if (paused)
            {
                //gameObject.GetComponent<AudioSource>().Play();
                //aud.Play();
                //previewAud.Pause();
                //if(Input.GetKey(KeyCode.X)) ModifierPreviewer.Instance.UpdateModifierList(ModifierHandler.Instance.modifiers, time.tick);
                if (metronome)
                {
                    songPlayback.StartMetronome();
                }

                songPlayback.Play(time);
                paused = false;
            }
            else
            {
                //aud.Pause();
                ModifierPreviewer.Instance.Stop();
                songPlayback.Stop();
                paused = true;
                //animationsNeedStopping = true;

                //Snap to the beat snap when we pause
                time = GetClosestBeatSnapped(time, (uint)beatSnap);

                float currentTimeSeconds = TimestampToSeconds(time);
                if (currentTimeSeconds > songPlayback.song.Length)
                {
                    time = ShiftTick(new QNT_Timestamp(0), songPlayback.song.Length);
                }

                SetBeatTime(time);
                SafeSetTime();
                SetCurrentTick();
                SetCurrentTime();
                UpdateState();
            }
            if (paused)
            {
                onPaused?.Invoke();
            }
            else
            {
                onPlay?.Invoke();
            }
        }

        public delegate void OnPaused();
        public static event OnPaused onPaused;

        public delegate void OnPlay();
        public static event OnPlay onPlay;

        public void SafeSetTime()
        {
            if (time.tick < 0) time = new QNT_Timestamp(0);
            if (!audioLoaded) return;

            float currentTimeSeconds = TimestampToSeconds(time);

            if (currentTimeSeconds > songPlayback.song.Length)
            {
                time = ShiftTick(new QNT_Timestamp(0), songPlayback.song.Length);
                currentTimeSeconds = songPlayback.song.Length;
            }
        }
        public delegate void OnAnimateSetTimeDone();
        public IEnumerator AnimateSetTime(QNT_Timestamp newTime, OnAnimateSetTimeDone callback = null)
        {

            animatingTimeline = true;

            if (!audioLoaded) yield break;

            if (TimestampToSeconds(newTime) > songPlayback.song.Length)
            {
                newTime = ShiftTick(new QNT_Timestamp(0), songPlayback.song.Length);
            }

            //DOTween.Play
            DOTween.To(t => SetBeatTime(new QNT_Timestamp((UInt64)Math.Round(t))), time.tick, newTime.tick, 0.2f).SetEase(Ease.InOutCubic);

            yield return new WaitForSeconds(0.2f);

            time = newTime;
            animatingTimeline = false;

            SafeSetTime();
            SetBeatTime(time);

            SetCurrentTime();
            SetCurrentTick();
            songPlayback.PlayPreview(time, new Relative_QNT((long)Constants.DurationFromBeatSnap((uint)beatSnap).tick));
            callback?.Invoke();
            yield break;

        }

        //Snap (rounded down) to the nearest beat given by `beatSnap`
        public QNT_Timestamp GetClosestBeatSnapped(QNT_Timestamp timeToSnap, uint beatSnap)
        {
            int tempoIndex = GetCurrentBPMIndex(timeToSnap);
            if (tempoIndex == -1)
            {
                timeToSnap = new QNT_Timestamp(timeToSnap.tick + bpmDragOffset.tick);
                return QNT_Timestamp.GetSnappedValue(timeToSnap, beatSnap);
            }
            TempoChange currentTempo = tempoChanges[tempoIndex];
            QNT_Duration offsetFromTempoChange = new QNT_Duration(timeToSnap.tick - currentTempo.time.tick);
            offsetFromTempoChange = QNT_Duration.GetSnappedValue(offsetFromTempoChange, beatSnap);
            return new QNT_Timestamp(currentTempo.time.tick + offsetFromTempoChange.tick + bpmDragOffset.tick);
        }

        private void OnMouseDown()
        {
            //We don't want to interfere with drag select
            if (EditorState.Tool.Current == EditorTool.DragSelect || EditorState.Tool.Current == EditorTool.Pathbuilder || EditorState.Tool.Current == EditorTool.ChainBuilder) return;
            JumpToX(mainCam.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>()).x);
        }

        public float GetPercentagePlayed()
        {
            if (songPlayback.song != null)
                return (TimestampToSeconds(time) / songPlayback.song.Length);

            else
                return 0;
        }

        public float GetPercentagePlayed(QNT_Timestamp tick)
        {
            if (songPlayback.song != null)
                return (TimestampToSeconds(tick) / songPlayback.song.Length);

            else
                return 0;
        }

        //Shifts `startTime` by `duration` seconds, respecting bpm changes in between
        public QNT_Timestamp ShiftTick(QNT_Timestamp startTime, float duration, bool useBinarySearch = true)
        {
            int currentBpmIdx = -1;

            //If we're stating at the beginning, jump to the nearest tempo marker
            if (startTime.tick == 0 && useBinarySearch)
            {
                if (duration < 0)
                {
                    return new QNT_Timestamp(0);
                }

                var res = BinarySearchBPMIndex(duration);
                currentBpmIdx = res.index;
                while (currentBpmIdx > 0 && tempoChanges[currentBpmIdx].secondsFromStart > duration)
                {
                    --currentBpmIdx;
                }

                startTime = tempoChanges[currentBpmIdx].time;
                duration -= tempoChanges[currentBpmIdx].secondsFromStart;
            }
            else
            {
                currentBpmIdx = GetCurrentBPMIndex(startTime);
            }

            if (currentBpmIdx == -1)
            {
                return startTime;
            }

            QNT_Timestamp currentTime = startTime;

            while (duration != 0 && currentBpmIdx >= 0 && currentBpmIdx < tempoChanges.Count)
            {
                var tempo = tempoChanges[currentBpmIdx];

                Relative_QNT remainingTime = Conversion.ToQNT(duration, tempo.microsecondsPerQuarterNote);
                QNT_Timestamp timeOfNextBPM = new QNT_Timestamp(0);
                int sign = Math.Sign(remainingTime.tick);

                currentBpmIdx += sign;
                if (currentBpmIdx > 0 && currentBpmIdx < tempoChanges.Count)
                {
                    timeOfNextBPM = tempoChanges[currentBpmIdx].time;
                }

                //If there is time to another bpm we need to shift to the next bpm point, then continue
                if (timeOfNextBPM.tick != 0 && timeOfNextBPM < (currentTime + remainingTime))
                {
                    Relative_QNT timeUntilTempoShift = timeOfNextBPM - currentTime;
                    currentTime += timeUntilTempoShift;
                    duration -= (float)Conversion.FromQNT(timeUntilTempoShift, tempo.microsecondsPerQuarterNote);
                }
                //No bpm change, apply the time and break
                else
                {
                    currentTime += remainingTime;
                    break;
                }
            }

            return currentTime;
        }

        public float TimestampToSeconds(QNT_Timestamp timestamp)
        {
            double duration = 0.0f;

            for (int i = 0; i < tempoChanges.Count; ++i)
            {
                var c = tempoChanges[i];

                if (timestamp >= c.time && (i + 1 >= tempoChanges.Count || timestamp < tempoChanges[i + 1].time))
                {
                    duration += Conversion.FromQNT(timestamp - c.time, c.microsecondsPerQuarterNote);
                    break;
                }
                else if (i + 1 < tempoChanges.Count)
                {
                    duration += Conversion.FromQNT(tempoChanges[i + 1].time - c.time, c.microsecondsPerQuarterNote);
                }
            }

            return (float)duration;
        }

        string prevTimeText;
        private void SetCurrentTime()
        {
            float timeSeconds = TimestampToSeconds(time);

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
            string currentTick = time.tick.ToString();
            if (currentTick != prevTickText)
            {
                prevTickText = currentTick;
                curTick.text = "<mspace=.5em>" + currentTick + "</mspace>";
            }
        }

        private void SetAudioDSP()
        {
            //Pull DSP setting from config
            var configuration = AudioSettings.GetConfiguration();
            configuration.dspBufferSize = NRSettings.config.audioDSP;
            AudioSettings.Reset(configuration);
        }

        public void PreviewCountIn(uint beats)
        {
            if (!paused)
            {
                TogglePlayback();
            }

            time = new QNT_Timestamp(0);
            SafeSetTime();

            TempoChange first = tempoChanges[0];
            QNT_Duration timeSignatureDuration = new QNT_Duration(Constants.PulsesPerWholeNote / first.timeSignature.Denominator) * beats;
            songPlayback.PlayClickTrack(new QNT_Timestamp(0) + timeSignatureDuration);
            if (paused)
            {
                TogglePlayback();
            }
        }

        bool convertWavToOgg(string wavPath, string oggPath)
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

        void ConvertOggToMogg(string oggPath, string moggPath)
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

        public void GenerateCountIn(uint beats)
        {
            TempoChange first = tempoChanges[0];
            QNT_Duration timeSignatureDuration = new QNT_Duration(Constants.PulsesPerWholeNote / first.timeSignature.Denominator) * beats;
            string appPath = Application.dataPath;
            string wavPath = $"{appPath}/.cache/" + "clickTrack.wav";
            string oggPath = $"{appPath}/.cache/" + "clickTrack.ogg";

            string moggName = "song_extras.mogg";
            string moggPath = $"{appPath}/.cache/" + moggName;
            SavWav.Save(wavPath, songPlayback.GenerateClickTrack(new QNT_Timestamp(0) + timeSignatureDuration));

            //Convert wav to ogg
            if (!convertWavToOgg(wavPath, oggPath))
            {
                return;
            }

            //Convert ogg to mogg
            ConvertOggToMogg(oggPath, moggPath);

            //Add extra to zip archive
            using (var archive = ZipArchive.Open(audicaFile.filepath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.ToString() == moggName)
                    {
                        archive.RemoveEntry(entry);
                    }
                }
                archive.AddEntry(moggName, moggPath);
                archive.SaveTo(audicaFile.filepath + ".temp", SharpCompress.Common.CompressionType.None);
                archive.Dispose();
            }
            File.Delete(audicaFile.filepath);
            File.Move(audicaFile.filepath + ".temp", audicaFile.filepath);

            //Load the generated extra sounds
            StartCoroutine(LoadExtraAudio($"file://{oggPath}"));
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
                    _p = audicaFile.desc.cachedMainSong;
                    moggName = "song.mogg";
                    PlayerPrefs.SetString("lastSong", Path.GetDirectoryName(filePath));
                    break;
                case LoadType.Sustain:
                    _p = audicaFile.desc.cachedSustainSongLeft;
                    moggName = "song_sustain_l.mogg";
                    PlayerPrefs.SetString("lastSustain", Path.GetDirectoryName(filePath));
                    /*if(track == UISustainHandler.SustainTrack.Left)
                    {
						_p = audicaFile.desc.cachedSustainSongLeft;
						moggName = "song_sustain_l.mogg";
                    }
					else if(track == UISustainHandler.SustainTrack.Right)
                    {
						_p = audicaFile.desc.cachedSustainSongRight;
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
                string sustainR = mainSongPathBase + $"{audicaFile.desc.cachedSustainSongRight}.ogg";
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
            using (var archive = ZipArchive.Open(audicaFile.filepath))
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
                archive.SaveTo(audicaFile.filepath + ".temp", SharpCompress.Common.CompressionType.None);
                archive.Dispose();
            }
            File.Delete(audicaFile.filepath);
            File.Move(audicaFile.filepath + ".temp", audicaFile.filepath);
            string file = $"file://{filePath}";
            if (type == LoadType.Song) StartCoroutine(LoadNewAudioClip(file));
            else
            {
                if (track == UISustainHandler.SustainTrack.Left)
                {
                    if (audicaFile.desc.sustainSongLeft != "") StartCoroutine(LoadLeftSustain(file));
                }
                else if (track == UISustainHandler.SustainTrack.Right)
                {
                    if (audicaFile.desc.sustainSongRight != "") StartCoroutine(LoadRightSustain(file));
                }
            }
            return true;
        }

        public enum LoadType
        {
            Song,
            Sustain
        }

        public void ShiftEverythingByTime(Relative_QNT shift_amount)
        {
            //Shift tempo markers
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
            foreach (Target note in orderedNotes)
            {
                note.data.SetTimeFromAction(note.data.time + shift_amount);
            }
        }

        public void RemoveOrAddTimeToAudio(Relative_QNT timeChange)
        {
            string appPath = Application.dataPath;
            string mainSongPath = $"{appPath}/.cache/" + $"{audicaFile.desc.cachedMainSong}";
            string leftSustatinPath = $"{appPath}/.cache/" + $"{audicaFile.desc.cachedSustainSongLeft}";
            string rightSustatinPath = $"{appPath}/.cache/" + $"{audicaFile.desc.cachedSustainSongRight}";
            string extraSongPath = $"{appPath}/.cache/" + $"{audicaFile.desc.cachedFxSong}";

            double beatTimeChange = Conversion.FromQNT(timeChange, tempoChanges[0].microsecondsPerQuarterNote);
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

                if (convertWavToOgg(basePath + ".wav", basePath + ".ogg"))
                {
                    File.Delete(basePath + ".wav");
                    return true;
                }

                return false;
            };

            bool modificationSucceeded = modifyAudio(songPlayback.song, mainSongPath);
            bool leftSustainSucceeded = modifyAudio(songPlayback.leftSustain, leftSustatinPath);
            bool rightSustainSucceeded = modifyAudio(songPlayback.rightSustain, rightSustatinPath);
            bool extraSongSucceeded = modifyAudio(songPlayback.songExtra, extraSongPath);
            //If success, Shift, then reload audio
            if (modificationSucceeded)
            {
                //Convert ogg to mogg
                ConvertOggToMogg(mainSongPath + ".ogg", $"{appPath}/.cache/" + $"{audicaFile.desc.moggMainSong}");

                HashSet<string> entriesToUpdate = new HashSet<string>();
                entriesToUpdate.Add(audicaFile.desc.moggMainSong);

                if (leftSustainSucceeded)
                {
                    ConvertOggToMogg(leftSustatinPath + ".ogg", $"{appPath}/.cache/" + $"{audicaFile.desc.moggSustainSongLeft}");
                    entriesToUpdate.Add(audicaFile.desc.moggSustainSongLeft);
                }

                if (rightSustainSucceeded)
                {
                    ConvertOggToMogg(rightSustatinPath + ".ogg", $"{appPath}/.cache/" + $"{audicaFile.desc.moggSustainSongRight}");
                    entriesToUpdate.Add(audicaFile.desc.moggSustainSongRight);
                }

                if (extraSongSucceeded)
                {
                    ConvertOggToMogg(extraSongPath + ".ogg", $"{appPath}/.cache/" + $"{audicaFile.desc.moggFxSong}");
                    entriesToUpdate.Add(audicaFile.desc.moggFxSong);
                }

                //Add extra to zip archive
                using (var archive = ZipArchive.Open(audicaFile.filepath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entriesToUpdate.Contains(entry.ToString()))
                        {
                            archive.RemoveEntry(entry);
                        }
                    }

                    archive.AddEntry(audicaFile.desc.moggMainSong, $"{appPath}/.cache/" + $"{audicaFile.desc.moggMainSong}");

                    if (leftSustainSucceeded)
                    {
                        archive.AddEntry(audicaFile.desc.moggSustainSongLeft, $"{appPath}/.cache/" + $"{audicaFile.desc.moggSustainSongLeft}");
                    }

                    if (rightSustainSucceeded)
                    {
                        archive.AddEntry(audicaFile.desc.moggSustainSongRight, $"{appPath}/.cache/" + $"{audicaFile.desc.moggSustainSongRight}");
                    }

                    if (extraSongSucceeded)
                    {
                        archive.AddEntry(audicaFile.desc.moggFxSong, $"{appPath}/.cache/" + $"{audicaFile.desc.moggFxSong}");
                    }

                    archive.SaveTo(audicaFile.filepath + ".temp", SharpCompress.Common.CompressionType.None);
                    archive.Dispose();
                }
                File.Delete(audicaFile.filepath);
                File.Move(audicaFile.filepath + ".temp", audicaFile.filepath);

                //After we have the new audica file, move the notes and load the new audio
                ShiftEverythingByTime(timeChange);

                audioLoaded = false;
                audicaLoaded = false;
                StartCoroutine(GetAudioClip($"file://{Application.dataPath}/.cache/{audicaFile.desc.cachedMainSong}.ogg"));

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

#if UNITY_EDITOR

        public void SetupBlankTest()
        {
            readyToRegenerate = false;

            SetBPM(new QNT_Timestamp(0), Constants.MicrosecondsPerQuarterNoteFromBPM(100), false);

            int sampleRate = 44100;
            float lengthSeconds = 100;
            AudioClip blankClip = AudioClip.Create("TestClip", (int)(sampleRate * lengthSeconds * 2), 2, sampleRate, false);
            songPlayback.LoadAudioClip(blankClip, PrecisePlayback.LoadType.MainSong);

            audioLoaded = true;
            audicaLoaded = true;

            readyToRegenerate = true;
            RegenerateBPMTimelineData();
        }

#endif

    }
}