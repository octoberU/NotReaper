using System;
using System.Collections.Generic;
using NotReaper.Timing;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class TrackManager : MonoBehaviour
    {
        [SerializeField] private GameObject sidebar;
        [SerializeField] private ModifierInputPanel inputPanel;
        [SerializeField] private Transform trackContainer;
        [SerializeField] private Track trackPrefab;

        [SerializeField, Tooltip("The amount of tracks that we can display at once")]
        private int capacity = 10;

        [NRInject] private ModifierTimeline timeline;

        private Dictionary<int, Track> tracks = new();

        //private Dictionary<ulong, Dictionary<ModifierType, Modifier>> occupiedTime = new();

        private bool isPrivateBuild = false;
        private SlidingRange range;

        private bool isInitialized = false;

        private List<Modifier> awaitOccupy = new List<Modifier>();

        public int CurrentIndex => range.start;

        private void Start()
        {
            NRSettings.OnLoad(CreateTracks);
            EditorFile.onAudicaFileLoaded += (_) => InitializeOccupiedTime();
            EditorState.OnEditorReset += OnReset;
        }

        private void OnReset()
        {
            foreach (var kvp in tracks)
            {
                kvp.Value.OnReset();
            }
        }

        private void InitializeOccupiedTime()
        {
            var trackLength = QNT_Timestamp.ShiftTick(Timeline.Instance.songPlayback.song.Length).tick;

            /*for (ulong i = 0; i < trackLength; i++)
            {
                occupiedTime.Add(i, new Dictionary<ModifierType, Modifier>());
                for (int j = 0; j < Enum.GetNames(typeof(ModifierType)).Length; j++)
                    occupiedTime[i].Add((ModifierType)j, null);
            }

            isInitialized = true;

            foreach (var modifier in awaitOccupy)
            {
                OccupyTime(modifier, modifier.timeframe);
            }
            awaitOccupy.Clear();*/
        }

        private void CreateTracks()
        {
            var savedTracks = NRSettings.config.modifierTrackOrder;
            var i = 0;
            foreach (var kvp in savedTracks)
            {
                var type = (ModifierType)kvp.Value;
                if (IsPrivateModifer(type)) continue;
                var track = Instantiate(trackPrefab, trackContainer);
                track.Initialize(type, kvp.Key, this);
                tracks.Add(kvp.Key, track);
                if (i >= capacity)
                    track.gameObject.SetActive(false);
                else
                    timeline.TrackContents[kvp.Key].track = track;


                i++;
            }

            range = new(0, capacity - 1, 0, tracks.Count - 1);
        }

        private bool IsPrivateModifer(ModifierType type) => type is ModifierType.ArenaPosition or ModifierType.ArenaScale or ModifierType.ArenaSpin;

        public void Show(bool show)
        {
            sidebar.SetActive(show);
            inputPanel.gameObject.SetActive(show);
        }

        internal void AddModifier(Modifier modifier)
        {
            tracks[(int)modifier.Type].AddModifier(modifier);
            //OccupyTime(modifier, modifier.timeframe);
            //modifier.onTimeChanged += OnModifierTimeChanged;
        }

        private void OccupyTime(Modifier modifier, Timeframe timeframe)
        {
            if (!isInitialized)
            {
                awaitOccupy.Add(modifier);
                return;
            }
            
            var start = timeframe.Start;
            var end = timeframe.End;
            var type = modifier.Type;
            for (ulong i = start; i <= end; i++)
            {
                //occupiedTime[i][type] = modifier;
            }
        }

        private void FreeTime(Modifier modifier, Timeframe timeframe)
        {
            var start = timeframe.Start;
            var end = timeframe.End;
            var type = modifier.Type;
           // for (ulong i = start; i <= end; i++)
                //occupiedTime[i][type] = null;
        }

        internal void RemoveModifier(Modifier modifier)
        {
            tracks[(int)modifier.Type].RemoveModifier(modifier);
            //FreeTime(modifier, modifier.timeframe);
            //modifier.onTimeChanged -= OnModifierTimeChanged;
        }
        
        private void OnModifierTimeChanged(Modifier modifier, Timeframe oldTime, Timeframe newTime)
        {
            //FreeTime(modifier, oldTime);
            //OccupyTime(modifier, newTime);
        }


        //internal bool ContainsModifierAtTime(Modifier modifier)
         //   => ContainsTimeframe(modifier, modifier.timeframe);

        //internal bool ContainsModifierAtTime(Modifier modifier, Timeframe timeframe)
        //    => ContainsTimeframe(modifier, timeframe);

        //internal bool ContainsModifierAtTime(ModifierType type, QNT_Timestamp time)
        //    => occupiedTime[time.tick][type] != null;

        internal bool ContainsModifierAtTime(Modifier modifier, Timeframe timeframe)
            => tracks[(int)modifier.Type].ContainsModifierAtTime(modifier, timeframe);

        internal bool ContainsModifierAtTime(ModifierType type, QNT_Timestamp time)
            => tracks[(int)type].ContainsModifierAtTime(time);

        internal bool TryGetModifier(ModifierType type, QNT_Timestamp time, out Modifier modifier)
        {
            return tracks[(int)type].TryGetModifier(time, out modifier);
               
            /*if (occupiedTime[time.tick].ContainsKey(type) && occupiedTime[time.tick][type] != null)
            {
                modifier = occupiedTime[time.tick][type];
                return true;
            }

            return false;*/
        }

        /*private bool ContainsTimeframe(Modifier modifier, Timeframe timeframe)
        {
            var type = modifier.Type;
            var start = timeframe.Start;
            var end = timeframe.End;
            for (var i = start; i <= end; i++)
            {
                var m = occupiedTime[i][type];
                if (m != null && m != modifier)
                    return true;
            }

            return false;
        }*/

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
            track.transform.SetSiblingIndex(track.Order);
        }

        private void UpdateVisibleTracks()
        {
            int trackIndex = 0;
            Transform parent = null;
            for (var i = 0; i < tracks.Count; i++)
            {
                var modifiers = tracks[i].Modifiers;
                bool inRange = range.IsInRange(i);
                
                tracks[i].gameObject.SetActive(inRange);

                if (inRange)
                {
                    parent = timeline.TrackContents[trackIndex].transform;
                    timeline.TrackContents[trackIndex].track = tracks[i];
                    trackIndex++;
                }

                foreach (var modifier in modifiers)
                {
                    //modifier.gameObject.SetActive(inRange);
                    modifier.Show(inRange);
                    //modifier.transform.SetParent(parent, false);
                    if (inRange)
                    {
                        var pos = modifier.transform.position;
                        pos.y = parent.position.y - .2f;
                        modifier.transform.position = pos;
                    }
                }
            }
        }

        public Track GetTrack(ModifierType type) => tracks[(int)type];
        public Track GetTrack(int type) => tracks[type];

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.DownArrow))
                ScrollDown();

            if (Input.GetKeyDown(KeyCode.UpArrow))
                ScrollUp();
        }

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