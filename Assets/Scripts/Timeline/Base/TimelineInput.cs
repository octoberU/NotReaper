using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Modifiers;
using NotReaper.Timing;
using NotReaper.UI;
using NotReaper.UserInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NotReaper
{
    public abstract class TimelineInput<TKeybinds, TData> : NRInput<TKeybinds> where TKeybinds : new() where TData : ContentData
    {
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private OnHover onHover;

        private TimelineManager<TData> manager;
        private TrackManager tracks;
        private GridTimeline timeline;
        
        private Camera cam;
        private Camera timelineCam;
        private bool mouseDown;

        private bool isActive = false;

        private bool _isHovering = false;

        private Vector2 dragStartPos;
        private bool allowDrag = false;
        private bool isDragging;

        private GameObject selectionBox;
        private Renderer selectionBoxRenderer;

        protected abstract bool AllowTrackSwitching { get; }
        protected abstract bool AllowContentMoving { get; }
        protected abstract TimelineManager<TData> GetManager();
        protected abstract TrackManager GetTrackManager();

        protected virtual void Start()
        {
            manager = GetManager();
            tracks = GetTrackManager();
            timeline = NRDependencyInjector.Get<GridTimeline>();
            
            cam = CameraProvider.menu;
            timelineCam = CameraProvider.timeline;
            onHover?.onHover.AddListener(OnSidebarHover);
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

        protected void EnableScrubbing(bool enable)
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
        
        protected void OnLeftClick()
        {
            var mousePos = GetMousePosition();
            mouseDown = true;
            dragStartPos = GetTimelineMousePosition();
            var trackContent = GetTrackContentUnderMouse(mousePos);

            if (trackContent != null)
            {
                var timeFromPosition = GetTimeFromPosition(mousePos);
                bool isCtrlDown = KeybindManager.Global.Modifier.IsCtrlDown();
                if (TryGetContentUnderMouse(out var content))
                {
                    manager.SelectContent(content, isCtrlDown);
                    
                    var timeframe = manager.CurrentContent.timeframe;
                    var endDiff = timeframe.End - timeFromPosition.tick;
                    var startDiff = timeFromPosition.tick - timeframe.Start;
                    manager.StartMove(GetMousePosition());
                    if (endDiff < startDiff || timeframe.End == timeframe.Start)
                    {
                        StartCoroutine(DragEnd(true, content, content.timeframe, false));
                    }
                    else
                    {
                        StartCoroutine(DragStart(content, content.timeframe));
                    }

                    allowDrag = false;
                    return;
                }
                else if (!isCtrlDown)
                {
                    DeselectAll();                    
                }

                allowDrag = true;
                if (isCtrlDown) return;
                
                if (manager.TryPlaceContent(GetSnappedTimeFromPosition(mousePos), trackContent))
                {
                    manager.SelectCurrentContent();
                    if (ModifierUtility.SupportsEndTime((ModifierType)trackContent.tracks[GridTimeline.Type].Type, false, false))
                    {
                        StartCoroutine(DragEnd(false, null, new(), true));
                    }
                }
            }
        }
        
        private IEnumerator DragStart(Content potentialReselectContent, Timeframe oldPotentialTimeframe)
        {
           
            var lastTime = GetSnappedTimeFromPosition(GetMousePosition());
            bool hasTriedReselect = false;
            Vector2 startMousePosition = GetMousePosition();
            while (mouseDown)
            {
                var mousePosition = GetMousePosition();
                
                if (!hasTriedReselect && potentialReselectContent != null && Vector2.Distance(mousePosition, startMousePosition) >= .01f)
                {
                    manager.ReselectContentFromDrag(potentialReselectContent, oldPotentialTimeframe, startMousePosition);
                    hasTriedReselect = true;
                }
                
                if (AllowTrackSwitching)
                {
                    manager.TrySwitchTrack(mousePosition);
                }

                if (AllowContentMoving)
                {
                    var currentTime = GetSnappedTimeFromPosition(mousePosition);
                    if (currentTime != lastTime)
                    {
                        bool increase = currentTime > lastTime;
                        lastTime = currentTime;
                        manager.SetStartTime(increase, !KeybindManager.Global.Modifier.IsShiftDown());
                    }
                }
                yield return null;
            }
        }

        private IEnumerator DragEnd(bool allowMove, Content potentialReselectContent, Timeframe oldPotentialTimeframe, bool initialTimeSet)
        {
            var lastTime = GetSnappedTimeFromPosition(GetMousePosition());
            bool hasTriedReselect = false;
            Vector2 startMousePosition = GetMousePosition();
            while (mouseDown)
            {
                var mousePosition = GetMousePosition();
                if (!hasTriedReselect && potentialReselectContent != null && Vector2.Distance(mousePosition, startMousePosition) >= .01f)
                {
                    manager.ReselectContentFromDrag(potentialReselectContent, oldPotentialTimeframe, startMousePosition);
                    hasTriedReselect = true;
                }
                    
                    
                if (AllowTrackSwitching)
                {
                    manager.TrySwitchTrack(mousePosition);
                }
                

                if (AllowContentMoving)
                {
                    var currentTime = GetSnappedTimeFromPosition(mousePosition);
                    if (currentTime != lastTime)
                    {
                        bool increase = currentTime > lastTime;
                        lastTime = currentTime;
                        manager.SetEndTime(increase, allowMove && !KeybindManager.Global.Modifier.IsShiftDown(), initialTimeSet);
                    }
                }
                yield return null;
            }
        }
        
        protected void EndDrag()
        {
            mouseDown = false;
            isDragging = false;
            timeline.selectionBox.SetActive(false);
            manager.EndMove();
            OnSidebarHover(_isHovering);
        }

        protected void OnRightClick()
        {
            if(TryGetContentUnderMouse(out var content))
                manager.TryRemoveContent(content);
        }

        protected bool TryGetContentUnderMouse(out Content content)
        {
            content = null;
            var hits = PerformRaycast();
            if (hits.Any(hit => hit.collider.CompareTag(RaycastContentTag)))
            {
                var hit = hits.First(hit => hit.collider.CompareTag(RaycastContentTag));
                if (hit.transform.TryGetComponent(out content))
                    return true;
            }

            return false;
        }
        
        private RaycastHit2D[] PerformRaycast() => Physics2D.RaycastAll(GetTimelineMousePosition(), Vector2.zero, 0f);
        protected abstract string RaycastContentTag { get; }
        
        protected void OnDeletePressed() => manager.RemoveSelectedContent();

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
        
        protected void OnScrub(bool up)
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
                ScrollUp();
            }
            else
            {
                ScrollDown();
            }
        }

        public void ScrollUp() => tracks.ScrollUp();
        public void ScrollDown() => tracks.ScrollDown();
        
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
                foreach (var modifier in manager.Content)
                {
                    if (modifier.IsInsideBounds(bounds))
                    {
                        manager.MultiSelectContent(modifier);
                    }
                    else
                    {
                        manager.DeselectMultiselectContent(modifier);
                    }
                }
            }
        }
        
        protected void MoveSelectedContentUp()
        {
            if (!AllowTrackSwitching) return;
            manager.MoveSelectedContentUp(GetMousePosition());
        }

        protected void MoveSelectedContentDown()
        {
            if (!AllowTrackSwitching) return;
            manager.MoveSelectedContentDown(GetMousePosition());
        }

        public virtual void Activate() => OnActivated();
        public virtual void Deactivate() => OnDeactivated();

        protected void Copy() => manager.CopySelectedContent();
        protected void Cut() => manager.CutSelectedContent();
        protected void Paste() => manager.PasteContent(EditorTime.Time);
        protected void SelectAll() => manager.SelectAll();
        protected void DeselectAll() => manager.DeselectAll();

        protected override void OnEscPressed(InputAction.CallbackContext context)
            => manager.ToggleTimeline();
    }
}