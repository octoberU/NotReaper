using System;
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

        protected override List<TrackOrder> GetSavedTracks()
            => SustainTrackOrder;

        protected override void SaveTrackOrder(List<TrackOrder> trackOrder)
        {
            
        }

        public override void Show(bool show)
        {
            sidebar.SetActive(show);
        }

        private List<TrackOrder> SustainTrackOrder = new()
        {
            new (0, 0),
            new(1, 1),
            new (2,2),
            new (3,3),
            new (4,4),
            new (5,5),
            new (6,6),
            new (7,7),
            new (8,8),
            new (9,9),
            new (10,10),
            new (11,11),
        };
        
        protected override void DeleteContent(Content content)
        {
            throw new Exception("Tried to delete a sustain item by deleting a track - this is not supported and should never happen!");
        }
    }
    
    
    
}
