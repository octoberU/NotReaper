using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NotReaper.Timing;
using NotReaper.UI;
using NotReaper.UserInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NotReaper.Modifiers
{
    public class ModifierInputManager : NRInput<ModifierKeybinds>
    {
        [SerializeField] private GameObject zOffsetBakingWindow;
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private OnHover onHover;
        [NRInject] private ModifierManager manager;
        [NRInject] private TrackManager tracks;
        [NRInject] private ModifierTimeline timeline;

        private Camera cam;
        private Camera timelineCam;
        private bool mouseDown;

        private bool isActive = false;

        private bool _isHovering = false;

        private QNT_Timestamp dragStartTime;

        private Vector2 dragStartPos;
        private bool allowDrag = false;
        private bool isDragging;
        private int dragStartIndex = 0;

        private GameObject selectionBox;
        private Renderer selectionBoxRenderer;

        private void Start()
        {
            cam = CameraProvider.menu;
            timelineCam = CameraProvider.timeline;
            onHover.onHover.AddListener(OnSidebarHover);
            selectionBox = timeline.selectionBox;
            selectionBoxRenderer = selectionBox.GetComponent<Renderer>();
        }

        private void OnSidebarHover(bool isHovering)
        {
            if (!manager.IsActive)
            {
                return;
            }
            
            EnableScrubbing(!isHovering);

            _isHovering = isHovering;
        }

        private void EnableScrubbing(bool enable)
        {
            if (enable)
            {
                KeybindManager.EnableKeybind("Scrub");
                KeybindManager.EnableKeybind("ScrubByTick");
            }
            else
            {
                KeybindManager.DisableKeybind("Scrub");
                KeybindManager.DisableKeybind("ScrubByTick");
            }
        }

        public void Activate() => OnActivated();
        public void Deactivate() => OnDeactivated();

        private void OnLeftClick()
        {
            var mousePos = GetMousePosition();
            mouseDown = true;
            dragStartPos = GetTimelineMousePosition();
            dragStartTime = new(QNT_Duration.FromBeatTime(dragStartPos.x).tick);
            dragStartIndex = tracks.CurrentIndex;
            var content = GetTrackContentUnderMouse(mousePos);

            if (content != null)
            {
                var timeFromPosition = GetTimeFromPosition(mousePos);
                bool isCtrlDown = KeybindManager.Global.Modifier.IsCtrlDown();
                if (TryGetModifierUnderMouse(out var modifier))
                {
                    manager.SelectModifier(modifier, isCtrlDown);
                    
                    var timeframe = manager.CurrentModifier.timeframe;
                    var endDiff = timeframe.End - timeFromPosition.tick;
                    var startDiff = timeFromPosition.tick - timeframe.Start;
                    if (endDiff < startDiff || timeframe.End == timeframe.Start)
                    {
                        StartCoroutine(DragEnd(true, modifier));
                    }
                    else
                    {
                        StartCoroutine(DragStart(modifier));
                    }

                    allowDrag = false;
                    return;
                }

                allowDrag = true;
                if (isCtrlDown) return;
                
                if (manager.TryPlaceModifier(GetSnappedTimeFromPosition(mousePos), content))
                {
                    manager.SelectCurrentModifier();
                    if (ModifierUtility.SupportsEndTime(content.track.Type, false, false))
                    {
                        StartCoroutine(DragEnd(false, null));
                    }
                }
            }
        }

        private IEnumerator DragStart(Modifier potentialReselectModifier)
        {
            var lastTime = GetSnappedTimeFromPosition(GetMousePosition());
            bool hasTriedReselect = false;
            while (mouseDown)
            {
                var currentTime = GetSnappedTimeFromPosition(GetMousePosition());
                if (currentTime != lastTime)
                {
                    if (!hasTriedReselect && potentialReselectModifier != null)
                    {
                        manager.MultiSelectModifier(potentialReselectModifier);
                        hasTriedReselect = true;
                    }
                    bool increase = currentTime > lastTime;
                    lastTime = currentTime;
                    manager.SetStartTime(increase, KeybindManager.Global.Modifier.IsShiftDown());
                }
                yield return null;
            }
        }

        private IEnumerator DragEnd(bool allowMove, Modifier potentialReselectModifier)
        {
            var lastTime = GetSnappedTimeFromPosition(GetMousePosition());
            bool hasTriedReselect = false;
            while (mouseDown)
            {
                var currentTime = GetSnappedTimeFromPosition(GetMousePosition());
                if (currentTime != lastTime)
                {
                    if (!hasTriedReselect && potentialReselectModifier != null)
                    {
                        manager.MultiSelectModifier(potentialReselectModifier);
                        hasTriedReselect = true;
                    }
                    
                    bool increase = currentTime > lastTime;
                    lastTime = currentTime;
                    manager.SetEndTime(increase, allowMove && KeybindManager.Global.Modifier.IsShiftDown());
                }
                yield return null;
            }
        }
        
        private void EndDrag()
        {
            mouseDown = false;
            isDragging = false;
            timeline.selectionBox.SetActive(false);
            OnSidebarHover(_isHovering);
        }

        private void OnRightClick()
        {
            if(TryGetModifierUnderMouse(out var modifier))
                manager.TryRemoveModifier(modifier);
        }

        private bool TryGetModifierUnderMouse(out Modifier modifier)
        {
            modifier = null;
            var hits = PerformRaycast();
            if (hits.Any(hit => hit.collider.tag == "Modifier"))
            {
                var hit = hits.First(hit => hit.collider.tag == "Modifier");
                if (hit.transform.TryGetComponent(out modifier))
                    return true;
            }

            return false;
        }
        
        private RaycastHit2D[] PerformRaycast() => Physics2D.RaycastAll(GetTimelineMousePosition(), Vector2.zero, 0f);
        private bool HasTag(RaycastHit2D[] hits, string tag) => hits.Any(hit => hit.collider.tag == tag);

        private void OnDeletePressed() => manager.RemoveSelectedModifiers();

        private Vector2 GetMousePosition()
            =>  cam.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>());

        private Vector2 GetTimelineMousePosition()
            => timelineCam.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>());

        private QNT_Timestamp GetTimeFromPosition(Vector2 mousePosition)
        {
            mousePosition.x /= Timeline.scaleTransform;
            mousePosition.x -= Timeline.timelineNotesStatic.parent.position.x;
            return EditorTime.Time + Relative_QNT.FromBeatTime(mousePosition.x);
        }

        private QNT_Timestamp GetSnappedTimeFromPosition(Vector2 mousePosition)
        {
            mousePosition.x /= Timeline.scaleTransform;
            mousePosition.x -= Timeline.timelineNotesStatic.parent.position.x;
            return SnapToBeat(mousePosition.x);
        }

        private QNT_Timestamp SnapToBeat(float posX)
        {
            QNT_Timestamp time = EditorTime.Time + Relative_QNT.FromBeatTime(posX);
            return EditorTime.GetSnappedTime(time + EditorBeatSnap.Duration / 2, EditorBeatSnap.BeatSnap);
        }
        
        private TrackContent GetTrackContentUnderMouse(Vector2 point)
        {
            var hit = Physics2D.Raycast(point, Vector2.zero, 100f, layerMask);
            if (hit.collider == null) return null;
            hit.transform.TryGetComponent(out TrackContent content);
            return content;
        }

        private void ToggleZOffsetWindow()
            => zOffsetBakingWindow.SetActive(!zOffsetBakingWindow.activeInHierarchy);

        protected override void RegisterCallbacks()
        {
            actions.Modifiers.LeftMouseClick.started += _ => OnLeftClick();
            actions.Modifiers.LeftMouseClick.canceled += _ => EndDrag();
            actions.Modifiers.RemoveModifier.started += _ => OnRightClick();
            actions.Modifiers.Delete.started += _ => OnDeletePressed();
            actions.Modifiers.BakeZOffset.started += _ => ToggleZOffsetWindow();
            actions.Modifiers.Scrub.started += (ctx) => OnScrub(ctx.ReadValue<float>() > 0);
            actions.Modifiers.Undo.started += _ => ModifierUndoRedo.Undo();
            actions.Modifiers.Redo.started += _ => ModifierUndoRedo.Redo();
            actions.Modifiers.Copy.started += _ => manager.CopySelectedModifiers();
            actions.Modifiers.Cut.started += _ => manager.CutSelectedModifiers();
            actions.Modifiers.Paste.started += _ => manager.PasteModifiers(EditorTime.Time);
            actions.Modifiers.DeselectAll.started += _ => manager.DeselectAll();
            actions.Modifiers.SelectAll.started += _ => manager.SelectAll();
        }

        private void OnScrub(bool up)
        {
            if (isDragging)
            {
                EnableScrubbing(false);
            }
            
            if (!_isHovering)
            {
                if (isDragging)
                {
                    EditorAudio.ScrubTimeline(!up, false);
                }
                return;
            }

            if (up)
            {
                tracks.ScrollUp();
            }
            else
            {
                tracks.ScrollDown();
            }
        }

        private void Update()
        {
            if (allowDrag && mouseDown && KeybindManager.Global.Modifier.IsCtrlDown())
            {
                UpdateDragSelect();
            }
            else if (timeline.selectionBox.activeInHierarchy)
            {
                timeline.selectionBox.SetActive(false);
            }
        }
        private void UpdateDragSelect()
        {
            var size = dragStartPos - GetTimelineMousePosition();
            if (size.magnitude >= .2f)
            {
                isDragging = true;
                if(!timeline.selectionBox.activeInHierarchy) timeline.selectionBox.SetActive(true);
                Vector2 newPos = dragStartPos;

                timeline.selectionBox.transform.localScale = size;
                newPos += -(size * .5f);
                timeline.selectionBox.transform.position = newPos;
                
                var bounds = selectionBoxRenderer.bounds;
                foreach (var modifier in manager.Modifiers)
                {
                    if (modifier.IsInsideBounds(bounds))
                    {
                        manager.MultiSelectModifier(modifier);
                    }
                    else
                    {
                        manager.DeselectMultiselectModifier(modifier);
                    }
                }
            }
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            manager.ToggleModifiers();
        }

        protected override void SetRebindConfiguration(ref RebindConfiguration options, ModifierKeybinds myKeybinds)
        {
            options.AddCustomKeybindName(actions.Modifiers.LeftMouseClick, "Place Modifier");
        }
    }
}
