using NotReaper.Targets;
using System.Collections;
using System.Collections.Generic;
using NotReaper.HitsoundTimeline;
using UnityEngine;

namespace NotReaper.TargetEditor
{
    public class TargetSpawner : MonoBehaviour
    {
        [SerializeField] private GridTargetPool gridPool;
        [SerializeField] private TimelineTargetPool timelinePool;
        private Transform gridCamera;

        private void Start()
        {
            gridCamera = CameraProvider.grid.transform;
        }
        /// <summary>
        /// Spawns a new target.
        /// </summary>
        /// <param name="data">The data for the target.</param>
        /// <param name="transient">True if a transient note (e.g. pathbuilder note)</param>
        /// <returns>The spawned target.</returns>
        public Target SpawnTarget(TargetData data, bool transient)
        {
            var timelineTargetIcon = timelinePool.Spawn();
            timelineTargetIcon.location = TargetIconLocation.Timeline;
            var transform1 = timelineTargetIcon.transform;
            transform1.localPosition = new Vector3(data.time.ToBeatTime(), 0, 0);

            Vector3 noteScale = transform1.localScale;
            noteScale.x = EditorScale.NoteScale;
            transform1.localScale = noteScale;

            var gridTargetIcon = gridPool.Spawn();
            gridTargetIcon.transform.localPosition = new Vector3(data.x, data.y, data.time.ToBeatTime());

            gridTargetIcon.transform.localScale = new Vector3(NRSettings.config.noteScale, NRSettings.config.noteScale, 1f);
            gridTargetIcon.location = TargetIconLocation.Grid;
            return new(data, timelineTargetIcon, gridTargetIcon, transient, gridCamera);
        }

        public void ReturnTarget(Target target)
        {
            if (target == null)
            {
                return;
            }

            timelinePool.Return(target.timelineTargetIcon);
            gridPool.Return(target.gridTargetIcon);
        }
    }

}
