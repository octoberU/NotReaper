using NotReaper;
using NotReaper.Grid;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Tools;
using NotReaper.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using NotReaper.Tools.SpacingSnap;
using NotReaper.Modifier;
using NotReaper.Tools.ChainBuilder;
using NotReaper.Tools.PathBuilder;
using NotReaper.Notifications;

namespace NotReaper.UserInput
{
	public class MappingInput : MonoBehaviour
	{

		[Header("Place Notes")]
		[SerializeField] private Transform ghost;
		[SerializeField] private ParallaxBG background;
		[Space, Header("Timeline")]
		[SerializeField] private Timeline timeline;
		[Space, Header("Tools")]
		[SerializeField] private UndoRedoManager undoRedo;
		[SerializeField] private SpacingSnapper snapper;
		[SerializeField] private DragSelect drag;
		[NRInject] private ModifierHandler modifiers;
		[NRInject] private Pathbuilder pathbuilder;
		[NRInject] private ChainBuilder chainbuilder;

		private List<TargetData> clipboard = new List<TargetData>();
        private CycleMode cycleMode = CycleMode.Beatsnap;

        private void Start()
        {
			NRSettings.OnLoad(() => cycleMode = (CycleMode)NRSettings.config.cycleMode);
        }

		public void PlaceNote()
		{
			if (!EditorState.IsOverGrid || EditorState.IsInUI || (EditorState.Tool.Current != EditorTool.None && EditorState.Tool.Current != EditorTool.SpacingSnapper)) return;
			timeline.AddTarget(ghost.position.x, ghost.position.y);
			background.OnPlaceNote();
		}

		public void Redo()
		{
			undoRedo.Redo();
		}

		public void RemoveNote()
		{
			var iconsUnderMouse = MouseUtil.IconsUnderMouse(timeline);
			TargetIcon targetIcon = iconsUnderMouse.Length > 0 ? iconsUnderMouse[0] : null;
			if (targetIcon)
			{
				timeline.DeleteTarget(targetIcon.target);
				timeline.UpdateLoadedNotes();
			}

		}

		public void Undo() => undoRedo.Undo();
		public void DeselectAllTargets() => EditorNotes.DeselectAllTargets();
		public void SelectAllTargets() => EditorNotes.SelectAllTargets();
		public void Save() => timeline.Export();

		[NRListener]
		private void OnHitsoundChanged(TargetHitsound hitsound)
        {
			SetTargetHitsoundAction(hitsound.ToInternalVelocty());
        }

		public void SetTargetHitsoundAction(InternalTargetVelocity velocity)
		{
			if (EditorNotes.SelectedNotes.Count == 0) return;

			var intents = new List<TargetSetHitsoundIntent>();
			foreach (var target in EditorNotes.SelectedNotes)
			{
				var intent = new TargetSetHitsoundIntent();

				intent.target = target.data;
				intent.startingVelocity = target.data.velocity;
				intent.newVelocity = velocity;

				intents.Add(intent);
			}
			timeline.SetTargetHitsounds(intents);
			/*if(EditorData.SelectedNotes.Count > 0)
            {
				NotificationCenter.SendNotification($"Converted hitsound{(EditorData.SelectedNotes.Count > 1 ? "s" : "")} to {velocity}.", NotificationType.Success, false);
            }*/
		}

		public void SetTargetBehaviorAction(TargetBehavior behavior)
		{
			NRActionSetTargetBehavior action = new NRActionSetTargetBehavior();
			action.newBehavior = behavior;
			EditorNotes.SelectedNotes.ForEach(target => {
				action.affectedTargets.Add(target.data);
			});

			timeline.SetTargetBehaviors(action);
		}

		public void MoveTargetsAction(Vector2 direction)
		{
			drag.MoveTargets(direction);
		}

		public void ActivateSnapper(bool enable)
		{
			if (enable) snapper.EnableSpacingSnap();
			else snapper.DisableSpacingSnap();
		}

		public void CopySelectedTargets(bool copyTimestamp = true)
		{
			if (copyTimestamp) timeline.CopyTimestampToClipboard();
			clipboard = new List<TargetData>();
			bool displayWarning = false;
			foreach (var target in EditorNotes.SelectedNotes)
			{
				if (target.data.isRepeaterTarget)
                {
					displayWarning = true;
					continue;
                }
				clipboard.Add(target.data);
			}
            if (displayWarning)
            {
				NotificationCenter.SendNotification("Repeater targets can't be copied.", NotificationType.Warning);
            }
		}

		public void CopyTargets(List<TargetData> targets)
        {
			clipboard = targets;
        }

		public void CutSelectedTargets()
		{
			CopySelectedTargets(false);
			DeleteSelectedTargets();
		}

		public void PasteSelectedTargets()
		{
			EditorNotes.DeselectAllTargets();
			timeline.PasteCues(clipboard, EditorTime.Time);
		}
		public void DeleteSelectedTargets()
		{
			if (EditorNotes.SelectedNotes.Count > 0)
			{
				timeline.DeleteTargets(EditorNotes.SelectedNotes);
			}
		}

		public void ScaleSelectedTargets(Vector2 scale)
		{
			timeline.ScaleSelectedTargets(scale);
		}

		public void FlipTargetsVertical()
		{
			timeline.FlipSelectedTargetsVertical();
		}

		public void FlipTargetsHorizontal()
		{
			timeline.FlipSelectedTargetsHorizontal();
		}

		public void FlipTargetColors()
		{
			timeline.SwapTargets(EditorNotes.SelectedNotes);
		}

		public void ImmediateFlipTargetColors()
        {
			var iconsUnderMouse = MouseUtil.IconsUnderMouse(timeline);
			Target target = iconsUnderMouse.Length > 0 ? iconsUnderMouse[0].target : null;
			if (target != null)
			{
				timeline.SwapTargets(new() { target });
			}
		}

