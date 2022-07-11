using NotReaper.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudicaTools;
using NAudio.Midi;
using NotReaper.Models;
using UnityEngine;

namespace NotReaper.Timing {
    class Constants {
        public static UInt64 PulsesPerQuarterNote = 480;
        public static UInt64 PulsesPerWholeNote = PulsesPerQuarterNote * 4;
        public static QNT_Duration QuarterNoteDuration = new QNT_Duration(PulsesPerQuarterNote);
        public static QNT_Duration EighthNoteDuration = new QNT_Duration(PulsesPerQuarterNote / 2);
        public static QNT_Duration SixteenthNoteDuration = new QNT_Duration(PulsesPerQuarterNote / 4);

        public static UInt64 OneMinuteInMicroseconds = 60000000;
        public static UInt64 SecondsToMicroseconds = 1000000;

        public static double GetBPMFromMicrosecondsPerQuaterNote(UInt64 microsecondsPerQuarterNote) {
            return (double)OneMinuteInMicroseconds / microsecondsPerQuarterNote;
        }

        public static string DisplayBPMFromMicrosecondsPerQuaterNote(UInt64 microsecondsPerQuarterNote) {
            double bpm = GetBPMFromMicrosecondsPerQuaterNote(microsecondsPerQuarterNote);
            
            //If we get the same value from the rounded version, then precision is throwing us off, and we should just round it to a whole number
            if(GetBPMFromMicrosecondsPerQuaterNote(MicrosecondsPerQuarterNoteFromBPM(Math.Round(bpm))) == bpm) {
                return ((uint)Math.Round(bpm)).ToString();
            }

            return String.Format("{0:0.###}", bpm);
        }

        public static UInt64 MicrosecondsPerQuarterNoteFromBPM(double bpm) {
            if(bpm == 0) { return 0; }
            return (UInt64)Math.Round((double)OneMinuteInMicroseconds / bpm);
        }

        public static QNT_Duration DurationFromBeatSnap(uint beatSnap) {
            QNT_Duration duration = Constants.QuarterNoteDuration;

            //Since we use PPQN, anything a quarter note or smaller is a division of the time
			if(beatSnap >= 4) {
                duration.tick = (UInt64)(duration.tick / (beatSnap / 4.0f));
			}
			//Otherwise, it is a multiplication
			else {
				duration.tick = (UInt64)((4.0f / beatSnap) * duration.tick);
			}

            return duration;
        }
    }

    class Conversion {
       public static Relative_QNT ToQNT(float seconds, UInt64 microsecondsPerQuarterNote) {
           return new Relative_QNT((long)Mathf.Round((float)(Constants.SecondsToMicroseconds * seconds) / microsecondsPerQuarterNote * Constants.PulsesPerQuarterNote));
       }

       public static double FromQNT(Relative_QNT duration, UInt64 microsecondsPerQuarterNote) {
        return (duration.tick * (long)microsecondsPerQuarterNote) / ((double)Constants.SecondsToMicroseconds * Constants.PulsesPerQuarterNote);
       }
    };

