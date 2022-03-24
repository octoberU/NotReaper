using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotReaper.Models;
using NotReaper.Grid;
using NotReaper.Tools.ChainBuilder;
using UnityEngine.Events;
using UnityEngine.Serialization;
using NotReaper.Timing;
using Newtonsoft.Json;

namespace NotReaper.Targets {

    [Serializable]
    public class PathbuilderData
    {
        public QNT_Duration BeatLength => IsSegmentScope ? TotalSegmentLength : BeatLengthOverride;
        
        [SerializeField] private QNT_Duration _beatLengthOverride = new QNT_Duration(480);
        [SerializeField] private List<Segment> _segments = new List<Segment>();
        [SerializeField] private Interval _intervalOverride = new Interval();
        [SerializeField] private bool _alternateHands = false;
        [SerializeField] private bool _isSegmentScope = true;
        [NonSerialized] private int _activeSegment = -1;
        public List<Segment> Segments
        {
            get { return _segments; }
            set { _segments = value; }
        }

        public Interval IntervalOverride
        {
            get { return _intervalOverride; }
            set { _intervalOverride = value; }
        }
        
        public int ActiveSegment
        {
            get { return _activeSegment == -1 ? _segments.Count - 1 : _activeSegment; }
            set { _activeSegment = value; }
        }

        public Interval ActiveInterval
        {
            get 
            {
                if (IsSegmentScope)
                {
                    return Segments[ActiveSegment].interval;
                }
                else
                {
                    return IntervalOverride;
                }
            }
        }

        public QNT_Duration BeatLengthOverride
        {
            get { return _beatLengthOverride; }
            set { _beatLengthOverride = value; }
        }

        public QNT_Duration TotalSegmentLength
        {
            get 
            {
                QNT_Duration length = new QNT_Duration(0);
                foreach(var segment in Segments)
                {
                    length += segment.beatLength;
                }
                return length;
            }
        }

        public bool AlternateHands
        {
            get { return _alternateHands; }
            set { _alternateHands = value; }
        }

        public bool IsSegmentScope
        {
            get { return _isSegmentScope; }
            set { _isSegmentScope = value; }
        }

        public PathbuilderData Copy(PathbuilderData data)
        {
            if (data == null) return new PathbuilderData();
            AlternateHands = data.AlternateHands;
            BeatLengthOverride = data.BeatLengthOverride;
            IntervalOverride = new Interval(data.IntervalOverride.nominator, data.IntervalOverride.denominator);
            IsSegmentScope = data.IsSegmentScope;
            ActiveSegment = data.ActiveSegment;
            Segments.Clear();
            foreach(var segment in data.Segments)
            {
                var targets = new List<TargetData>();
                foreach(var target in targets)
                {
                    TargetData t = new TargetData();
                    t.Copy(target);
                    targets.Add(t);
                }
                Segments.Add(new Segment(segment.startPoint, segment.startPointHandle, segment.endPoint, segment.endPointHandle, new Interval(segment.interval.nominator, segment.interval.denominator), segment.beatLength, targets));
            }
            return data;
        }

        public void MoveBy(Vector2 amount)
        {
            foreach(var segment in Segments)
            {
                segment.startPoint += amount;
                segment.startPointHandle += amount;
                segment.endPoint += amount;
                segment.endPointHandle += amount;

                foreach(var node in segment.generatedNodes)
                {
                    node.position += amount;
                }
            }      
        }

        public void Flip(Vector2 axis)
        {
            foreach (var segment in Segments)
            {
                segment.startPoint *= axis;
                segment.startPointHandle *= axis;
                segment.endPoint *= axis;
                segment.endPointHandle *= axis;

                foreach(var node in segment.generatedNodes)
                {
                    node.position *= axis;
                }
            }
        }

