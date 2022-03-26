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
        public bool iconEnabled = true;
        public GameObject cursor;
        public Image cursorTint;

        private Camera cam;
        private Canvas canvas;
        private bool spacingLocked = false;
        private bool isBehavior = true;

        [SerializeField] private Image standard;
        [SerializeField] private Image hold;
        [SerializeField] private Image horizontal;
        [SerializeField] private Image vertical;
        [SerializeField] private Image chainstart;
        [SerializeField] private Image chainnode;
        [SerializeField] private Image melee;
        [SerializeField] private Image mine;
        [SerializeField] private TextMeshProUGUI distanceText;

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
        }

        private void Start()
        {
            canvas.overrideSorting = false;
            //UpdateUIHandColor(EditorState.GetSelectedColor());
            //UpdateUITool(EditorState.Tool.Current);
        }

        public void Enable()
        {
            iconEnabled = true;
            icon.SetActive(true);
        }
        public void TryDisable()
        {
            switch (EditorState.Tool.Current)
            {
                case EditorTool.ChainBuilder:
                case EditorTool.DragSelect:
                case EditorTool.Pathbuilder:
                    break;

                default:
                    iconEnabled = false;
                    icon.SetActive(false);
                    break;
            }
        }

        public void UpdateDistance(string text)
        {
            distanceText.text = text.ToLower();
        }

        public void LockSpacing(bool doLock)
        {
            spacingLocked = doLock;
        }

        private void Update()
        {

            if (!iconEnabled || spacingLocked) return;

            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position = isBehavior ? NoteGridSnap.SnapToGrid(new Vector3(mousePos.x, mousePos.y, -1f), EditorState.Snapping.Current) : new Vector3(mousePos.x, mousePos.y, -1f);
            /*
            switch (EditorState.Tool.Current) {
                case EditorTool.ChainBuilder:
                case EditorTool.DragSelect:
                case EditorTool.Pathbuilder:

                    transform.position = new Vector3(mousePos.x, mousePos.y, -1f);

                    break;

                default:
                    Vector3 newPos = NoteGridSnap.SnapToGrid(new Vector3(mousePos.x, mousePos.y, -1f), EditorState.Snapping.Current);
                    //if (newPos == lastPos) {
                    //return;
                    //}
                    //lastPos = newPos;
                    //newPos.z = 1;
                    transform.position = newPos;
                    //transform.position = Vector3.SmoothDamp(transform.position, newPos, ref velocity, 0.3f);
                    break;
            }
            */
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
                cursor.SetActive(true);
                EnableIcon(TargetBehavior.None);
                isBehavior = false;
                canvas.overrideSorting = true;
            }

        }

        [NRListener]
        private void UpdateUIBehavior(TargetBehavior behavior)
        {
            cursor.SetActive(false);
            iconEnabled = false;
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
        }
        /*
        [NRListener]
        private void UpdateUITool(EditorTool tool) {
            if (tool == EditorTool.DragSelect || tool == EditorTool.ChainBuilder || tool == EditorTool.Pathbuilder) {
                cursor.SetActive(true);
            } else {
                cursor.SetActive(false);
                iconEnabled = false;
                icon.SetActive(false);
            }
            standard.gameObject.SetActive(tool == EditorTool.Standard);
            hold.gameObject.SetActive(tool == EditorTool.Sustain);
            horizontal.gameObject.SetActive(tool == EditorTool.Horizontal);
            vertical.gameObject.SetActive(tool == EditorTool.Vertical);
            chainstart.gameObject.SetActive(tool == EditorTool.ChainStart);
            chainnode.gameObject.SetActive(tool == EditorTool.ChainNode);
            melee.gameObject.SetActive(tool == EditorTool.Melee);
            mine.gameObject.SetActive(tool == EditorTool.Mine);
            //UpdateUIHandColor(EditorState.GetSelectedColor());
            foreach(var callback in callbacks)
            {
                callback.Invoke(tool);
            }
        }
        */

    }

}