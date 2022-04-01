using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NotReaper
{
    /// <summary>
    /// Responsible for managing the editor's timeline scale.
    /// </summary>
    public static class EditorScale
    {
        /// <summary>
        /// The timeline's default scale.
        /// </summary>
        public const int DefaultScale = 20;
        /// <summary>
        /// The minimum allowed scale.
        /// </summary>
        public const int MinScale = 5;
        /// <summary>
        /// The maximum allowed scale.
        /// </summary>
        public const int MaxScale = 100;
        /// <summary>
        /// The amount to scale notes by.
        /// </summary>
        private const float NoteScaleMultiplier = 1.75f;
        /// <summary>
        /// The current scale of notes.
        /// </summary>
        public static float NoteScale { get; private set; } = .7f;
        /// <summary>
        /// The result of <see cref="DefaultNoteScale"/> / <see cref="NoteScaleMultiplier"/>
        /// </summary>
        private static float NoteScaleAmount => NoteScale / NoteScaleMultiplier;
        /// <summary>
        /// The timeline's scale.
        /// </summary>
        public static int Scale { get; private set; } = DefaultScale;
        /// <summary>
        /// The amount the timeline is scaled by. This is equal to <see cref="Scale"/> / <see cref="DefaultScale"/>
        /// </summary>
        public static float ScaleAmount => (float)Scale / DefaultScale;
        /// <summary>
        /// The amount the timeline is scaled by, inverted. This is equal to <see cref="DefaultScale"/> / <see cref="Scale"/> 
        /// </summary>
        public static float InvertedScaleAmount => (float)DefaultScale / Scale;
        /// <summary>
        /// Raised when <see cref="Scale"/> changes.
        /// </summary>
        public static OnScaleChanged onScaleChanged;
        public delegate void OnScaleChanged(int scale);

        private static Timeline timeline;

        static EditorScale()
        {
            NRSettings.OnLoad(() => timeline = Timeline.Instance);
        }

        /// <summary>
        /// Sets the timeline's scale.
        /// </summary>
        /// <param name="scale">The new scale.</param>
        private static void SetScale(int scale, bool zoom)
        {
            if (scale == Scale || scale < MinScale || scale > MaxScale)
                return;

            if (zoom && !timeline.hover)
                return;

            NoteScale *= (float)scale / Scale;
            Scale = scale;
            onScaleChanged?.Invoke(scale);
        }
        /// <summary>
        /// Applies the current scale again.
        /// </summary>
        public static void ReapplyScale() => onScaleChanged?.Invoke(Scale);

        /// <summary>
        /// Zooms the timeline in by one unit.
        /// </summary>
        public static void ZoomIn() => SetScale(Scale + 1, true);

        /// <summary>
        /// Zooms the timeline out by one unit.
        /// </summary>
        public static void ZoomOut() => SetScale(Scale - 1, true);
        /// <summary>
        /// Zooms the timeline in or out, depending on the zoomIn param.
        /// </summary>
        /// <param name="zoomIn">True for zooming in, false for zooming out.</param>
        public static void Zoom(bool zoomIn) => SetScale(Scale + (zoomIn ? 1 : -1), true);

        /// <summary>
        /// Get the note's X-axis scaled by <see cref="NoteScale"/>
        /// </summary>
        /// <param name="noteScaleX">The note's local X-scale.</param>
        /// <returns>noteScaleX / <see cref="NoteScale">/></returns>
        public static Vector3 GetNoteScale(Vector3 localNoteScale)
            => new Vector3(NoteScaleAmount, localNoteScale.y, localNoteScale.z);
    }
}
