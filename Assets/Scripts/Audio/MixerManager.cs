using NotReaper.Notifications;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace NotReaper.Audio 
{

    public class MixerManager : MonoBehaviour
    {
        [SerializeField] AudioMixer audioMixer;
        [SerializeField] List<AudioMixerSnapshot> snapshots;
        private Dictionary<PresetType, Preset> snapshotMap;
        private int currentPreset;
        

        // Start is called before the first frame update
        void Start()
        {
            snapshotMap = new Dictionary<PresetType, Preset>();
            foreach (var preset in snapshots)
            {
                snapshotMap.Add(ParseType(preset.name), new Preset(preset,0));
            }

            NRSettings.OnLoad(() =>
           {
               currentPreset = NRSettings.config.mixerPreset;
               SetPreset();

           });
        }
        private PresetType ParseType(string name)
        {
            return (PresetType) Enum.Parse(typeof(PresetType), name);
        }
        private void ApplyPreset() 
        {
            List<AudioMixerSnapshot> snaps = new List<AudioMixerSnapshot>();
            List<float> weights = new List<float>();
            foreach(var preset in snapshotMap)
            {
                snaps.Add(preset.Value.snapshot);
                weights.Add(preset.Value.weight);
            }
            audioMixer.TransitionToSnapshots(snaps.ToArray(), weights.ToArray(), 0);
        }

        private void CyclePreset()
        {
            currentPreset++;
            currentPreset %= 3;
            NotificationCenter.SendNotification($"Changed Audio Preset To {(PresetType)currentPreset}", NotificationType.Info,false);
            SetPreset();
            NRSettings.SaveSettingsJson();
        }
        private void SetPreset()
        {
            foreach(var preset in snapshotMap)
            {
                preset.Value.weight = 0;
            }
            snapshotMap[(PresetType)currentPreset].weight = 1f;
            ApplyPreset();
        }


        internal void TogglePreset()
        {
            CyclePreset();
        }

        private class Preset 
        {
            public AudioMixerSnapshot snapshot;
            public float weight;

            public Preset(AudioMixerSnapshot snapshot, float weight)
            {
                this.snapshot = snapshot;
                this.weight = weight;
            }
        }
        private enum PresetType 
        {
            Default=0,
            ClearDrums=1,
            BassBoost=2
        }
    }
    
}
