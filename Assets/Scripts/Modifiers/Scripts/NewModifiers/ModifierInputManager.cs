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

        public void ToggleZOffsetWindow()
        {
            bool enabled = !zOffsetBakingWindow.activeInHierarchy;
            zOffsetBakingWindow.SetActive(enabled);

            TryEnableKeybind(actions.Modifiers.LeftMouseClick, !enabled);
            TryEnableKeybind(actions.Modifiers.RemoveModifier, !enabled);
            if (enabled)
            {
                KeybindManager.DisableKeybind("OpenModifiers");
            }
            else
            {
                KeybindManager.EnableKeybind("OpenModifiers");
            }
        }

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

        public void EnableKeybinds(bool enable)
        {
            TryEnableKeybind(actions.Modifiers.BakeZOffset, enable);
            if (enable)
            {
                KeybindManager.EnableKeybind("OpenModifiers");
                KeybindManager.EnableKeybind("TogglePlay");
            }
            else
            {
                KeybindManager.DisableKeybind("OpenModifiers");
                KeybindManager.DisableKeybind("TogglePlay");
            }
        }

        private void TryEnableKeybind(InputAction action, bool enable)
        {
            if(!action.enabled && enable)
                action.Enable();
            else if(action.enabled && !enable)
                action.Disable();
        }

        protected override bool AllowTrackSwitching => false;
        protected override bool AllowContentMoving => true;

        protected override TimelineManager<Data> GetManager()
            => NRDependencyInjector.Get<ModifierManager>();

        protected override TrackManager GetTrackManager()
            => NRDependencyInjector.Get<ModifierTrackManager>();

        protected override TimelineType TimelineType => TimelineType.Modifier;

        protected override string RaycastContentTag => "Modifier";

        protected override void SetRebindConfiguration(ref RebindConfiguration options, ModifierKeybinds myKeybinds)
        {
            options.AddCustomKeybindName(actions.Modifiers.LeftMouseClick, "Place Modifier");
        }
    }
}
