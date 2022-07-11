using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Models;
using NotReaper.Tools;
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

        private bool initialized = false;

        private void Initialize()
        {
            if (initialized) return;
            
            defaultCollider = GetComponent<BoxCollider2D>();
            defaultSize = defaultCollider.size;
            defaultOffset = defaultCollider.offset;
            cam = CameraProvider.main;
            mousePosition = KeybindManager.Global.MousePosition;

            initialized = true;
        }

        private void OnDisable() => StopCoroutine(Raycast());

        private void OnEnable()
        {
            Initialize();
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

            if (!EditorState.IsOverGrid || EditorState.IsInUI || EditorState.IsOverTimeline || 
                (EditorState.Tool.Current != EditorTool.None && EditorState.Tool.Current != EditorTool.SpacingSnapper) || 
                TransformTool.IsPointerOverTransformOverlay())
                return false;

            hover.UpdatePosition();
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