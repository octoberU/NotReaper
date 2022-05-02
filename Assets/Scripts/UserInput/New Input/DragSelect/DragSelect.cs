using System;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Grid;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.UserInput;
using UnityEngine;
using NotReaper.Timing;
using NotReaper.Modifier;
using NotReaper.ReviewSystem;
using UnityEngine.InputSystem;
using NotReaper.UI;
using NotReaper.Repeaters;
using UnityEngine.EventSystems;

namespace NotReaper.Tools
{
	public class DragSelect : NRInput<DragSelectKeybinds>
	{
        #region References
        [Header("References")]
		public Timeline timeline;
		public Transform dragSelectTimeline;
		public Transform timelineNotes;
		public Transform timelineCamera;
		public GameObject dragSelectGrid;
        #endregion

        #region Fields
        private List<Target> dragTimelineSelectedTargets = new List<Target>();
		private List<Target> dragGridSelectedTarget = new List<Target>();

		private List<TargetGridMoveIntent> gridTargetMoveIntents = new List<TargetGridMoveIntent>();
		private List<TargetTimelineMoveIntent> timelineTargetMoveIntents = new List<TargetTimelineMoveIntent>();

		private TargetIcon[] _iconsUnderMouse = new TargetIcon[0];

		private Vector2 startGridMovePos;
		private QNT_Timestamp startTimelineMoveTime;

		private DragState state = DragState.None;
		private SnappingMode oldSnappingMode;
		private Camera cam;
		private Camera timelineCam;
		private Vector2 mouseStartPosWorld;
		private Vector2 mouseStartPosScreen;

		private bool isActive;
		private bool isMouseDown;
		private Vector2 lastGridMovePosition;
		private List<Target> gridChainStarts = new();

		[NRInject] private RepeaterManager repeaterManager;
        #endregion

        #region Properties
        /** Fetch all icons currently under the mouse
		 *  Will only ever happen once per frame */
        public TargetIcon[] iconsUnderMouse
		{
			get
			{
				return _iconsUnderMouse = _iconsUnderMouse == null
					? MouseUtil.IconsUnderMouse(timeline)
					: _iconsUnderMouse;
			}
			set { _iconsUnderMouse = value; }
		}

		// Fetch the highest priority target (closest to current time)
		public TargetIcon iconUnderMouse
		{
			get
			{
				return iconsUnderMouse != null && iconsUnderMouse.Length > 0
					? iconsUnderMouse.OrderBy(t => Mathf.Abs(t.target.GetRelativeBeatTime())).First()
					: null;
			}
		}
		#endregion

		#region Awake and Update
		protected override void Awake()
			=> base.Awake();

		private void Start()
        {
			cam = CameraProvider.menu;
			timelineCam = CameraProvider.timeline;
		}

        public void Update()
		{
			if (state == DragState.None) return;
			if(state == DragState.DetectIntent)
            {
				float moveDistance = Math.Abs(mouseStartPosScreen.magnitude - actions.DragSelect.MousePosition.ReadValue<Vector2>().magnitude);
				if (moveDistance > 2)
                {
                    if (!iconUnderMouse)
                    {
						StartDrag();
                    }
                    else
                    {
                        if (!iconUnderMouse.isSelected)
                        {
							TryToggleSelection();
                        }
						state = EditorState.IsOverTimeline ? DragState.WantDragTargetsTimeline : DragState.WantDragTargetsGrid;
						StartDragTargets();
                    }
                }
			}
			UpdateSelections();
			UpdateTargetDragging();
			iconsUnderMouse = null;
			if (state == DragState.WantGridSelection) state = DragState.GridSelection;
			else if (state == DragState.WantTimelineSelection) state = DragState.TimelineSelection;
		}
        #endregion

        #region Enable and Disable
		[NRListener]
		private void OnToolChanged(EditorTool tool)
        {
			if(tool == EditorTool.DragSelect)
            {
                if (!isActive)
                {
					isActive = true;
					EnableDragSelect();
                }
            }
			else
            {
                if (isActive)
                {
					isActive = false;
					DisableDragSelect();
                }
            }
        }
        public void EnableDragSelect()
        {
			oldSnappingMode = EditorState.Snapping.Current;
			isActive = true;
			OnActivated();
			EditorTargets.EnableNearSustainButtons();
        }

