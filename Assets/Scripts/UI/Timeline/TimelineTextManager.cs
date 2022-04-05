using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace NotReaper.UI
{
    public class TimelineTextManager : MonoBehaviour
    {
        public static TimelineTextManager Instance { get; private set; } = null;

        private static Dictionary<ulong, TimelineTextContainer> containers = new();

        [SerializeField] private TimelineTextContainer containerPrefab;
        [SerializeField] private Transform timelineParent;
        [SerializeField] private Camera timelineCam;
        [NRInject] private Timeline timeline;

        private int id = 0;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.Log("TimelineTextManager already exists.");
                return;
            }
            Instance = this;

            EditorScale.onScaleChanged += OnScaleChanged;
            EditorState.OnEditorReset += ClearTimelineTexts;
        }

        /// <summary>
        /// Adds text to timeline. Returns ID beloning to the text that is needed for updating or removing that text.
        /// </summary>
        /// <param name="text">The text you want to appear in the timeline.</param>
        /// <param name="timestamp">The timestamp at which the text should appear.</param>
        /// <returns>The text object's ID</returns>
        public int AddText(string text, QNT_Timestamp timestamp)
        {
            return AddText(text, timestamp.tick);
        }

        /// <summary>
        /// Adds text to timeline. Returns ID beloning to the text that is needed for updating or removing that text.
        /// </summary>
        /// <param name="text">The text you want to appear in the timeline.</param>
        /// <param name="time">The time at which the text should appear.</param>
        /// <returns>The text object's ID</returns>
        public int AddText(string text, ulong time)
        {
            if (string.IsNullOrEmpty(text)) text = "";
            text = text.ToLower();
            id++;
            if (containers.ContainsKey(time))
            {
                containers[time].AddText(id, text);
            }
            else
            {
                var instance = Instantiate(containerPrefab, timelineParent);
                var pos = instance.transform.localPosition;
                pos.x = new QNT_Timestamp(time).ToBeatTime();
                instance.transform.localPosition = pos;
                instance.AddText(id, text);
                containers.Add(time, instance);
            }
            return id;
        }

        /// <summary>
        /// Updates a timeline text object with the given id.
        /// </summary>
        /// <param name="text">The updated text</param>
        /// <param name="id">The id of the text object</param>
        public void UpdateText(string text, int id)
        {
            if (containers.Any(c => c.Value.HasID(id)))
            {
                containers.First(c => c.Value.HasID(id)).Value.UpdateText(id, text);
            }
        }
        /// <summary>
        /// Removes a timeline text object with the given id.
        /// </summary>
        /// <param name="id">THe id of the text object.</param>
        public void RemoveText(int id)
        {
            if (containers.Any(c => c.Value.HasID(id)))
            {
                var container = containers.First(c => c.Value.HasID(id));
                container.Value.RemoveText(id);
                if (!container.Value.HasAnyTextObjects())
                {
                    containers.Remove(container.Key);
                }
            }
        }

        /// <summary>
        /// Removes all Timeline text objects.
        /// </summary>
        public void ClearTimelineTexts()
        {
            foreach(var container in containers)
            {
                container.Value.ClearTexts();
            }

            containers.Clear();
        }

        private void OnScaleChanged(int _)
        {
            timelineParent.localScale = Vector3.one;

            foreach(var container in containers)
            {
                var scale = Vector3.one;
                scale.x *= EditorScale.ScaleAmount;
                container.Value.transform.localScale = scale;
            }
        }
    }
}