        public void Rotate(TargetData data, Vector2 center, float angle)
        {
            
            data.x -= center.x;
            data.y -= center.y;
            angle = -angle;

            Vector2 rotate;

            rotate.x = (float)(data.x * Math.Cos(angle / 180f * Math.PI) + data.y * Math.Sin(angle / 180f * Math.PI));
            rotate.y = (float)(data.x * -Math.Sin(angle / 180f * Math.PI) + data.y * Math.Cos(angle / 180f * Math.PI));
            rotate.x += center.x;
            rotate.y += center.y;


            data.x = rotate.x;
            data.y = rotate.y;

            foreach(var segment in Segments)
            {
                segment.startPoint = RotatePoint(segment.startPoint, center, angle);
                segment.startPointHandle = RotatePoint(segment.startPointHandle, center, angle);
                segment.endPoint = RotatePoint(segment.endPoint, center, angle);
                segment.endPointHandle = RotatePoint(segment.endPointHandle, center, angle);
            }
        }
        private Vector2 RotatePoint(Vector2 point, Vector2 center, float angle)
        {
            point.x -= center.x;
            point.y -= center.y;

            Vector2 rotate;

            rotate.x = (float)(point.x * Math.Cos(angle / 180f * Math.PI) + point.y * Math.Sin(angle / 180f * Math.PI));
            rotate.y = (float)(point.x * -Math.Sin(angle / 180f * Math.PI) + point.y * Math.Cos(angle / 180f * Math.PI));
            rotate.x += center.x;
            rotate.y += center.y;


            point.x = rotate.x;
            point.y = rotate.y;

            return point;
        }

        public void Scale(TargetData data, Vector2 scale, bool scaleUp)
        {
            if (scaleUp)
            {
                data.y *= scale.y;
                data.x *= scale.x;
            }
            else
            {
                data.y /= scale.y;
                data.x /= scale.x;
            }

            foreach(var segment in Segments)
            {
                segment.startPoint = ScalePoint(segment.startPoint, scale, scaleUp);
                segment.startPointHandle = ScalePoint(segment.startPointHandle, scale, scaleUp);
                segment.endPoint = ScalePoint(segment.endPoint, scale, scaleUp);
                segment.endPointHandle = ScalePoint(segment.endPointHandle, scale, scaleUp);
            }
        }

        private Vector2 ScalePoint(Vector2 point, Vector2 scale, bool scaleUp)
        {
            if (scaleUp)
            {
                point.x *= scale.x;
                point.y *= scale.y;
            }
            else
            {
                point.x /= scale.x;
                point.y /= scale.y;
            }
            return point;
        }

        public void UpdateNodeHandType(TargetHandType newType)
        {
            var hand = newType;
            foreach (var segment in Segments)
            {
                foreach (var node in segment.generatedNodes)
                {
                    if (AlternateHands)
                    {
                        hand = hand == TargetHandType.Left ? TargetHandType.Right : TargetHandType.Left;
                    }
                    node.handType = hand;
                }
            }
        }
        internal void SetHitsound(InternalTargetVelocity velocity)
        {
            foreach(var segment in Segments)
            {
                foreach(var node in segment.generatedNodes)
                {
                    node.velocity = velocity;
                }
            }
        }
        internal void SetBehavior(TargetBehavior behavior)
        {
            foreach (var segment in Segments)
            {
                foreach (var node in segment.generatedNodes)
                {
                    node.behavior = behavior;
                }
            }
        }

        [Serializable]
        public class Interval
        {
            public int nominator;
            public int denominator;

            [JsonConstructor]
            public Interval(int nominator, int denominator)
            {
                this.nominator = nominator;
                this.denominator = denominator;
            }

            public Interval()
            {
                nominator = 1;
                denominator = 16;
            }
        }

