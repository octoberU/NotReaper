using NotReaper.Grid;
using NotReaper.Models;
using NotReaper.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NotReaper.Tools.PathBuilder
{

    public class Point : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Segment segment;
        private Pathbuilder pathbuilder;
        private Image image;

        private bool initialized = false;

        private bool isMouseDown = false;
        private Vector3 startMousePosition;
        private Vector3 startPosition;
        private Camera cam;
        private bool shouldSnap;
        private float minMoveDistanceBeforeDragStart = 1f;
        private Vector2 mouseStartPosScreen;
        private InputAction mousePosition;

        public void Initialize(Segment segment, Pathbuilder pathbuilder)
        {
            if (initialized) return;
            cam = Camera.main;
            mousePosition = KeybindManager.Global.MousePosition;
            this.segment = segment;
            this.pathbuilder = pathbuilder;
            image = GetComponent<Image>();
            initialized = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pathbuilder.IsDraggingNote() || eventData.button != PointerEventData.InputButton.Left) return;

            segment.OnHandleDragStart();       
            pathbuilder.OnPointClicked(segment, this);

            startPosition = transform.position;
            startMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseStartPosScreen = mousePosition.ReadValue<Vector2>();
            isMouseDown = true;
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            
            segment.OnHandleDragStop();
            isMouseDown = false;
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        public void SetPosition(Vector2 position)
        {
            transform.position = position;
        }

        public void SetColor(Color color)
        {
            image.color = color;
        }

        public void ShouldSnap(bool snap)
        {
            //drag.shouldSnap = snap;
            shouldSnap = snap;
        }

        void Update()
        {
            if (isMouseDown)
            {
                float moveDistance = Math.Abs(mouseStartPosScreen.magnitude - mousePosition.ReadValue<Vector2>().magnitude);
                if (moveDistance > minMoveDistanceBeforeDragStart)
                {
                    Vector3 mousePos = cam.ScreenToWorldPoint(mousePosition.ReadValue<Vector2>());

                    Vector3 currentPosition = mousePos;

                    Vector3 diff = currentPosition - startMousePosition;

                    Vector3 pos = startPosition + diff;
                    pos.z = transform.position.z;

                    if (!shouldSnap)
                    {
                        transform.position = pos;
                    }
                    else transform.position = NoteGridSnap.SnapToGrid(new Vector3(pos.x, pos.y, -1f), SnappingMode.Grid);
                }
            }
        }
    }
}