		public void DisableDragSelect()
        {
			EndDrag();
			OnDeactivated();
			EditorTargets.EnableNearSustainButtons();
        }
        #endregion

        #region Drag Selection

        #region Start
        private void StartDrag()
		{
			if (EditorState.IsOverTimeline) StartTimelineSelection();
			else StartGridSelection();
		}
		private void StartTimelineSelection()
		{
			EditorNotes.DeselectAllTargets();
			float mouseX = mouseStartPosWorld.x;
			dragSelectTimeline.SetParent(timelineNotes);
			dragSelectTimeline.position = new Vector3(mouseX + timelineCamera.position.x, 0, 1);
			dragSelectTimeline.localPosition = new Vector3(dragSelectTimeline.transform.localPosition.x, 0.03f, 1);
			dragSelectTimeline.localScale = new Vector3(0f, 1.1f, 1f);
			dragSelectTimeline.gameObject.SetActive(true);
			dragTimelineSelectedTargets = new List<Target>();
			state = DragState.WantTimelineSelection;
		}

		private void StartGridSelection()
		{
			EditorNotes.DeselectAllTargets();
			Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			dragSelectGrid.transform.position = new Vector3(mousePos.x, mousePos.y, 0f);
			dragSelectGrid.transform.localScale = new Vector3(0, 0, 1f);
			dragSelectGrid.SetActive(true);
			dragGridSelectedTarget = new List<Target>();
			state = DragState.WantGridSelection;
		}
		#endregion

		#region Update
		// Update the selection box and individually added / removed targets
		private void UpdateSelections()
		{
			if (state == DragState.TimelineSelection) UpdateTimelineSelection();
			else if (state == DragState.GridSelection) UpdateGridSelection();
		}

		private void UpdateTimelineSelection()
		{
			float diff = cam.ScreenToWorldPoint(actions.DragSelect.MousePosition.ReadValue<Vector2>()).x - dragSelectTimeline.position.x + timelineCamera.position.x;
			float timelineScaleMulti = EditorScale.ScaleAmount;
			dragSelectTimeline.localScale = new Vector3(diff * timelineScaleMulti, 1.1f, 1);

			Vector2 topLeft = dragSelectTimeline.transform.TransformPoint(0, 0, 0);
			Vector2 size = dragSelectTimeline.transform.TransformVector(1, 1, 1);

			Vector2 center = new Vector2(topLeft.x + size.x / 2, topLeft.y - size.y / 2);

			float minX = Math.Min(center.x - size.x / 2, center.x + size.x / 2);
			float maxX = Math.Max(center.x - size.x / 2, center.x + size.x / 2);
			float minY = Math.Min(center.y - size.y / 2, center.y + size.y / 2);
			float maxY = Math.Max(center.y - size.y / 2, center.y + size.y / 2);

			Rect selectionRect = Rect.MinMaxRect(minX, minY, maxX, maxY);

			float offscreenOffset = timelineCamera.position.x; //timelineNotes.parent.position.x;
			QNT_Timestamp start = EditorTime.Time + Relative_QNT.FromBeatTime((minX - offscreenOffset - 1.0f) * timelineScaleMulti);
			QNT_Timestamp end = EditorTime.Time + Relative_QNT.FromBeatTime((maxX - offscreenOffset + 1.0f) * timelineScaleMulti);
			if (start > end)
			{
				QNT_Timestamp temp = start;
				start = end;
				end = temp;
			}

			List<Target> newSelectedTargets = new List<Target>();
			foreach (Target target in new NoteEnumerator(start, end))
			{
				if (target.IsTimelineInsideRect(selectionRect))
				{
					newSelectedTargets.Add(target);
				}
			}

			var deselectedTargets = dragTimelineSelectedTargets.Except(newSelectedTargets);
			foreach (Target t in deselectedTargets)
			{
				t.Deselect();
			}

			var selectedTargets = newSelectedTargets.Except(dragTimelineSelectedTargets);
			foreach (Target t in selectedTargets)
			{
				t.Select();
			}

			dragTimelineSelectedTargets = newSelectedTargets;
		}

