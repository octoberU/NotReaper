using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ModifierManager : MonoBehaviour
    {
        [SerializeField] private TrackManager tracks;
        
        [NRInject] private ModifierTimeline timeline;
        
        public bool IsActive { get; private set; }

        public void ToggleModifiers()
        {
            IsActive = !IsActive;
            
            Show(IsActive);
        }
        
        public void Show(bool show)
        {
            timeline.ShowModifierTimeline(show);
            tracks.Show(IsActive);
        }
    }
}
