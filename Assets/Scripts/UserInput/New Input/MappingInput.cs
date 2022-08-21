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
using Melanchall.DryWetMidi.Smf;
using TMPro;
using UnityEngine;
using NotReaper.Tools.SpacingSnap;
using NotReaper.Modifier;
using NotReaper.Modifiers;
using NotReaper.Tools.PathBuilder;
using NotReaper.Notifications;
using NotReaper.Timing;
using NotReaper.Audio;
using NotReaper.HitsoundTimeline;
using NotReaper.Modifiers.Preview;
using NotReaper.SustainTimeline;

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
		//[SerializeField] private UndoRedoManager undoRedo;
		[SerializeField] private SpacingSnapper snapper;
		[SerializeField] private DragSelect drag;
		[NRInject] private Pathbuilder pathbuilder;
		[NRInject] private IsHoveringGrid gridHover;
		[NRInject] private MixerManager mixerManager;
		[NRInject] private ModifierManager modifierManager;
		[NRInject] private HitsoundManager hitsoundManager;
		//[NRInject] private SustainTimelineManager sustainManager;

		private List<TargetData> clipboard = new List<TargetData>();
        private CycleMode cycleMode = CycleMode.Beatsnap;

        private void Start()
        {
			NRSettings.OnLoad(() => cycleMode = (CycleMode)NRSettings.config.cycleMode);
        }

		public void PlaceNote()
		{
			if (!gridHover.CanPlaceNote())
				return;

			EditorTargets.AddTarget(ghost.position.x, ghost.position.y);
			background.OnPlaceNote();
		}

		public void Redo() => UndoRedoManager.Redo();

		public void RemoveNote()
		{
			TargetIcon targetIcon = GetNearestTarget(MouseUtil.IconsUnderMouse(timeline));

			if (targetIcon != null)
				EditorTargets.DeleteTarget(targetIcon.target);
		}

		private TargetIcon GetNearestTarget(TargetIcon[] targets) 
			=> targets != null && targets.Length > 0
					? targets.OrderBy(t => Mathf.Abs(t.target.GetRelativeBeatTime())).First()
					: null;

		public void Undo() => UndoRedoManager.Undo();
		public void DeselectAllTargets() => EditorNotes.DeselectAllTargets();
		public void SelectAllTargets()
        {
			if(KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.Ctrl)
				EditorNotes.SelectAllTargets();
        }
		//public void Save() => timeline.Export();
		public void Save() => EditorIO.SaveMap();
		public void SelectUntilNextBookmark()
        {
	        if (MiniTimeline.Instance.bookmarks.Count == 0)
	        {
		        SelectAllTargets();
				return;
	        }
			EditorNotes.DeselectAllTargets();
			var from = EditorTime.Time;
			var buffer = new QNT_Duration(1);
			var bookmarks = MiniTimeline.Instance.bookmarks;
			foreach(var bookmark in bookmarks.OrderBy(b => b.transform.position.x))
            {
				if (bookmark.time <= from)
					continue;

				SelectNotes(from, bookmark.time);
				return;
            }
			
			SelectNotes(from, EditorAudio.SongEndTime);

			void SelectNotes(QNT_Timestamp from, QNT_Timestamp to)
			{
				var notes = new NoteEnumerator(from - buffer, to).ToList();
				for (int i = notes.Count - 1; i >= 0; i--)
				{
					var data = notes[i].data;
					if(data.time < from || data.time >= to)
						notes.RemoveAt(i);
				}
				EditorNotes.SelectTargets(notes);
			}
        }

		public void DuplicateAndSwap()
        {
			if (!EditorNotes.HasSelectedNotes)
				return;

			List<TargetData> copyData = new();
			foreach(var selected in EditorNotes.SelectedNotesData)
            {
				var data = new TargetData();
				data.Copy(selected);
				if (data.handType == TargetHandType.Left)
					data.handType = TargetHandType.Right;
				else if (data.handType == TargetHandType.Right)
					data.handType = TargetHandType.Left;
				
				
				if (data.behavior == TargetBehavior.Legacy_Pathbuilder)
				{
					data.legacyPathbuilderData = new LegacyPathbuilderData();
					data.legacyPathbuilderData.Copy(selected.legacyPathbuilderData);
					data.legacyPathbuilderData.handType = data.handType;
				}
				else if (data.isPathbuilderTarget)
				{
					data.pathbuilderData = new PathbuilderData();
					data.pathbuilderData.Copy(selected.pathbuilderData);
				}
				
				copyData.Add(data);
            }
			var action = new NRActionMultiAddNote(copyData);
			UndoRedoManager.AddAction(action);
			EditorNotes.DeselectAllTargets();
			EditorNotes.SelectTargets(action.createdTargets);
        }

		public void SetTargetHitsoundAction(InternalTargetVelocity velocity)
		{
			bool needDeselect = false;
			if (EditorNotes.SelectedNotes.Count == 0)
			{
				if (hitsoundManager.IsActive)
				{
					if (hitsoundManager.TryGetTargetUnderMouse(out var targets))
					{
						EditorNotes.SelectTargets(targets);
						needDeselect = true;
					}
					else
					{
						return;
					}
				}
				else
				{
					return;
				}
			}
			var intents = new List<TargetSetHitsoundIntent>();
			bool showMeleeNotif = false;
			foreach (var target in EditorNotes.SelectedNotes)
			{
				if (target.data.behavior.IsMine()) continue;
				
				if (target.data.behavior.IsMelee())
				{
					if (velocity != InternalTargetVelocity.Melee && velocity != InternalTargetVelocity.Snare)
					{
						showMeleeNotif = true;
						continue;
					}
				}
				var intent = new TargetSetHitsoundIntent();

				intent.target = target;
				intent.startingVelocity = target.data.velocity;
				intent.newVelocity = velocity;

				intents.Add(intent);
			}

			if (showMeleeNotif)
			{
				NotificationCenter.SendNotification($"Can't set melee hitsound to something that isn't Melee or Snare.", NotificationType.Warning, false);
			}
			EditorTargets.SetTargetHitsounds(intents);
			if (needDeselect)
			{
				EditorNotes.DeselectAllTargets();
			}
		}

		public void SetTargetBehaviorAction(TargetBehavior behavior)
		{
			NRActionSetTargetBehavior action = new NRActionSetTargetBehavior();
			action.newBehavior = behavior;
			foreach (var target in EditorNotes.SelectedNotesData)
			{
				if (target.behavior.IsMine()) continue;
				action.affectedTargets.Add(target);
			}

			if (action.affectedTargets.Count == 0) return;
			
			EditorTargets.SetTargetBehaviors(action);
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

		public void ScaleSelectedTargets(Vector2 scale)
		{
			EditorTargets.ScaleSelectedTargets(scale);
		}

		public void FlipTargetsVertical()
		{
			EditorTargets.FlipSelectedTargetsVertical();
		}

		public void FlipTargetsHorizontal()
		{
			EditorTargets.FlipSelectedTargetsHorizontal();
		}

		public void FlipTargetColors()
		{
			EditorTargets.SwapSelecedTargetsColor();
		}

		public void ImmediateFlipTargetColors()
        {
			var iconsUnderMouse = MouseUtil.IconsUnderMouse(timeline);
			Target target = iconsUnderMouse.Length > 0 ? iconsUnderMouse[0].target : null;
			if (target != null)
			{
				if (target.data.behavior.IsMelee())
				{
					EditorTargets.FlipTargetHorizontal(target);
				}
				else
				{
					EditorTargets.SwapTargetColors(target);
				}
			}
		}

		public void TogglePlayPause(bool metronome)
			=> EditorAudio.TogglePlay(metronome);
		public void RotateSelectedTargetsRight()
			=> EditorTargets.RotateSelectedTargets(-15);

		public void RotateSelectedTargetsLeft()
			=> EditorTargets.RotateSelectedTargets(15);

		public void RotateSelectedTargets90()
			=> EditorTargets.RotateSelectedTargets(90);

		public void ReverseSelectedTargets()
			=> EditorTargets.ReverseSelectedTargets();

		public void ScrubTimeline(float direction, bool byTick)
			=> EditorAudio.ScrubTimeline(direction < 0f, byTick);

		public void ChangeBeatSnap(float direction)
			=> timeline.ChangeBeatSnap(direction > 0f);

		public void ZoomTimeline(float direction)
			=> EditorScale.Zoom(direction < 0f);

		public void DragSelectTool(bool enable)
		{
			if (enable) drag.EnableDragSelect();
			else drag.DisableDragSelect();

		}

		public void ToggleModifiers()
			=> modifierManager.ToggleTimeline();

		public void ToggleHitsoundTimeline()
			=> hitsoundManager.ToggleTimeline();

		public void ToggleSustainTimeline()
		{
			//=> sustainManager.ToggleTimeline();
		}

		public void TogglePathbuilder() => EditorState.SelectTool(EditorTool.Pathbuilder);

		internal void ToggleChainbuilder()
        {
	        Debug.Log("If you see this - how the fuck did you manage to get here?");
	        return;
        }

		internal void ToggleModifierPreview()
        {
	        ModifierPreviewer.Instance.StartPreview();
		}

        internal void GoToStartOfSong()
        {
			EditorAudio.ForceJumpToPercent(0f);
        }

        internal void GoToEndOfSong()
        {
			EditorAudio.ForceJumpToPercent(1f);
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
			NotificationCenter.SendNotification($"Changed Cycle Mode to {cycleMode}", NotificationType.Info, false);
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

		private void OnApplicationFocus(bool focus)
		{
			if (focus)
			{
				if (EditorState.IsToolActive(EditorTool.DragSelect))
				{
					DragSelectTool(false);
				}
				if(EditorState.IsToolActive(EditorTool.SpacingSnapper))
                {
					ActivateSnapper(false);
                }
			}
		}

        internal void BakeSelectedPath()
        {
			if (EditorNotes.SelectedNotes.Count != 1)
				return;

			var target = EditorNotes.SelectedNotes[0];
            if (target.data.isPathbuilderTarget)
            {
				var action = new NRActionBakePathbuilderTarget(target, pathbuilder);
				UndoRedoManager.AddAction(action);
            }
        }

        internal void TogglePreset()
        {
           mixerManager.TogglePreset();
        }
    }
}