		public void TogglePlayPause(bool metronome)
		{
			timeline.TogglePlayback(metronome);
		}

		public void RotateSelectedTargetsRight()
		{
			timeline.Rotate(EditorNotes.SelectedNotes, -15);
		}

		public void RotateSelectedTargetsLeft()
		{
			timeline.Rotate(EditorNotes.SelectedNotes, 15);
		}

		public void ReverseSelectedTargets()
		{
			timeline.Reverse(EditorNotes.SelectedNotes);
		}

		public void ScrubTimeline(float direction, bool byTick)
		{
			timeline.ScrubTimeline(direction < 0f, byTick);
		}

		public void ChangeBeatSnap(float direction)
		{
			timeline.ChangeBeatSnap(direction > 0f);
		}

		public void ZoomTimeline(float direction)
		{
			EditorScale.Zoom(direction < 0f);
		}

		public void DragSelectTool(bool enable)
		{
			if (enable) drag.EnableDragSelect();
			else drag.DisableDragSelect();

		}

		public void ToggleModifiers()
		{
			modifiers.ToggleModifiers();
		}

		private bool useLegacy = false;
		public void TogglePathbuilder()
        {
			if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
			{
				useLegacy = !useLegacy;
				if (useLegacy && EditorState.IsToolActive(EditorTool.Pathbuilder)) EditorState.SelectTool(EditorTool.Pathbuilder);
				else if (!useLegacy && EditorState.IsToolActive(EditorTool.ChainBuilder))
				{
					ToggleChainbuilder();
				}
			}
			if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
			{
				if (useLegacy)
				{
					ToggleChainbuilder();
				}
				else
				{
					EditorState.SelectTool(EditorTool.Pathbuilder);
				}
			}
		}

        internal void ToggleChainbuilder()
        {
            if (pathbuilder.isActive)
            {
				pathbuilder.Activate(false);
				return;
            }
			chainbuilder.Activate(!chainbuilder.activated);   
        }

        internal void ToggleModifierPreview()
        {
			ModifierPreviewer.Instance.UpdateModifierList(EditorTime.Time.tick);
		}

        internal void GoToStartOfSong()
        {
			timeline.JumpToPercent(0f);
        }

        internal void GoToEndOfSong()
        {
			timeline.JumpToPercent(1f);
        }

        internal void NextBookmark()
        {
			MiniTimeline.Instance.JumpToNextBookmark();
        }

        internal void PreviousBookmark()
        {
			MiniTimeline.Instance.JumpToPreviousBookmark();
        }

        internal void CyclePrevious()
        {
			if (KeybindManager.Global.Modifier.IsShiftDown())
			{
				ChangeCycleMode(false);
				return;
			}
			switch (cycleMode)
			{
				case CycleMode.Behavior:
					CycleBehavior(false);
					break;
				case CycleMode.Hitsound:
					CycleHitsound(false);
					break;
				case CycleMode.Beatsnap:
					CycleBeatsnap(false);
					break;
				case CycleMode.Bookmark:
					PreviousBookmark();
					break;
				case CycleMode.UndoRedo:
					Undo();
					break;
			}
		}

        internal void CycleNext()
        {
            if (KeybindManager.Global.Modifier.IsShiftDown())
            {
				ChangeCycleMode(true);
				return;
            }

            switch (cycleMode)
            {
				case CycleMode.Behavior:
					CycleBehavior(true);
					break;
				case CycleMode.Hitsound:
					CycleHitsound(true);
					break;
				case CycleMode.Beatsnap:
					CycleBeatsnap(true);
					break;
				case CycleMode.Bookmark:
					NextBookmark();
					break;
				case CycleMode.UndoRedo:
					Redo();
					break;
            }
        }

		private void CycleBehavior(bool next)
        {
			string current = Enum.GetName(typeof(TargetBehavior), EditorState.Behavior.Current);
			if (current == "Mine") current = "Melee";
			OrderedTargetBehavior ordered = (OrderedTargetBehavior)Enum.Parse(typeof(OrderedTargetBehavior), current);
			int index = (int)ordered;
			index = GetWrappedValue(6, index + (next ? 1 : -1));
			ordered = (OrderedTargetBehavior)index;
			current = Enum.GetName(typeof(OrderedTargetBehavior), ordered);
			TargetBehavior newBehavior = (TargetBehavior)Enum.Parse(typeof(TargetBehavior), current);
			EditorState.SelectBehavior(newBehavior);
        }
		private void CycleHitsound(bool next)
        {
			int current = (int)EditorState.Hitsound.Current;
			current = GetWrappedValue(6, current + (next ? 1 : -1));
			EditorState.SelectHitsound((TargetHitsound)current);
		}
		private void CycleBeatsnap(bool next)
        {
			timeline.ChangeBeatSnap(next);
        }

		private void ChangeCycleMode(bool next)
        {
			int current = (int)cycleMode;
			current = GetWrappedValue(4, current + (next ? 1 : -1));
			cycleMode = (CycleMode)current;
			NRSettings.config.cycleMode = current;
			NRSettings.SaveSettingsJson();
			NotificationCenter.SendNotification($"Changed Cylce Mode to {cycleMode}", NotificationType.Info, false);
        }

		private int GetWrappedValue(int max, int value)
        {
			if (value < 0) return max;
			else if (value > max) return 0;
			else return value;
        }

		private enum OrderedTargetBehavior
        {
			Standard,
			Sustain,
			Horizontal,
			Vertical,
			ChainStart,
			ChainNode,
			Melee
        }

		private enum CycleMode
        {
			Behavior,
			Hitsound,
			Beatsnap,
			Bookmark,
			UndoRedo
        }
    }
}

