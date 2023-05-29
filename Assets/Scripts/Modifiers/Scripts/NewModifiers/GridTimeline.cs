using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using NotReaper;
using NotReaper.Models;
using NotReaper.Modifier;
using NotReaper.Targets;
using NotReaper.Timing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem.XR.Haptics;

namespace NotReaper
{
    public class GridTimeline : MonoBehaviour
    {

        [SerializeField] private Transform timelineParent;
        [SerializeField] internal GameObject selectionBox;
        [SerializeField] private List<GameObject> modifierTimeline;
        [SerializeField] private List<GameObject> objectsToHide = new();
        [SerializeField] private List<TrackContent> trackContents = new();
        [SerializeField] private List<GameObject> trackBackgrounds = new();
        [SerializeField] private RectTransform trackBackgroundParent;
        [NRInject] private Timeline timeline;

        public static TimelineType Type { get; private set; }
        
        internal List<TrackContent> TrackContents => trackContents;

        public float width = 1f;
        public float maxHeight = 2f;
        public float zIndex = 3f;

        private MeshFilter[] meshFilters;

        private const float CameraScrollAmount = .5f;

        public delegate void TimelineOpenedEventHandler(TimelineType type, bool show);

        public static event TimelineOpenedEventHandler onTimelineOpened;

        private List<BoxCollider2D> trackContentColliders = new();

        private const float ReducedBackgroundWidth = 580f;
        private const float FullBackgroundWidth = 825f;
        private const float ReducedTrackColliderWidth = 9.9f;
        private const float ReducedTrackColliderXOffset = -1.15f;
        private const float FullTrackColliderWidth = 15.1f;
        private const float FullTrackColliderXOffset = 1.45f;
        private const float ColliderYOffset = -.25f;
        private const float BackgroundHeight = 249.72f;

        public enum WidthType
        {
            Reduced,
            Full
        }
               
        private void Awake()
        {
            meshFilters = GetComponentsInChildren<MeshFilter>();
            foreach(var obj in modifierTimeline)
                obj.SetActive(false);

            foreach (var track in trackContents)
            {
                trackContentColliders.Add(track.GetComponent<BoxCollider2D>());
            }
        }

        private void Start()
        {
            EditorBeatSnap.onBeatSnapChanged += (_, __) => RegenerateTimeline();
            EditorFile.onAudicaFileLoaded += (_) => RegenerateTimeline();
            EditorScale.onScaleChanged += OnScaleChanged;
        }

        public void SetTimelineType(TimelineType type, int trackCount, WidthType widthType)
        {
            Type = type;

            for (int i = 0; i < trackContents.Count; i++)
            {
                bool show = i < trackCount;
                trackContents[i].gameObject.SetActive(show);
                trackBackgrounds[i].SetActive(show);
            }

            float colliderWidth;
            float colliderXOffset;
            float backgroundWidth;

            if (widthType == WidthType.Full)
            {
                colliderWidth = FullTrackColliderWidth;
                colliderXOffset = FullTrackColliderXOffset;
                backgroundWidth = FullBackgroundWidth;
            }
            else
            {
                colliderWidth = ReducedTrackColliderWidth;
                colliderXOffset = ReducedTrackColliderXOffset;
                backgroundWidth = ReducedBackgroundWidth;
            }
            
            Vector2 offset = new(colliderXOffset, ColliderYOffset);
            Vector2 size = new(colliderWidth, .5f);
            
            foreach (var collider in trackContentColliders)
            {
                collider.offset = offset;
                collider.size = size;
            }

            trackBackgroundParent.sizeDelta = new(backgroundWidth, BackgroundHeight);
        }

