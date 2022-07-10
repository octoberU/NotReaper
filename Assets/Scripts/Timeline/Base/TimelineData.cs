using NotReaper.Timing;
using UnityEngine;

namespace NotReaper
{
    
    public struct Timeframe
    {
        public ulong Start { get; }
        public ulong End { get; }
        public QNT_Timestamp StartTime { get; }
        public QNT_Timestamp EndTime { get; }
        public QNT_Duration Duration { get; }

        public Timeframe(QNT_Timestamp start, QNT_Timestamp end)
        {
            Start = start.tick; 
            End = end.tick;
            StartTime = start;
            EndTime = end;
            Duration = new(End - Start);
        }

        public Timeframe(int start, int end)
        {
            Start = (ulong)start;
            End = (ulong)end;
            StartTime = new(Start);
            EndTime = new(End);
            Duration = new(End - Start);
        }

        public bool Contains(QNT_Timestamp time) => Contains(time.tick);

        public bool Contains(ulong time)
            => time == Start || time == End || (time > Start && time < End);

        public bool Contains(Timeframe other)
            => (other.Start <= Start && other.End >= Start) || (other.Start <= End && other.End >= End) || (other.Start >= Start && other.End <= End);
        

        public bool Contains(Bounds bounds)
            => Contains(new Timeframe((int)QNT_Duration.FromBeatTime(bounds.min.x).tick, (int)QNT_Duration.FromBeatTime(bounds.max.x).tick));

        public bool IsInside(Timeframe other)
            => Start >= other.Start && End <= other.End;
        
        public static bool operator == (Timeframe timeframe, Timeframe otherTimeframe)
            => timeframe.Start == otherTimeframe.Start && timeframe.End == otherTimeframe.End;

        public static bool operator !=(Timeframe timeframe, Timeframe otherTimeframe)
            => timeframe.Start != otherTimeframe.Start || timeframe.End != otherTimeframe.End;
    }
    
    public enum TimelineType
    {
        Modifier,
        Hitsound
    }
}