    /// <summary>
    /// QNT (or quarter note ticks), represents a value in pulses per quarter note
    /// This value represents a point in time in the current song
    /// </summary>
    [Serializable]
    public struct QNT_Timestamp {
        public QNT_Timestamp(UInt64 tick) {
            this.tick = tick;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string ToString() {
            return tick.ToString();
        }

        [SerializeField]
        public UInt64 tick;

        public static QNT_Timestamp GetSnappedValue(QNT_Timestamp time, uint beatSnap) {
            QNT_Duration snapValue = Constants.DurationFromBeatSnap(beatSnap);
			return (time / snapValue) * snapValue;
        }

        public float ToBeatTime() {
            return tick / (float)Constants.PulsesPerQuarterNote;
        }

        public int CompareTo(QNT_Timestamp other) {
            return tick.CompareTo(other.tick);
        }

        public static bool operator ==(QNT_Timestamp a, QNT_Timestamp b) {
            return a.tick == b.tick;
        }

        public static bool operator !=(QNT_Timestamp a, QNT_Timestamp b) {
            return a.tick != b.tick;
        }

        public static bool operator <(QNT_Timestamp a, QNT_Timestamp b) {
            return a.tick < b.tick;
        }
        public static bool operator >(QNT_Timestamp a, QNT_Timestamp b) {
            return a.tick > b.tick;
        }
        public static bool operator <=(QNT_Timestamp a, QNT_Timestamp b) {
            return a.tick <= b.tick;
        }
        public static bool operator >=(QNT_Timestamp a, QNT_Timestamp b) {
            return a.tick >= b.tick;
        }

        public static QNT_Timestamp operator +(QNT_Timestamp a, Relative_QNT b) {
            Int64 result = (Int64)a.tick + b.tick;
            if(result < 0) {
                result = 0;
            }

            return new QNT_Timestamp((UInt64)result);
        }

        public static QNT_Timestamp operator -(QNT_Timestamp a, Relative_QNT b) {
            Int64 result = (Int64)a.tick - b.tick;
            if(result < 0) {
                result = 0;
            }

            return new QNT_Timestamp((UInt64)result);
        }

        public static Relative_QNT operator -(QNT_Timestamp a, QNT_Timestamp b) {
            return new Relative_QNT((Int64)a.tick - (Int64)b.tick);
        }

        public static QNT_Timestamp operator +(QNT_Timestamp a, QNT_Duration b) {
            return new QNT_Timestamp(a.tick + b.tick);
        }

        public static QNT_Timestamp operator -(QNT_Timestamp a, QNT_Duration b) {
            if(b.tick > a.tick) {
                return new QNT_Timestamp(0);
            }

            return new QNT_Timestamp(a.tick - b.tick);
        }

        public static QNT_Timestamp operator *(QNT_Timestamp a, QNT_Duration b) {
            return new QNT_Timestamp(a.tick * b.tick);
        }

        public static QNT_Timestamp operator /(QNT_Timestamp a, QNT_Duration b) {
            return new QNT_Timestamp(a.tick / b.tick);
        }

        public float ToSeconds()
        {
            QNT_Timestamp timestamp = new(tick);
            double duration = 0.0f;
            var tempoChanges = EditorTempo.TempoChanges;
            for (int i = 0; i < tempoChanges.Count; ++i)
            {
                var c = tempoChanges[i];

                if (timestamp >= c.time && (i + 1 >= tempoChanges.Count || timestamp < tempoChanges[i + 1].time))
                {
                    duration += Conversion.FromQNT(timestamp - c.time, c.microsecondsPerQuarterNote);
                    break;
                }
                else if (i + 1 < tempoChanges.Count)
                {
                    duration += Conversion.FromQNT(tempoChanges[i + 1].time - c.time, c.microsecondsPerQuarterNote);
                }
            }

            return (float)duration;
        }

        public float ToMs() => ToSeconds() * 1000f;
        /// <summary>
        /// Shifts from tick 0 by `duration` seconds, respecting bpm changes in between
        /// </summary>
        /// <param name="byDuration">The duration in seconds to shift the initial time by</param>
        /// <returns>Tick at duration</returns>
        public static QNT_Timestamp ShiftTick(double byDuration)
            => ShiftTick(new QNT_Timestamp(0), (float)byDuration);
        /// <summary>
        /// Shifts from tick 0 by `duration` seconds, respecting bpm changes in between
        /// </summary>
        /// <param name="byDuration">The duration in seconds to shift the initial time by</param>
        /// <returns>Tick at duration</returns>
        public static QNT_Timestamp ShiftTick(float byDuration)
            => ShiftTick(new QNT_Timestamp(0), byDuration);

        public static QNT_Timestamp ShiftTick(int tick)
            => ShiftTick(new QNT_Timestamp(0), new QNT_Timestamp((ulong)tick).ToSeconds());
        /// <summary>
        /// Shifts `startTime` by `duration` seconds, respecting bpm changes in between
        /// </summary>
        /// <param name="from">The initial time</param>
        /// <param name="byDuration">The duration in seconds to shift the initial time by</param>
        /// <returns>Initial tick shifted by duration</returns>
        public static QNT_Timestamp ShiftTick(int from, float byDuration) 
            => ShiftTick(new QNT_Timestamp((uint) from), byDuration);

        /// <summary>
        /// Shifts `startTime` by `duration` seconds, respecting bpm changes in between
        /// </summary>
        /// <param name="from">The initial time</param>
        /// <param name="byDuration">The duration in seconds to shift the initial time by</param>
        /// <returns>Initial tick shifted by duration</returns>
        public static QNT_Timestamp ShiftTick(int from, double byDuration)
            => ShiftTick(new QNT_Timestamp((uint)from), (float)byDuration);
        /// <summary>
        /// Shifts `startTime` by `duration` seconds, respecting bpm changes in between
        /// </summary>
        /// <param name="from">The initial time</param>
        /// <param name="byDuration">The duration in seconds to shift the initial time by</param>
        /// <returns>Initial tick shifted by duration</returns>
        public static QNT_Timestamp ShiftTick(QNT_Timestamp from, float byDuration)
        {
            int currentBpmIdx = -1;
            QNT_Timestamp startTime = from;
            var tempoChanges = EditorTempo.TempoChanges;
            //If we're stating at the beginning, jump to the nearest tempo marker
            if (startTime.tick == 0)
            {
                if (byDuration < 0)
                {
                    return new QNT_Timestamp(0);
                }

                var res = BinarySearch.SearchBPMIndex(byDuration);
                currentBpmIdx = res.index;
                while (currentBpmIdx > 0 && tempoChanges[currentBpmIdx].secondsFromStart > byDuration)
                {
                    --currentBpmIdx;
                }

                startTime = tempoChanges[currentBpmIdx].time;
                byDuration -= tempoChanges[currentBpmIdx].secondsFromStart;
            }
            else
            {
                currentBpmIdx = BinarySearch.GetCurrentBPMIndex(startTime);
            }

            if (currentBpmIdx == -1)
            {
                return startTime;
            }

            QNT_Timestamp currentTime = startTime;

            while (byDuration != 0 && currentBpmIdx >= 0 && currentBpmIdx < tempoChanges.Count)
            {
                var tempo = tempoChanges[currentBpmIdx];

                Relative_QNT remainingTime = Conversion.ToQNT(byDuration, tempo.microsecondsPerQuarterNote);
                QNT_Timestamp timeOfNextBPM = new QNT_Timestamp(0);
                int sign = Math.Sign(remainingTime.tick);

                currentBpmIdx += sign;
                if (currentBpmIdx > 0 && currentBpmIdx < tempoChanges.Count)
                {
                    timeOfNextBPM = tempoChanges[currentBpmIdx].time;
                }

                //If there is time to another bpm we need to shift to the next bpm point, then continue
                if (timeOfNextBPM.tick != 0 && timeOfNextBPM < (currentTime + remainingTime))
                {
                    Relative_QNT timeUntilTempoShift = timeOfNextBPM - currentTime;
                    currentTime += timeUntilTempoShift;
                    byDuration -= (float)Conversion.FromQNT(timeUntilTempoShift, tempo.microsecondsPerQuarterNote);
                }
                //No bpm change, apply the time and break
                else
                {
                    currentTime += remainingTime;
                    break;
                }
            }

            return currentTime;
        }
        
        public static float TickToMilliseconds(int tick)
        {
            List<TempoChange> tempoDataList = EditorTempo.TempoChanges;
            UInt64 microsecond = 0;
            TempoChange prevTempoData = tempoDataList[0];
            for (int i = 1; i < tempoDataList.Count && (int)tempoDataList[i].time.tick < tick; i++)
            {
                TempoChange nextTempoData = tempoDataList[i];
                microsecond += prevTempoData.microsecondsPerQuarterNote * (UInt64)(nextTempoData.time.tick - prevTempoData.time.tick) / 480;
                prevTempoData = nextTempoData;
            }
            microsecond += prevTempoData.microsecondsPerQuarterNote * (UInt64)(tick - (int)prevTempoData.time.tick) / 480;
            return (float)microsecond/1000;
        }
    }

