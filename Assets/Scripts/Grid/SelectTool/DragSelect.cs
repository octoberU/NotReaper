using System.Collections.Generic;
using NotReaper.Models;
using NotReaper.Targets;
using UnityEngine;

namespace NotReaper.Grid.SelectTool {

	public class DragSelect : MonoBehaviour {

		bool isSelecting = false;
		Vector3 mousePosition1;

		public static List<GridTarget> selectedTargets = new List<GridTarget>();

		//TODO: New input manager with SO

		void Update() {
			// If we press the left mouse button, save mouse location and begin selection
			if (Input.GetKeyDown(KeyCode.F)) {
				isSelecting = true;
				mousePosition1 = Input.mousePosition;

				selectedTargets = new List<GridTarget>();
				foreach (GridTarget target in Timeline.orderedNotes) {
					target.Deselect();
				}
			}
			// If we let go of the left mouse button, end selection
			if (Input.GetKeyUp(KeyCode.F)) {


				foreach (GridTarget target in Timeline.importantNotes) {

					if (target == null) return;

					if (IsWithinSelectionBounds(target.gameObject)) {
						target.Select();
						selectedTargets.Add(target);
					}
				}


				isSelecting = false;
			}


		}

		void OnGUI() {
			if (isSelecting) {
				// Create a rect from both mouse positions
				var rect = DragUtil.GetScreenRect(mousePosition1, Input.mousePosition);
				DragUtil.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
				DragUtil.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));
			}


		}


		public bool IsWithinSelectionBounds(GameObject gameObject) {
			if (!isSelecting)
				return false;

			var camera = Camera.main;
			var viewportBounds =
				DragUtil.GetViewportBounds(camera, mousePosition1, Input.mousePosition);

			return viewportBounds.Contains(
				camera.WorldToViewportPoint(gameObject.transform.position));
		}


	}
}