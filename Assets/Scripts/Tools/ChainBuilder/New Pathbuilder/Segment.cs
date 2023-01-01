using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Tools.ChainBuilder;
using NotReaper.UserInput;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NotReaper.ObjectPooling;
using NotReaper.Timing;
using System.Linq;

namespace NotReaper.Tools.PathBuilder
{
    [RequireComponent(typeof(LineRenderer))]
    public class Segment : MonoBehaviour
    {
        public int Index { get; private set; }
        internal Segment childSegment { get; set; }
        internal Segment parentSegment { get; set; }
        internal PathbuilderData.Interval interval { get; private set; } = new PathbuilderData.Interval(1, 16);
        internal QNT_Duration beatLength { get; private set; }

        internal PathbuilderMode Mode { get; private set; } = PathbuilderMode.Advanced;

        private const int NODE_COUNT = 20;

        #region Editor References
        [Header("Line Renderer")]
        [SerializeField] private LineRenderer bezier;
        [SerializeField] internal LineRenderer startConnector;
        [SerializeField] internal LineRenderer endConnector;
        [SerializeField] private LineRenderer handleConnector;

        [Space, Header("Points")]
        [SerializeField] internal Point startPointHandle;
        [SerializeField] internal Point endPoint;
        [SerializeField] internal Point endPointHandle;
        internal Transform startPoint;
        #endregion

        #region Fields
        private Pathbuilder pathbuilder;
        private TargetHandType handType;
        private BezierCurve curve;
        private InputAction mousePosition;
        private State state;
        private bool initialized = false;
        private PathbuilderData.Segment segmentData = new PathbuilderData.Segment();
        #endregion

        /// <summary>
        /// Reset everything when we disable a segment, making sure we don't get old or stale references.
        /// </summary>
        public void OnDisable()
        {
            state = State.Idle;
            childSegment = null;
            if (parentSegment != null) parentSegment.childSegment = null;
            parentSegment = null;
            interval = new PathbuilderData.Interval();
            segmentData = new PathbuilderData.Segment();
            Index = -1;
            bezier.startColor = GetNeutralColor();
            bezier.endColor = GetNeutralColor();
            bezier.enabled = false;
            EnableConnectorsAndHandles(false);
            bezier.positionCount = 1;
        }
        /// <summary>
        /// Initialize the segment and it's points. This only needs to be done once, after the segment gets pooled for the first time.
        /// </summary>
        /// <param name="pathbuilder"></param>
        /// <param name="actions"></param>
        public void Initialize(Pathbuilder pathbuilder, PathbuilderKeybinds actions)
        {
            if (initialized) return;
            this.pathbuilder = pathbuilder;
            mousePosition = actions.Pathbuilder.MousePosition;
            curve = new BezierCurve();
            startPointHandle.Initialize(this, pathbuilder, false);
            endPointHandle.Initialize(this, pathbuilder, false);
            endPoint.Initialize(this, pathbuilder, true);
            initialized = true;
        }

        private Color GetOtherHandColor()
        {
            return handType == TargetHandType.Left ? NRSettings.config.rightColor : NRSettings.config.leftColor;
        }
        private Color GetSameHandColor()
        {
            return handType == TargetHandType.Right ? NRSettings.config.rightColor : NRSettings.config.leftColor;
        }
        private Color GetMixedColor()
        {
            return Color.Lerp(NRSettings.config.leftColor, NRSettings.config.rightColor, .5f);
        }
        private Color GetNeutralColor()
        {
            var color =  Color.white;
            color.a = .5f;
            return color;
        }

        public void StartNewSegment(PathbuilderKeybinds actions, Transform startPoint, Target target, Pathbuilder pathbuilder, int index)
        {
            Initialize(pathbuilder, actions);
            //set data
            this.Index = index;
            this.handType = target.data.handType;
            this.startPoint = startPoint;
            //disable handles and their connectors
            //EnableConnectorsAndHandles(false);
            //initialize bezier curve
            //bezier.positionCount = 1;
            bezier.SetPosition(0, (Vector2)this.startPoint.position);
            bezier.startColor = GetSameHandColor();
            bezier.endColor = GetSameHandColor();
            bezier.enabled = true;
            bezier.positionCount++;
            //initialize endpoint
            endPoint.gameObject.SetActive(true);
            endPoint.SetColor(GetOtherHandColor());
            //initialize handles
            startPointHandle.SetColor(GetOtherHandColor());
            endPointHandle.SetColor(GetOtherHandColor());
            //set state
            state = State.SettingEndPoint;
            lastMousePos = GetMousePosition();
        }

        public void UpdateColors(TargetHandType handType)
        {
            this.handType = handType;
            bezier.startColor = GetSameHandColor();
            bezier.endColor = GetSameHandColor();
            endPoint.SetColor(GetOtherHandColor());
            startPointHandle.SetColor(GetOtherHandColor());
            endPointHandle.SetColor(GetOtherHandColor());
        }

        internal PathbuilderData.Segment LoadedData { get; private set; }
        
        public void LoadSegment(Pathbuilder pathbuilder, PathbuilderKeybinds actions, Transform startPoint, Target target, PathbuilderData.Segment data, int index, PathbuilderMode mode)
        {
            LoadedData = data;
            segmentData.Copy(data);
            interval = data.interval;
            beatLength = data.beatLength;
            StartNewSegment(actions, startPoint, target, pathbuilder, index);
            state = State.Idle;
            startPointHandle.transform.position = data.startPointHandle;
            endPoint.transform.position = data.endPoint;
            endPointHandle.transform.position = data.endPointHandle;
            bezier.positionCount = NODE_COUNT;
            EnableConnectorsAndHandles(true);
            SetMode(mode);
            UpdateSegment(false);
        }

