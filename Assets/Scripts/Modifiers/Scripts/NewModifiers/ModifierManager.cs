using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.HtmlControls;
using NotReaper.Notifications;
using NotReaper.Timing;
using NotReaper.UI.Particles;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ModifierManager : MonoBehaviour
    {
        [SerializeField] private TrackManager tracks;
        [SerializeField] private Modifier modifierPrefab;
        [NRInject] private ModifierTimeline timeline;
        [NRInject] private ModifierInputManager inputManager;
        
        public bool IsActive { get; private set; }
        public Modifier CurrentModifier { get; private set; }
        public List<Modifier> Modifiers { get; private set; } = new();

        public List<Modifier> SelectedModifiers { get; private set; } = new();
        
        private List<Data> copiedModifiers = new();

        public delegate void OnModifierSelected(Modifier modifier);
        public static event OnModifierSelected onModifierSelected;

        public delegate void ModifierEvent();
        public static event ModifierEvent onSelectedModifierRemoved;
        public static event ModifierEvent onMultiSelect;

        private void Start()
        {
            EditorState.OnEditorReset += OnReset;
        }

        private void OnReset()
        {
            SelectedModifiers.Clear();
            copiedModifiers.Clear();
            CurrentModifier = null;
            
            for (int i = Modifiers.Count - 1; i >= 0; i--)
            {
                Destroy(Modifiers[i].gameObject);
            }
            
            Modifiers.Clear();
        }


        public void ToggleModifiers()
        {
            IsActive = !IsActive;
            
            Show(IsActive);
            
        }
        
        private void Show(bool show)
        {
            timeline.ShowModifierTimeline(show);
            tracks.Show(IsActive);
            
            if(show) inputManager.Activate();
            else inputManager.Deactivate();

            CameraProvider.grid.enabled = !show;
            EditorTargets.ShowVisuals(!show);
            GridParticles.AllowEmission(!show);
        }

        public void MultiSelectModifier(Modifier modifier)
        {
            if (!SelectedModifiers.Contains(modifier))
            {
                SelectedModifiers.Add(modifier);
                modifier.SetSelected(true);
            }
        }

        public void DeselectMultiselectModifier(Modifier modifier)
        {
            if (SelectedModifiers.Contains(modifier))
            {
                SelectedModifiers.Remove(modifier);
                modifier.SetSelected(false);
            }
        }

        private Modifier SelectModifier(QNT_Timestamp time, TrackContent content, bool multiSelect)
        {
            if (tracks.TryGetModifier(content.track.Type, time, out var modifier))
            {
                SelectModifier(modifier, multiSelect);
                return modifier;
            }

            return null;
        }

        public void SelectModifier(Modifier modifier, bool multiSelect)
        {
            if (multiSelect && CurrentModifier != null)
            {
                if (modifier.Selected)
                {
                    modifier.SetSelected(false);
                    if (SelectedModifiers.Contains(modifier))
                        SelectedModifiers.Remove(modifier);
                }
                else if(!SelectedModifiers.Contains(modifier))
                {
                    modifier.SetSelected(true);
                    SelectedModifiers.Add(modifier);
                    onMultiSelect?.Invoke();
                }
            }
            else
            {
                foreach(var m in SelectedModifiers)
                    m.SetSelected(false);
                    
                SelectedModifiers.Clear();
                    
                UpdateCurrentModifier(modifier);
                SelectCurrentModifier();
            }
        }

        public void SelectCurrentModifier()
        {
            if (CurrentModifier != null)
            {
                foreach(var modifier in SelectedModifiers)
                    modifier.SetSelected(false);
                
                SelectedModifiers.Clear();
                
                CurrentModifier.SetSelected(true);
                
                if(!SelectedModifiers.Contains(CurrentModifier))
                    SelectedModifiers.Add(CurrentModifier);
                
                onModifierSelected?.Invoke(CurrentModifier);
            }
        }

        public bool TrySelectModifier(QNT_Timestamp time, TrackContent content, bool multiSelect, out Modifier selectedModifier)
        {
            selectedModifier = null;
            
            if (tracks.ContainsModifierAtTime(content.track.Type, time))
            {
                selectedModifier = SelectModifier(time, content, multiSelect);
                return true;
            }

            return false;
        }
        public bool TryPlaceModifier(QNT_Timestamp startTime, TrackContent content)
        {
            if (tracks.ContainsModifierAtTime(content.track.Type, startTime))
            {
                SelectModifier(startTime, content, false);
                return false;
            }

            if (content.track.Type.IsUpdateModifier(out var baseModifier))
            {
                if (!tracks.ContainsModifierAtTime(baseModifier, startTime))
                {
                    NotificationCenter.SendNotification($"{content.track.Type.ToDisplayName()} modifiers can only be placed during active {baseModifier.ToDisplayName()} modifiers.");
                    return false;
                }
            }
            
            ModifierUndoRedo.AddAction(new AddModifierAction(startTime, content.track));
            return true;
        }

        public Modifier PlaceModifierFromAction(QNT_Timestamp startTime, Track track)
        {
            var modifier = Instantiate(modifierPrefab);
            modifier.Initialize(track);
            timeline.PlaceModifier(modifier);
            modifier.SetStartTime(startTime);
            UpdateCurrentModifier(modifier);
            tracks.AddModifier(CurrentModifier);
            Modifiers.Add(CurrentModifier);
            SelectCurrentModifier();
            return modifier;
        }

        public Modifier PlaceModifierFromAction(Timeframe timeframe, Track track)
        {
            var modifier = Instantiate(modifierPrefab);
            modifier.Initialize(track);
            timeline.PlaceModifier(modifier);
            modifier.SetTime(timeframe);
            UpdateCurrentModifier(modifier);
            tracks.AddModifier(CurrentModifier);
            Modifiers.Add(CurrentModifier);
            SelectCurrentModifier();
            return modifier;
        }

        private void UpdateCurrentModifier(Modifier newCurrent, bool select = true)
        {
            if (CurrentModifier != null)
            {
                CurrentModifier.SetSelected(false);
                if (SelectedModifiers.Contains(CurrentModifier))
                    SelectedModifiers.Remove(CurrentModifier);
            }
            CurrentModifier = newCurrent;
            
            if (select)
            {
                SelectCurrentModifier();
            }
        }

        public Modifier LoadModifier(Data data)
        {
            var modifier = Instantiate(modifierPrefab);
            modifier.Initialize(tracks.GetTrack(data.type));
            timeline.PlaceModifier(modifier);
            modifier.LoadData(data);
            tracks.AddModifier(modifier);
            Modifiers.Add(modifier);
            return modifier;
        }

        public void SetStartTime(bool increase, bool move)
        {
            if (SelectedModifiers.Count == 0) return;
            Relative_QNT beatSnap = new((long)EditorBeatSnap.Duration.tick * (increase ? 1 : -1));
            Dictionary<Modifier, Timeframe> newTimes = new();
            foreach (var modifier in SelectedModifiers)
            {
                if (!modifier.SupportsEndTime) move = true;
                var currentDuration = modifier.duration;
                if ((long)currentDuration.tick - beatSnap.tick <= 0) return; //don't allow setting time if we get 0 or less duration
                var newStartTime = modifier.startTime + beatSnap;
                var end = (move ? newStartTime : modifier.startTime) + currentDuration;
                Timeframe newTimeframe = new(newStartTime, end);
                if (tracks.ContainsModifierAtTime(modifier, newTimeframe)) return;
                
                newTimes.Add(modifier, newTimeframe);
            }

            foreach (var kvp in newTimes)
            {
                kvp.Key.SetTime(kvp.Value);
            }
        }

        public void SetEndTime(bool increase, bool move)
        {
            if (SelectedModifiers.Count == 0) return;
            Relative_QNT beatSnap = new((long)EditorBeatSnap.Duration.tick * (increase ? 1 : -1));
            Dictionary<Modifier, Timeframe> newTimes = new();

            foreach (var modifier in SelectedModifiers)
            {
                if (!modifier.SupportsEndTime) move = true;
                var currentDuration = modifier.duration;
                if ((long)currentDuration.tick + beatSnap.tick <= 0 && !move) return; //don't allow setting time if we get 0 or less duration
                var newEndTime = modifier.endTime + beatSnap;
                var start = (move ? newEndTime : modifier.endTime) - currentDuration;
                Timeframe newTimeframe = new(start, newEndTime);
                if (tracks.ContainsModifierAtTime(modifier, newTimeframe)) return;
                
                newTimes.Add(modifier, newTimeframe);
            }
            
            foreach(var kvp in newTimes)
                kvp.Key.SetTime(kvp.Value);
        }

        public void MoveModifierFromAction(Modifier modifier, Timeframe timeframe) => modifier.SetTime(timeframe);

        public void TryRemoveModifier(QNT_Timestamp time, TrackContent content)
        {
            if(tracks.TryGetModifier(content.track.Type, time, out var modifier))
            {
                TryRemoveModifier(modifier);
            }
        }

        public void TryRemoveModifier(Modifier modifier) =>  ModifierUndoRedo.AddAction(new RemoveModifierAction(modifier));

        public void RemoveModifierFromAction(Modifier modifier)
        {
            tracks.RemoveModifier(modifier);
            Modifiers.Remove(modifier);
            if(CurrentModifier == modifier)
                UpdateCurrentModifier(null);

            if (SelectedModifiers.Contains(modifier))
                SelectedModifiers.Remove(modifier);

            if(SelectedModifiers.Count == 0)
                onSelectedModifierRemoved?.Invoke();
            
            Destroy(modifier.gameObject);
        }

        public void RemoveSelectedModifiers()
        {
            if (CurrentModifier == null) return;
            if (CurrentModifier.Selected)
            {
                TryRemoveModifier(CurrentModifier);
            }
        }

        public void CopySelectedModifiers()
        {
            if (SelectedModifiers.Count == 0) return;
            copiedModifiers.Clear();

            foreach (var modifier in SelectedModifiers)
            {
                copiedModifiers.Add(modifier.Data);
            }
            copiedModifiers.Sort((m1, m2) => m1.startTick.CompareTo(m2.startTick));
        }

        public void CutSelectedModifiers()
        {
            if (SelectedModifiers.Count == 0) return;
            CopySelectedModifiers();
            
            ModifierUndoRedo.AddAction(new MultiRemoveModifierAction(SelectedModifiers));
            SelectedModifiers.Clear();
        }

        public void PasteModifiers(QNT_Timestamp currentTime)
        {
            if (copiedModifiers.Count == 0) return;
            
            var delta = (int)currentTime.tick - copiedModifiers[0].startTick;
            foreach (var modifier in copiedModifiers)
            {
                modifier.startTick += delta;
                modifier.endTick += delta;
            }
            
            ModifierUndoRedo.AddAction(new MultiAddModifierAction(copiedModifiers));
        }

        public void SelectAll()
        {
            SelectedModifiers.Clear();

            foreach (var modifier in Modifiers)
            {
                modifier.SetSelected(true);
                SelectedModifiers.Add(modifier);
            }
            onMultiSelect?.Invoke();
        }

        public void DeselectAll()
        {
            foreach (var modifier in SelectedModifiers)
            {
                modifier.SetSelected(false);
            }
            
            SelectedModifiers.Clear();
            onMultiSelect?.Invoke();
        }
    }
}
