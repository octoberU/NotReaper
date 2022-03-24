using NotReaper.Models;
using NotReaper.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NotReaper
{
    public class TimelineCameraMouseHandler : MonoBehaviour
    {
        private Timeline timeline;
        private MiniTimeline miniTimeline;
        private Camera cam;
        private InputAction mousePosition;
        private bool hasClickedOnMiniTimeline;

        [SerializeField] private LayerMask layerMask;
        [SerializeField, Range(1, 60), Tooltip("How many times per second we raycast")] private int raycastsPerSecond = 10;

        private bool mouseDown;

        private void Start()
        {
            timeline = NRDependencyInjector.Get<Timeline>();
            miniTimeline = NRDependencyInjector.Get<MiniTimeline>();
            cam = CameraProvider.menu;
            mousePosition = KeybindManager.Global.MousePosition;
            KeybindManager.onMouseDown += MouseDown;
            StartCoroutine(Raycast());
        }

        private void MouseDown(bool down)
        {
            mouseDown = down;

            if (mouseDown)
            {
                OnClick();
            }
            else
            {
                if (hasClickedOnMiniTimeline)
                {
                    hasClickedOnMiniTimeline = false;
                    miniTimeline.MouseUp();
                }
                
            }
        }
       
        private void OnClick()
        {
            Vector2 point = cam.ScreenToWorldPoint(mousePosition.ReadValue<Vector2>());
            RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero, 0f);
            if (hit.collider != null)
            {
                if (hit.collider.tag == "Timeline" || hit.collider.name == "Timeline")
                {
                    if (EditorState.Tool.Current == EditorTool.DragSelect || EditorState.Tool.Current == EditorTool.Pathbuilder || EditorState.Tool.Current == EditorTool.ChainBuilder) return;
                    timeline.JumpToX(cam.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>()).x - cam.transform.position.x);
                }
                else if(hit.collider.tag == "MiniTimeline")
                {
                    hasClickedOnMiniTimeline = true;
                    miniTimeline.MouseDown();
                    StartCoroutine(DoDrag());
                }
            }
        }

        private IEnumerator DoDrag()
        {
            while (hasClickedOnMiniTimeline)
            {               
                miniTimeline.DoDrag();
                yield return null;               
            }
            yield return null;
        }

        private IEnumerator Raycast()
        {
            while (true)
            {
                Vector2 point = cam.ScreenToWorldPoint(mousePosition.ReadValue<Vector2>());
                RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero, 0f, layerMask);
                if (hit.collider != null)
                {
                    if (hit.collider.tag == "Timeline")
                    {
                        timeline.hover = true;
                    }
                    else
                    {
                        timeline.hover = false;
                    }
                }
                else
                {
                    timeline.hover = false;
                }

                yield return new WaitForSeconds(1f / raycastsPerSecond);
            }
        }
    }
}

