using Michsky.UI.ModernUIPack;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace NotReaper
{
    /// <summary>
    /// Responsible for managing the Editor's beat snap.
    /// </summary>
    public class EditorBeatSnap : MonoBehaviour
    {
        /// <summary>
        /// The editor's selected beatsnap.
        /// </summary>
        public static int BeatSnap { get; private set; } = 4;
        /// <summary>
        /// The duration of the currently selected beatnsp <see cref="BeatSnap"/>.
        /// </summary>
        public static QNT_Duration Duration => Constants.DurationFromBeatSnap((uint)BeatSnap);

        /// <summary>
        /// Raised when <see cref="BeatSnap"/> changes.
        /// </summary>
        public static OnSnapChanged onBeatSnapChanged;
        public delegate void OnSnapChanged(int snap, bool next);

        /// <summary>
        /// Sets the editor's current beat snap.
        /// </summary>
        /// <param name="snap">The new snap.</param>
        private static void SetBeatSnap(int snap, bool next)
        {
            if (BeatSnap != snap)
            {
                BeatSnap = snap;
                onBeatSnapChanged?.Invoke(snap, next);
            }
        }
        /// <summary>
        /// Advances to the next beat snap.
        /// </summary>
        public static void NextBeatSnap()
        {
            var snaps = NRSettings.config.snaps;
            
            for(int i = 0; i < snaps.Count; i++)
            {
                var snap = snaps[i];
                int.TryParse(snap.Substring(2), out int parsed);
                if(BeatSnap == parsed)
                {
                    if(i + 1 >= snaps.Count)
                    {
                        int.TryParse(snaps[0].Substring(2), out int firstSnap);
                        SetBeatSnap(firstSnap, true);
                    }
                    else
                    {
                        int.TryParse(snaps[i + 1].Substring(2), out int nextSnap);
                        SetBeatSnap(nextSnap, true);
                    }
                    break;
                }
            }
        }
        /// <summary>
        /// Reverses to the previous beat snap.
        /// </summary>
        public static void PreviousBeatSnap()
        {
            var snaps = NRSettings.config.snaps;

            for (int i = 0; i < snaps.Count; i++)
            {
                var snap = snaps[i];
                int.TryParse(snap.Substring(2), out int parsed);
                if (BeatSnap == parsed)
                {
                    if (i - 1 < 0)
                    {
                        int.TryParse(snaps.Last().Substring(2), out int lastSnap);
                        SetBeatSnap(lastSnap, false);
                    }
                    else
                    {
                        int.TryParse(snaps[i - 1].Substring(2), out int previousSnap);
                        SetBeatSnap(previousSnap, false);
                    }
                    break;
                }
            }
        }
    }
}
