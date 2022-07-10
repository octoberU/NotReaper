using System.Collections;
using System.Collections.Generic;
using NotReaper;
using NotReaper.HitsoundTimeline;
using NotReaper.Tools;
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

        protected override void SetRebindConfiguration(ref RebindConfiguration options, HitsoundKeybinds myKeybinds)
        {
            
        }

        protected override bool AllowTrackSwitching => true;
        protected override bool AllowContentMoving => false;

        protected override TimelineManager<HitsoundData> GetManager()
            => NRDependencyInjector.Get<HitsoundManager>();

        protected override TrackManager GetTrackManager()
            => NRDependencyInjector.Get<HitsoundTrackManager>();

        protected override string RaycastContentTag => "HitsoundMarker";
    }
}
