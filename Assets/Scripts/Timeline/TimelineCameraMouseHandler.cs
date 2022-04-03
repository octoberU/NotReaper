using NotReaper.Models;
using NotReaper.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NotReaper
{
    public class TimelineCameraMouseHandler : MonoBehaviour
    {
        private Timeline timeline;
        private MiniTimeline miniTimeline;
        private Camera menuCam;
        private Camera timelineCam;
        private InputAction mousePosition;
        private bool hasClickedOnMiniTimeline;

        [SerializeField] private LayerMask layerMask;
        [SerializeField, Range(1, 60), Tooltip("How many times per second we raycast")] private int raycastsPerSecond = 10;

        private bool mouseDown;

        private void Start()
        {
            timeline = NRDependencyInjector.Get<Timeline>();
            miniTimeline = NRDependencyInjector.Get<MiniTimeline>();
            menuCam = CameraProvider.menu;
            timelineCam = CameraProvider.timeline;
            mousePosition = KeybindManager.Global.MousePosition;
            KeybindManager.onMouseDown += MouseDown;
            StartCoroutine(Raycast());
        }

        private void MouseDown(bool down)
        {
            if (EditorState.IsInUI || EditorState.Tool.Current == EditorTool.ChainBuilder || EditorState.Tool.Current == EditorTool.Pathbuilder) return;

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
            var pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = mousePosition.ReadValue<Vector2>();
            List<RaycastResult> result = new();
            EventSystem.current.RaycastAll(pointerData, result);
            if (result.Any(r => r.gameObject.tag == "BeatLengthLine"))
            {
                //we don't want to drag the timeline if we're dragging a sustain.
                return;
            }


            Vector2 point = menuCam.ScreenToWorldPoint(mousePosition.ReadValue<Vector2>());
            var hits = Physics2D.RaycastAll(point, Vector2.zero, 0f);
            if (hits != null)
            {
                if(HasTag(hits, "Timeline"))
                {
                    if (EditorState.Tool.Current == EditorTool.DragSelect || EditorState.Tool.Current == EditorTool.Pathbuilder || EditorState.Tool.Current == EditorTool.ChainBuilder) return;
                    EditorAudio.JumpToBeat(menuCam.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>()).x + timelineCam.transform.position.x); //- cam.transform.position.x);
                }
                else if(HasTag(hits, "MiniTimeline"))
                {
                    hasClickedOnMiniTimeline = true;
                    miniTimeline.MouseDown();
                    StartCoroutine(DoDrag());
                }
            }
        }

        private bool HasTag(RaycastHit2D[] hits, string tag) => hits.Any(hit => hit.collider.tag == tag);

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
                Vector2 point = menuCam.ScreenToWorldPoint(mousePosition.ReadValue<Vector2>());
                RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero, 0f, layerMask);
                if (hit.collider != null)
                {
                    if (hit.collider.tag == "Timeline")
                    {
                        EditorState.SetOverTimeline(true);
                    }
                    else
                    {
                        EditorState.SetOverTimeline(false);
                    }
                }
                else
                {
                    EditorState.SetOverTimeline(false);
                }

                yield return new WaitForSeconds(1f / raycastsPerSecond);
            }
        }
    }
}

