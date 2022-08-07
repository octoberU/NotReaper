using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Timing;
using UnityEngine;

namespace NotReaper.SustainTimeline
{
    [RequireComponent(typeof(AudioSource))]
    public class SustainPlayback : MonoBehaviour
    {
        [SerializeField] private SustainPreset preset;
        
        [NRInject] private static SustainTimelineManager timeline;

        private static Dictionary<Pitch, AudioClip> pitches = new();

        private static AudioSource source;
        
        private static AudioClip sustainClip = null;

        private void Start()
        {
            source = GetComponent<AudioSource>();
            
            pitches.Add(Pitch.C, preset.c);
            pitches.Add(Pitch.CSharp, preset.cSharp);
            pitches.Add(Pitch.D, preset.d);
            pitches.Add(Pitch.DSharp, preset.dSharp);
            pitches.Add(Pitch.E, preset.e);
            pitches.Add(Pitch.F, preset.f);
            pitches.Add(Pitch.FSharp, preset.fSharp);
            pitches.Add(Pitch.G, preset.g);
            pitches.Add(Pitch.GSharp, preset.gSharp);
            pitches.Add(Pitch.A, preset.a);
            pitches.Add(Pitch.ASharp, preset.aSharp);
            pitches.Add(Pitch.B, preset.b);
            
            source.loop = true;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            Debug.Log("Data length: " + data.Length);
        }

        public static void PlaySustain(QNT_Timestamp time)
        {
            CreateClip();
            bool found = false;
            foreach (var content in timeline.Content)
            {
                if (content.timeframe.Contains(time))
                {
                    source.clip = pitches[((SustainMarker)content).Data.type];
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                source.Stop();
                return;
            }
            
            if(!source.isPlaying)
                source.Play();
        }

        private static void CreateClip()
        {
            
        }

        public static void StopSustain()
        {
            source.Stop();
        }
    }
}
