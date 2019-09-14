using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Grid;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.UserInput;
using UnityEngine;

namespace NotReaper.Tools {

	public class DragSelect : MonoBehaviour {

		public Timeline timeline;

		public bool activated = false;
		bool isDraggingTimeline = false;
		bool isDraggingGrid = false;
		bool isDraggingNotes = false;
		bool isSelectionDown = false;
		bool hasMovedOutOfClickBounds = false;

		Vector3 startDragMovePos;
		Vector2 startClickDetectPos;

		public Transform dragSelectTimeline;
		public Transform timelineNotes;

		public GameObject dragSelectGrid;

		public LayerMask notesLayer;

		public List<Target> clipboardNotes = new List<Target>();
		public float clipboardBeatTime = 0f;


		//INFO: Code for selecting targets is on the drag select timline thing itself

		/// <summary>
		/// Sets if the tool is active or not.
		/// </summary>
		/// <param name="active"></param>
		public void Activate(bool active) {
			activated = active;

			if (!active) {
				if (isDraggingTimeline) EndTimelineDrag();
				else if (isDraggingGrid) EndGridDrag();
				timeline.DeselectAllTargets();
			}


		}

		private void StartTimelineDrag() {

			timeline.DeselectAllTargets();

			float mouseX = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;

			dragSelectTimeline.SetParent(timelineNotes);
			dragSelectTimeline.position = new Vector3(mouseX, 0, 0);
			dragSelectTimeline.localPosition = new Vector3(dragSelectTimeline.transform.localPosition.x, 0.03f, 0);

			dragSelectTimeline.gameObject.SetActive(true);

			isDraggingTimeline = true;
		}

		private void EndTimelineDrag() {
			isDraggingTimeline = false;

			dragSelectTimeline.gameObject.SetActive(false);

		}


		private void StartGridDrag() {

			timeline.DeselectAllTargets();

			Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);


			dragSelectGrid.transform.position = new Vector3(mousePos.x, mousePos.y, 0f);
			dragSelectGrid.transform.localScale = new Vector3(0, 0, 1f);

			dragSelectGrid.SetActive(true);


			isDraggingGrid = true;
		}

		private void EndGridDrag() {

			dragSelectGrid.SetActive(false);
			isDraggingGrid = false;
		}

		private void StartDragAction(TargetIcon icon) {
			isDraggingNotes = true;
			startDragMovePos = icon.transform.position;
		}

		private void EndDragAction() {
			isDraggingNotes = false;
		}
		private void StartSelectionAction() {
			isSelectionDown = true;
			hasMovedOutOfClickBounds = false;
			startClickDetectPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

		}

		private void EndSelectionAction() {

			isSelectionDown = false;

		}

		private void TryToggleSelection() {
			TargetIcon underMouse = IconUnderMouse();
			if (underMouse && underMouse.isSelected) {
				underMouse.TryDeselect();
			}
			else if (underMouse && !underMouse.isSelected) {
				underMouse.TrySelect();
			}
		}

		private TargetIcon IconUnderMouse() {
			RaycastHit hit;
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			ray.origin = new Vector3(ray.origin.x, ray.origin.y, -1.7f);
			ray.direction = Vector3.forward;
			Debug.DrawRay(ray.origin, ray.direction);
			//TODO: Tweakable selection distance for raycast
			//Box collider size currently 2.2
			if (Physics.Raycast(ray, out hit, 3.4f, notesLayer)) {
				Transform objectHit = hit.transform;

				TargetIcon targetIcon = objectHit.GetComponent<TargetIcon>();

				return targetIcon;
			}
			return null;
		}


		public Vector3 CalcAvgNotePos(List<Target> targets) {

			Vector3 avgPos = new Vector3(0, 0, 0);

			foreach (Target target in targets) {
				avgPos += target.gridTargetPos;

			}
			return avgPos / targets.Count;
		}


