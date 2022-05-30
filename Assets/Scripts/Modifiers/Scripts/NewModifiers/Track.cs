using System.Collections;
using System.Collections.Generic;
using NotReaper.Modifier;
using TMPro;
using UnityEngine;

namespace  NotReaper.Modifiers
{
    public class Track : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private GameObject sortButtons;

        [NRInject] private TrackManager trackManager;
        
        internal int Order { get; private set; }
        internal ModifierHandler.ModifierType Type { get; private set; }

        internal void SetTrackType(ModifierHandler.ModifierType type)
        {
            Type = type;
            text.text = ModifierUtility.GetDisplayName(type).ToLower();
        }

        internal void SetOrder(int order)
            => Order = order;

        public void MoveTrackUp()
            => trackManager.MoveTrackUp(this);

        public void MoveTrackDown()
            => trackManager.MoveTrackDown(this);


    }
}
