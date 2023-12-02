using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper;
using NotReaper.HitsoundTimeline;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools;
using NotReaper.UI;
using NotReaper.UserInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.HitsoundTimeline
{
    public class HitsoundInputManager : TimelineInput<HitsoundKeybinds, HitsoundData>
    {

        protected override void RegisterCallbacks()
        {
            actions.Hitsounds.Copy.started += _ => Copy();
            actions.Hitsounds.Paste.started += _ => Paste();
            actions.Hitsounds.Undo.started += _ => UndoRedoManager.Undo();
            actions.Hitsounds.Redo.started += _ => UndoRedoManager.Redo();
            actions.Hitsounds.Scrub.started += (ctx) => OnScrub(ctx.ReadValue<float>() > 0);
            actions.Hitsounds.DeselectAll.started += _ => DeselectAll();
            actions.Hitsounds.SelectAll.started += _ => SelectAll();
            actions.Hitsounds.PlaceMarker.started += _ => OnLeftClick();
            actions.Hitsounds.PlaceMarker.canceled += _ => EndDrag();
            actions.Hitsounds.MoveSelectedHitsoundsDown.started += _ => MoveSelectedContentDown();
            actions.Hitsounds.MoveSelectedHitsoundsUp.started += _ => MoveSelectedContentUp();
            actions.Hitsounds.Bookmark.started += _ => MiniTimeline.Instance.SetBookmark();
        }

        public bool TryGetContentFromTarget(Target target, out Content foundContent)
        {
            var hitsound = (int)target.data.velocity.ToTimelineHitsound();
            
            foreach (var kvp in trackManager.Tracks)
            {
                if (kvp.Key.type != hitsound)
                    continue;

                var track = kvp.Value;
                
                foreach (var content in track.Content)
                {
                    if (content.GetData() is not HitsoundData data)
                        continue;

                    if (data.target == target)
                    {
                        foundContent = content;
                        return true;
                    }
                }
            }

            foundContent = null;
            return false;
        }

        public bool TryGetTargetUnderMouse(out List<Target> targets)
        {
            var mousePos = GetMousePosition();
            var timeFromPosition = GetTimeFromPosition(mousePos);
            var trackContent = GetTrackContentUnderMouse(mousePos);
            if (trackContent != null)
            {
                var position = CameraProvider.timeline.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>());
                bool isMelee = ((TimelineHitsound)trackContent.tracks[TimelineType].Type).IsMelee();
                if (TryGetClosestContentUnderMouse(timeFromPosition, trackContent, position, out var contents))
                {
                    targets = new();
                    foreach (var content in contents)
                    {
                        var marker = content as HitsoundMarker;
                        targets.Add(marker.Data.target);
                    }
                    return true;
                }
            }

            targets = null;
            return false;
        }

        private bool TryGetClosestContentUnderMouse(QNT_Timestamp time, TrackContent track, Vector3 mousePosition, out List<Content> foundContent)
        {
            List<Content> candidates = new();

            foreach (var c in track.tracks[TimelineType].Content)
                if (c.IsNearTime(time) && c.IsNearPoint(mousePosition))
                    candidates.Add(c);
            

            if (candidates.Count == 0)
            {
                foundContent = null;
                return false;
            }

            var closestTime = candidates.OrderBy(c => Mathf.Abs((time - c.startTime).tick)).First().startTime;
            foundContent = new();

            foreach (var c in candidates)
            {
                if (c.startTime == closestTime)
                    foundContent.Add(c);
            }

            return true;
        }

        protected override void SetRebindConfiguration(ref RebindConfiguration options, HitsoundKeybinds myKeybinds)
        {
            
        }

        protected override bool AllowTrackSwitching => true;
        protected override bool AllowContentMoving => false;

        public void ShowHelp() => NRHelp.Instance.ShowHitsoundTimeline();

        protected override TimelineManager<HitsoundData> GetManager()
            => NRDependencyInjector.Get<HitsoundManager>();

        protected override TrackManager GetTrackManager()
            => NRDependencyInjector.Get<HitsoundTrackManager>();

        protected override TimelineType TimelineType => TimelineType.Hitsound;

        protected override string RaycastContentTag => "HitsoundMarker";
    }
}
