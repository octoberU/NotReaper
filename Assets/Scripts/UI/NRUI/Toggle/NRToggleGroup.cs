using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.UI.Components
{
    public class NRToggleGroup : MonoBehaviour
    {
        [Header("Skin")]
        public NRToggleSkin skin;
        [Space, Header("Group")]
        public bool allowMultipleSelected = false;
        public bool allowNoneSelected = true;
        [Header("Animation")]
        public float animationDuration = .3f;
        [Space, Header("Text")]
        public float textSize = 20f;
        public bool autoSize = false;
        [Space, Header("Icon")]
        public float iconScale = 1f;
        [SerializeField] private NRToggle defaultToggle;

        [HideInInspector, SerializeField] private List<NRToggle> toggles = new();
        private NRToggle selectedToggle;

        private void Awake()
        {
            selectedToggle = defaultToggle;
        }

        public void RegisterToggle(NRToggle toggle)
        {
            if (!toggles.Contains(toggle))
            {
                toggles.Add(toggle);
                if (toggle.selected && defaultToggle == null && selectedToggle == null)
                {
                    SetSelectedToggle(toggle);
                }
            }

            if (!allowNoneSelected && selectedToggle == null && defaultToggle == null)
            {
                if(toggles.Count > 0 && !toggles.Any(t => t.isOn))
                {
                    SetSelectedToggle(toggle);
                }
            }
        }

        public void UnregisterToggle(NRToggle toggle)
        {
            if (toggles.Contains(toggle))
            {
                toggles.Remove(toggle);
            }
        }

        public void SetSelectedToggle(NRToggle toggle)
        {
            if (toggle == selectedToggle) return;
            if (allowMultipleSelected)
            {
                toggle.selected = true;
                return;
            }
            if (selectedToggle != null)
            {
                selectedToggle.Deselect();
            }
            selectedToggle = toggle;
            selectedToggle.selected = true;
        }

        public void DeselectToggle(NRToggle toggle)
        {
            if (allowNoneSelected)
            {
                toggle.selected = false;
                return;
            }
            if(!allowNoneSelected && toggles.Where(t => t.isOn).Count() == 0)
            {
                toggle.selected = true;
                return;
            }
            toggle.Deselect();
            if(toggle == selectedToggle)
            {
                selectedToggle = null;
            }
        }

        public void TryDeselectToggle(NRToggle toggle)
        {
            if (selectedToggle == toggle)
                selectedToggle = null;
        }

        public void DeselectAllAndForgetSelected()
        {
            foreach(var toggle in toggles)
            {
                toggle.selected = false;
            }
            selectedToggle = null;
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            bool hasSelectedToggle = false;
            foreach (var toggle in toggles)
            {
                toggle.UpdateVisuals();
                if (toggle.isOn) hasSelectedToggle = true;
            }
            if(!allowNoneSelected && !hasSelectedToggle && toggles.Count > 0)
            {
                foreach(var toggle in toggles)
                {
                    toggle.isOn = false;
                }
                toggles.First().isOn = true;
                toggles.First().UpdateVisuals();
                toggles.First().UpdateValues();
            }
        }
    }
}

