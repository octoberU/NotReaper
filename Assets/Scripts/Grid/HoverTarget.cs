using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.UserInput;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NotReaper.Grid
{


    public class HoverTarget : MonoBehaviour
    {

        public GameObject icon;
        public bool IconEnabled => icon.activeInHierarchy;
        public GameObject cursor;
        public Image cursorTint;

        private Camera cam;
        private Canvas canvas;
        private bool spacingLocked = false;
        private bool isBehavior = true;
        private bool useCustomCursor = false;

        [SerializeField] private Image standard;
        [SerializeField] private Image hold;
        [SerializeField] private Image horizontal;
        [SerializeField] private Image vertical;
        [SerializeField] private Image chainstart;
        [SerializeField] private Image chainnode;
        [SerializeField] private Image melee;
        [SerializeField] private Image mine;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private Texture2D cursorTexture;

        private List<Image> behaviors = new List<Image>();

        private void Awake()
        {
            cam = Camera.main;
            canvas = GetComponent<Canvas>();
            behaviors.Add(standard);
            behaviors.Add(hold);
            behaviors.Add(horizontal);
            behaviors.Add(vertical);
            behaviors.Add(chainstart);
            behaviors.Add(chainnode);
            behaviors.Add(melee);
            behaviors.Add(mine);

            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);
        }

        private void Start()
        {
            canvas.overrideSorting = false;
            NRSettings.OnLoad(() =>
            {
                EnableCustomCursor(NRSettings.config.useNRCursor);
            });

            NRSettings.onSettingsSaved += (NRJsonSettings config) => EnableCustomCursor(config.useNRCursor);
        }

        private void EnableCustomCursor(bool enable)
        {
            Cursor.SetCursor(enable ? cursorTexture : null, Vector2.zero, enable ? CursorMode.ForceSoftware : CursorMode.Auto);
            useCustomCursor = enable;
        }

        public void Enable() => icon.SetActive(true);

        public void TryDisable()
        {
            if (EditorState.Tool.Current == EditorTool.ChainBuilder || 
                EditorState.Tool.Current == EditorTool.DragSelect || 
                EditorState.Tool.Current == EditorTool.Pathbuilder)
                return;

            icon.SetActive(false);
        }

        public void UpdateDistance(string text) => distanceText.text = text.ToLower();
        public void LockSpacing(bool doLock) => spacingLocked = doLock;

        private void Update()
        {
            if (!IconEnabled || spacingLocked) return;

            UpdatePosition();
        }
        
        

        public void UpdatePosition()
        {
            if (spacingLocked)
                return;
            
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position = isBehavior ? NoteGridSnap.SnapToGrid(new Vector3(mousePos.x, mousePos.y, -1f), EditorState.Snapping.Current) : new Vector3(mousePos.x, mousePos.y, -1f);
        }


        public float animColorSpeed = 0.3f;
        [NRListener]
        private void UpdateUIHandColor(TargetHandType _)
        {
            var color = (EditorState.Behavior.Current == TargetBehavior.Melee ? Color.gray : NRSettings.GetSelectedColor());

            foreach (var behavior in behaviors)
            {
                behavior.DOColor(color, animColorSpeed);
            }
            cursorTint.DOColor(color, animColorSpeed);
        }

        public delegate void OnUIToolUpdated(EditorTool tool);
        private List<OnUIToolUpdated> callbacks = new List<OnUIToolUpdated>();
        public void RegisterOnUIToolUpdatedCallback(OnUIToolUpdated callback)
        {
            callbacks.Add(callback);
        }

        [NRListener]
        private void UpdateUITool(EditorTool tool)
        {
            if (tool == EditorTool.None)
            {
                UpdateUIBehavior(EditorState.Behavior.Current);
            }
            else
            {
                if (!useCustomCursor)
                {
                    cursor.SetActive(true);
                    Cursor.visible = false;
                }
                EnableIcon(TargetBehavior.None);
                isBehavior = false;
                canvas.overrideSorting = true;
            }

        }

        [NRListener]
        private void UpdateUIBehavior(TargetBehavior behavior)
        {
            if (!useCustomCursor)
            {
                cursor.SetActive(false);
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            icon.SetActive(false);
            EnableIcon(behavior);
            isBehavior = true;
            canvas.overrideSorting = false;
        }

        private void EnableIcon(TargetBehavior behavior)
        {
            standard.gameObject.SetActive(behavior == TargetBehavior.Standard);
            hold.gameObject.SetActive(behavior == TargetBehavior.Sustain);
            horizontal.gameObject.SetActive(behavior == TargetBehavior.Horizontal);
            vertical.gameObject.SetActive(behavior == TargetBehavior.Vertical);
            chainstart.gameObject.SetActive(behavior == TargetBehavior.ChainStart);
            chainnode.gameObject.SetActive(behavior == TargetBehavior.ChainNode);
            melee.gameObject.SetActive(behavior == TargetBehavior.Melee);
            mine.gameObject.SetActive(behavior == TargetBehavior.Mine);
            icon.SetActive(IconEnabled);
        }

    }

}