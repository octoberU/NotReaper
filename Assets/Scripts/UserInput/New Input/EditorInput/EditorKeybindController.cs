using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NotReaper.UserInput;
using NotReaper.Grid;
using NotReaper.UI;
using NotReaper.Tools;
using NotReaper;
using NotReaper.Managers;
using NotReaper.ReviewSystem;
using System;
using UnityEditor;

namespace NotReaper.UserInput
{
    public partial class EditorKeybindController : NRInput<EditorKeybinds>
    {

        protected override void RegisterCallbacks()
        {
            #region Mapping
            actions.Mapping.PlaceNote.performed += PlaceNote;
            actions.Mapping.RemoveNote.performed += RemoveNote;
            actions.Mapping.MultiRemoveNote.performed += DeleteSelectedTargets;
            actions.Mapping.SwitchHand.performed += ToggleHandColor;
            actions.Mapping.FlipTargetColors.performed += FlipTargetColors;
            actions.Mapping.ImmediateTargetColorSwap.performed += ImmediateFlipTargetColors;
            actions.Mapping.DeleteSelectedTargets.performed += DeleteSelectedTargets;
            actions.Mapping.CycleNext.performed += CycleNext;
            actions.Mapping.CyclePrevious.performed += CyclePrevious;
            #endregion

            #region Drag Select
            actions.DragSelect.SelectDragTool.started += SelectDrag;
            actions.DragSelect.SelectDragTool.canceled += SelectDrag;

            actions.DragSelect.FlipTargetsHorizontally.performed += FlipTargetsHorizontal;
            actions.DragSelect.FlipTargetsVertically.performed += FlipTargetsVertical;

            actions.DragSelect.IncreaseScaleHorizontally.performed += IncreaseScaleHorizontal;
            actions.DragSelect.IncreaseScaleVertically.performed += IncreaseScaleVertical;

            actions.DragSelect.DecreaseScaleHorizontally.performed += DecreaseScaleHorizontal;
            actions.DragSelect.DecreaseScaleVertically.performed += DecreaseScaleVertical;

            actions.DragSelect.RotateSelectedTargetsLeft.performed += RotateSelectedTargetsLeft;
            actions.DragSelect.RotateSelectedTargetsRight.performed += RotateSelectedTargetsRight;

            actions.DragSelect.ReverseSelectedTargets.performed += ReverseSelectedTargets;

            actions.DragSelect.MoveTargetsUp.performed += MoveTargetsUp;
            actions.DragSelect.MoveTargetsDown.performed += MoveTargetsDown;
            actions.DragSelect.MoveTargetsLeft.performed += MoveTargetsLeft;
            actions.DragSelect.MoveTargetsRight.performed += MoveTargetsRight;

            actions.DragSelect.BakePath.performed += BakePath;
            #endregion

            #region Spacing Snap
            actions.SpacingSnap.EnableSpacingSnap.performed += EnableSpacingSnap;
            actions.SpacingSnap.EnableSpacingSnap.canceled += EnableSpacingSnap;
            #endregion

            #region Modifiers
            actions.Modifiers.OpenModifiers.performed += SelectModifiers;
            actions.Modifiers.ModifierPreview.performed += ActivateModifierPreview;
            #endregion

            #region Pathbuilder
            actions.Pathbuilder.EnablePathbuilder.performed += SelectPathbuilder;
            #endregion

            #region Behaviors
            actions.BehaviorSelect.SelectBehaviorChain.performed += SelectChain;
            actions.BehaviorSelect.SelectBehaviorChainstart.performed += SelectChainStart;
            actions.BehaviorSelect.SelectBehaviorHorizontal.performed += SelectHorizontal;
            actions.BehaviorSelect.SelectBehaviorMelee.performed += SelectMelee;
            actions.BehaviorSelect.SelectBehaviorMine.performed += SelectMine;
            actions.BehaviorSelect.SelectBehaviorStandard.performed += SelectStandard;
            actions.BehaviorSelect.SelectBehaviorSustain.performed += SelectSustain;
            actions.BehaviorSelect.SelectBehaviorVertical.performed += SelectVertical;

            actions.BehaviorConvert.ConvertBehaviorChain.performed += ConvertToChain;
            actions.BehaviorConvert.ConvertBehaviorChainstart.performed += ConvertToChainstart;
            actions.BehaviorConvert.ConvertBehaviorHorizontal.performed += ConvertToHorizontal;
            actions.BehaviorConvert.ConvertBehaviorMelee.performed += ConvertToMelee;
            actions.BehaviorConvert.ConvertBehaviorStandard.performed += ConvertToStandard;
            actions.BehaviorConvert.ConvertBehaviorSustain.performed += ConvertToSustain;
            actions.BehaviorConvert.ConvertBehaviorVertical.performed += ConvertToVertical;
            #endregion

            #region Hitsounds
            actions.HitsoundSelect.SelectHitsoundChain.performed += SelectHitsoundChain;
            actions.HitsoundSelect.SelectHitsoundChainstart.performed += SelectHitsoundChainStart;
            actions.HitsoundSelect.SelectHitsoundKick.performed += SelectHitsoundKick;
            actions.HitsoundSelect.SelectHitsoundMelee.performed += SelectHitsoundMelee;
            actions.HitsoundSelect.SelectHitsoundPercussion.performed += SelectHitsoundPercussion;
            actions.HitsoundSelect.SelectHitsoundSilent.performed += SelectHitsoundSilent;
            actions.HitsoundSelect.SelectHitsoundSnare.performed += SelectHitsoundSnare;

            actions.HitsoundConvert.ConvertHitsoundChain.performed += ConvertHitsoundChain;
            actions.HitsoundConvert.ConvertHitsoundChainstart.performed += ConvertHitsoundChainStart;
            actions.HitsoundConvert.ConvertHitsoundKick.performed += ConvertHitsoundKick;
            actions.HitsoundConvert.ConvertHitsoundMelee.performed += ConvertHitsoundMelee;
            actions.HitsoundConvert.ConvertHitsoundPercussion.performed += ConvertHitsoundPercussion;
            actions.HitsoundConvert.ConvertHitsoundSilent.performed += ConvertHitsoundSilent;
            actions.HitsoundConvert.ConvertHitsoundSnare.performed += ConvertHitsoundSnare;
            #endregion

            #region Grid
            actions.Grid.GridView.performed += SelectSnapGrid;
            actions.Grid.MeleeView.performed += SelectSnapMelee;
            actions.Grid.NoGridView.performed += SelectSnapNone;
            actions.Grid.QuickSwitchGrid.started += QuickSwitchNoGrid;
            actions.Grid.QuickSwitchGrid.canceled += QuickSwitchNoGrid;
            actions.Grid.MoveGridUp.performed += MoveGridUp;
            actions.Grid.MoveGridLeft.performed += MoveGridLeft;
            actions.Grid.MoveGridDown.performed += MoveGridDown;
            actions.Grid.MoveGridRight.performed += MoveGridRight;
            #endregion

            #region Timeline
            actions.Timeline.TogglePlay.performed += TogglePlay;
            actions.Timeline.StartMetronome.performed += StartMetronome;
            actions.Timeline.ToggleWaveform.performed += ToggleWaveform;
            actions.Timeline.Scrub.performed += ScrubTimeline;
            actions.Timeline.ScrubByTick.performed += ScrubByTick;
            actions.Timeline.ChangeBeatSnap.performed += ChangeBeatSnap;
            actions.Timeline.ZoomTimeline.performed += ZoomTimeline;
            actions.Timeline.GoToNextBookmark.performed += NextBookmark;
            actions.Timeline.GoToPreviousBookmark.performed += PreviousBookmark;
            actions.Timeline.GoToStartOfSong.performed += GoToStartOfSong;
            actions.Timeline.GoToEndOfSong.performed += GoToEndOfSong;
            #endregion

            #region BPM
            actions.BPM.BPMMarker.performed += PlaceBpmMarker;
            actions.BPM.DetectBPM.performed += DetectBpm;
            actions.BPM.ShiftBPMMarker.performed += ShiftBpmMarker;
            actions.BPM.PreviewPoint.performed += SetPreviewPoint;
            #endregion

            #region Menus
            actions.Menus.Pause.performed += ShowPause;
            actions.Menus.Help.performed += ShowHelp;
            actions.Menus.Countin.performed += ShowCountin;
            actions.Menus.Bookmark.performed += OpenBookmarks;
            actions.Menus.ModifyAudio.performed += ShowModifyAudio;
            actions.Menus.TimingPoints.performed += ShowTimingPoints;
            actions.Menus.ModifierHelp.performed += ShowModifierHelp;
            actions.Menus.ReviewMenu.performed += ShowReviewMenu;
            actions.Menus.Repeater.performed += ShowRepeaterWindow;
            actions.Menus.GridSize.performed += ShowGridSizeMenu;
            #endregion

            #region Utility
            actions.Utility.SelectAll.performed += SelectAll;
            actions.Utility.DeselectAll.performed += DeselectAllTargets;
            actions.Utility.Copy.performed += Copy;
            actions.Utility.Paste.performed += Paste;
            actions.Utility.Cut.performed += Cut;
            actions.Utility.Save.performed += Save;
            actions.Utility.Undo.performed += DoUndo;
            actions.Utility.Redo.performed += DoRedo;
            actions.Utility.SelectFromCurrentToNextBookmark.performed += SelectUntilNextBookmark;
            actions.Utility.DuplicateAndSwap.performed += DuplicateAndSwap;
            actions.Utility.TogglePreset.performed += TogglePreset;
            #endregion
            
            #region Hitsound Timeline

            actions.HitsoundTimeline.OpenHitsoundTimeline.started += ToggleHitsoundTimeline;
            #endregion
           

            actions.Disable();
            KeybindManager.SetStandardKeybinds(actions.asset);

        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {

        }

        protected override void SetRebindConfiguration(ref RebindConfiguration options, EditorKeybinds myKeybinds)
        {
            //Asset and maps
            options.SetAssetTitle("Editor").SetPriority(99999).SetRebindable(true);
            options.AddCustomMapTitle(myKeybinds.HitsoundSelect, "Hitsound Selection").AddCustomMapTitle(myKeybinds.HitsoundConvert, "Hitsound Conversion");
            options.AddCustomMapTitle(myKeybinds.BehaviorSelect, "Behavior Selection").AddCustomMapTitle(myKeybinds.BehaviorConvert, "Behavior Conversion");
            options.AddCustomMapTitle(myKeybinds.SpacingSnap, "Spacing Snapper");

            //Hitsound select
            options.AddCustomKeybindName(myKeybinds.HitsoundSelect.SelectHitsoundChain, "Chain").AddCustomKeybindName(myKeybinds.HitsoundSelect.SelectHitsoundChainstart, "Chainstart");
            options.AddCustomKeybindName(myKeybinds.HitsoundSelect.SelectHitsoundKick, "Kick").AddCustomKeybindName(myKeybinds.HitsoundSelect.SelectHitsoundMelee, "Melee");
            options.AddCustomKeybindName(myKeybinds.HitsoundSelect.SelectHitsoundPercussion, "Percussion").AddCustomKeybindName(myKeybinds.HitsoundSelect.SelectHitsoundSilent, "Silent");
            options.AddCustomKeybindName(myKeybinds.HitsoundSelect.SelectHitsoundSnare, "Snare");

            //Hitsound convert
            options.AddCustomKeybindName(myKeybinds.HitsoundConvert.ConvertHitsoundChain, "To chain").AddCustomKeybindName(myKeybinds.HitsoundConvert.ConvertHitsoundChainstart, "To chainstart");
            options.AddCustomKeybindName(myKeybinds.HitsoundConvert.ConvertHitsoundKick, "To kick").AddCustomKeybindName(myKeybinds.HitsoundConvert.ConvertHitsoundMelee, "To melee");
            options.AddCustomKeybindName(myKeybinds.HitsoundConvert.ConvertHitsoundPercussion, "To percussion").AddCustomKeybindName(myKeybinds.HitsoundConvert.ConvertHitsoundSilent, "To silent");
            options.AddCustomKeybindName(myKeybinds.HitsoundConvert.ConvertHitsoundSnare, "To snare");

            //Behavior select
            options.AddCustomKeybindName(myKeybinds.BehaviorSelect.SelectBehaviorChain, "Chain").AddCustomKeybindName(myKeybinds.BehaviorSelect.SelectBehaviorChainstart, "Chainstart");
            options.AddCustomKeybindName(myKeybinds.BehaviorSelect.SelectBehaviorHorizontal, "Horizontal").AddCustomKeybindName(myKeybinds.BehaviorSelect.SelectBehaviorMelee, "Melee");
            options.AddCustomKeybindName(myKeybinds.BehaviorSelect.SelectBehaviorMine, "Mine").AddCustomKeybindName(myKeybinds.BehaviorSelect.SelectBehaviorStandard, "Standard");
            options.AddCustomKeybindName(myKeybinds.BehaviorSelect.SelectBehaviorSustain, "Sustain").AddCustomKeybindName(myKeybinds.BehaviorSelect.SelectBehaviorVertical, "Vertical");

            //Behavior convert
            options.AddCustomKeybindName(myKeybinds.BehaviorConvert.ConvertBehaviorChain, "To chain").AddCustomKeybindName(myKeybinds.BehaviorConvert.ConvertBehaviorChainstart, "To chainstart");
            options.AddCustomKeybindName(myKeybinds.BehaviorConvert.ConvertBehaviorHorizontal, "To horizontal").AddCustomKeybindName(myKeybinds.BehaviorConvert.ConvertBehaviorMelee, "To melee");
            options.AddCustomKeybindName(myKeybinds.BehaviorConvert.ConvertBehaviorSustain, "To sustain").AddCustomKeybindName(myKeybinds.BehaviorConvert.ConvertBehaviorVertical, "To vertical");
            options.AddCustomKeybindName(myKeybinds.BehaviorConvert.ConvertBehaviorStandard, "To standard");

            //Grid
            options.AddCustomKeybindName(myKeybinds.DragSelect.MoveTargetsHalfModifier, "Move by half").AddCustomKeybindName(myKeybinds.DragSelect.MoveTargetsQuarterModifier, "Move by quarter");

            //Mixer
            
            
            //Non-rebindables
            //options.AddNonRebindableKeybinds(myKeybinds.DragSelect.SelectDragTool).AddNonRebindableKeybinds(myKeybinds.SpacingSnap.EnableSpacingSnap);
            options.AddNonRebindableKeybinds(myKeybinds.DragSelect.MoveTargetsHalfModifier, myKeybinds.DragSelect.MoveTargetsQuarterModifier);
            options.AddNonRebindableKeybinds(myKeybinds.Mapping.PlaceNote, myKeybinds.Mapping.RemoveNote);
            options.AddNonRebindableKeybinds(myKeybinds.Menus.Pause);
            options.AddNonRebindableKeybinds(myKeybinds.Timeline.Scrub, myKeybinds.Timeline.ScrubByTick);
            options.AddNonRebindableKeybinds(myKeybinds.Timeline.ScrubByTick, myKeybinds.Timeline.ChangeBeatSnap);
            options.AddNonRebindableKeybinds(myKeybinds.Timeline.ZoomTimeline);
            options.AddNonRebindableKeybinds(myKeybinds.Grid.QuickSwitchGrid);
            options.AddNonRebindableKeybinds(myKeybinds.Utility.TogglePreset);

            //Hidden keybinds and maps
            options.AddHiddenMaps(myKeybinds.HitsoundConvert);

        }
    }

}
