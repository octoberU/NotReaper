using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Timing;
using TMPro;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class Track : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private GameObject sortButtons;

        internal int Order { get; private set; }

        private TrackManager trackManager;
        internal ModifierType Type { get; private set; }

        internal List<Modifier> Modifiers { get; private set; } = new();

        internal void Initialize(ModifierType type, int order, TrackManager manager)
        {
            Order = order;
            Type = type;
            text.text = type.ToDisplayName().ToLower();
            trackManager = manager;
        }

        internal void SetOrder(int order) => Order = order;
        public void MoveTrackUp() => trackManager.MoveTrackUp(this);
        public void MoveTrackDown() => trackManager.MoveTrackDown(this);
        public void AddModifier(Modifier modifier) => Modifiers.Add(modifier);
        public void RemoveModifier(Modifier modifier) => Modifiers.Remove(modifier);
        public void ShowSortButtons(bool show) => sortButtons.SetActive(show);

        public bool ContainsModifierAtTime(QNT_Timestamp time)
            => Modifiers.Any(modifier => modifier.timeframe.Contains(time));

        public bool ContainsModifierAtTime(Modifier modifier, Timeframe timeframe)
            => Modifiers.Any(m => m.timeframe.Contains(timeframe) && m != modifier);

        public bool TryGetModifier(QNT_Timestamp time, out Modifier modifier)
        {
            modifier = Modifiers.FirstOrDefault(m => m.timeframe.Contains(time));
            return modifier != null;
        }

        public void OnReset() => Modifiers.Clear();


    }
}