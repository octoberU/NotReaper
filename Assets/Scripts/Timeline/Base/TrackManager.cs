using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.ModelBinding;
using NotReaper.Modifiers;
using NotReaper.Timing;
using UnityEngine;

namespace NotReaper
{
    public abstract class TrackManager : MonoBehaviour
    {
        [SerializeField] private Track trackPrefab;
        [SerializeField] private Transform trackContainer;
        [SerializeField, Tooltip("The amount of tracks that we can display at once")]
        private int capacity = 10;
        [SerializeField] private CanvasGroup canvas;
        
        protected Dictionary<int, Track> tracks = new();
        private SlidingRange range;
        public int CurrentIndex => range.start;

        private GridTimeline timeline;

        private Dictionary<int, int> savedTracks = new();
        private int previousScale = EditorScale.DefaultScale;
        
        protected abstract TimelineType TimelineType { get; }
        internal int TrackCount => savedTracks.Count;

        public Dictionary<int, Track> Tracks => tracks;

        protected virtual void Start()
        {
            timeline = NRDependencyInjector.Get<GridTimeline>();
            NRSettings.OnLoad(CreateTracks);
            EditorState.OnEditorReset += OnReset;
            GridTimeline.onTimelineOpened += OnTimelineOpened;
            EditorScale.onScaleChanged += OnScaleChanged;
            EditorFile.onLoaded += HideAllContent;
        }
        
        private void OnScaleChanged(int scale)
        {
            float scaleAmount = EditorScale.ScaleAmount;
            
            Vector3 currentScale = canvas.transform.localScale;
            currentScale.x *= (float)previousScale / scale;
            canvas.transform.localScale = currentScale;
            previousScale = scale;
            
            foreach (var track in tracks)
            {
                track.Value.OnScaleChanged(scaleAmount);
            }
        }

        private void OnReset()
        {
            foreach (var kvp in tracks)
            {
                kvp.Value.OnReset();
            }
        }

        private void OnTimelineOpened(TimelineType type, bool show)
        {
            if (type != TimelineType) return;

            if (show) UpdateVisibleTracks();
            else HideAllContent();
        }

        private void ShowAllContent() => canvas.alpha = 1f;

        private void HideAllContent()
        {
            foreach (var track in Tracks)
            {
                foreach (var content in track.Value.Content)
                {
                    content.Show(false);
                }
            }
        }

        [Serializable]
        public class TrackOrder
        {
            public int type;
            public int order;

            public TrackOrder(int type, int order)
            {
                this.type = type;
                this.order = order;
            }
        }

        /// <summary>
        /// Key: Type as int
        /// Value: Order
        /// </summary>
        /// <returns></returns>
        protected abstract List<TrackOrder> GetSavedTracks();

        protected abstract void SaveTrackOrder(List<TrackOrder> trackOrder);
        
        private void CreateTracks()
        {
            var saved = GetSavedTracks();
            foreach(var s in saved)
                savedTracks.Add(s.type, s.order);

            var i = 0;
            foreach (var trackOrder in savedTracks)
            {
                var track = Instantiate(trackPrefab, trackContainer);
                track.Initialize(trackOrder.Key, trackOrder.Value, this);
                tracks.Add(trackOrder.Key, track);
                if (trackOrder.Value < capacity && trackOrder.Value < savedTracks.Count)
                {
                    timeline.TrackContents[trackOrder.Value].SetTrack(TimelineType, track);
                }
                
                track.gameObject.SetActive(false);
                track.transform.SetSiblingIndex(trackOrder.Value);
                i++;
            }
            range = new(0, capacity - 1, 0, tracks.Count - 1);

            foreach (var track in tracks)
                track.Value.transform.SetSiblingIndex(track.Value.Order);
        }
        
        internal void AddContent(Content content) => tracks[content.Type].Add(content);

        internal void RemoveContent(Content content) => tracks[content.Type].Remove(content);

        internal void SortTrackContent(int type) =>  tracks[type].SortContent();

        internal void SortAllTrackContent()
        {
            foreach (var track in tracks)
            {
                track.Value.SortContent();
            }
        }


