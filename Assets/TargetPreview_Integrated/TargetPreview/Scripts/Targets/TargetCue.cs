using System;
using AudicaTools;
using TargetPreview.Math;
using TargetPreview.Targets;

namespace TargetPreview.Scripts.Targets
{
    public struct TargetCue : IEquatable<TargetCue>
    {
        public int tick, tickLength, pitch, velocity;
        public float xOffset, yOffset, zOffset, timeMs, timeEndMs;
        public TargetHandType handType;
        public TargetBehavior behavior;
        public TargetCue[] children;

        public TargetCue(int tick, int tickLength, int pitch, int velocity, float xOffset, float yOffset, float zOffset, TargetHandType handType, TargetBehavior behavior, float timeMs, float timeEndMs, TargetCue[] children)
        {
            this.tick = tick;
            this.tickLength = tickLength;
            this.pitch = pitch;
            this.velocity = velocity;
            this.xOffset = xOffset;
            this.yOffset = yOffset;
            this.zOffset = zOffset;
            this.handType = handType;
            this.behavior = behavior;
            this.timeMs = timeMs;
            this.timeEndMs = timeEndMs;
            this.children = children;
        }

        public static implicit operator TargetData(TargetCue targetCue) =>
            new TargetData()
            {
                time = targetCue.timeMs,
                transformData = TargetTransform.CalculateTargetTransform(targetCue.pitch, (targetCue.xOffset, targetCue.yOffset, targetCue.zOffset)),
                handType = targetCue.handType,
                behavior = targetCue.behavior,
                cue = targetCue
            };

        public static implicit operator TargetCue(Cue cue)
            => new TargetCue(tick: cue.tick, tickLength: cue.tickLength, pitch: cue.pitch, velocity: cue.velocity,
                xOffset: cue.gridOffset.x, yOffset: cue.gridOffset.y, zOffset: cue.zOffset,
                handType: (TargetHandType)cue.handType, behavior: (TargetBehavior)cue.behavior,
                timeMs: cue.GetMsTime(), cue.GetEndMsTime(), new TargetCue[0]);
        
        public static bool operator ==(TargetCue a, TargetCue b) => a.Equals(b);

        public static bool operator !=(TargetCue a, TargetCue b) =>
            !(a == b);


        public bool Equals(TargetCue other) =>
            tick == other.tick && 
            tickLength == other.tickLength &&
            pitch == other.pitch &&
            velocity == other.velocity &&
            xOffset.Equals(other.xOffset) &&
            yOffset.Equals(other.yOffset) &&
            zOffset.Equals(other.zOffset) &&
            timeMs.Equals(other.timeMs) &&
            handType == other.handType &&
            behavior == other.behavior;

        public override bool Equals(object obj) =>
            obj is TargetCue other && Equals(other);

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(tick);
            hashCode.Add(tickLength);
            hashCode.Add(pitch);
            hashCode.Add(velocity);
            hashCode.Add(xOffset);
            hashCode.Add(yOffset);
            hashCode.Add(zOffset);
            hashCode.Add(timeMs);
            hashCode.Add((int)handType);
            hashCode.Add((int)behavior);
            return hashCode.ToHashCode();
        }
    }
}