using System;
using System.Collections.Generic;
using System.Linq;
using TargetPreview.Display;
using UnityEngine;

namespace TargetPreview.Scripts.Targets
{
    public class CueManager : MonoBehaviour, IReceiveTimeUpdates
    {
        [SerializeField] TargetPool targetPool;
        [SerializeField] TargetConnectorManager targetConnectorManager;
        
        public float cueLookAheadTimeMs = 3000f;

        TargetCue[] targetCues = { };

        public TargetCue[] TargetCues
        {
            get => targetCues;
            set
            {
                targetCues = 
                    value
                        .OrderBy(x => x.timeMs)
                        .ThenBy(x => (int)x.behavior)
                        .ThenBy(x => (int)x.handType)
                        .ToArray(); //Ensure that cues are always sorted by time and behavior. This is extremely important when handling chains.
                OnTimeUpdated(TimeController.Time);
            }
        }

        public List<TargetReference> ActiveCues =>
            activeCues;

        List<TargetReference> activeCues = new List<TargetReference>(300);
        
        Dictionary<float, TargetReference> activeCueTickLookup = new (300);

        void Awake() =>
            TimeController.AddListener(this);

        void OnDestroy() =>
            TimeController.RemoveListener(this);

        public void OnTimeUpdated(float time)
        {
            //Clean up any active cues.
            for (var i = ActiveCues.Count - 1; i >= 0; i--)
            {
                if (ShouldCullCue(ActiveCues[i].cue))
                    RemoveActiveCue(ActiveCues[i]);
            }
            
            for (int i = 0; i < targetCues.Length; i++)
            {
                if(!ShouldCullCue(targetCues[i]) && !IsCueCurrentlyActive(targetCues[i]))
                {
                    AddActiveCue(targetCues[i]);
                }
            }
            
            bool WithinLookAheadTime(float cueTimeMs) =>
                Mathf.Abs(cueTimeMs - time) <= cueLookAheadTimeMs;

            bool CurrentlySustained(TargetCue cue) =>
                (time >= cue.timeMs && time <= cue.timeEndMs);

            bool ShouldCullCue(TargetCue cue) =>
                !WithinLookAheadTime(cue.timeMs) && !CurrentlySustained(cue);

            bool IsCueCurrentlyActive(TargetCue cue)
            {
                bool currentlyActive = false;
                for (var i = 0; i < ActiveCues.Count; i++)
                {
                    if (ActiveCues[i].cue == cue)
                    {
                        currentlyActive = true;
                        break;
                    }
                }

                return currentlyActive;
            }
        }

        void AddActiveCue(TargetCue cue)
        {
            var targetReference = new TargetReference()
            {
                target = targetPool.Take(cue),
                cue = cue
            };
            
            var connection = FindCueConnection(targetReference); //Handle connecting cues.
            if(connection.HasValue)
                targetConnectorManager.TryAddConnection(connection.Value.Item1, connection.Value.Item2);
            
            ActiveCues.Add(targetReference);
        }

        (TargetReference, TargetReference)? FindCueConnection(TargetReference newReference)
        {
            foreach (var targetReference in ActiveCues)
            {
                if(targetReference.cue.tick == newReference.cue.tick &&
                   targetReference.cue.behavior == newReference.cue.behavior &&
                     targetReference.cue.handType != newReference.cue.handType)
                {
                    return (targetReference, newReference);
                }
            }

            return null;
        }

        void RemoveActiveCue(TargetReference reference)
        {
            targetPool.Return(reference.target);
            ActiveCues.Remove(reference);
            
            var connection = FindCueConnection(reference);
            if(connection.HasValue)
                targetConnectorManager.RemoveConnection(connection.Value.Item1, connection.Value.Item2);
        }

        public void ClearActiveCues()
        {
            activeCues.Clear();
        }


    }
}