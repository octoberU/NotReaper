using NotReaper.Models;
using NotReaper.Timing;
using NotReaper.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NotReaper
{
    /// <summary>
    /// Responsible for managing the editor's time.
    /// </summary>
    public static class EditorTime
    {
        /// <summary>
        /// The current time in the song.
        /// </summary>
        public static QNT_Timestamp Time { get; private set; } = new(0);

        /// <summary>
        /// The current time in the song, snapped to the currently selected beat snap.
        /// </summary>
        public static QNT_Timestamp SnappedTime => GetSnappedTime(Time, EditorBeatSnap.BeatSnap);
        /// <summary>
        /// The current time in the song in seconds.
        /// </summary>
        public static float Seconds => Time.ToSeconds();

        /// <summary>
        /// Raised when <see cref="Time"/> changes.
        /// </summary>
        public static OnTimeChanged onTimeChanged;
        public delegate void OnTimeChanged(QNT_Timestamp time);

        /// <summary>
        /// Sets the current time of the song.
        /// </summary>
        /// <param name="time">The new time.</param>
        public static void SetTime(QNT_Timestamp time)
        {
            if (Time != time)
            {
                Time = time;
                onTimeChanged?.Invoke(time);
            }
        }
        /// <summary>
        /// Sets the current time of the song.
        /// </summary>
        /// <param name="time">The new time.</param>
        public static void SetTime(float time) => SetTime(new QNT_Timestamp((uint)time));
        /// <summary>
        /// Sets the current time of the song.
        /// </summary>
        /// <param name="time">The new time.</param>
        public static void SetTime(uint time) => SetTime(new QNT_Timestamp(time));
        /// <summary>
        /// Sets the current time of the song.
        /// </summary>
        /// <param name="time">The new time.</param>
        public static void SetTime(ulong time) => SetTime(new QNT_Timestamp(time));

        /// <summary>
        /// Gets the time value snapped to the supplied beatsnap.
        /// </summary>
        /// <param name="time">The time to snap.</param>
        /// <param name="snap">The beat snap to snap time to.</param>
        /// <returns>Snapped time value.</returns>
        public static QNT_Timestamp GetSnappedTime(QNT_Timestamp time, int snap)
        {
            var snappedTime = time;
            uint beatSnap = (uint)snap;
            int tempoIndex = BinarySearch.GetCurrentBPMIndex(snappedTime);
            if (tempoIndex == -1)
            {
                snappedTime = new QNT_Timestamp(snappedTime.tick + Timeline.Instance.bpmDragOffset.tick);
                return QNT_Timestamp.GetSnappedValue(snappedTime, beatSnap);
            }
            TempoChange currentTempo = EditorTempo.TempoChanges[tempoIndex];
            QNT_Duration offsetFromTempoChange = new QNT_Duration(snappedTime.tick - currentTempo.time.tick);
            offsetFromTempoChange = QNT_Duration.GetSnappedValue(offsetFromTempoChange, beatSnap);
            return new QNT_Timestamp(currentTempo.time.tick + offsetFromTempoChange.tick + Timeline.Instance.bpmDragOffset.tick);           
        }
        /// <summary>
        /// Gets the time value snapped to the supplied beatsnap.
        /// </summary>
        /// <param name="time">The time to snap.</param>
        /// <param name="snap">The beat snap to snap time to.</param>
        /// <returns>Snapped time value.</returns>
        public static QNT_Timestamp GetSnappedTime(QNT_Timestamp time, uint snap) => GetSnappedTime((QNT_Timestamp)time, (int)snap);
    }
}
