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
    public class ModifierInputManager : TimelineInput<ModifierKeybinds, Data>
    {
        [SerializeField] private GameObject zOffsetBakingWindow;

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
            actions.Modifiers.Copy.started += _ => Copy();
            actions.Modifiers.Cut.started += _ => Cut();
            actions.Modifiers.Paste.started += _ => Paste();
            actions.Modifiers.SelectAll.started += _ => SelectAll();
            actions.Modifiers.DeselectAll.started += _ => DeselectAll();
            actions.Modifiers.MoveTracksUp.started += _ => ScrollUp();
            actions.Modifiers.MoveTracksDown.started += _ => ScrollDown();
        }

        protected override bool AllowTrackSwitching => false;
        protected override bool AllowContentMoving => true;

        protected override TimelineManager<Data> GetManager()
            => NRDependencyInjector.Get<ModifierManager>();

        protected override TrackManager GetTrackManager()
            => NRDependencyInjector.Get<ModifierTrackManager>();

        protected override string RaycastContentTag => "Modifier";

        protected override void SetRebindConfiguration(ref RebindConfiguration options, ModifierKeybinds myKeybinds)
        {
            options.AddCustomKeybindName(actions.Modifiers.LeftMouseClick, "Place Modifier");
        }
    }
}
