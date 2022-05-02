using NotReaper.Targets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using NotReaper.Repeaters;
using NotReaper.Models;

namespace NotReaper.Tools
{
    public class TransformScaleTool : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private ScaleDirection direction;
        [SerializeField] private TransformTool transformTool;

        private Vector2 mouseStartPos;
        private InputAction actionMousePos;
        private bool isMouseDown = false;
        private Vector2 mousePosition => cam.ScreenToWorldPoint(actionMousePos.ReadValue<Vector2>());
        private Camera cam;

        private Dictionary<TargetGridMoveIntent, TargetMoveData> startPositionMap = new();

        private List<Target> chainStarts = new();
        private List<TargetGridMoveIntent> moveIntents = new();

        [NRInject] private RepeaterManager repeaters;

        private void Start()
        {
            cam = CameraProvider.main;
            actionMousePos = KeybindManager.Global.MousePosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (KeybindManager.Global.Modifier.IsCtrlDown())
                return;

            SetPivot();
            float minX = 999f, minY = 999f, maxX = -999f, maxY = -999f;
            foreach (var selectedTarget in EditorNotes.SelectedNotesData)
            {

                if (selectedTarget.x > maxX) maxX = selectedTarget.x;
                if (selectedTarget.y > maxY) maxY = selectedTarget.y;
                if (selectedTarget.x < minX) minX = selectedTarget.x;
                if (selectedTarget.y < minY) minY = selectedTarget.y;

                TargetGridMoveIntent intent = new();
                intent.target = selectedTarget;
                intent.startingPosition = selectedTarget.position;
                if(selectedTarget.isRepeaterTarget)
                    intent.orientation = new(selectedTarget.repeaterData.Section.mirrorHorizontally ? -1f : 1f, selectedTarget.repeaterData.Section.mirrorVertically ? -1f : 1f);
                moveIntents.Add(intent);
            }

            MinMax x = new(minX, maxX);
            MinMax y = new(minY, maxY);
            foreach(var intent in moveIntents)
            {
                CalculateDistance(intent.startingPosition, x, y, out float relativeDistance, out float distanceToMid);
                startPositionMap.Add(intent, new(relativeDistance, distanceToMid));
            }
            mouseStartPos = mousePosition;
            isMouseDown = true;
        }

        private void CalculateDistance(Vector2 targetPosition, MinMax x, MinMax y, out float relativeDistance, out float distanceToMid)
        {

            switch (direction)
            {
                case ScaleDirection.Up:
                    relativeDistance = targetPosition.y - y.min;
                    distanceToMid = targetPosition.y - y.mid;
                    break;
                case ScaleDirection.Down:
                    relativeDistance = targetPosition.y - y.max;
                    distanceToMid = targetPosition.y - y.mid;
                    break;
                case ScaleDirection.Left:
                    relativeDistance = targetPosition.x - x.max;
                    distanceToMid = targetPosition.x - x.mid;
                    break;
                default:
                    relativeDistance = targetPosition.x - x.min;
                    distanceToMid = targetPosition.x - x.mid;
                    break;
            }
            relativeDistance = Mathf.Abs(relativeDistance);            
            
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if(moveIntents.Count > 0)
            {
                List<TargetGridMoveIntent> tempIntents = new();
                foreach(var intent in moveIntents)
                {
                    if (!intent.target.isRepeaterTarget)
                    {
                        tempIntents.Add(intent);
                        continue;
                    }

                    var parentTarget = repeaters.GetParentTarget(intent.target);
                    TargetGridMoveIntent parentIntent = new();
                    parentIntent.target = parentTarget;
                    parentIntent.startingPosition = intent.startingPosition * intent.orientation;
                    parentIntent.intendedPosition = intent.intendedPosition * intent.orientation;
                    tempIntents.Add(parentIntent);

                    foreach (var child in repeaters.GetMatchingRepeaterTargets(parentTarget))
                    {
                        TargetGridMoveIntent childIntent = new();
                        childIntent.target = child;
                        childIntent.orientation = new(child.repeaterData.Section.mirrorHorizontally ? -1f : 1f, child.repeaterData.Section.mirrorVertically ? -1f : 1f);
                        childIntent.startingPosition = parentIntent.startingPosition * childIntent.orientation;
                        childIntent.intendedPosition = parentIntent.intendedPosition * childIntent.orientation;
                        tempIntents.Add(childIntent);
                    }
                }

                UndoRedoManager.AddAction(new NRActionGridMoveNotes(tempIntents));

                foreach (var intent in tempIntents)
                {
                    if (intent.target.behavior.IsChain())
                    {
                        var start = TargetFinder.FindChainStart(intent.target);
                        if (start != null)
                        {
                            if (!chainStarts.Contains(start))
                                chainStarts.Add(start);
                        }
                    }
                }
                foreach (var start in chainStarts)
                    EditorTargets.UpdateChainConnector(start);
            }
            moveIntents.Clear();
            chainStarts.Clear();
            isMouseDown = false;
            startPositionMap.Clear();
        }

        private void SetPivot()
        {
            Vector2 pivot;
            switch (direction)
            {
                case ScaleDirection.Left:
                    pivot = Vector2.right;
                    break;
                case ScaleDirection.Down:
                    pivot = Vector2.up;
                    break;
                default:
                    pivot = Vector2.zero;
                    break;
            }

            transformTool.SetPivot(pivot);
        }

        private void Update()
        {
            if (!isMouseDown)
                return;

            var distance = (mousePosition - mouseStartPos) * .5f;

            if (direction == ScaleDirection.Up || direction == ScaleDirection.Down)
            {
                distance.x = 0;
            }
            else
            {
                distance.y = 0;
            }

            bool isShiftDown = KeybindManager.Global.Modifier.IsShiftDown();
            float dir = isShiftDown && (direction == ScaleDirection.Left || direction == ScaleDirection.Down) ? -1f : 1f;
            foreach (var entry in startPositionMap)
            {
                float mult = isShiftDown ? entry.Value.distanceToMid : entry.Value.relativeDistance;
                Vector2 newPos = entry.Key.startingPosition + (distance * mult * dir);
                Vector2 delta = newPos - entry.Key.target.position;

                entry.Key.target.position = newPos;
                entry.Key.intendedPosition = newPos;

                if (entry.Key.target.isPathbuilderTarget)
                {
                    entry.Key.target.pathbuilderData.MoveBy(delta);
                }
            }

            transformTool.UpdateOverlay();
        }

        private struct TargetMoveData
        {
            public float relativeDistance;
            public float distanceToMid;
            public TargetMoveData(float relativeDistance, float distanceToMid)
            {
                this.relativeDistance = relativeDistance;
                this.distanceToMid = distanceToMid;
            }
        }

        private struct MinMax
        {
            public float min;
            public float max;
            public float mid;

            public MinMax(float min, float max)
            {
                this.min = min;
                this.max = max;

                mid = (min + max) * .5f;
            }
        }


        private enum ScaleDirection
        {
            Up,
            Down,
            Left,
            Right
        }
    }

}