        public PathbuilderData.Segment GetSegmentData()
        {
            return new PathbuilderData.Segment(startPoint.position, startPointHandle.transform.position, endPoint.transform.position, endPointHandle.transform.position, interval, beatLength, segmentData.generatedNodes);
        }

        public void SetSegmentEndPoint()
        {
            var position = lastMousePos;
            state = State.Idle;
            endPoint.transform.position = position;
            bezier.positionCount = NODE_COUNT;
            
            EnableConnectorsAndHandles(pathbuilder.Mode == PathbuilderMode.Advanced);

            //set handles in a straight line, inwards from start and end point, so we always start with a straight line
            var perpendicular = -Vector2.Perpendicular(((Vector2)endPoint.transform.position - (Vector2)startPoint.position).normalized);
            startPointHandle.transform.position = (Vector2)startPoint.transform.position + perpendicular;
            endPointHandle.transform.position = (Vector2)endPoint.transform.position + perpendicular;

            UpdateSegment();
            pathbuilder.SetActiveSegment(this);
        }

        private void EnableConnectorsAndHandles(bool enable)
        {
            startConnector.enabled = enable;
            endConnector.enabled = enable;
            handleConnector.enabled = enable;
            startPointHandle.gameObject.SetActive(enable);
            endPointHandle.gameObject.SetActive(enable);
        }

        public void SetBeatlength(QNT_Duration beatLength)
        {
            this.beatLength = beatLength;
        }

        public void SetInterval(PathbuilderData.Interval interval)
        {
            this.interval = interval;
        }

        public void SetNominator(int nominator)
        {
            interval.nominator = nominator;
        }

        public void SetDenominator(int denominator)
        {
            interval.denominator = denominator;
        }

        public void SetSelected(bool selected)
        {
            Color color = selected ? GetSameHandColor() : GetNeutralColor();
            bezier.startColor = color;
            bezier.endColor = color;
        }

        public void UpdateSegment(bool align = true)
        {
            if (Mode == PathbuilderMode.Simple) 
                return;

            UpdateLineRenderer();
            UpdateNodePositions();
        }

        private void UpdateLineRenderer()
        {
            bezier.positionCount = NODE_COUNT;
            for (int i = 0; i < NODE_COUNT; i++)
            {
                bezier.SetPosition(i, 
                    curve.CubicLerp((Vector2)startPoint.position, (Vector2)startPointHandle.transform.position, 
                        (Vector2)endPointHandle.transform.position, (Vector2)endPoint.transform.position,
                        (float)i / (NODE_COUNT - 1)));
            }
        }

        internal Vector3 lastMousePos { get; private set; } = Vector3.zero;
        private void Update()
        {
            if(state == State.Idle)
            {
                return;
            }

            if(state == State.SettingEndPoint)
            {
                if (EditorState.IsOverGrid)
                {
                    lastMousePos = GetMousePosition();
                    bezier.SetPosition(1, lastMousePos);
                    endPoint.transform.position = lastMousePos;                
                }
            }
            else
            {
                UpdateSegment();
                if (childSegment)
                {
                    childSegment.UpdateSegment();
                }
            }
        }

        private void UpdateNodePositions()
        {
            if (segmentData == null) return;
            
            for (int i = 1; i <= segmentData.generatedNodes.Count; i++)
            {
                var position = curve.CubicLerp(startPoint.position, startPointHandle.transform.position, endPointHandle.transform.position, endPoint.transform.position, (float)i / (segmentData.generatedNodes.Count));
                segmentData.generatedNodes[i - 1].data.position = position;
            }
        }

        public void MoveNodes(Vector2 amount)
        {
            
            var move = (Vector3)amount;
            if (parentSegment != null)
            {
                startPoint.position += move;
            }
            
            startPointHandle.transform.position += move;
            endPointHandle.transform.position += move;
            endPoint.transform.position += move;
            
            UpdateSegment();
        }

        public void OnHandleDragStart()
        {
            state = State.Editing;
        }

        public void OnHandleDragStop()
        {
            state = State.Idle;
            pathbuilder.Realign();
            pathbuilder.SaveTargetState();
            pathbuilder.ClearActivePoint();
        }

        public Transform GetSegmentEndPoint()
        {
            return endPoint.transform;
        }

        private Vector3 GetMousePosition()
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(mousePosition.ReadValue<Vector2>());
            pos.z = 0;
            return pos;
        }

        private enum State
        {
            Idle,
            SettingEndPoint,
            Editing
        }

        internal void SetData(PathbuilderData.Segment segment)
        {
            segmentData = segment;
        }

        public float GetAngle()
        {
            var direction = endPoint.transform.position - startPoint.transform.position;
            var angle = Vector2.SignedAngle(direction.normalized, new Vector2(0, 1));
            
            float snappedAngle;
            snappedAngle = Mathf.Floor((Math.Abs(angle) + 2.5f) / 5.0f) * 5.0f;
            
            if (Math.Sign(angle) < 0)
            {
                snappedAngle = 180 + (180 - snappedAngle);
            }
            return snappedAngle;
        }

        public void SetMode(PathbuilderMode mode)
        {
            Mode = mode;
            var isAdvanced = mode == PathbuilderMode.Advanced;
            EnableConnectorsAndHandles(isAdvanced);
            endPoint.SetActive(isAdvanced);
            bezier.enabled = isAdvanced;
        }
    }

}