		private void UpdateGridSelection()
		{
			Vector3 diff = Camera.main.ScreenToWorldPoint(Input.mousePosition) -
						   dragSelectGrid.transform.position;
			dragSelectGrid.transform.localScale = new Vector3(diff.x, diff.y * -1, 1f);

			Vector2 topLeft = new Vector2(dragSelectGrid.transform.position.x, dragSelectGrid.transform.position.y);
			Vector2 size = new Vector2(dragSelectGrid.transform.localScale.x, dragSelectGrid.transform.localScale.y);

			Vector2 center = new Vector2(topLeft.x + size.x / 2, topLeft.y - size.y / 2);

			float minX = Math.Min(center.x - size.x / 2, center.x + size.x / 2);
			float maxX = Math.Max(center.x - size.x / 2, center.x + size.x / 2);
			float minY = Math.Min(center.y - size.y / 2, center.y + size.y / 2);
			float maxY = Math.Max(center.y - size.y / 2, center.y + size.y / 2);

			Rect selectionRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
			List<Target> newSelectedTargets = new List<Target>();
			QNT_Timestamp start = EditorTime.Time - Relative_QNT.FromBeatTime(10f);
			QNT_Timestamp end = EditorTime.Time + Relative_QNT.FromBeatTime(10f);
			NoteEnumerator closeTargets = new NoteEnumerator(start, end);
			foreach (Target t in closeTargets)
			{
				if (t.IsInsideRectAtTime(EditorTime.Time, selectionRect))
				{
					newSelectedTargets.Add(t);
				}
			}

			var deselectedTargets = dragGridSelectedTarget.Except(newSelectedTargets);
			foreach (Target t in deselectedTargets)
			{
				t.Deselect();
			}

			var selectedTargets = newSelectedTargets.Except(dragGridSelectedTarget);
			foreach (Target t in selectedTargets)
			{
				t.Select();
			}

			dragGridSelectedTarget = newSelectedTargets;
		}
		#endregion

		#region End
		public void EndDrag()
		{
			if (state == DragState.DragTargetsTimeline) EndDragTimelineTargetAction();
			else if (state == DragState.DragTargetsGrid) EndDragGridTargetAction();
			dragSelectTimeline.gameObject.SetActive(false);
			dragSelectGrid.SetActive(false);
			state = DragState.None;
		}
		#endregion

		#endregion

		#region Target Dragging

		#region Start
		private void StartDragTargets()
		{
			if (state == DragState.WantDragTargetsTimeline)
			{
				StartDragTimelineTargetAction(iconUnderMouse);
				state = DragState.DragTargetsTimeline;
			}
			else if (state == DragState.WantDragTargetsGrid)
			{
				StartDragGridTargetAction(iconUnderMouse);
				state = DragState.DragTargetsGrid;
			}
		}

		private void StartDragTimelineTargetAction(TargetIcon icon)
		{
			startTimelineMoveTime = icon.data.time;
			timelineTargetMoveIntents = new List<TargetTimelineMoveIntent>();
			EditorNotes.SelectedNotes.ForEach(target => {
				var intent = new TargetTimelineMoveIntent();
				intent.targetData = target.data;
				intent.startTick = target.data.time;

                if (target.data.isRepeaterTarget)
                {
					foreach(var sibling in timeline.repeaterManager.GetMatchingRepeaterTargets(target.data))
                    {
						TargetTimelineMoveIntent siblingIntent = new();
						siblingIntent.targetData = sibling;
						siblingIntent.startTick = sibling.time;
						timelineTargetMoveIntents.Add(siblingIntent);
                    }
                }

				timelineTargetMoveIntents.Add(intent);
			});
			timelineTargetMoveIntents = timelineTargetMoveIntents.OrderBy(intent => (int)intent.targetData.behavior).ToList();
		}

