using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifiers;
using UnityEngine;

namespace NotReaper
{
    public class TrackContent : MonoBehaviour
    {
        internal Dictionary<TimelineType, Track> tracks { get; } = new();
        private Bounds bounds;

        public ModifierType modifierType;

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

            if (TimelineType.Modifier == type)
                modifierType = (ModifierType)track.Type;
        }

        internal bool ContainsPoint(Vector2 point) => bounds.Contains(point);
    }
}
