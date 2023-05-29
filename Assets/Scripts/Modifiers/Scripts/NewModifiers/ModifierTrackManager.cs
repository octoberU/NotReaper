using System;
using System.Collections.Generic;
using NotReaper.Timing;
using NotReaper.UI.Components;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ModifierTrackManager : TrackManager
    {
        [SerializeField] private GameObject sidebar;
        [SerializeField] private ModifierInputPanel inputPanel;
        [Space]
        [SerializeField] private GameObject editBackground;
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private float defaultWidth = 112;
        [SerializeField] private float editWidth = 192;
        [Space] 
        [SerializeField] private GameObject modifierContentPanel;
        [SerializeField] private GameObject addTrackPanel;
        [SerializeField] private GameObject editTemplatePanel;

        private bool _isInEditMode = false;
        private bool _isInTemplateEditingMode = false;

        [NRInject] private ModifierManager _modifierManager;
        [NRInject] private ModifierInputManager _inputManager;
        
        protected override TimelineType TimelineType => TimelineType.Modifier;
        public static bool IsPrivateModifer(ModifierType type) => type is ModifierType.ArenaPosition or ModifierType.ArenaScale or ModifierType.ArenaSpin;

        public bool IsInTemplateEditingMode => _isInTemplateEditingMode;


        protected override List<TrackOrder> GetSavedTracks()
        {
            List<TrackOrder> privateTracks = new();
            var savedTracks = NRSettings.config.modifierTrackOrder;
            foreach (var kvp in savedTracks)
            {
                if(IsPrivateModifer((ModifierType)kvp.type))
                    privateTracks.Add(kvp);
            }

            foreach (var track in privateTracks)
                savedTracks.Remove(track);

            return savedTracks;
        }

        protected override void SaveTrackOrder(List<TrackOrder> trackOrder)
        {
            //NRSettings.config.modifierTrackOrder = trackOrder;
            //NRSettings.SaveSettingsJson();
        }

        protected override void DeleteContent(Content content)
        {
            _modifierManager.RemoveContentFromAction(content, false);
        }

        public override void Show(bool show)
        {
            sidebar.SetActive(show);
            inputPanel.gameObject.SetActive(show);

            if(_isInEditMode)
                ToggleEditMode();
        }

        public void ToggleEditMode()
        {
            _isInEditMode = !_isInEditMode;
            editBackground.gameObject.SetActive(_isInEditMode);

            foreach (var track in tracks)
                track.Value.ToggleEditMode(_isInEditMode);

            var size = contentRect.sizeDelta;
            size.x = _isInEditMode ? editWidth : defaultWidth;
            contentRect.sizeDelta = size;
            
            addTrackPanel.SetActive(_isInEditMode);
            modifierContentPanel.SetActive(!_isInEditMode);

            if (_isInTemplateEditingMode)
                ToggleEditTemplate();

        }

        public void ToggleEditTemplate()
        {
            editTemplatePanel.SetActive(!editTemplatePanel.activeSelf);
            
            _isInTemplateEditingMode = editTemplatePanel.activeSelf;
            
            _inputManager.OnEditTemplateToggle(_isInTemplateEditingMode);
            
            sidebar.SetActive(!editTemplatePanel.activeSelf);
        }
        
        public static readonly List<TrackManager.TrackOrder> DefaultModifierTrackOrder = new()
        {
            new (0, 0),
            new(1, 1),
            new (2,2),
            new (3,3),
            new (4,4),
            new (5,5),
            new (6,6),
            new (7,7),
            new (8,8),
            new (9,9),
            new (10,10),
            new (11,11),
            new (12,12),
            new (13,13),
            new (14,14),
            new (15,15),
            new (16,16),
            new (17,17),
            new (18,18),
            new (19,19),
            new (20,20),
            new (21,21),
            new (22,22),
        };
        
    }
}