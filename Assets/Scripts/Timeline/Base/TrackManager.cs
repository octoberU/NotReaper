using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.ModelBinding;
using I18N.Common;
using NotReaper.Modifiers;
using NotReaper.Notifications;
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
        
        protected Dictionary<TrackID, Track> tracks = new();
        private SlidingRange range;
        public int CurrentIndex => range.start;

        private GridTimeline timeline;
        
        private int previousScale = EditorScale.DefaultScale;
        
        protected abstract TimelineType TimelineType { get; }
        internal int TrackCount => _trackArrangement.TrackCount;

        public Dictionary<TrackID, Track> Tracks => tracks;

        private TrackArrangement _trackArrangement = new();

        public struct TrackID
        {
            public readonly int type;
            public readonly int index;

            public TrackID(int type, int typeIndex)
            {
                this.type = type;
                this.index = typeIndex; 
            }
            
            private bool IsEqual(TrackID other)
                => type == other.type && index == other.index;
            
            public override bool Equals(object obj)
            {
                if (obj is TrackID other)
                    return IsEqual(other);

                return false;
            }

            public static bool operator ==(TrackID a, TrackID b) => a.IsEqual(b);

            public static bool operator !=(TrackID a, TrackID b) => !a.IsEqual(b);
            
            public override int GetHashCode()
            {
                unchecked
                {
                    return (type * 397) ^ index;
                }
            }
        }

        private class TrackArrangement
        {
            private Dictionary<int, TrackMapping> _collection = new();
            
            public int TrackCount => _collection.Sum(kvp => kvp.Value.mapping.Count);
            
            public void Load(List<TrackOrder> trackOrder)
            {
                _collection.Clear();
                
                foreach (var track in trackOrder)
                    Add(track);
            }
            
            public void Add(TrackOrder track)
            {
                if (!_collection.ContainsKey(track.type))
                    _collection[track.type] = new TrackMapping(track.type);

                _collection[track.type].Add(track);
            }

            public void Remove(TrackID id)
            {
                if (!_collection.ContainsKey(id.type))
                    return;
                
                _collection[id.type].TryRemove(id.index);
                
                if (_collection[id.type].mapping.Count == 0)
                    _collection.Remove(id.type);
            }
            
            public void Remove(int type, int typeIndex)
                => Remove(new(type, typeIndex));
            
            
            public void SetOrder(int type, int typeIndex, int order)
            {
                if (!_collection.TryGetValue(type, out var mapping))
                    return;
                
                mapping.SetOrder(typeIndex, order);
            }

            public List<TrackOrder> GetTrackOrder()
            {
                List<TrackOrder> trackOrder = new();
                
                foreach(var kvp in _collection)
                    trackOrder.AddRange(kvp.Value.GetTrackOrder());
                
                return trackOrder;
            }

            public int NumTracksOfType(int type)
            {
                if (!_collection.ContainsKey(type))
                    return 0;

                return _collection[type].mapping.Count;
            }

            public int GetLastTrackOrderOfType(int type)
            {
                if (!_collection.ContainsKey(type))
                    return TrackCount - 1;

                int maxOrder = 0;

                foreach (var order in _collection[type].mapping.Values)
                {
                    if (order > maxOrder)
                        maxOrder = order;
                }

                return maxOrder;
            }

            public TrackID? GetTrackAbove(TrackID track) 
                => GetTrackAtOrder(_collection[track.type].mapping[track.index] - 1);

            public TrackID? GetTrackBelow(TrackID track)
                => GetTrackAtOrder(_collection[track.type].mapping[track.index] + 1);

            private TrackID? GetTrackAtOrder(int desiredOrder)
            {
                foreach (var trackMapping in _collection)
                {
                    foreach (var order in trackMapping.Value.mapping)
                    {
                        if (order.Value == desiredOrder)
                        {
                            return new(trackMapping.Key, order.Key);
                        }
                    }
                }

                return null;
            }
        }

        private class TrackMapping
        {
            /// <summary>
            /// The type of track
            /// </summary>
            public int type;

            /// <summary>
            /// key = typeIndex, value = order
            /// </summary>
            public Dictionary<int, int> mapping = new();

            public TrackMapping(int type) 
                => this.type = type;

            public void Add(TrackOrder track) 
                => mapping[track.typeIndex] = track.order;

            public void SetOrder(int typeIndex, int order)
                => mapping[typeIndex] = order;

            public bool TryRemove(int typeIndex)
            {
                if (!mapping.ContainsKey(typeIndex))
                    return false;

                mapping.Remove(typeIndex);
                return true;
            }

            public List<TrackOrder> GetTrackOrder()
            {
                List<TrackOrder> trackOrder = new();
                
                foreach(var kvp in mapping)
                    trackOrder.Add(new TrackOrder(type, kvp.Value, kvp.Key));

                return trackOrder;
            }
        }

        public TrackID? GetTrackAbove(TrackID track)
            => _trackArrangement.GetTrackAbove(track);
        
        public TrackID? GetTrackBelow(TrackID track)
            => _trackArrangement.GetTrackBelow(track);

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
            public int typeIndex;

            public TrackOrder(int type, int order)
            {
                this.type = type;
                this.order = order;
                this.typeIndex = 0;
            }

            public TrackOrder(int type, int order, int typeIndex)
            {
                this.type = type;
                this.order = order;
                this.typeIndex = typeIndex;
            }
        }

        /// <summary>
        /// Key: Type as int
        /// Value: Order
        /// </summary>
        /// <returns></returns>
        protected abstract List<TrackOrder> GetSavedTracks();

        protected abstract void SaveTrackOrder(List<TrackOrder> trackOrder);

        private void CreateTracks() => LoadTrackArrangement(GetSavedTracks());

        public List<TrackOrder> GetTrackArrangementData() => _trackArrangement.GetTrackOrder();

        public void LoadTrackArrangement(List<TrackOrder> trackArrangement)
        {
            List<Track> existingTracks = new();
            foreach (var track in tracks.Values)
                existingTracks.Add(track);

            for (int i = existingTracks.Count - 1; i >= 0; i--)
                Destroy(existingTracks[i].gameObject);
            
            tracks.Clear();
            
            
            
            _trackArrangement.Load(trackArrangement);
            
            var trackCount = TrackCount;

            foreach (var trackOrder in _trackArrangement.GetTrackOrder())
            {
                var track = Instantiate(trackPrefab, trackContainer);
                track.Initialize(trackOrder.type, trackOrder.order, trackOrder.typeIndex, this);
                tracks.Add(new(trackOrder.type, trackOrder.typeIndex), track);
                if (trackOrder.order < capacity && trackOrder.order < trackCount)
                {
                    timeline.TrackContents[trackOrder.order].SetTrack(TimelineType, track);
                }
                
                track.gameObject.SetActive(false);
                track.transform.SetSiblingIndex(trackOrder.order);
            }
            range = new(0, capacity - 1, 0, tracks.Count - 1);

            foreach (var track in tracks)
                track.Value.transform.SetSiblingIndex(track.Value.Order);
        }
        
        internal void AddContent(Content content) => tracks[new(content.Type, content.TypeIndex)].Add(content);

        internal void RemoveContent(Content content) => tracks[new(content.Type, content.TypeIndex)].Remove(content);

        internal void SortTrackContent(int type, int typeIndex) =>  tracks[new(type, typeIndex)].SortContent();

        internal void SortAllTrackContent()
        {
            foreach (var track in tracks)
            {
                track.Value.SortContent();
            }
        }

        internal bool ContainsContentAtTimeInType<T>(T type, QNT_Timestamp timestamp)
            => ContainsContentAtTimeInType((int)(object)type, timestamp);

        internal bool ContainsContentAtTimeInType(int type, QNT_Timestamp timestamp)
        {
            foreach (var track in tracks)
            {
                if (track.Key.type != type)
                    continue;

                if (track.Value.ContainsContentAtTime(timestamp))
                    return true;
            }

            return false;
        }

        internal bool ContainsContentAtTimeInType(int type, Timeframe timeframe)
        {
            foreach (var track in tracks)
            {
                if (track.Key.type != type)
                    continue;

                if (track.Value.ContainsContentAtTime(timeframe))
                    return true;
            }

            return false;
        }

        internal bool ContainsContentAtTimeInType(Content content, int type, Timeframe timeframe)
        {
            foreach (var track in tracks)
            {
                if (track.Key.type != type)
                    continue;

                if (track.Value.ContainsContentAtTime(content, timeframe))
                    return true;
            }

            return false;
        }

        internal bool ContainsContentAtTime(TrackID id, QNT_Timestamp time)
            => tracks[id].ContainsContentAtTime(time);
        internal bool ContainsContentAtTime(Content content, Timeframe timeframe)
            => tracks[new(content.Type, content.TypeIndex)].ContainsContentAtTime(content, timeframe);

        internal bool ContainsContentAtTime<T>(T type, int typeIndex, Timeframe timeframe)
            => tracks[new((int)(object)type, typeIndex)].ContainsContentAtTime(timeframe);

        internal bool ContainsContentAtTime<T>(T type, int typeIndex, QNT_Timestamp time)
            => tracks[new((int)(object)type, typeIndex)].ContainsContentAtTime(time);

        internal bool TryGetContent(int type, int typeIndex, QNT_Timestamp time, out Content content)
            => TryGetContent(new(type, typeIndex), time, out content);
        internal bool TryGetContent(TrackID id, QNT_Timestamp time, out Content content)
            => tracks[id].TryGetContent(time, out content);

        internal bool TryGetContent(int type, int typeIndex, Timeframe timeframe, out Content content)
            => tracks[new(type, typeIndex)].TryGetContent(timeframe, out content);

        internal bool TryGetContent(TrackID id, Timeframe timeframe, out Content content)
            => tracks[id].TryGetContent(timeframe, out content);
        

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

        protected abstract void DeleteContent(Content content);

        internal void RemoveTrack(Track track)
        {
            for (int i = track.Content.Count - 1; i >= 0; i--)
                DeleteContent(track.Content[i]);

            tracks.Remove(track.ID);
            _trackArrangement.Remove(track.ID);


            var removedTrackOrder = track.Order;
            Destroy(track.gameObject);

            foreach (var kvp in tracks)
            {
                if (kvp.Value.Order <= removedTrackOrder)
                    continue;
                
                kvp.Value.SetOrder(kvp.Value.Order - 1);
                kvp.Value.transform.SetSiblingIndex(kvp.Value.Order);
                _trackArrangement.SetOrder(kvp.Value.Type, kvp.Value.TypeIndex, kvp.Value.Order);
            }

            SaveTrackOrder(_trackArrangement.GetTrackOrder());
            
            var oldStart = range.start;
            var oldEnd = range.end;

            if (oldEnd == tracks.Count)
            {
                oldEnd--;
                oldStart--;
            }

            range = new(oldStart, oldEnd, 0, tracks.Count - 1);

            UpdateVisibleTracks();
        }

        internal void AddTrack(int type)
        {
            var track = Instantiate(trackPrefab, trackContainer);


            int desiredOrder = _trackArrangement.GetLastTrackOrderOfType(type) + 1;
            var typeIndex = _trackArrangement.NumTracksOfType(type);
            track.Initialize(type, desiredOrder, typeIndex, this);
            tracks.Add(new(type, typeIndex), track);
            track.transform.SetSiblingIndex(desiredOrder);
            _trackArrangement.Add(new TrackOrder(type, desiredOrder, typeIndex));

            foreach (var t in tracks.Values)
            {
                if(t.Order < desiredOrder || t == track)
                    continue;
                
                t.SetOrder(t.Order + 1);
                t.transform.SetSiblingIndex(t.Order);
                _trackArrangement.SetOrder(t.Type, t.TypeIndex, t.Order);
            }

            SaveTrackOrder(_trackArrangement.GetTrackOrder());
            
            var oldStart = range.start;
            var oldEnd = range.end;
            range = new(oldStart, oldEnd, 0, tracks.Count - 1);
            
            UpdateVisibleTracks();
            track.ToggleEditMode(true);
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

            _trackArrangement.SetOrder(track.Type, track.TypeIndex, track.Order);
            _trackArrangement.SetOrder(track.Type, otherTrack.TypeIndex, otherTrack.Order);
            
            track.transform.SetSiblingIndex(track.Order);

            SaveTrackOrder(_trackArrangement.GetTrackOrder());
        }

        public void UpdateVisibleTracks()
        {
            int trackIndex = 0;
            Transform parent = null;
            foreach(var kvp in tracks.Reverse())
            {
                var track = kvp.Value;
                var contents = track.Content;
                bool inRange = range.IsInRange(track.Order);
                
                track.gameObject.SetActive(inRange);

                if (inRange)
                {
                    trackIndex = track.Order - range.start;
                    parent = timeline.TrackContents[trackIndex].transform;
                    timeline.TrackContents[trackIndex].SetTrack(TimelineType, track);
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

        public Track GetTrack<T>(T type, int typeIndex) 
            => GetTrack((int)(object)type, typeIndex);
        public Track GetTrack(int type, int typeIndex) 
            => tracks.TryGetValue(new(type, typeIndex), out var track) ? track : null;

        public Track GetTrack(TrackID id)
            => tracks.TryGetValue(id, out var track) ? track : null;

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
                start = Mathf.Clamp(start, min, max);
                end = Mathf.Clamp(end, min, max);

                if (min < 0)
                    min = 0;

                if (max < min)
                    max = min;
                
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
