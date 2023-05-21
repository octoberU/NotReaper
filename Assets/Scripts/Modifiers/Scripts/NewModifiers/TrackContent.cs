using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifiers;
using UnityEngine;

namespace NotReaper
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class TrackContent : MonoBehaviour
    {
        internal Dictionary<TimelineType, Track> tracks { get; } = new();
        private Bounds _bounds;

        private BoxCollider2D _collider;

        private float _defaultSize;

        private bool _initialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized)
                return;
            
            _collider = GetComponent<BoxCollider2D>();
            _bounds = _collider.bounds;
            var extents = _bounds.extents;
            extents.z = float.PositiveInfinity;
            _bounds.extents = extents;
            _defaultSize = _collider.size.x;
            
            _initialized = true;
        }

        internal void SetTrack(TimelineType type, Track track)
            => tracks[type] = track;
        

        internal bool ContainsPoint(Vector2 point) => _bounds.Contains(point);

        internal void OnScaleChanged(float scale)
        {
            Initialize();
            
            var size = _collider.size;
            size.x = _defaultSize * EditorScale.ScaleAmount;
            _collider.size = size;
        }
    }
}
