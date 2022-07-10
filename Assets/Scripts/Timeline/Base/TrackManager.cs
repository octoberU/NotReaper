using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        
        protected Dictionary<int, Track> tracks = new();
        private SlidingRange range;
        public int CurrentIndex => range.start;

        private GridTimeline timeline;

        private SerializableDictionary<int, int> savedTracks = new();
        
        protected abstract TimelineType TimelineType { get; }
        internal int TrackCount => GetSavedTracks().Count;

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

            if(show) UpdateVisibleTracks();
            else HideAllContent();
        }

        private void HideAllContent()
        {
            foreach (var track in tracks)
            {
                var content = track.Value.Content;
                foreach (var c in content)
                {
                    c.Show(false);
                }
            }
        }

        /// <summary>
        /// Key: Type as int
        /// Value: Order
        /// </summary>
        /// <returns></returns>
        protected abstract SerializableDictionary<int, int> GetSavedTracks();

        protected abstract void SaveTrackOrder(SerializableDictionary<int, int> trackOrder);
        
        private void CreateTracks()
        {
            savedTracks = GetSavedTracks();
            var i = 0;
            foreach (var kvp in savedTracks)
            {
                var track = Instantiate(trackPrefab, trackContainer);
                track.Initialize(kvp.Key, kvp.Key, this);
                tracks.Add(kvp.Key, track);
                if (i >= capacity || i >= savedTracks.Count)
                    track.gameObject.SetActive(false);
                else
                    timeline.TrackContents[kvp.Key].SetTrack(TimelineType, track);


                i++;
            }
            range = new(0, capacity - 1, 0, tracks.Count - 1);
        }
        
        internal void AddContent(Content content) => tracks[content.Type].Add(content);

        internal void RemoveContent(Content content) => tracks[content.Type].Remove(content);


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
            var otherTrack = tracks[track.Order + direction];
            tracks[otherTrack.Order] = track;
            tracks[track.Order] = otherTrack;

            track.SetOrder(otherTrack.Order);
            otherTrack.SetOrder(track.Order - direction);

            savedTracks[track.Type] = track.Order;
            savedTracks[otherTrack.Type] = otherTrack.Order;
            
            track.transform.SetSiblingIndex(track.Order);
            SaveTrackOrder(savedTracks);
        }

        private void UpdateVisibleTracks()
        {
            int trackIndex = 0;
            Transform parent = null;
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                var contents = track.Content;
                bool inRange = range.IsInRange(i);
                
                track.gameObject.SetActive(inRange);

                if (inRange)
                {
                    parent = timeline.TrackContents[trackIndex].transform;
                    timeline.TrackContents[trackIndex].SetTrack(TimelineType, track);
                    trackIndex++;
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

        public Track GetTrack<T>(T type) => tracks[(int)(object)type];
        public Track GetTrack(int type) => tracks[type];
        
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
