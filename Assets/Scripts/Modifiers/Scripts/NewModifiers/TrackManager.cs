using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Keybinds;
using NotReaper.Modifier;
using UnityEngine;
using UnityEngine.Rendering;

namespace NotReaper.Modifiers
{
    public class TrackManager : MonoBehaviour
    {
        [SerializeField] private GameObject sidebar;
        [SerializeField] private Transform trackContainer;
        [SerializeField] private Track trackPrefab;

        [SerializeField, Tooltip("The amount of tracks that we can display at once")]
        private int capacity = 10;

        private Dictionary<int, Track> tracks = new();

        private bool isPrivateBuild = false;
        private SlidingRange range;

        private void Start()
        {
            NRSettings.OnLoad(CreateTracks);
        }

        private void CreateTracks()
        {
            var savedTracks = NRSettings.config.modifierTrackOrder;
            var i = 0;
            foreach (var kvp in savedTracks)
            {
                var type = (ModifierHandler.ModifierType)kvp.Value;
                if (IsPrivateModifer(type)) continue;
                var track = Instantiate(trackPrefab, trackContainer);
                track.SetTrackType(type);
                tracks.Add(kvp.Key, track);

                if (i >= capacity)
                    track.gameObject.SetActive(false);

                i++;
            }

            range = new(0, capacity - 1, 0, tracks.Count);
        }

        private bool IsPrivateModifer(ModifierHandler.ModifierType type)
            => type is ModifierHandler.ModifierType.ArenaPosition or ModifierHandler.ModifierType.ArenaScale
                or ModifierHandler.ModifierType.ArenaSpin;

        public void Show(bool show)
        {
            sidebar.SetActive(show);
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
        }

        private void UpdateVisibleTracks()
        {
            for (var i = 0; i < tracks.Count; i++)
            {
                tracks[i].gameObject.SetActive(i >= range.start && i <= range.end);
            }
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.DownArrow))
                ScrollDown();
            
            if(Input.GetKeyDown(KeyCode.UpArrow))
                ScrollUp();
        }

        public void ScrollUp()
        {
            range.Up();
            UpdateVisibleTracks();
        }

        public void ScrollDown()
        {
            range.Down();
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

            public void Up()
            {
                if (end == max) return;

                start++;
                end++;
            }

            public void Down()
            {
                if (start == min) return;

                start--;
                end--;
            }
        }
    }
}