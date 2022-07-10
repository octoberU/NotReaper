using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.HitsoundTimeline
{
    public class HitsoundMoveAction : TimelineMoveAction<HitsoundData>
    {
        public HitsoundMoveAction(List<MoveData> moveData) : base(moveData)
        {
        }

        public override void DoAction(TimelineManager<HitsoundData> manager)
        {
            foreach (var data in moveData)
            {
                var hitData = data.content.GetData() as HitsoundData;
                hitData.targetData.velocity = ((TimelineHitsound)data.newTrack).ToInternalVelocity();
            }
        }

        public override void UndoAction(TimelineManager<HitsoundData> manager)
        {
            foreach (var data in moveData)
            {
                var hitData = data.content.GetData() as HitsoundData;
                hitData.targetData.velocity = ((TimelineHitsound)data.oldTrack).ToInternalVelocity();
            }
        }
    }
}
