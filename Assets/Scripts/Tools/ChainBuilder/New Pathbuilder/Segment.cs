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

        private const int NODE_COUNT = 20;

        #region Editor References
        [Header("Line Renderer")]
        [SerializeField] private LineRenderer bezier;
        [SerializeField] private LineRenderer startConnector;
        [SerializeField] private LineRenderer endConnector;
        [SerializeField] private LineRenderer handleConnector;

        [Space, Header("Points")]
        [SerializeField] private Point startPointHandle;
        [SerializeField] private Point endPoint;
        [SerializeField] private Point endPointHandle;
        private Transform startPoint;
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
            startPointHandle.Initialize(this, pathbuilder);
            endPointHandle.Initialize(this, pathbuilder);
            endPoint.Initialize(this, pathbuilder);
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
            return Color.white;
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

        public void LoadSegment(Pathbuilder pathbuilder, PathbuilderKeybinds actions, Transform startPoint, Target target, PathbuilderData.Segment data, int index)
        {
            segmentData = data;
            interval = data.interval;
            beatLength = data.beatLength;
            StartNewSegment(actions, startPoint, target, pathbuilder, index);
            state = State.Idle;
            startPointHandle.transform.position = data.startPointHandle;
            endPoint.transform.position = data.endPoint;
            endPointHandle.transform.position = data.endPointHandle;
            bezier.positionCount = NODE_COUNT;
            EnableConnectorsAndHandles(true);
            UpdateSegment();
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
            EnableConnectorsAndHandles(true);

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

        public void UpdateSegment()
        {
            UpdateLineRenderer();
            UpdateNodePositions();
        }

        private void UpdateLineRenderer()
        {
            bezier.positionCount = NODE_COUNT;
            for(int i = 0; i < NODE_COUNT; i++)
            {
                bezier.SetPosition(i, curve.CubicLerp((Vector2)startPoint.position, (Vector2)startPointHandle.transform.position, (Vector2)endPointHandle.transform.position, (Vector2)endPoint.transform.position, (float)i / (NODE_COUNT - 1)));
            }
            startConnector.SetPosition(0, (Vector2)startPoint.position);
            startConnector.SetPosition(1, (Vector2)startPointHandle.transform.position);
            endConnector.SetPosition(0, (Vector2)endPoint.transform.position);
            endConnector.SetPosition(1, (Vector2)endPointHandle.transform.position);
            handleConnector.SetPosition(0, (Vector2)startPointHandle.transform.position);
            handleConnector.SetPosition(1, (Vector2)endPointHandle.transform.position);
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

        public void OnHandleDragStart()
        {
            state = State.Editing;
        }

        public void OnHandleDragStop()
        {
            state = State.Idle;
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
    }

}