		public static List<TargetGridMoveIntent> GenerateGridMoveIntentsFromSelectedTargets(out List<Target> chainStarts)
        {
			var gridIntents = new List<TargetGridMoveIntent>();
			EditorNotes.SelectedNotes.ForEach(target => {
				var intent = new TargetGridMoveIntent();
				intent.target = target.data;
				intent.startingPosition = new Vector2(target.data.x, target.data.y);
				bool targetIsParent = false;
				bool isMirroredHorizontally = false;
				bool isMirroredVertically = false;

				if (intent.target.isRepeaterTarget)
				{
					targetIsParent = intent.target.repeaterData.Section.isParent;
					isMirroredHorizontally = intent.target.repeaterData.Section.mirrorHorizontally;
					isMirroredVertically = intent.target.repeaterData.Section.mirrorVertically;
				}
				gridIntents.Add(intent);

				if (target.data.isRepeaterTarget)
				{
					foreach (var node in RepeaterManager.Instance.GetMatchingRepeaterTargets(target.data))
					{
						var childIntent = new TargetGridMoveIntent();
						childIntent.target = node;
						childIntent.startingPosition = node.position;
						if (targetIsParent)
						{
							if (node.repeaterData.Section.mirrorHorizontally)
								childIntent.orientation.x = -1f;
							if (node.repeaterData.Section.mirrorVertically)
								childIntent.orientation.y = -1f;
						}
						else
						{
							if (node.repeaterData.Section.isParent)
							{
								if (isMirroredHorizontally)
									childIntent.orientation.x = -1f;
								if (isMirroredVertically)
									childIntent.orientation.y = -1f;
							}
							else
							{
								if (isMirroredHorizontally && !node.repeaterData.Section.mirrorHorizontally)
									childIntent.orientation.x = -1f;
								if (isMirroredVertically && !node.repeaterData.Section.mirrorVertically)
									childIntent.orientation.y = -1f;
							}
						}


						gridIntents.Add(childIntent);
					}
				}
			});
			chainStarts = new();
			foreach (var intent in gridIntents)
			{
				if (intent.target.behavior.IsChain())
				{
					var start = TargetFinder.FindChainStart(intent.target);
					if (start != null)
					{
						if (!chainStarts.Contains(start))
							chainStarts.Add(start);
					}
				}
			}
			return gridIntents;
		}

		private void StartDragGridTargetAction(TargetIcon icon)
		{
			startGridMovePos = icon.data.position;
			gridTargetMoveIntents = GenerateGridMoveIntentsFromSelectedTargets(out gridChainStarts);
		}
		#endregion

		#region Update
		private void UpdateTargetDragging()
		{
			if (state == DragState.DragTargetsGrid) UpdateDragGridTargetAction();
			else if (state == DragState.DragTargetsTimeline) UpdateDragTimelineTargetAction();
		}

		private void UpdateDragTimelineTargetAction()
		{
			foreach (TargetTimelineMoveIntent intent in timelineTargetMoveIntents)
			{
				Relative_QNT offsetFromDragPoint = intent.startTick - startTimelineMoveTime;
				var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				mousePos.x /= Timeline.scaleTransform;
				mousePos.x -= timelineNotes.parent.position.x;
				QNT_Timestamp newTime = SnapToBeat(mousePos.x);

				newTime += offsetFromDragPoint;
				intent.targetData.SetTimeFromAction(newTime);
				intent.intendedTick = newTime;
			}
		}

		private void UpdateDragGridTargetAction()
		{
			var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			var newPosVec3 = NoteGridSnap.SnapToGrid(mousePos, EditorState.Snapping.Current);
			Vector2 newPos = new Vector2(newPosVec3.x, newPosVec3.y);

			if (lastGridMovePosition == newPos)
				return;

			foreach (TargetGridMoveIntent intent in gridTargetMoveIntents)
			{
				UpdateGridMoveIntent(intent, startGridMovePos, newPos);
			}
			lastGridMovePosition = newPos;
		}

		public static void UpdateGridMoveIntent(TargetGridMoveIntent intent, Vector2 startPos, Vector2 newPos)
        {
			newPos *= intent.orientation;

			var offsetFromDragPoint = intent.startingPosition - (startPos * intent.orientation);
			var tempNewPos = newPos + offsetFromDragPoint;

			Vector2 delta = tempNewPos - intent.target.position;

			intent.target.position = tempNewPos;
			intent.intendedPosition = tempNewPos;

			if (intent.target.isPathbuilderTarget)
			{
				intent.target.pathbuilderData.MoveBy(delta);
			}
		}
		#endregion