		void Update() {

			if (!activated) return;
			TargetIcon iconUnderMouse = IconUnderMouse();

			/** Click Detection **/
			if (isSelectionDown && !hasMovedOutOfClickBounds) {

				// Check for a tiny amount of mouse movement to ensure this was meant to be a click

				float movement = Math.Abs(startClickDetectPos.magnitude - Input.mousePosition.magnitude);
				if (movement > 2) {
					hasMovedOutOfClickBounds = true;
				}
			}

			/** Cut Copy Paste Delete **/
			// TODO: Move these actions into timeline to record sane undo actions!
			Action delete = () => {
				if (timeline.selectedNotes.Count > 0) {
					timeline.DeleteTargets(timeline.selectedNotes, true, true);
				}
				timeline.selectedNotes = new List<Target>();
			};

			Action copy = () => {
				clipboardNotes = new List<Target>();
				clipboardNotes = timeline.selectedNotes;

				if (clipboardNotes.Count < 1) return;

				clipboardBeatTime = Mathf.Infinity;

				//Find the soonest target in the selection
				foreach (Target target in clipboardNotes) {
					float pos = target.gridTargetIcon.transform.localPosition.z;
					if (pos < clipboardBeatTime) {
						clipboardBeatTime = pos;
					}
				}
			};

			Action paste = () => {
				timeline.DeselectAllTargets();

				List<Target> pasteNotes = new List<Target>();

				float diff = Timeline.BeatTime() - clipboardBeatTime;

				foreach (Target target in clipboardNotes) {
					target.gridTargetPos.z += diff;
					pasteNotes.Add(target);

				}

				clipboardBeatTime = Timeline.BeatTime();
				timeline.AddTargets(clipboardNotes, true, true);
			};

			if (Input.GetKeyDown(KeyCode.Delete)) {
				delete();
			}

			bool dev = false;
			bool modifierHeld = dev ?
				Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) :
				Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

			if (modifierHeld) {
				if (Input.GetKeyDown(KeyCode.X)) {
					copy();
					delete();
				}

				if (Input.GetKeyDown(KeyCode.C)) {
					copy();
				}

				if (Input.GetKeyDown(KeyCode.V)) {
					paste();
				}
			}

			/** Note flipping **/
			if (Input.GetKeyDown(KeyCode.F)) {

				var ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
				var shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

				// flip horizontal
				if (ctrlHeld && !shiftHeld) {
					timeline.FlipTargetsHorizontal(timeline.selectedNotes);
				}

				// flip vertical
				else if (shiftHeld) {
					timeline.FlipTargetsVertical(timeline.selectedNotes);
				}

				// invert
				else {
					timeline.SwapTargets(timeline.selectedNotes);
				}
			}

			/** Click + Drag Handling **/
			//TODO: it should deselect when resiszing the grid dragger, but not deselect when scrubbing through the timeline while grid dragging

			if (EditorInput.selectedTool == EditorTool.DragSelect) {

				//TODO: Moving notes on timeline

				if (Input.GetMouseButtonDown(0) && !timeline.hover) {

					if (iconUnderMouse) {
						if (!iconUnderMouse.isSelected) return;
						StartDragAction(iconUnderMouse);
					}
				}

				if (Input.GetMouseButtonUp(0)) {
					EndDragAction();

					foreach (Target target in timeline.selectedNotes) {

						//TODO: does changing target in one list change it in another? no i don't think so

						target.gridTargetPos = target.gridTargetIcon.transform.localPosition;
					}
				}

				if (Input.GetMouseButton(0)) {

					//If we're not already dragging

					if (
						!isDraggingTimeline &&
						!isDraggingGrid &&
						!isDraggingNotes &&
						!iconUnderMouse
					) {
						if (timeline.hover) {
							StartTimelineDrag();
						}
						else {
							StartGridDrag();
						}
					}
					else if (isDraggingTimeline) {
						float diff = Camera.main.ScreenToWorldPoint(Input.mousePosition).x - dragSelectTimeline.position.x;
						float timelineScaleMulti = Timeline.scale / 20f;
						dragSelectTimeline.localScale = new Vector3(diff * timelineScaleMulti, 1.1f * (Timeline.scale / 20f), 1);
					}
					else if (isDraggingGrid) {

						Vector3 diff = Camera.main.ScreenToWorldPoint(Input.mousePosition) - dragSelectGrid.transform.position;
						dragSelectGrid.transform.localScale = new Vector3(diff.x, diff.y * -1, 1f);

					}

					else if (iconUnderMouse && !isSelectionDown) {
						StartSelectionAction();
					}
					else if (iconUnderMouse && timeline.selectedNotes.Count == 0 && hasMovedOutOfClickBounds) {
						iconUnderMouse.TrySelect();
						StartDragAction(iconUnderMouse);
					}
				}
				else {
					if (isDraggingTimeline) EndTimelineDrag();
					else if (isDraggingGrid) EndGridDrag();
				}
				if (Input.GetMouseButtonUp(0)) {
					EndSelectionAction();
					if (!hasMovedOutOfClickBounds) TryToggleSelection();
				}

				if (isDraggingNotes) {


					foreach (Target target in timeline.selectedNotes) {

						var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
						var offsetFromDragPoint = target.gridTargetPos - startDragMovePos;
						Vector3 newPos = NoteGridSnap.SnapToGrid(mousePos, EditorInput.selectedSnappingMode);
						newPos += offsetFromDragPoint;
						target.gridTargetIcon.transform.localPosition = new Vector3(newPos.x, newPos.y, target.gridTargetPos.z);
						//target.gridTargetPos = target.gridTargetIcon.transform.localPosition;

					}
				}
			}
		}
	}
}