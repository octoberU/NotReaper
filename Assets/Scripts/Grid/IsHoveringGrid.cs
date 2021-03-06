﻿using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.UserInput;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NotReaper.Grid {


    public class IsHoveringGrid : MonoBehaviour//, IPointerEnterHandler, IPointerExitHandler
    {
        public static IsHoveringGrid Instance = null;

        public HoverTarget hover;

        private BoxCollider2D defaultCollider;
        private BoxCollider2D pathBuilderCollider;


        private Vector2 defaultSize;
        private Vector2 defaultOffset;

        private Vector2 pathBuilderSize = new Vector2(628.7609f, 339.6537f);
        private Vector2 pathBuilderOffset = new Vector2(-0.9622803f, 27.91457f);
        /*
        public void OnPointerEnter(PointerEventData eventData)
        {
            EditorInput.isOverGrid = true;
            hover.TryEnable();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            EditorInput.isOverGrid = false;
            hover.TryDisable();
        }

        */
        public void Start()
        {
            if (Instance is null) Instance = this;
            else
            {
                Debug.LogWarning("IsHoverhingGrid already exists.");
                return;
            }
            //BoxCollider2D[] colliders = GetComponents<BoxCollider2D>();
            //defaultCollider = colliders[0];
            //pathBuilderCollider = colliders[1];
            defaultCollider = GetComponent<BoxCollider2D>();
            defaultSize = defaultCollider.size;
            defaultOffset = defaultCollider.offset;
            //pathBuilderCollider = gameObject.AddComponent<BoxCollider2D>();
            //pathBuilderCollider.size = new Vector2(628.7609f, 339.6537f);
            //pathBuilderCollider.offset = new Vector2(-0.9622803f, 27.91457f);
            //pathBuilderCollider.enabled = false;
            hover.RegisterOnUIToolUpdatedCallback(OnUIToolUpdated);
        }

        private void EnableCollider(bool enableDefault)
        {
            if (enableDefault)
            {
                defaultCollider.size = defaultSize;
                defaultCollider.offset = defaultOffset;
            }
            else
            {
                defaultCollider.size = pathBuilderSize;
                defaultCollider.offset = pathBuilderOffset;
            }
            defaultCollider.enabled = false;
            defaultCollider.enabled = true;
            //defaultCollider.enabled = enableDefault;
            //pathBuilderCollider.enabled = !enableDefault;
        }

        private void OnUIToolUpdated(EditorTool tool)
        {
            EnableCollider(tool != EditorTool.ChainBuilder);
        }


        public void OnMouseOver()
        {
            if(EditorInput.inUI)
            {
                if (hover.iconEnabled)
                {
                    hover.TryDisable();
                }
                return;
            }
            if (!hover.iconEnabled || ((EditorInput.selectedTool == EditorTool.ChainBuilder || EditorInput.selectedTool == EditorTool.DragSelect) && !EditorInput.isOverGrid))
            {
                EditorInput.isOverGrid = true;
                hover.TryEnable();
            }
        }
        /*
        public void OnMouseEnter()
        {
            EditorInput.isOverGrid = true;
            hover.TryEnable();
        }
        */
        public void OnMouseExit()
        {
            EditorInput.isOverGrid = false;
            hover.TryDisable();
        }
       
    }

}