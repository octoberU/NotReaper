using UnityEngine;
using NotReaper.Timing;
using System.Collections.Generic;

namespace NotReaper.Targets {
	public class TargetGridMoveIntent {
		public TargetGridMoveIntent() {}
		
		public TargetGridMoveIntent(TargetGridMoveIntent other) {
			target = other.target;
			startingPosition = other.startingPosition;
			intendedPosition = other.intendedPosition;
		}

		public TargetData target;
		public Vector2 startingPosition;
		public Vector2 intendedPosition;
		public Vector2 orientation = Vector2.one;
		public bool hasPerformedUndo = false;
	}

	public class TargetTimelineMoveIntent {
		public TargetTimelineMoveIntent() {}

		public TargetTimelineMoveIntent(TargetTimelineMoveIntent other) 
		{
			startTick = other.startTick;
			intendedTick = other.intendedTick;    
            targetData = other.targetData;
		}

        //These are the only data needed to be filled out. The rest will be calculated by `MoveTimelineTargets`
        public TargetData targetData;
		public QNT_Timestamp startTick;
		public QNT_Timestamp intendedTick;
    }
}