        [Serializable]
        public class Segment
        {
            public Vector2 startPoint;
            public Vector2 startPointHandle;
            public Vector2 endPoint;
            public Vector2 endPointHandle;
            public Interval interval;
            public QNT_Duration beatLength;
            public bool alternateHands;
            public List<TargetData> generatedNodes;
            [JsonConstructor]
            public Segment(Vector2 startPoint, Vector2 startPointHandle, Vector2 endPoint, Vector2 endPointHandle, Interval interval, QNT_Duration beatLength, List<TargetData> generatedNodes)
            {
                this.startPoint = startPoint;
                this.startPointHandle = startPointHandle;
                this.endPoint = endPoint;
                this.endPointHandle = endPointHandle;
                this.interval = interval;
                this.beatLength = beatLength;
                this.generatedNodes = generatedNodes;
            }

            public Segment() 
            {
                this.generatedNodes = new List<TargetData>();
            }
        }
    }

    public class RepeaterData
    {
        private QNT_Timestamp _relativeTime;
        private Repeaters.RepeaterSection _section;
        public long targetID { get; set; } = -1;
        public QNT_Timestamp RelativeTime
        {
            get { return _relativeTime; }
            set { _relativeTime = value; }
        }

        public Repeaters.RepeaterSection Section
        {
            get { return _section; }
            set { _section = value; }
        }

        public void Copy(RepeaterData data)
        {
            targetID = data.targetID;
            _relativeTime = data._relativeTime;
            _section = new Repeaters.RepeaterSection();
            _section.Copy(data._section);
        }
    }

    [Serializable]
    public class LegacyPathbuilderData {

        [SerializeField]
        private TargetBehavior _behavior;

        [SerializeField]
        private InternalTargetVelocity _velocity;

        [SerializeField]
        private TargetHandType _handType;

        [SerializeField]
        private int _interval = 16;

        [SerializeField]
        private float _initialAngle = 0.0f;

        [SerializeField]
        private float _angle = 0.0f;

        [SerializeField]
        private float _angleIncrement = 0.0f;

        [SerializeField]
        private float _stepDistance = 0.5f;

        [SerializeField]
        private float _stepIncrement = 0.0f;

        public TargetBehavior behavior {
            get { return _behavior; }
            set { _behavior = value; RecalculateChain(); }
        }
        public InternalTargetVelocity velocity {
            get { return _velocity; }
            set { _velocity = value; RecalculateChain(); }
        }
        public TargetHandType handType {
            get { return _handType; }
            set { _handType = value; RecalculateChain(); }
        }
        public int interval {
            get { return _interval; }
            set { _interval = value; RecalculateChain(); }
        }
        public float initialAngle {
            get { return _initialAngle; }
            set { _initialAngle = value; InitialAngleChangedEvent(); RecalculateChain(); }
        }
        public float angle {
            get { return _angle; }
            set { _angle = value; RecalculateChain(); }
        }
        public float angleIncrement {
            get { return _angleIncrement; }
            set { _angleIncrement = value; RecalculateChain(); }
        }
        public float stepDistance {
            get { return _stepDistance; }
            set { _stepDistance = value; RecalculateChain(); }
        }
        public float stepIncrement {
            get { return _stepIncrement; }
            set { _stepIncrement = value; RecalculateChain(); }
        }

        public void RecalculateChain() {
            RecalculateEvent();
        }

        public void OnFinishRecalculate() {
            RecalculateFinishedEvent();
        }

        [NonSerialized] public Action RecalculateEvent = delegate { };
        [NonSerialized] public Action RecalculateFinishedEvent = delegate { };
        [NonSerialized] public Action InitialAngleChangedEvent = delegate { };
        [NonSerialized] public List<TargetData> generatedNotes = new List<TargetData>();
        [NonSerialized] public HashSet<TargetData> parentNotes = new HashSet<TargetData>();
        [NonSerialized] public bool createdNotes = false;

