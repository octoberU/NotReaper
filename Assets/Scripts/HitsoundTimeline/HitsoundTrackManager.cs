using System.Collections;
using System.Collections.Generic;
using NotReaper.Models;
using NotReaper.Modifiers;
using NotReaper.Targets;
using NotReaper.Timing;
using UnityEngine;

namespace NotReaper.HitsoundTimeline
{
    public class HitsoundTrackManager : TrackManager
    {
        [SerializeField] private GameObject sidebar;

        protected override TimelineType TimelineType => TimelineType.Hitsound;

        protected override List<TrackOrder> GetSavedTracks()
            => NRSettings.config.hitsoundTrackOrder;

        protected override void SaveTrackOrder(List<TrackOrder> trackOrder)
        {
        }

        public bool ContainsContentAtTime(QNT_Timestamp time, bool isMelee)
        {
            foreach (var track in tracks)
            {
                if (((TimelineHitsound)track.Value.Type).IsMelee() != isMelee) continue;
                return track.Value.ContainsContentAtTime(time);
            }

            return false;
        }

        public bool TryGetContent(QNT_Timestamp time, bool isMelee, HitsoundMarker excludeMarker, out HitsoundMarker marker)
        {
            foreach (var track in tracks)
            {
                if (((TimelineHitsound)track.Key).IsMelee() != isMelee) continue;
                if (track.Value.TryGetContent(time, excludeMarker, out var content))
                {
                    marker = content as HitsoundMarker;
                    return true;
                }
            }

            marker = null;
            return false;
        }

        public bool TryGetContent(TargetData data, out HitsoundMarker marker)
        {
            bool isMelee = data.velocity.ToTimelineHitsound(data.behavior is TargetBehavior.Melee).IsMelee();
            QNT_Timestamp time = data.time;
            foreach (var track in tracks)
            {
                if (((TimelineHitsound)track.Key).IsMelee() != isMelee) continue;
                var hitsoundTrack = track.Value as HitsoundTrack;
                if (hitsoundTrack.TryGetContent(time, data, out var content))
                {
                    marker = content;
                    return true;
                }
            }

            marker = null;
            return false;
        }

        public override void Show(bool show)
        {
            sidebar.SetActive(show);
        }
    }
}
