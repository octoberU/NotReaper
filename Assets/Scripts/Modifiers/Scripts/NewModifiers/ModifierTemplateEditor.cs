using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Notifications;
using NotReaper.UI.Components;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ModifierTemplateEditor : MonoBehaviour
    {
        [SerializeField] private ModifierTemplateTrack trackPrefab;
        [SerializeField] private RectTransform trackContainer;


        private List<ModifierTemplateTrack> _tracks = new();
        
        public void OnEnable()
        {
            var savedTemplate = NRSettings.config.modifierTrackOrder;

            foreach (var track in savedTemplate)
            {
                var entry = Instantiate(trackPrefab, trackContainer);
                entry.Init(this, track);
                _tracks.Add(entry);
            }
            
            foreach(var track in _tracks)
                track.transform.SetSiblingIndex(track.Order);
        }

        public void OnDisable()
        {
            for (int i = _tracks.Count - 1; i >= 0; i--)
            {
                var track = _tracks[i];
                Destroy(track.gameObject);
            }
            
            _tracks.Clear();
        }

        public void AddTrack(int type)
        {
            int typeIndex = -1;
            int order = _tracks.Count;
            
            foreach (var track in _tracks)
            {
                if (track.TypeIndex <= typeIndex) 
                    continue;
                
                typeIndex = track.TypeIndex;
                order = track.Order;
            }

            typeIndex++;
            order++;

            var entry = Instantiate(trackPrefab, trackContainer);
            entry.Init(this, new TrackManager.TrackOrder(type, order, typeIndex));

            foreach (var track in _tracks)
            {
                if (track.Order < order) 
                    continue;
                
                var index = track.transform.GetSiblingIndex();
                track.transform.SetSiblingIndex(index + 1);
                track.Order++;
            }
            
            entry.transform.SetSiblingIndex(order);
            _tracks.Add(entry);
        }

        public void RemoveTrack(ModifierTemplateTrack track)
        {
            _tracks.Remove(track);
            Destroy(track.gameObject);
        }

        public void SaveTemplate()
        {
            List<TrackManager.TrackOrder> trackOrder = new();
            
            foreach (var track in _tracks)
                trackOrder.Add(track.GetData());

            NRSettings.config.modifierTrackOrder = trackOrder;
            NRSettings.SaveSettingsJson();

            NotificationCenter.SendNotification("Track template saved!", NotificationType.Success);
        }
    }
}
