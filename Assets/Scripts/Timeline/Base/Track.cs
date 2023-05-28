using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Modifiers;
using NotReaper.Targets;
using NotReaper.Timing;
using TMPro;
using UnityEngine;

namespace NotReaper
{
    public abstract class Track : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private GameObject editButtons;
        
        public int Order { get; protected set; }
        public int Type { get; private set; }
        public int TypeIndex { get; private set; }
        public TrackManager.TrackID ID { get; private set; }

        public List<Content> Content { get; private set; } = new();

        protected TrackManager trackManager;
        
        protected abstract string TypeToDisplayName(int type);

        public void Initialize(int type, int order, int typeIndex, TrackManager manager)
        {
            Order = order;
            text.text = TypeToDisplayName(type).ToLower();
            trackManager = manager;
            Type = type;
            TypeIndex = typeIndex;
            ID = new(Type, TypeIndex);
        }
        
        internal void SetOrder(int order) => Order = order;
        public void MoveTrackUp() => trackManager.MoveTrackUp(this);
        public void MoveTrackDown() => trackManager.MoveTrackDown(this);
        public void RemoveTrack()
        {
            trackManager.RemoveTrack(this);
        }

        public void Add(Content content) => Content.Add(content);
        public void Remove(Content content) => Content.Remove(content);
        public bool ContainsContentAtTime(QNT_Timestamp time)
            => Content.Any(content => content.timeframe.Contains(time));

        public bool ContainsContentAtTime(Timeframe timeframe)
            => Content.Any(content => content.timeframe.Contains(timeframe));

        public bool ContainsContentAtTime(Content content, Timeframe timeframe)
            => Content.Any(c => c.timeframe.Contains(timeframe) && c != content);

        public bool TryGetContent(QNT_Timestamp time, out Content content)
        {
            content = Content.FirstOrDefault(c => c.timeframe.Contains(time));
            return content != null;
        }

        public bool TryGetContent(QNT_Timestamp time, Content excludeContent, out Content content)
        {
            content = null;
            foreach (var c in Content)
            {
                if (c.timeframe.Contains(time) && c != excludeContent)
                {
                    content = c;
                    break;
                }
            }
            return content != null;
        }

        public bool TryGetContent(Timeframe timeframe, out Content content)
        {
            content = null;
            foreach (var c in Content)
            {
                if (c.timeframe.Contains(timeframe))
                {
                    content = c;
                    break;
                }
            }
            return content != null;
        }

        public virtual void OnReset() => Content.Clear();

        public void OnScaleChanged(float scaleAmount)
        {
            foreach (var content in Content)
            {
                content.OnScaleChanged(scaleAmount);
            }
        }

        public void SortContent() => Content.Sort((c1, c2) => c1.startTime.tick.CompareTo(c2.startTime.tick));

        public void ToggleEditMode(bool on)
        {
            editButtons.SetActive(on);
        }
    }
}