    /// <summary>
    /// QNT (or quarter note ticks), represents a value in pulses per quarter note
    /// This value represents a relative offset from a timestamp
    /// </summary>
    public struct Relative_QNT {
        public Relative_QNT(Int64 tick) {
            this.tick = tick;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString() {
            return tick.ToString();
        }

        public Int64 tick;

        public float ToBeatTime() {
            return tick / (float)Constants.PulsesPerQuarterNote;
        }

        public QNT_Duration ToDuration() {
            if(tick < 0) {
                return new QNT_Duration(0);
            }
            else {
                return new QNT_Duration((UInt64)tick);
            }
        }

        public static Relative_QNT FromBeatTime(float beatTime) {
            return new Relative_QNT((Int64)Mathf.Round(beatTime * (float)Constants.PulsesPerQuarterNote));
        }

        public int CompareTo(Relative_QNT other) {
            return tick.CompareTo(other.tick);
        }

        public static bool operator ==(Relative_QNT a, long b) {
            return a.tick == b;
        }

        public static bool operator !=(Relative_QNT a, long b) {
            return a.tick != b;
        }

        public static bool operator ==(Relative_QNT a, Relative_QNT b) {
            return a.tick == b.tick;
        }

        public static bool operator !=(Relative_QNT a, Relative_QNT b) {
            return a.tick != b.tick;
        }

        public static bool operator <(Relative_QNT a, Relative_QNT b) {
            return a.tick < b.tick;
        }
        public static bool operator >(Relative_QNT a, Relative_QNT b) {
            return a.tick > b.tick;
        }
        public static bool operator <=(Relative_QNT a, Relative_QNT b) {
            return a.tick <= b.tick;
        }
        public static bool operator >=(Relative_QNT a, Relative_QNT b) {
            return a.tick >= b.tick;
        }

        public static Relative_QNT operator +(Relative_QNT a, Relative_QNT b)
            => new Relative_QNT(a.tick + b.tick);

        public static Relative_QNT operator -(Relative_QNT a, Relative_QNT b)
            => new Relative_QNT(a.tick - b.tick);

        public static Relative_QNT operator *(Relative_QNT a, Relative_QNT b)
            => new Relative_QNT(a.tick * b.tick);

        public static Relative_QNT operator /(Relative_QNT a, Relative_QNT b)
            => new Relative_QNT(a.tick / b.tick);
    }