        public void PlaceContent(Content content)
        {
            TrackContent trackContent = null;
            int i = 0;
            foreach (var track in TrackContents)
            {
                if (track.tracks[content.TimelineType].ID == content.Track.ID)
                {
                    trackContent = track;
                    break;
                }

                i++;
            }
            var parent = trackContent == null ? null : trackContent.transform;
            
            //content.transform.SetParent(timelineParent);
            bool show = trackContent != null;
            if (show)
            {
                var pos = content.transform.position;
                pos.y = parent.position.y - .2f;
                content.transform.position = pos;
            }
            content.Show(show);
        }

        public bool TrySwitchTrack(TimelineType type, Content currentContent, Vector2 mousePosition)
        {
            foreach (var trackContent in trackContents)
            {
                if (!trackContent.tracks.ContainsKey(type)) continue;
                
                if (trackContent.gameObject.activeSelf && trackContent.ContainsPoint(mousePosition))
                {
                    var track = trackContent.tracks[type];
                    
                    if (currentContent.Track == track) //|| track.ContainsContentAtTime(currentContent, currentContent.timeframe))
                    {
                        return false;
                    }

                    if (currentContent.SwitchTrack(track))
                    {
                        var pos = currentContent.transform.position;
                        pos.y = trackContent.transform.position.y - .2f;
                        currentContent.transform.position = pos;
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        public void SwitchTrack(TimelineType type, Content content, TrackManager.TrackID newTrack)
        {
            foreach (var trackContent in trackContents)
            {
                if (!trackContent.tracks.ContainsKey(type)) continue;
                
                if (trackContent.gameObject.activeSelf && trackContent.tracks[type].ID == newTrack)
                {
                    content.SwitchTrack(trackContent.tracks[type]);
                    var pos = content.transform.position;
                    pos.y = trackContent.transform.position.y - .2f;
                    content.transform.position = pos;
                }
            }
        }


        /// <summary>
        /// Show or hide the modifier timeline.
        /// </summary>
        /// <param name="show">True to show the modifier timeline, false to show default timeline.</param>
        public void ShowTimeline(bool show)
        {
            onTimelineOpened?.Invoke(Type, show);
            
            foreach (var obj in objectsToHide)
            {
                obj.SetActive(!show);
            }
            foreach(var obj in modifierTimeline)
                obj.SetActive(show);
        }

        [Button]
        private void Redraw()
        {
            var filters = GetComponentsInChildren<MeshFilter>();
            var mesh = filters[0].mesh;
            DebugDrawTimeline(mesh, width, maxHeight, zIndex);
            
            for (int i = 1; i < filters.Length; i++)
            {
                filters[i].mesh = mesh;
            }
        }

        private void DebugDrawTimeline(Mesh mesh, float width, float maxHeight, float zIndex)
        {
            QNT_Timestamp endOfAudio = new QNT_Timestamp(1920 * 60);

            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();

            TempoChange currentTempo = new TempoChange();
            currentTempo.timeSignature = new TimeSignature(4, 4);
            currentTempo.time = new QNT_Timestamp(0);
            currentTempo.secondsFromStart = 0f;
            currentTempo.microsecondsPerQuarterNote = (UInt64)Math.Round((double)Constants.OneMinuteInMicroseconds / 120);
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

        private int oldScale = EditorScale.DefaultScale;
        private void OnScaleChanged(int scale)
        {
            Vector3 timelineTransformScale = timelineParent.localScale;
            timelineTransformScale.x *= (float)oldScale / scale;
            timelineParent.transform.localScale = timelineTransformScale;

            
            foreach(var trackContent in trackContents)
                trackContent.OnScaleChanged(timelineTransformScale.x);
            
            
            RegenerateTimeline();
            oldScale = scale;
        }

        private void RegenerateTimeline()
        {
            if (EditorFile.IsLoading) return;
            var mesh = meshFilters[0].mesh;
            timeline.DrawTimingBars(mesh, width, maxHeight, zIndex);
            
            for (int i = 1; i < meshFilters.Length; i++)
            {
                meshFilters[i].mesh = mesh;
            }
        }
    }
}
