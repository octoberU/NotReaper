using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper;
using NotReaper.UserInput;
using UnityEngine;

namespace NotReaper.SustainTimeline
{
    public class SustainInputManager : TimelineInput<SustainKeybinds, SustainData>
    {

        protected override void RegisterCallbacks()
        {
            actions.Sustains.RemoveSustain.started += _ => OnRightClick();
            actions.Sustains.Copy.started += _ => Copy();
            actions.Sustains.Cut.started += _ => Cut();
            actions.Sustains.Paste.started += _ => Paste();
            actions.Sustains.Delete.started += _ => OnDeletePressed();
            actions.Sustains.Undo.started += _ => SustainUndoRedo.Undo();
            actions.Sustains.Redo.started += _ => SustainUndoRedo.Redo();
            actions.Sustains.Scrub.started += ctx => OnScrub(ctx.ReadValue<float>() > 0f);
            actions.Sustains.DeselectAll.started += _ => DeselectAll();
            actions.Sustains.SelectAll.started += _ => SelectAll();
            actions.Sustains.LeftMouseClick.started += _ => OnLeftClick();
            actions.Sustains.LeftMouseClick.canceled += _ => EndDrag();
            actions.Sustains.MoveTracksDown.started += _ => ScrollDown();
            actions.Sustains.MoveTracksUp.started += _ => ScrollUp();
        }

        protected override void SetRebindConfiguration(ref RebindConfiguration options, SustainKeybinds myKeybinds)
        {
            
        }

        protected override bool AllowTrackSwitching => true;
        protected override bool AllowContentMoving => true;

        protected override TimelineManager<SustainData> GetManager()
            => NRDependencyInjector.Get<SustainTimelineManager>();

        protected override TrackManager GetTrackManager()
            => NRDependencyInjector.Get<SustainTrackManager>();

        protected override TimelineType TimelineType => TimelineType.Sustain;
        protected override string RaycastContentTag => "";
    }
}