    /// <summary>
    /// QNT (or quarter note ticks), represents a value in pulses per quarter note
    /// This value represents a positive duration of time
    /// </summary>
    [Serializable]
    public struct QNT_Duration {
        public QNT_Duration(UInt64 tick) {
            this.tick = tick;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString() {
            return tick.ToString();
        }

        public UInt64 tick;

        public static QNT_Duration GetSnappedValue(QNT_Duration duration, uint beatSnap) {
            QNT_Duration snapValue = Constants.DurationFromBeatSnap(beatSnap);
			return (duration / snapValue.tick) * snapValue.tick;
        }

        public static QNT_Duration FromBeatTime(float beatTime) {
            //No negative beat-time
            if(beatTime < 0.0f) {
                beatTime = 0.0f;
            }

            return new QNT_Duration((UInt64)Mathf.Round(beatTime * (float)Constants.PulsesPerQuarterNote));
        }

        public float ToBeatTime() {
            return tick / (float)Constants.PulsesPerQuarterNote;
        }

        public int CompareTo(QNT_Duration other) {
            return tick.CompareTo(other.tick);
        }

        public static bool operator ==(QNT_Duration a, UInt64 b) {
            return a.tick == b;
        }

        public static bool operator !=(QNT_Duration a, UInt64 b) {
            return a.tick != b;
        }

        public static bool operator ==(QNT_Duration a, QNT_Duration b) {
            return a.tick == b.tick;
        }

        public static bool operator !=(QNT_Duration a, QNT_Duration b) {
            return a.tick != b.tick;
        }

        public static bool operator <(QNT_Duration a, QNT_Duration b) {
            return a.tick < b.tick;
        }
        public static bool operator >(QNT_Duration a, QNT_Duration b) {
            return a.tick > b.tick;
        }
        public static bool operator <=(QNT_Duration a, QNT_Duration b) {
            return a.tick <= b.tick;
        }
        public static bool operator >=(QNT_Duration a, QNT_Duration b) {
            return a.tick >= b.tick;
        }

        public static QNT_Duration operator +(QNT_Duration a, QNT_Duration b)
            => new QNT_Duration(a.tick + b.tick);

        public static QNT_Duration operator -(QNT_Duration a, QNT_Duration b) {
            if(b.tick > a.tick) {
                return new QNT_Duration(0);
            }

            return new QNT_Duration(a.tick - b.tick);
        }

        public static QNT_Duration operator *(QNT_Duration a, UInt64 b)
            => new QNT_Duration(a.tick * b);

        public static QNT_Duration operator /(QNT_Duration a, UInt64 b)
            => new QNT_Duration(a.tick / b);
    }

    class BPM {
        private const int MIN_BPM = 60;
        private const int MAX_BPM = 400;

        private const int BASE_SPLIT_SAMPLE_SIZE = 2205;

        public struct BpmMatchData {
            public int bpm;
            public float match;
        }

        private static BpmMatchData[] bpmMatchDatas = new BpmMatchData[MAX_BPM - MIN_BPM + 1];

