using System.Collections;
using System.Collections.Generic;
using NotReaper;
using NotReaper.HitsoundTimeline;
using NotReaper.Targets;
using NotReaper.Tools;
using NotReaper.UI;
using NotReaper.UserInput;
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

        public bool TryGetTargetUnderMouse(out Target target)
        {
            var mousePos = GetMousePosition();
            var timeFromPosition = GetTimeFromPosition(mousePos);
            var trackContent = GetTrackContentUnderMouse(mousePos);
            if (trackContent != null)
            {
                if (TryGetContentUnderMouse(timeFromPosition, trackContent.tracks[TimelineType], out var content))
                {
                    var marker = content as HitsoundMarker;
                    target = marker.Data.target;
                    return true;
                }
            }

            target = null;
            return false;
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
