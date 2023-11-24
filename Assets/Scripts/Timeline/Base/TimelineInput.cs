using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Modifiers;
using NotReaper.Timing;
using NotReaper.UI;
using NotReaper.UserInput;
using Sirenix.Utilities;
using Tayx.Graphy.Utils.NumString;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NotReaper
{
    public abstract class TimelineInput<TKeybinds, TData> : NRInput<TKeybinds> where TKeybinds : new() where TData : ContentData
    {
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private OnHover onHover;

        private TimelineManager<TData> manager;
        protected TrackManager trackManager;
        private GridTimeline timeline;
        
        private Camera cam;
        private Camera timelineCam;
        private bool mouseDown;

        private bool isActive = false;

        private bool _isHovering = false;

        private int dragStartTrackIndex;
        private int currentTrackIndex;
        private QNT_Timestamp dragStartTime;
        private Vector2 dragStartPos;

        private bool allowDrag;
        private bool isDragging;

        private GameObject selectionBox;
        private Renderer selectionBoxRenderer;

        private bool _isScrollLocked = false;

        protected abstract bool AllowTrackSwitching { get; }
        protected abstract bool AllowContentMoving { get; }
        protected abstract TimelineManager<TData> GetManager();
        protected abstract TrackManager GetTrackManager();
        protected abstract TimelineType TimelineType { get; }

        protected virtual void Start()
        {
            manager = GetManager();
            trackManager = GetTrackManager();
            timeline = NRDependencyInjector.Get<GridTimeline>();
            
            cam = CameraProvider.menu;
            timelineCam = CameraProvider.timeline;
            
            if(onHover != null)
                onHover.onHover.AddListener(OnSidebarHover);
            
            selectionBox = timeline.selectionBox;
            selectionBoxRenderer = selectionBox.GetComponent<Renderer>();
            GridTimeline.onTimelineOpened += OnTimelineOpened;
            enabled = false;
        }

        private void OnTimelineOpened(TimelineType type, bool show)
        {
            if(type == TimelineType)
            {
                enabled = show;
            }
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

        internal void EnableScrubbing(bool enable, bool toggleLocked = false)
        {
            if (enable)
            {
                if (_isScrollLocked && !toggleLocked)
                    return;
                
                KeybindManager.EnableKeybind("Scrub");
                KeybindManager.EnableKeybind("ScrubByTick");
                _isScrollLocked = false;
            }
            else
            {
                if (_isScrollLocked)
                    return;
                
                KeybindManager.DisableKeybind("Scrub");
                KeybindManager.DisableKeybind("ScrubByTick");

                if (toggleLocked)
                    _isScrollLocked = true;
            }
        }
        
        protected void OnLeftClick()
        {
            if (_isHovering)
                return;
            
            var mousePos = GetMousePosition();
            mouseDown = true;
            dragStartPos = GetTimelineMousePosition();
            dragStartTime = new QNT_Timestamp(QNT_Duration.FromBeatTime(dragStartPos.x).tick);
            var trackContent = GetTrackContentUnderMouse(mousePos);
            dragStartTrackIndex = 0;
            currentTrackIndex = 0;

            if (trackContent == null)
            {
                dragStartTrackIndex = mousePos.y < 0 ? trackManager.TrackCount - 1 : 0;
                allowDrag = true;
                return;
            }
            
            
            dragStartTrackIndex = trackContent.tracks[GridTimeline.Type].Order;
            var timeFromPosition = GetTimeFromPosition(mousePos);
            bool isCtrlDown = KeybindManager.Global.Modifier.IsCtrlDown();
            if(TryGetContentUnderMouse(timeFromPosition, trackContent.tracks[GridTimeline.Type], out var content))
            {
                bool hasSelected = manager.SelectContent(content, isCtrlDown);
                
                var timeframe = manager.CurrentContent.timeframe;
                var endDiff = timeframe.End - timeFromPosition.tick;
                var startDiff = timeFromPosition.tick - timeframe.Start;
                manager.StartMove(mousePos);
                if (endDiff < startDiff || timeframe.End == timeframe.Start)
                {
                    StartCoroutine(DragEnd(true, content, content.timeframe, !hasSelected));
                }
                else
                {
                    StartCoroutine(DragStart(content, content.timeframe, !hasSelected));
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
                if (ModifierUtility.SupportsEndTime((ModifierType)trackContent.tracks[GridTimeline.Type].Type, false, false))
                {
                    StartCoroutine(DragEnd(false, null, new(), false));
                }
            }
        }
        
        private IEnumerator DragStart(Content potentialReselectContent, Timeframe oldPotentialTimeframe, bool shouldReselect)
        {
           
            var lastTime = GetSnappedTimeFromPosition(GetMousePosition());
            bool hasTriedReselect = false;
            Vector2 startMousePosition = GetMousePosition();
            while (mouseDown)
            {
                var mousePosition = GetMousePosition();
                
                if (!hasTriedReselect && shouldReselect && potentialReselectContent != null && Vector2.Distance(mousePosition, startMousePosition) >= .01f)
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

        private IEnumerator DragEnd(bool allowMove, Content potentialReselectContent, Timeframe oldPotentialTimeframe, bool shouldReselect)
        {
            var lastTime = GetSnappedTimeFromPosition(GetMousePosition());
            bool hasTriedReselect = false;
            Vector2 startMousePosition = GetMousePosition();
            while (mouseDown)
            {
                var mousePosition = GetMousePosition();
                if (!hasTriedReselect && shouldReselect && potentialReselectContent != null && Vector2.Distance(mousePosition, startMousePosition) >= .01f)
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
                    if (currentTime != lastTime && currentTime <= EditorAudio.SongEndTime)
                    {
                        bool increase = currentTime > lastTime;
                        lastTime = currentTime;
                        manager.SetEndTime(increase, allowMove && !KeybindManager.Global.Modifier.IsShiftDown());
                    }
                }
                yield return null;
            }
        }
        
        protected void EndDrag()
        {
            mouseDown = false;
            isDragging = false;
            allowDrag = false;
            timeline.selectionBox.SetActive(false);
            manager.EndMove();
            OnSidebarHover(_isHovering);
        }

        protected void OnRightClick()
        {
            if (_isHovering)
                return;
            
            var mousePos = GetMousePosition();
            var trackContent = GetTrackContentUnderMouse(mousePos);
            if (trackContent != null)
            {
                if (TryGetContentUnderMouse(GetTimeFromPosition(mousePos), trackContent.tracks[TimelineType], out var content))
                {
                    manager.TryRemoveContent(content);
                }
            }
        }

        protected bool TryGetContentUnderMouse(QNT_Timestamp time, Track track, out Content content)
        {
            if (track.Content.Count == 0)
            {
                content = null;
                return false;
            }
            
            foreach (var c in track.Content)
            {
                if (c.timeframe.Contains(time, true))
                {
                    content = c;
                    return true;
                }
            }
            
            content = null;
            return false;
        }
        
        private RaycastHit2D[] PerformRaycast() => Physics2D.RaycastAll(GetTimelineMousePosition(), Vector2.zero, 10f);
        protected abstract string RaycastContentTag { get; }
        
        protected void OnDeletePressed() => manager.RemoveSelectedContent();

        protected Vector2 GetMousePosition()
            =>  cam.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>());

        protected Vector2 GetTimelineMousePosition()
            => timelineCam.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>());

        protected QNT_Timestamp GetTimeFromPosition(Vector2 mousePosition)
        {
            mousePosition.x /= Timeline.scaleTransform;
            mousePosition.x -= Timeline.timelineNotesStatic.parent.position.x;
            return EditorTime.Time + Relative_QNT.FromBeatTime(mousePosition.x);
        }

        protected QNT_Timestamp GetSnappedTimeFromPosition(Vector2 mousePosition)
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
        
        protected TrackContent GetTrackContentUnderMouse(Vector2 point)
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

        public void ScrollUp() => trackManager.ScrollUp();
        public void ScrollDown() => trackManager.ScrollDown();
        
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
            var mousePos = GetTimelineMousePosition();
            var size = dragStartPos - mousePos;
            if (size.magnitude >= .2f)
            {
                isDragging = true;
                if(!timeline.selectionBox.activeInHierarchy) timeline.selectionBox.SetActive(true);
                Vector2 newPos = dragStartPos;
                timeline.selectionBox.transform.localScale = size;
                newPos += -(size * .5f);
                timeline.selectionBox.transform.position = new Vector3(newPos.x, newPos.y, 1);
                var trackContent = GetTrackContentUnderMouse(GetMousePosition());
                TrackManager.TrackID? trackID = null;
                if (trackContent != null)
                {
                    currentTrackIndex = trackContent.tracks[GridTimeline.Type].Order;
                    trackID = trackContent.tracks[GridTimeline.Type].ID;
                }
                
                if(Input.GetMouseButtonDown(2))
                    Debug.Log("");

                var currentDragTime = new QNT_Timestamp(QNT_Duration.FromBeatTime(GetTimelineMousePosition().x).tick);
                QNT_Timestamp startTime;
                QNT_Timestamp endTime;

                if (dragStartTime < currentDragTime)
                {
                    startTime = dragStartTime;
                    endTime = currentDragTime;
                }
                else
                {
                    startTime = currentDragTime;
                    endTime = dragStartTime;   
                }
                
                var timeframe = new Timeframe(startTime, endTime);

                var trackStart = 0;
                var trackEnd = 0;
                if (dragStartTrackIndex < currentTrackIndex)
                {
                    trackStart = dragStartTrackIndex;
                    trackEnd = currentTrackIndex;
                }
                else
                {
                    trackStart = currentTrackIndex;
                    trackEnd = dragStartTrackIndex;
                }

                List<Content> contents = new();
                if (trackID.HasValue || trackStart != trackEnd)
                {
                    if (trackStart == trackEnd)
                    {
                        contents.AddRange(trackManager.Tracks[trackID.Value].Content);
                    }
                    else
                    {
                        foreach (var kvp in trackManager.Tracks)
                            if(kvp.Value.Order >= trackStart && kvp.Value.Order <= trackEnd)
                                contents.AddRange(kvp.Value.Content);
                    }
                }
                manager.DeselectAll();
                
                List<Content> newlySelected = new();
                foreach (var content in contents)
                {
                    if (content.timeframe.Contains(timeframe))
                    {
                        manager.MultiSelectContent(content);
                        newlySelected.Add(content);
                    }
                    else
                    {
                        manager.DeselectMultiselectContent(content);
                    }
                }
                
                for (int i = manager.SelectedContent.Count - 1; i >= 0; i--)
                {
                    var c = manager.SelectedContent[i];
                    if(!newlySelected.Contains(c))
                        c.SetSelected(false);
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