        public static List<float> Detect(ClipData src, Timeline timeline, QNT_Timestamp start, QNT_Timestamp end) {
             for (int i = 0; i < bpmMatchDatas.Length; i++) {
                bpmMatchDatas[i].match = 0f;
            }

            if(start == end) {
                return new List<float>();
            }

            if(end < start) {
                QNT_Timestamp temp = start;
                start = end;
                end = temp;
            }

            float startTime = start.ToSeconds();
            float endTime = end.ToSeconds();

            int channels = src.channels;
            int frequency = src.frequency;
            uint numSamples = (uint)(frequency * (endTime - startTime) * channels);

            int splitFrameSize = BASE_SPLIT_SAMPLE_SIZE;

            var volumeArr = new float[Mathf.CeilToInt((float)src.samples.Length / (float)splitFrameSize)];
            int powerIndex = 0;

            // Sample data analysis start
            for (int sampleIndex = 0; sampleIndex < src.samples.Length; sampleIndex += splitFrameSize) {
                float sum = 0f;
                for (int frameIndex = sampleIndex; frameIndex < sampleIndex + splitFrameSize; frameIndex++) {
                    if (src.samples.Length <= frameIndex) {
                        break;
                    }
                    // Use the absolute value, because left and right value is -1 to 1
                    float absValue = Mathf.Abs(src.samples[frameIndex]);
                    if (absValue > 1f) {
                        continue;
                    }

                    // Calculate the amplitude square sum
                    sum += (absValue * absValue);
                }

                // Set volume value
                volumeArr[powerIndex] = Mathf.Sqrt(sum / splitFrameSize);
                powerIndex++;
            }

            // Representing a volume value from 0 to 1
            float maxVolume = volumeArr.Max();
            for (int i = 0; i < volumeArr.Length; i++) {
                volumeArr[i] = volumeArr[i] / maxVolume;
            }

            // Create volume diff list
            var diffList = new List<float>();
            for (int i = 1; i < volumeArr.Length; i++)
            {
                diffList.Add(Mathf.Max(volumeArr[i] - volumeArr[i - 1], 0f));
            }

            // Calculate the degree of coincidence in each BPM
            int index = 0;
            float splitFrequency = (float)frequency / (float)splitFrameSize;
            for (int bpm = MIN_BPM; bpm <= MAX_BPM; bpm++) {
                float sinMatch = 0f;
                float cosMatch = 0f;
                float bps = (float)bpm / 60f;

                if (diffList.Count > 0) {
                    for (int i = 0; i < diffList.Count; i++) {
                        sinMatch += (diffList[i] * Mathf.Cos(i * 2f * Mathf.PI * bps / splitFrequency));
                        cosMatch += (diffList[i] * Mathf.Sin(i * 2f * Mathf.PI * bps / splitFrequency));
                    }

                    sinMatch *= (1f / (float)diffList.Count);
                    cosMatch *= (1f / (float)diffList.Count);
                }

                float match = Mathf.Sqrt((sinMatch * sinMatch) + (cosMatch * cosMatch));

                bpmMatchDatas[index].bpm = bpm;
                bpmMatchDatas[index].match = match;
                index++;
            }

            // Returns a high degree of coincidence BPM
            int matchIndex = Array.FindIndex(bpmMatchDatas, x => x.match == bpmMatchDatas.Max(y => y.match));
            var sort = bpmMatchDatas.OrderBy(x => x.match).Reverse().ToList();
            List<float> results = new List<float>();
            for(int i = 0; i < 10; ++i) {
                results.Add(sort[i].bpm);
            }

            return results;
        }
    }


    //Represents a musical time signature (i.e 4/4)
    public struct TimeSignature {
        public TimeSignature(uint numer, uint denom) {
            Numerator = numer;
            Denominator = denom;
        }

        public uint Numerator;
        public uint Denominator;

        public static uint GetMIDIDenominator(uint denom) {
            if(denom == 0) {
                return 0;
            }

            uint ret = 0;
            while (denom != 1) {
                denom = denom >> 1;
                ret += 1;
            }

            return ret;
        }

        public override string ToString() {
            return Numerator.ToString() + "/" + Denominator.ToString();
        }

        public static bool operator ==(TimeSignature ts1, TimeSignature ts2)
            => ts1.Numerator == ts2.Numerator && ts1.Denominator == ts2.Denominator;

        public static bool operator !=(TimeSignature ts1, TimeSignature ts2)
            => ts1.Numerator != ts2.Numerator || ts1.Denominator != ts2.Denominator;
    }
}