        internal bool ContainsContentAtTime(Content content, Timeframe timeframe)
            => tracks[content.Type].ContainsContentAtTime(content, timeframe);

        internal bool ContainsContentAtTime<T>(T type, QNT_Timestamp time)
            => tracks[(int)(object)type].ContainsContentAtTime(time);

        internal bool TryGetContent(int type, QNT_Timestamp time, out Content content)
            => tracks[type].TryGetContent(time, out content);

        internal bool TryGetContent(int type, Timeframe timeframe, out Content content)
            => tracks[type].TryGetContent(timeframe, out content);

        internal bool ContainsContentAtTimeInAnyTrack(QNT_Timestamp time, params int[] excludeTypes)
        {
            foreach (var track in tracks)
            {
                if (excludeTypes.Contains(track.Value.Type)) continue;
                
                if (track.Value.ContainsContentAtTime(time)) return true;
            }

            return false;
        }

        internal bool ContainsContentAtTimeInAnyTrack(Content content, Timeframe timeframe, params int[] excludeTypes)
        {
            foreach (var track in tracks)
            {
                if (excludeTypes.Contains(track.Value.Type)) continue;
                if (track.Value.ContainsContentAtTime(content, timeframe)) return true;
            }

            return false;
        }

        internal void MoveTrackUp(Track track)
        {
            if (track.Order == 0) return;
            SwapTracks(track, -1);
            UpdateVisibleTracks();
        }

        internal void MoveTrackDown(Track track)
        {
            if (track.Order == range.max) return;
            SwapTracks(track, 1);
            UpdateVisibleTracks();
        }

        private void SwapTracks(Track track, int direction)
        {
            var oldOrder = track.Order;
            var newOrder = track.Order + direction;
            Track otherTrack = null;

            foreach (var t in tracks)
            {
                if (t.Value.Order == newOrder)
                {
                    otherTrack = t.Value;
                    break;
                }
            }

            if (otherTrack == null)
                return;
            
            track.SetOrder(newOrder);
            otherTrack.SetOrder(oldOrder);

            savedTracks[track.Type] = track.Order;
            savedTracks[otherTrack.Type] = otherTrack.Order;
            
            track.transform.SetSiblingIndex(track.Order);

            List<TrackOrder> saved = new();
            foreach(var t in savedTracks)
                saved.Add(new TrackOrder(t.Key, t.Value));
            
            SaveTrackOrder(saved);
        }

        public void UpdateVisibleTracks()
        {
            int trackIndex = 0;
            Transform parent = null;
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                var contents = track.Content;
                bool inRange = range.IsInRange(track.Order);
                
                track.gameObject.SetActive(inRange);

                if (inRange)
                {
                    trackIndex = track.Order - range.start;
                    parent = timeline.TrackContents[trackIndex].transform;
                    timeline.TrackContents[trackIndex].SetTrack(TimelineType, track);
                    //trackIndex++;
                }

                foreach (var modifier in contents)
                {
                    modifier.Show(inRange);
                    
                    if (inRange)
                    {
                        var pos = modifier.transform.position;
                        pos.y = parent.position.y - .2f;
                        modifier.transform.position = pos;
                    }
                }
            }
        }

        public Track GetTrack<T>(T type) => GetTrack((int)(object)type);
        public Track GetTrack(int type) => tracks.ContainsKey(type) ? tracks[type] : null;
        
        public void ScrollUp()
        {
            if (!range.Up()) return;
            
            UpdateVisibleTracks();
        }

        public void ScrollDown()
        {
            if (!range.Down()) return;
            
            UpdateVisibleTracks();
        }

        public abstract void Show(bool show);

        private class SlidingRange
        {
            public readonly int min;
            public readonly int max;
            public int start { get; private set; }
            public int end { get; private set; }

            public SlidingRange(int start, int end, int min, int max)
            {
                this.start = start;
                this.end = end;
                this.min = min;
                this.max = max;
            }

            public bool IsInRange(int index)
                => index >= start && index <= end;

            public bool Up()
            {
                if (start == min) return false;
                start--;
                end--;
                return true;
            }

            public bool Down()
            {
                if (end == max) return false;
                start++;
                end++;
                return true;
            }
        }
    }
}
