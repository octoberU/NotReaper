using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifier;
using UnityEngine;

namespace NotReaper
{
    public class TrackContent : MonoBehaviour
    {
        internal Dictionary<TimelineType, Track> tracks { get; } = new();
        private Bounds bounds;

        private void Start()
        {
            bounds = GetComponent<BoxCollider2D>().bounds;
            var extents = bounds.extents;
            extents.z = float.PositiveInfinity;
            bounds.extents = extents;
        }

        internal void SetTrack(TimelineType type, Track track)
        {
            if (!tracks.ContainsKey(type))
            {
                tracks.Add(type, track);
            }
            else
            {
                tracks[type] = track;
            }
        }

        internal bool ContainsPoint(Vector2 point) => bounds.Contains(point);
    }
}