        public void Copy(LegacyPathbuilderData data, bool notify = true) {
            if (notify)
            {
                behavior = data.behavior;
                velocity = data.velocity;
                handType = data.handType;
                interval = data.interval;
                initialAngle = data.initialAngle;
                angle = data.angle;
                angleIncrement = data.angleIncrement;
                stepDistance = data.stepDistance;
                stepIncrement = data.stepIncrement;
            }
            else
            {
                _behavior = data.behavior;
                _velocity = data.velocity;
                _handType = data.handType;
                _interval = data.interval;
                _initialAngle = data.initialAngle;
                _angle = data.angle;
                _angleIncrement = data.angleIncrement;
                _stepDistance = data.stepDistance;
                _stepIncrement = data.stepIncrement;
            }
        }

        public void DeleteCreatedNotes(Timeline timeline) {
            if (createdNotes) {
                generatedNotes.ForEach(t => {
                    timeline.DeleteTargetFromAction(t);
                });
                generatedNotes.Clear();
                createdNotes = false;
            }
        }
    }

    internal class TargetDataInternal {
        private static uint TargetDataId = 0;

        public static uint GetNextId() { return TargetDataId++; }

        public TargetDataInternal() {
            InternalId = GetNextId();
        }

        public uint InternalId { get; private set; }

        private float _x;
        private float _y;
        private QNT_Duration _beatLength;
        private InternalTargetVelocity _velocity;
        private TargetHandType _handType;
        private TargetBehavior _behavior;
        public LegacyPathbuilderData legacyPathbuilderData;
        public PathbuilderData pathbuilderData;
        public bool isPathbuilderTarget;
        

        public float x {
            get { return _x; }
            set { _x = value; if (PositionChangeEvent != null) PositionChangeEvent(x, y); }
        }

        public float y {
            get { return _y; }
            set { _y = value; if (PositionChangeEvent != null) PositionChangeEvent(x, y); }
        }

        public Vector2 position {
            get { return new Vector2(x, y); }
            set { _x = value.x; _y = value.y; if (PositionChangeEvent != null) PositionChangeEvent(x, y); }
        }

        public QNT_Duration beatLength {
            get { return _beatLength; }
            set { _beatLength = value; if (BeatLengthChangeEvent != null) BeatLengthChangeEvent(beatLength); }
        }

        public InternalTargetVelocity velocity {
            get { return _velocity; }
            set { _velocity = value; if (VelocityChangeEvent != null) VelocityChangeEvent(velocity); }
        }
        public TargetHandType handType {
            get { return _handType; }
            set { _handType = value; if (HandTypeChangeEvent != null) HandTypeChangeEvent(handType); }
        }
        public TargetBehavior behavior {
            get { return _behavior; }
            set { var prevBehavior = _behavior; _behavior = value; if (BehaviourChangeEvent != null) BehaviourChangeEvent(prevBehavior, behavior); }
        }


        public Action<float, float> PositionChangeEvent;
        public Action<QNT_Duration> BeatLengthChangeEvent;
        public Action<InternalTargetVelocity> VelocityChangeEvent;
        public Action<TargetHandType> HandTypeChangeEvent;
        public Action<TargetBehavior, TargetBehavior> BehaviourChangeEvent;
    }

    public class TargetData {
        internal TargetDataInternal data;

        public uint ID { get; protected set; }

        public TargetData() {
            ID = TargetDataInternal.GetNextId();
            data = new TargetDataInternal();

            beatLength = Constants.SixteenthNoteDuration;
            velocity = InternalTargetVelocity.Kick;
            handType = TargetHandType.Either;
            behavior = TargetBehavior.Standard;
        }

        public TargetData(Cue cue) {
            ID = TargetDataInternal.GetNextId();
            data = new TargetDataInternal();

            Vector2 pos = NotePosCalc.PitchToPos(cue);
            x = pos.x;
            y = pos.y;
            time = new QNT_Timestamp((UInt64)cue.tick);
            beatLength = new QNT_Duration((UInt64)cue.tickLength);
            velocity = cue.velocity;
            handType = cue.handType;
            behavior = cue.behavior;
        }

