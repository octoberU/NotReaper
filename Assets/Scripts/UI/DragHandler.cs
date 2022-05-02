using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using NotReaper.UserInput;
using NotReaper.Grid;
using NotReaper.Models;
using System;
using UnityEngine.InputSystem;

public class DragHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    internal bool isMouseDown = false;
    private Vector3 startMousePosition;
    private Vector3 startPosition;
    public bool shouldReturn;
    private Camera cam;
    public bool shouldSnap;
    public float minMoveDistanceBeforeDragStart = 0f;
    private Vector2 mouseStartPosScreen;
    internal bool isCanvasInOverlayMode;
    private bool threshholdMet = false;
    private InputAction mousePosition;
    public bool preventInputWhenCtrlDown = false;
    private void Awake()
    {
        cam = Camera.main;
    }

    private void Start()
    {
        mousePosition = KeybindManager.Global.MousePosition;
    }

    public void OnPointerDown(PointerEventData dt)
    {
        if (preventInputWhenCtrlDown && KeybindManager.Global.Modifier.IsCtrlDown())
            return;

        isMouseDown = true;
        startPosition = transform.position;
        if (isCanvasInOverlayMode)
        {
            startMousePosition = Input.mousePosition;
        }
        else
        {
            startMousePosition = cam.ScreenToWorldPoint(Input.mousePosition);
        }
        mouseStartPosScreen = mousePosition.ReadValue<Vector2>();
    }

    public void OnPointerUp(PointerEventData dt)
    {
        isMouseDown = false;
        threshholdMet = false;
        if (shouldReturn)
        {
            transform.position = startPosition;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isMouseDown)
        {
            float moveDistance = Math.Abs(mouseStartPosScreen.magnitude - mousePosition.ReadValue<Vector2>().magnitude);
            if (moveDistance > minMoveDistanceBeforeDragStart || threshholdMet)
            {
                threshholdMet = true;
                Vector3 mousePos;
                if (isCanvasInOverlayMode)
                {
                    mousePos = mousePosition.ReadValue<Vector2>();
                }
                else
                {
                    mousePos = cam.ScreenToWorldPoint(mousePosition.ReadValue<Vector2>());
                }

                Vector3 currentPosition = mousePos;

                Vector3 diff = currentPosition - startMousePosition;

                Vector3 pos = startPosition + diff;
                pos.z = transform.position.z;

                if (!shouldSnap)
                {
                    transform.position = pos;
                }
                else if(shouldSnap && !isCanvasInOverlayMode) transform.position = NoteGridSnap.SnapToGrid(new Vector3(pos.x, pos.y, -1f), SnappingMode.Grid);
            }          
        }
    }
}