		#region End
		private void EndDragTimelineTargetAction()
		{
			if (timelineTargetMoveIntents.Count > 0)
			{
				EditorTargets.MoveTimelineTargets(timelineTargetMoveIntents);
				timelineTargetMoveIntents = new List<TargetTimelineMoveIntent>();
			}
		}

		private void EndDragGridTargetAction()
		{
			if (gridTargetMoveIntents.Count > 0)
			{
				EditorTargets.MoveGridTargets(gridTargetMoveIntents);

				foreach (var start in gridChainStarts)
					EditorTargets.UpdateChainConnector(start);

				gridTargetMoveIntents = new();
				gridChainStarts = new();
			}
		}
		#endregion

		#endregion
		
        #region Target Selection
        private void TryToggleSelection()
		{
			IsHoveringGrid.Instance.CheckGrid();
			iconsUnderMouse = null;
			if (NRSettings.config.singleSelectCtrl)
			{
				if (KeybindManager.Global.Modifier != KeybindManager.Global.Modifiers.Shift)
				{
					EditorNotes.DeselectAllTargets();
				}
			}
			var icon = iconUnderMouse;
			if (icon != null && icon.isSelected)
			{
				icon.TryDeselect();
			}
			else if (icon != null && !icon.isSelected)
			{
				if (KeybindManager.Global.Modifier.IsShiftDown())
				{
					if (EditorNotes.HasSelectedNotes && icon.location == TargetIconLocation.Timeline)
					{
						NoteEnumerator targets;
						if(icon.data.time > EditorNotes.SelectedNotes.Last().data.time)
                        {
							targets = new NoteEnumerator(EditorNotes.SelectedNotes[0].data.time, icon.data.time);
                        }
                        else
                        {
							targets = new NoteEnumerator(icon.data.time, EditorNotes.SelectedNotes.Last().data.time);

                        }
						foreach (var target in targets) target.Select();
					}
					else
					{
						icon.TrySelect();
					}
				}
				else
				{
					icon.TrySelect();
				}
			}
			else if(EditorState.IsOverGrid)
			{
				EditorNotes.DeselectAllTargets();
			}
		}
		#endregion

		#region Arrow Move
		public void MoveTargets(Vector2 direction)
        {
			Vector2 noteMovement = direction;
			noteMovement.x *= NotePosCalc.xSize;
			noteMovement.y *= NotePosCalc.ySize;

			if (KeybindManager.Global.Modifier.IsCtrlDown()) noteMovement *= .5f;
			if (KeybindManager.Global.Modifier.IsShiftDown()) noteMovement *= .25f;
			List<TargetGridMoveIntent> intents = new();

			foreach(var target in EditorNotes.SelectedNotes)
            {
				var intent = new TargetGridMoveIntent();
				intent.target = target.data;
				intent.startingPosition = new Vector2(target.data.x, target.data.y);
				intent.intendedPosition = new Vector2(target.data.x + noteMovement.x, target.data.y + noteMovement.y);
				bool targetIsParent = false;
				bool isMirroredHorizontally = false;
				bool isMirroredVertically = false;

                if (intent.target.isRepeaterTarget)
                {
					targetIsParent = intent.target.repeaterData.Section.isParent;
					isMirroredHorizontally = intent.target.repeaterData.Section.mirrorHorizontally;
					isMirroredVertically = intent.target.repeaterData.Section.mirrorVertically;
                }

				if (intent.target.data.isPathbuilderTarget)
				{
					intent.target.data.pathbuilderData.MoveBy(noteMovement);
				}
				intents.Add(intent);

				if (target.data.isRepeaterTarget)
				{
					foreach (var node in repeaterManager.GetMatchingRepeaterTargets(target.data))
					{
						var childIntent = new TargetGridMoveIntent();
						childIntent.target = node;
						childIntent.startingPosition = node.position;
						
						if (targetIsParent)
						{
							if (node.repeaterData.Section.mirrorHorizontally)
								childIntent.orientation.x = -1f;
							if (node.repeaterData.Section.mirrorVertically)
								childIntent.orientation.y = -1f;
						}
						else
						{
							if (node.repeaterData.Section.isParent)
							{
								if (isMirroredHorizontally)
									childIntent.orientation.x = -1f;
								if (isMirroredVertically)
									childIntent.orientation.y = -1f;
							}
							else
							{
								if (isMirroredHorizontally && !node.repeaterData.Section.mirrorHorizontally)
									childIntent.orientation.x = -1f;
								if (isMirroredVertically && !node.repeaterData.Section.mirrorVertically)
									childIntent.orientation.y = -1f;
							}
						}
						var amount = noteMovement * childIntent.orientation;
						childIntent.intendedPosition = node.position + amount;

						if (node.isPathbuilderTarget)
						{
							node.pathbuilderData.MoveBy(amount);
						}

						intents.Add(childIntent);
					}
				}
			}
			EditorTargets.MoveGridTargets(intents);
		}
		#endregion

