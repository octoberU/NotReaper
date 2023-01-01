using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

namespace NotReaper.Tools.PathBuilder
{
    public class AnchorPoint
    {
        private Transform anchor;
        private Point leftHandle;
        private Point rightHandle;

        private LineRenderer leftConnector;
        private LineRenderer rightConnector;

        private AnchorType type;
        
        private AnchorPoint previousPoint;
        private AnchorPoint nextPoint;
        private HandleType _handleType;

        private Pathbuilder pathbuilder;

        private enum AnchorType
        {
            Default,
            Start,
            End
        }

        public HandleType HandleType
        {
            get => _handleType;
            set
            {
                _handleType = value;
                OnHandleTypeChanged();
            }
        }

        public void SetHandleTypeSilent(HandleType type)
        {
            _handleType = type;
        }

        public AnchorPoint(Pathbuilder pathbuilder, Transform anchor, Point leftHandle, Point rightHandle, LineRenderer leftConnector, LineRenderer rightConnector, Point anchorPoint)
        {
            anchorPoint.SetAnchor(this);
            this.pathbuilder = pathbuilder;
            this.anchor = anchor;
            this.leftHandle = leftHandle;
            this.rightHandle = rightHandle;
            this.leftConnector = leftConnector;
            this.rightConnector = rightConnector;
            type = AnchorType.Default;
            
            leftHandle.SetAnchor(this);
            rightHandle.SetAnchor(this);
        }

        public AnchorPoint(Pathbuilder pathbuilder, Transform anchor, Point handleA, LineRenderer leftConnector, bool isStartAnchor, Point anchorPoint = null)
        {
            if(anchorPoint != null)
                anchorPoint.SetAnchor(this);
            
            this.pathbuilder = pathbuilder;
            this.anchor = anchor;
            if (isStartAnchor)
            {
                rightHandle = handleA;
                rightConnector = leftConnector;
                rightHandle.SetAnchor(this);
            }
            else
            {
                this.leftHandle = handleA;
                this.leftConnector = leftConnector;
                handleA.SetAnchor(this);
            }
            
            type = isStartAnchor ? AnchorType.Start : AnchorType.End;
        }

        public void SetNextAnchor(AnchorPoint nextPoint)
            => this.nextPoint = nextPoint;
        
        public void SetPreviousAnchor(AnchorPoint previousPoint)
            => this.previousPoint = previousPoint;
        
        private void OnHandleTypeChanged()
        {
            bool interactable = HandleType is HandleType.Aligned or HandleType.Free;

            if (type != AnchorType.Start)
            {
                leftConnector.startColor = leftConnector.endColor = GetColorForHandleType();
                leftHandle.SetInteractable(interactable);
            }

            if (type != AnchorType.End)
            {
                rightConnector.startColor = rightConnector.endColor = GetColorForHandleType();
                rightHandle.SetInteractable(interactable);
            }

            Realign();
            
            Color GetColorForHandleType()
                => HandleType switch
                {
                    HandleType.Free => pathbuilder.freeColor,
                    HandleType.Aligned => pathbuilder.alignedColor,
                    HandleType.Vector => pathbuilder.vectorColor,
                    HandleType.Automatic => pathbuilder.autoColor,
                    _ => throw new ArgumentOutOfRangeException()
                };
        }

        internal void OnPointMoved(Point point)
        {
            if (HandleType is HandleType.Aligned)
            {
                if (point == leftHandle)
                    SmoothAlign(leftHandle, rightHandle);
                else
                    SmoothAlign(rightHandle, leftHandle);
            }

            pathbuilder.Realign();
        }

        public void Realign()
        {
            switch (HandleType)
            {
                case HandleType.Aligned:
                    SmoothAlign(leftHandle, rightHandle);
                    break;
                case HandleType.Vector:
                    VectorAlign();
                    break;
                case HandleType.Automatic:
                    AutoAlign();
                    break;
            }

            UpdateHandleConnectors();
        }

        private void SmoothAlign(Point movedPoint, Point pointToAlign)
        {
            if (type != AnchorType.Default)
                return;

            var anchorPos = anchor.position;

            var displacement = movedPoint.transform.position - anchorPos;
            pointToAlign.transform.position = anchorPos - displacement.normalized * displacement.magnitude;
            
            leftConnector.SetPosition(0, leftHandle.transform.position);
            leftConnector.SetPosition(1, anchorPos);
            
            rightConnector.SetPosition(0, rightHandle.transform.position);
            rightConnector.SetPosition(1, anchorPos);
        }

        private void VectorAlign()
        {
            if (type != AnchorType.End)
                Align(nextPoint.anchor, rightHandle);
            
            if(type != AnchorType.Start)
                Align(previousPoint.anchor, leftHandle);
            
            void Align(Transform next, Point handle)
            {
                Vector3 displacement = next.position - anchor.transform.position;
                var step = displacement.magnitude / 3f;

                handle.transform.position = anchor.transform.position + displacement.normalized * step;
            }
        }

        private void AutoAlign()
        {
            Vector3 anchorPos = anchor.position;
            switch (type)
            {
                case AnchorType.Start:
                {
                    Vector2 distance = nextPoint.leftHandle.transform.position - anchorPos;
                    rightHandle.transform.position = anchorPos + (Vector3)distance.normalized * (distance.magnitude * .5f);
                    break;
                }
                case AnchorType.End:
                {
                    Vector2 distance = previousPoint.rightHandle.transform.position - anchorPos;
                    leftHandle.transform.position = anchorPos + (Vector3)distance.normalized * (distance.magnitude * .5f);
                    break;
                }
                default:
                {
                    Vector2 displacementStart = previousPoint.anchor.position - anchorPos;
                    Vector2 displacementEnd = nextPoint.anchor.transform.position - anchorPos;

                    var direction = (displacementStart.normalized - displacementEnd.normalized).normalized;
                    leftHandle.transform.position = (Vector2)anchorPos + direction * (displacementStart.magnitude * .5f);
                    rightHandle.transform.position = (Vector2)anchorPos - direction * (displacementEnd.magnitude * .5f);
                    break;
                }
            }
        }

        private void UpdateHandleConnectors()
        {
            Vector2 anchorPos = anchor.position;
            
            if (type != AnchorType.Start)
            {
                leftConnector.SetPosition(0, (Vector2)leftHandle.transform.position);
                leftConnector.SetPosition(1, anchorPos);
            }
            
            if (type != AnchorType.End)
            {
                rightConnector.SetPosition(0, (Vector2)rightHandle.transform.position);
                rightConnector.SetPosition(1, anchorPos);
            }
        }



        public void EnableConnectorsAndAnchor(bool enable)
        {
            anchor.gameObject.SetActive(enable);
            leftHandle.gameObject.SetActive(enable);
            rightHandle.gameObject.SetActive(enable);
        }

        public void UpdateLineRenderers()
        {
            var anchorPos = (Vector2)anchor.transform.position;

            leftConnector.SetPosition(0, anchorPos);
            leftConnector.SetPosition(1, (Vector2)leftHandle.transform.position);

            rightConnector.SetPosition(0, anchorPos);
            rightConnector.SetPosition(1, (Vector2)rightHandle.transform.position);
        }
    }
}
