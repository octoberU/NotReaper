using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifiers;
using NotReaper.Timing;
using UnityEngine;

namespace NotReaper.SustainTimeline
{
    public class SustainTimelineManager : TimelineManager<SustainData>
    {
        // Start is called before the first frame update
        protected override TimelineType TimelineType => TimelineType.Sustain;
        protected override GridTimeline.WidthType TimelineWidthType => GridTimeline.WidthType.Full;

        [NRInject] private SustainInputManager inputManager;
        
        protected override void Show(bool show)
        {
            if (show) inputManager.Activate();
            else inputManager.Deactivate();
            
            base.Show(show);
        }

        protected override void AddContentAction(QNT_Timestamp startTime, TrackContent trackContent)
        {
            SustainUndoRedo.AddAction(new AddContentAction<SustainData>(startTime, trackContent.tracks[TimelineType]));
        }

        protected override bool CheckAlwaysMoveRequirements(Content content) => false;

        protected override void RemoveContentAction(Content content)
            => SustainUndoRedo.AddAction(new RemoveSustainAction(content));

        protected override void MultiRemoveContentAction(List<Content> content)
            => SustainUndoRedo.AddAction(new MultiRemoveSustainAction(content));

        protected override void MultiAddContentAction(List<SustainData> content)
            => SustainUndoRedo.AddAction(new MultiAddSustainAction(content));

        protected override void MoveContentAction(List<MoveData> moveData)
            => SustainUndoRedo.AddAction(new TimelineMoveAction<SustainData>(moveData));

        protected override bool CheckSpecialPlaceRequirements(QNT_Timestamp startTime, TrackContent content) => true;

        protected override bool CanSwitchTrack(Content content, int currentTrack, int nextTrack) => true;

        public SustainMarker LoadSustainMarker(SustainData data)
        {
            var sustain = base.LoadContent((int)data.type) as SustainMarker;
            sustain.LoadData(data);
            if (!IsLoadingContent)
            {
                tracks.SortTrackContent((int)data.type);
            }
            return sustain;
        }
    }
}