		#region Helper
		private QNT_Timestamp SnapToBeat(float posX)
		{
			QNT_Timestamp time = EditorTime.Time + Relative_QNT.FromBeatTime(posX);
			return EditorTime.GetSnappedTime(time + EditorBeatSnap.Duration / 2, EditorBeatSnap.BeatSnap);
		}
		#endregion

		#region Overrides
		protected override void RegisterCallbacks()
        {
			actions.DragSelect.Drag.performed += _ => OnMouseClick();
			actions.DragSelect.Drag.canceled += _ => OnMouseRelease();
			actions.DragSelect.Scrub.performed += obj => OnScrub(obj.ReadValue<float>() < 0);
        }

		protected override void OnEscPressed(InputAction.CallbackContext context) { }
		#endregion

		#region Callbacks
		private void OnMouseClick()
		{
			Vector2 mousePos = actions.DragSelect.MousePosition.ReadValue<Vector2>();
			if (HasClickedSustain(mousePos))
				return;
			else if (TransformTool.IsPointerOverTransformOverlay())
				return;

			isMouseDown = true;
			mouseStartPosScreen = mousePos;
			mouseStartPosWorld = cam.ScreenToWorldPoint(mouseStartPosScreen);
			state = DragState.DetectIntent;
			TryToggleSelection();
		}

		/// <summary>
		/// Starts sustain line drag if we clicked on a beatline.
		/// </summary>
		/// <param name="mousePos">The current mouse position.</param>
		/// <returns>True if the user clicked on a sustain.</returns>
		private bool HasClickedSustain(Vector2 mousePos)
        {
			Vector2 point = timelineCam.ScreenToWorldPoint(mousePos);
			var hits = Physics2D.RaycastAll(point, Vector2.zero, 1f);
			if (hits != null)
			{
				if (hits.Any(hit => hit.transform.tag == "BeatLengthLine"))
				{
					var hit = hits.First(hit => hit.collider.tag == "BeatLengthLine");
					hit.transform.GetComponent<BeatLine>().OnLinePressed();
					return true;
				}
			}

			return false;
		}

		private void OnMouseRelease()
		{
			EndDrag();
			isMouseDown = false;
		}

		private void OnScrub(bool forward)
        {
            if (KeybindManager.Global.Modifier.IsAltDown())
            {
				if (!forward)
					EditorBeatSnap.NextBeatSnap();
				else
					EditorBeatSnap.PreviousBeatSnap();
            }
            else
            {
				EditorAudio.ScrubTimeline(forward, !isMouseDown);
            }
        }

        protected override void SetRebindConfiguration(ref RebindConfiguration options, DragSelectKeybinds myKeybinds)
        {
			options.SetRebindable(false);
			options.AddHiddenKeybinds(myKeybinds.DragSelect.MousePosition, myKeybinds.DragSelect.Scrub);
        }
        #endregion

        #region Enum
        private enum DragState
		{
			None,
			DetectIntent,
			WantDragTargetsTimeline,
			WantDragTargetsGrid,
			WantTimelineSelection,
			WantGridSelection,
			DragTargetsTimeline,
			DragTargetsGrid,
			TimelineSelection,
			GridSelection
		}
		#endregion
	}
}