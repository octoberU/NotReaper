using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Models;
using NotReaper.UserInput;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NotReaper.Grid {


    public class IsHoveringGrid : Singleton<IsHoveringGrid>//, IPointerEnterHandler, IPointerExitHandler
    {

        public HoverTarget hover;
        public LayerMask layerMask;
        [Range(1, 144)] public int raycastsPerSecond = 10;
        private BoxCollider2D defaultCollider;
        private BoxCollider2D pathBuilderCollider;

        private Camera cam;
        private InputAction mousePosition;

        private Vector2 defaultSize;
        private Vector2 defaultOffset;

        private Vector2 pathBuilderSize = new Vector2(628.7609f, 339.6537f);
        private Vector2 pathBuilderOffset = new Vector2(-0.9622803f, 27.91457f);

        protected override void Awake()
        {
            base.Awake();
        }
        public void Start()
        {
            defaultCollider = GetComponent<BoxCollider2D>();
            defaultSize = defaultCollider.size;
            defaultOffset = defaultCollider.offset;
            cam = CameraProvider.main;
            mousePosition = KeybindManager.Global.MousePosition;
            StartCoroutine(Raycast());
        }

        private IEnumerator Raycast()
        {
            var waitItem = new WaitForSeconds(1f / raycastsPerSecond);
            while (true)
            {
                if (EditorState.IsInUI)
                {
                    if (hover.IconEnabled)
                    {
                        hover.TryDisable();
                        EditorState.SetIsOverGrid(false);
                    }
                }
                else
                {

                    if(!hover.IconEnabled && (EditorState.Tool.Current == EditorTool.ChainBuilder || EditorState.Tool.Current == EditorTool.DragSelect))
                    {
                        EditorState.SetIsOverGrid(true);
                        hover.Enable();
                    }
                    else
                    {
                        CheckGrid();
                    }
                }

                yield return waitItem;
            }
        }

        public void CheckGrid()
        {
            Vector2 point = cam.ScreenToWorldPoint(mousePosition.ReadValue<Vector2>());
            RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero, 0f, layerMask);
            if (hit.collider != null)
            {
                if (hit.collider.tag == "Grid")
                {
                    if(!EditorState.IsOverGrid || !hover.IconEnabled)
                    {
                        EditorState.SetIsOverGrid(true);
                        hover.Enable();
                    }
                }
                else
                {
                    if(EditorState.IsOverGrid || hover.IconEnabled)
                    {
                        EditorState.SetIsOverGrid(false);
                        hover.TryDisable();
                    }
                }
            }
            else
            {
                if (EditorState.IsOverGrid || hover.IconEnabled)
                {
                    EditorState.SetIsOverGrid(false);
                    hover.TryDisable();
                }
            }
        }

        public bool CanPlaceNote()
        {
            CheckGrid();

            if (!EditorState.IsOverGrid || EditorState.IsInUI || EditorState.IsOverTimeline || (EditorState.Tool.Current != EditorTool.None && EditorState.Tool.Current != EditorTool.SpacingSnapper))
                return false;

            var pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = mousePosition.ReadValue<Vector2>();
            List<RaycastResult> result = new();
            EventSystem.current.RaycastAll(pointerData, result);
            if (result.Any(r => r.gameObject.tag == "TransformOverlay"))
                return false;

            return true;
             
        }

        public void ChangeColliderSize(bool grow)
        {
            if (grow)
            {
                defaultCollider.size = pathBuilderSize;
                defaultCollider.offset = pathBuilderOffset;

                if (!EditorState.IsOverGrid)
                {
                    EditorState.SetIsOverGrid(true);
                    hover.Enable();
                }
            }
            else
            {
                defaultCollider.size = defaultSize;
                defaultCollider.offset = defaultOffset;
            }
            defaultCollider.enabled = false;
            defaultCollider.enabled = true;
        }

        [NRListener]
        private void OnUIToolUpdated(EditorTool tool)
        {
            ChangeColliderSize(tool == EditorTool.ChainBuilder || tool == EditorTool.Pathbuilder);
        }   
    }

}