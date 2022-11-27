using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper;
using NotReaper.HitsoundTimeline;
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
        }

        public bool TryGetTargetUnderMouse(out List<Target> targets)
        {
            var mousePos = GetMousePosition();
            var timeFromPosition = GetTimeFromPosition(mousePos);
            var trackContent = GetTrackContentUnderMouse(mousePos);
            if (trackContent != null)
            {
                bool isMelee = ((TimelineHitsound)trackContent.tracks[TimelineType].Type).IsMelee();
                if (TryGetClosestContentUnderMouse(timeFromPosition, trackContent, isMelee, out var contents))
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

        private bool TryGetClosestContentUnderMouse(QNT_Timestamp time, TrackContent track, bool isMelee, out List<Content> foundContent)
        {
            List<Content> candidates = new();
            bool hasFoundSomething = false;

            foreach (var c in track.tracks[TimelineType].Content)
            {
                if (c.IsNearTime(time))
                {
                    candidates.Add(c);
                    hasFoundSomething = true;
                }
                else if(hasFoundSomething)
                {
                    break;
                }
            }
            
            /*foreach (var track in trackManager.Tracks)
            {
                var isMeleeTrack = ((TimelineHitsound)track.Value.Type).IsMelee();
                if ((isMelee && !isMeleeTrack) || (!isMelee && isMeleeTrack)) continue;

                foreach (var c in track.Value.Content)
                {
                    if (c.IsNearTime(time))
                    {
                        candidates.Add(c);
                        hasFoundSomething = true;
                    }
                    else if (hasFoundSomething)
                    {
                        break;
                    }
                }
            }*/

            if (!hasFoundSomething)
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
           /* foreach (var track in trackManager.Tracks)
            {
                var isMeleeTrack = ((TimelineHitsound)track.Value.Type).IsMelee();
                if ((isMelee && !isMeleeTrack) || (!isMelee && isMeleeTrack)) continue;

                foreach (var c in track.Value.Content)
                {
                    if(c.startTime == closestTime)
                        foundContent.Add(c);
                }
            }*/
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
