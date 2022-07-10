using System;
using System.Collections.Generic;
using NotReaper.Timing;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ModifierTrackManager : TrackManager
    {
        [SerializeField] private GameObject sidebar;
        [SerializeField] private ModifierInputPanel inputPanel;


        protected override TimelineType TimelineType => TimelineType.Modifier;

        private bool isPrivateBuild = false;

        private bool IsPrivateModifer(ModifierType type) => type is ModifierType.ArenaPosition or ModifierType.ArenaScale or ModifierType.ArenaSpin;


        protected override SerializableDictionary<int, int> GetSavedTracks()
        {
            List<int> privateTracks = new();
            var savedTracks = NRSettings.config.modifierTrackOrder;
            foreach (var kvp in savedTracks)
            {
                if(IsPrivateModifer((ModifierType)kvp.Key))
                    privateTracks.Add(kvp.Key);
            }

            foreach (var track in privateTracks)
                savedTracks.Remove(track);

            return savedTracks;
        }

        protected override void SaveTrackOrder(SerializableDictionary<int, int> trackOrder)
        {
            NRSettings.config.modifierTrackOrder = trackOrder;
            NRSettings.SaveSettingsJson();
        }

        public override void Show(bool show)
        {
            sidebar.SetActive(show);
            inputPanel.gameObject.SetActive(show);
        }
    }
}