        public TargetData(TargetData other, QNT_Timestamp? timeOverride = null) {
            ID = TargetDataInternal.GetNextId();
            data = other.data;

            if (!timeOverride.HasValue) {
                time = other.time;
            }
            else {
                time = timeOverride.Value;
            }
        }

        public void Copy(TargetData data) {
            x = data.x;
            y = data.y;
            time = data.time;
            beatLength = data.beatLength;
            velocity = data.velocity;
            handType = data.handType;
            behavior = data.behavior;
            legacyPathbuilderData = data.legacyPathbuilderData;
            pathbuilderData = data.pathbuilderData;
            repeaterData = data.repeaterData;
            isPathbuilderTarget = data.isPathbuilderTarget;
        }

        public bool isPathbuilderTarget
        {
            get { return data.isPathbuilderTarget; }
            set { data.isPathbuilderTarget = value; }
        }

        public PathbuilderData pathbuilderData
        {
            get { return data.pathbuilderData; }
            set { data.pathbuilderData = value; }
        }

        public LegacyPathbuilderData legacyPathbuilderData
        {
            get { return data.legacyPathbuilderData; }
            set { data.legacyPathbuilderData = value; }
        }

        public RepeaterData repeaterData;
        public bool isRepeaterTarget => repeaterData != null;

        private QNT_Timestamp _time;

		public float x
		{
			get { return data.x; }
			set { data.x = value; }
		}

		public float y
		{
			get { return data.y; }
			set { data.y = value; }
		}

		public Vector2 position {
			get { return data.position; }
			set { data.position = value; }
		}

		public virtual QNT_Timestamp time
		{
			get { return _time; }
			protected set 
            {
                var oldTime = _time;
                _time = value; 
                if (TickChangeEvent != null) TickChangeEvent(time, oldTime); 
            }
		}

		//This should only be used when you need to set time directly, and are handling repeaters yourself.
		//In all other cases, NRActionTimelineMoveNotes should be used
		public virtual void SetTimeFromAction(QNT_Timestamp time) {
			this.time = time;
		}

		public QNT_Duration beatLength
		{
			get { return data.beatLength; }
			set { data.beatLength = value; }
		}

		public InternalTargetVelocity velocity
		{
			get { return data.velocity; }
			set { data.velocity = value; }
		}
		public TargetHandType handType
		{
			get { return data.handType; }
			set { data.handType = value; }
		}
		public TargetBehavior behavior
		{
			get { return data.behavior; }
			set { data.behavior = value; }
		}

		public bool supportsBeatLength
		{
			get {  return BehaviorSupportsBeatLength(behavior, isPathbuilderTarget); }
		}

		public static bool BehaviorSupportsBeatLength(TargetBehavior behavior, bool isPathbuilderTarget) {
			return behavior == TargetBehavior.Sustain || behavior == TargetBehavior.Legacy_Pathbuilder || isPathbuilderTarget;
		}

		public event Action<float, float> PositionChangeEvent {
			add { data.PositionChangeEvent += value; }
			remove {data.PositionChangeEvent -= value; }
		}
		
        public Action<QNT_Timestamp, QNT_Timestamp> TickChangeEvent;

        public event Action<QNT_Duration> BeatLengthChangeEvent {
			add { data.BeatLengthChangeEvent += value; }
			remove {data.BeatLengthChangeEvent -= value; }
		}
		public event Action<InternalTargetVelocity> VelocityChangeEvent {
			add { data.VelocityChangeEvent += value; }
			remove {data.VelocityChangeEvent -= value; }
		}
		public event Action<TargetHandType> HandTypeChangeEvent {
			add { data.HandTypeChangeEvent += value; }
			remove {data.HandTypeChangeEvent -= value; }
		}
		public event Action<TargetBehavior, TargetBehavior> BehaviourChangeEvent {
			add { data.BehaviourChangeEvent += value; }
			remove {data.BehaviourChangeEvent -= value; }
		}
	}
}