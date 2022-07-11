using NotReaper.Models;
using UnityEngine;

namespace NotReaper.Targets {
	public class TargetSetHitsoundIntent {
		public TargetSetHitsoundIntent() {}

		public TargetSetHitsoundIntent(TargetSetHitsoundIntent other) {
			target = other.target;
			startingVelocity = other.startingVelocity;
			newVelocity = other.newVelocity;
		}

		public TargetSetHitsoundIntent(Target target, InternalTargetVelocity startingVelocity, InternalTargetVelocity newVelocity)
		{
			this.target = target;
			this.startingVelocity = startingVelocity;
			this.newVelocity = newVelocity;
		}

		//public TargetData target;
		public Target target;
		public InternalTargetVelocity startingVelocity;
		public InternalTargetVelocity newVelocity;
	}
}