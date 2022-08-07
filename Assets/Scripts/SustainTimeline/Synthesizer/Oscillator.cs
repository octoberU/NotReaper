using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Audio.Synthesizer
{
    [RequireComponent(typeof(AudioSource))]
    public class Oscillator : MonoBehaviour
    {
        //frequency in HZ of the tone the oscillator produces
        [SerializeField] private double frequency = 440d;
        [SerializeField] private float volume = .1f;
        
        //distance the wave moves each frame - determined by frequency
        private double increment;
        
        //location on the wave (y-axis)
        private double phase;

        //frequency at which Unity's Audio Engine runs
        private double samplingFrequency = 48000d;

        private void OnAudioFilterRead(float[] data, int channels)
        {
            //how far to move on the x-axis of the waveform
            increment = frequency * 2f * Mathf.PI / samplingFrequency;

            for (int i = 0; i < data.Length; i += channels)
            {
                phase += increment;
                data[i] = volume * Mathf.Sin((float)phase);

                if (channels == 2) //copy if stereo
                {
                    data[i + 1] = data[i];
                }

                if (phase > Mathf.PI * 2f) //reset when we do a full revolution
                {
                    phase = 0d;
                }
            }
        }
    }
}
