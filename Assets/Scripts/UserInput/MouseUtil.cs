using System.Linq;
using UnityEngine;
using NotReaper.Targets;
using System.Collections.Generic;
using NotReaper.Timing;

namespace NotReaper.UserInput {
	public class MouseUtil {
		public static TargetIcon[] IconsUnderMouse(Timeline timeline) {

			Vector3 cameraPoint;
			TargetIconLocation desiredLocation;
            if (EditorState.IsOverTimeline)
            {
				cameraPoint = CameraProvider.timeline.ScreenToWorldPoint(Input.mousePosition);
				desiredLocation = TargetIconLocation.Timeline;
            }
            else
            {
				cameraPoint = CameraProvider.main.ScreenToWorldPoint(Input.mousePosition);
				desiredLocation = TargetIconLocation.Grid;
            }

			/*Vector2 gridPoint = new Vector2(cameraPoint.x, cameraPoint.y);
			Vector2 timelinePoint = gridPoint;
			timelinePoint.x += Timeline.Instance.timelineCamera.position.x;*/

			Vector2 point = cameraPoint;

			List<TargetIcon> targetsUnderMouse = new List<TargetIcon>();
			foreach(Target target in EditorNotes.LoadedNotes) {
				target.AddTargetIconsCloseToPointAtTime(targetsUnderMouse, EditorTime.Time, point, desiredLocation);
			}

			return targetsUnderMouse
			.Where(result => result.transform.GetComponent<TargetIcon>() != null && !result.transform.GetComponent<TargetIcon>().target.transient && result.transform.GetComponent<TargetIcon>().location == desiredLocation)
			.OrderBy(result => {
				// sort by the distance from the centre of the timeline (closest = 0)
				var target = result.transform.GetComponent<TargetIcon>();
				bool isTimeline = target.location == TargetIconLocation.Timeline;
				var distance = isTimeline ?
					Mathf.Abs(target.transform.localPosition.x) :
					Mathf.Abs(target.transform.position.z);
				return distance;
			})
			.Select(result => result.transform.GetComponent<TargetIcon>())
			.ToArray();
		}
	}
}