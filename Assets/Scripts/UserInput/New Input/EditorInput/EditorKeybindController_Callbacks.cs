using NotReaper;
using NotReaper.Grid;
using NotReaper.Managers;
using NotReaper.Models;
using NotReaper.Tools;
using NotReaper.UI;
using NotReaper.UserInput;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NotReaper.UserInput
{
    public partial class EditorKeybindController
    {
        [Header("References")]
        [SerializeField] private MappingInput mapping;
        [SerializeField] private UIInput ui;

        public void DoRedo(InputAction.CallbackContext obj)
        {
            mapping.Redo();
        }

        public void DoUndo(InputAction.CallbackContext obj)
        {
            mapping.Undo();
        }

        public void Save(InputAction.CallbackContext obj)
        {
            mapping.Save();
        }

        public void DeleteSelectedTargets(InputAction.CallbackContext obj)
        {
            mapping.DeleteSelectedTargets();
        }

        public void Cut(InputAction.CallbackContext obj)
        {
            mapping.CutSelectedTargets();
        }

        public void Paste(InputAction.CallbackContext obj)
        {
            mapping.PasteSelectedTargets();
        }

        public void Copy(InputAction.CallbackContext obj)
        {
            mapping.CopySelectedTargets();
        }

        public void DeselectAllTargets(InputAction.CallbackContext obj)
        {
            mapping.DeselectAllTargets();
        }

        public void SelectAll(InputAction.CallbackContext obj)
        {
            mapping.SelectAllTargets();
        }

        public void ShowReviewMenu(InputAction.CallbackContext obj)
        {
            ui.ShowReviewWindow();
        }

        public void ShowModifierHelp(InputAction.CallbackContext obj)
        {
            ui.ShowModifierHelpWindow();
        }

        public void ShowTimingPoints(InputAction.CallbackContext obj)
        {
            ui.ShowTimingPointsWindow();
        }

        public void ShowModifyAudio(InputAction.CallbackContext obj)
        {
            ui.ShowModifyAudioWindow();
        }

        public void ShowCountin(InputAction.CallbackContext obj)
        {
            ui.ShowCountinWindow();
        }

        public void ToggleWaveform(InputAction.CallbackContext obj)
        {
            ui.ToggleWaveform();
        }

        public void ShowHelp(InputAction.CallbackContext obj)
        {
            ui.ShowHelpWindow();
        }

        public void ShowPause(InputAction.CallbackContext obj)
        {
            ui.ShowPauseWindow();
        }

        public void SetPreviewPoint(InputAction.CallbackContext obj)
        {
            ui.SetPreviewPoint();
        }

        public void MoveTargetsUp(InputAction.CallbackContext obj)
        {
            mapping.MoveTargetsAction(new Vector2(0, 1));
        }
        public void MoveTargetsDown(InputAction.CallbackContext obj)
        {
            mapping.MoveTargetsAction(new Vector2(0, -1));
        }
        public void MoveTargetsLeft(InputAction.CallbackContext obj)
        {
            mapping.MoveTargetsAction(new Vector2(-1, 0));
        }
        public void MoveTargetsRight(InputAction.CallbackContext obj)
        {
            mapping.MoveTargetsAction(new Vector2(1, 0));
        }
        public void MoveGridUp(InputAction.CallbackContext obj)
        {
            ui.MoveGrid(new Vector2(0, 1));
        }
        public void MoveGridDown(InputAction.CallbackContext obj)
        {
            ui.MoveGrid(new Vector2(0, -1));
        }
        public void MoveGridLeft(InputAction.CallbackContext obj)
        {
            ui.MoveGrid(new Vector2(-1, 0));
        }
        public void MoveGridRight(InputAction.CallbackContext obj)
        {
            ui.MoveGrid(new Vector2(1, 0));
        }

        public void OpenBookmarks(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
            {
                ui.SetBookmark();
            }
        }

        public void ShiftBpmMarker(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Shift)
            {
                ui.ShiftBpmMarker();
            }
        }

        public void DetectBpm(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Shift)
            {
                ui.DetectBpm();
            }
        }

        public void PlaceBpmMarker(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
            {
                ui.ShowBpmWindow();
            }
        }

        public void ConvertHitsoundSnare(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetHitsoundAction(InternalTargetVelocity.Snare);
        }

        public void ConvertHitsoundSilent(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetHitsoundAction(InternalTargetVelocity.Silent);
        }

        public void ConvertHitsoundPercussion(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetHitsoundAction(InternalTargetVelocity.Percussion);
        }

        public void ConvertHitsoundMelee(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetHitsoundAction(InternalTargetVelocity.Melee);
        }

        public void ConvertHitsoundKick(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetHitsoundAction(InternalTargetVelocity.Kick);
        }

        public void ConvertHitsoundChainStart(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetHitsoundAction(InternalTargetVelocity.ChainStart);
        }

        public void ConvertHitsoundChain(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetHitsoundAction(InternalTargetVelocity.Chain);
        }

        public void SelectHitsoundSnare(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectHitsound(TargetHitsound.Snare);
        }

        public void SelectHitsoundSilent(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectHitsound(TargetHitsound.Silent);
        }

        public void SelectHitsoundPercussion(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectHitsound(TargetHitsound.Percussion);
        }

        public void SelectHitsoundMelee(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectHitsound(TargetHitsound.Melee);
        }

        public void SelectHitsoundKick(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectHitsound(TargetHitsound.Standard);
        }

        public void SelectHitsoundChainStart(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectHitsound(TargetHitsound.ChainStart);
        }

        public void SelectHitsoundChain(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectHitsound(TargetHitsound.ChainNode);
        }
        public void QuickSwitchNoGrid(InputAction.CallbackContext obj)
        {
            if (obj.performed) EditorState.SelectSnappingMode(EditorState.Snapping.Current == SnappingMode.None ? EditorState.Behavior.Current == TargetBehavior.Melee ? SnappingMode.Melee : SnappingMode.Grid : SnappingMode.None);
            else if (obj.canceled) EditorState.SelectSnappingMode(EditorState.Snapping.Previous);
        }

        public void SelectSnapNone(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectSnappingMode(SnappingMode.None);
        }

        public void SelectSnapMelee(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectSnappingMode(SnappingMode.Melee);
        }

        public void SelectSnapGrid(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectSnappingMode(SnappingMode.Grid);
        }

        public void ConvertToVertical(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetBehaviorAction(TargetBehavior.Vertical);
        }

        public void ConvertToSustain(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetBehaviorAction(TargetBehavior.Sustain);
        }

        public void ConvertToStandard(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetBehaviorAction(TargetBehavior.Standard);
        }

        public void ConvertToMine(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetBehaviorAction(TargetBehavior.Mine);
        }

        public void ConvertToMelee(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetBehaviorAction(TargetBehavior.Melee);
        }

        public void ConvertToHorizontal(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetBehaviorAction(TargetBehavior.Horizontal);
        }

        public void ConvertToChainstart(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetBehaviorAction(TargetBehavior.ChainStart);
        }

        public void ConvertToChain(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.SetTargetBehaviorAction(TargetBehavior.ChainNode);
        }

        public void SelectDrag(InputAction.CallbackContext obj)
        {
            EditorState.SelectTool(EditorTool.DragSelect);
        }

        public void SelectModifiers(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                mapping.ToggleModifiers();
        }

        public void SelectPathbuilder(InputAction.CallbackContext obj)
        {
            mapping.TogglePathbuilder();
        }

        public void ActivateModifierPreview(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Alt)
            {
                mapping.ToggleModifierPreview();
                mapping.TogglePlayPause(false);
            }
        }

        public void SelectVertical(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectBehavior(TargetBehavior.Vertical);
        }

        public void SelectSustain(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectBehavior(TargetBehavior.Sustain);
        }

        public void SelectStandard(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectBehavior(TargetBehavior.Standard);
        }

        public void SelectMine(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectBehavior(TargetBehavior.Mine);
        }

        public void SelectMelee(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectBehavior(TargetBehavior.Melee);
        }

        public void SelectHorizontal(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectBehavior(TargetBehavior.Horizontal);
        }

        public void SelectChainStart(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectBehavior(TargetBehavior.ChainStart);
        }

        public void SelectChain(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectBehavior(TargetBehavior.ChainNode);
        }
        public void RotateSelectedTargetsRight(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.CtrlShift)
                mapping.RotateSelectedTargetsRight();
        }

        public void RotateSelectedTargetsLeft(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.CtrlShift)
                mapping.RotateSelectedTargetsLeft();
        }

        public void ReverseSelectedTargets(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.ReverseSelectedTargets();
        }

        public void DecreaseScaleVertical(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Shift)
                mapping.ScaleSelectedTargets(new Vector2(0f, -.1f));
        }

        public void DecreaseScaleHorizontal(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.ScaleSelectedTargets(new Vector2(-.1f, 0f));
        }

        public void IncreaseScaleVertical(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Shift)
                mapping.ScaleSelectedTargets(new Vector2(0f, .1f));
        }

        public void IncreaseScaleHorizontal(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.ScaleSelectedTargets(new Vector2(.1f, 0f));
        }

        public void FlipTargetsVertical(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Shift)
                mapping.FlipTargetsVertical();
        }

        public void FlipTargetsHorizontal(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.FlipTargetsHorizontal();
        }

        public void FlipTargetColors(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                mapping.FlipTargetColors();
        }

        public void ToggleHandColor(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                EditorState.SelectHand(EditorState.Hand.Current == TargetHandType.Left ? TargetHandType.Right : TargetHandType.Left);
        }
        public void ScrubByTick(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.ScrubTimeline(obj.ReadValue<float>(), true);
        }

        public void ChangeBeatSnap(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.CtrlAlt)
                mapping.ChangeBeatSnap(obj.ReadValue<float>());
        }

        public void ScrubTimeline(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                mapping.ScrubTimeline(obj.ReadValue<float>(), false);
        }

        public void ZoomTimeline(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Shift)
                mapping.ZoomTimeline(obj.ReadValue<float>());
        }

        public void StartMetronome(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
                mapping.TogglePlayPause(true);
        }

        public void TogglePlay(InputAction.CallbackContext obj)
        {
            if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
                mapping.TogglePlayPause(false);
        }

        public void EnableSpacingSnap(InputAction.CallbackContext obj)
        {
            mapping.ActivateSnapper(obj.performed);
        }

        public void RemoveNote(InputAction.CallbackContext obj)
        {
            if (!KeybindManager.Global.Modifier.IsShiftDown())
            {
                mapping.RemoveNote();
            }
        }

        public void PlaceNote(InputAction.CallbackContext obj)
        {
            mapping.PlaceNote();
        }

        public void ShowRepeaterWindow(InputAction.CallbackContext obj)
        {
            ui.ShowRepeaterWindow();
        }
    }

}
