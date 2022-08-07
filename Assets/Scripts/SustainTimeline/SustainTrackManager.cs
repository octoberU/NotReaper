using System.Collections;
using System.Collections.Generic;
using NotReaper;
using UnityEngine;

namespace NotReaper.SustainTimeline
{
    public class SustainTrackManager : TrackManager
    {
        [SerializeField] private GameObject sidebar;
        protected override TimelineType TimelineType => TimelineType.Sustain;

        protected override SerializableDictionary<int, int> GetSavedTracks()
            => SustainTrackOrder;

        protected override void SaveTrackOrder(SerializableDictionary<int, int> trackOrder)
        {
            
        }

        public override void Show(bool show)
        {
            sidebar.SetActive(show);
        }

        private SerializableDictionary<int, int> SustainTrackOrder = new()
        {
            { 0, 0 },
            { 1, 1 },
            { 2, 2 },
            { 3, 3 },
            { 4, 4 },
            { 5, 5 },
            { 6, 6 },
            { 7, 7 },
            { 8, 8 },
            { 9, 9 },
            { 10, 10 },
            { 11, 11 }
        };
    }
    
    
    
}
