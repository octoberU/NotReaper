using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Audio.Noise
{
    
public class AudioPeer : MonoBehaviour    
{
        private float[] samplesLeft = new float[512];
        private float[] samplesRight = new float[512];

        private float[] freqBand = new float[8];
        private float[] bandBuffer = new float[8];
        private float[] bufferDecrease = new float[8];
        private float[] freqBandHighest = new float[8];
        //audio 64 bands
        private float[] freqBand64 = new float[64];
        private float[] bandBuffer64 = new float[64];
        private float[] bufferDecrease64 = new float[64];
        private float[] freqBandHighest64 = new float[64];

        public static float[] audioBand;
        public static float[] audioBandBuffer;

        public static float[] audioBand64;
        public static float[] audioBandBuffer64;

        public static float amplitude;
        public static float amplitudeBuffer;

        private float amplitudeHighest;

        public float audioProfile;
        public Channel channel = Channel.Stereo;

        public delegate void OnVisualizationStart();
        public static event OnVisualizationStart onVisualizationStart;

        public delegate void OnVisualizationEnd();
        public static event OnVisualizationEnd onVisualizationEnd;

        private AudioSource source;

        private float previousVolume;
        private bool volumeChanged = false;
        private bool isVisualizing = false;

        public enum Channel
        {
            Stereo,
            Left,
            Right
        }

        private void Start()
        {
            audioBand = new float[8];
            audioBandBuffer = new float[8];
            audioBand64 = new float[64];
            audioBandBuffer64 = new float[64];
            source = NRDependencyInjector.Get<PrecisePlayback>().GetSource();
            AudioProfile(audioProfile);
        }

        public void StartVisualization()
        {
            if (!NRSettings.config.enableAudioVisualization) return;
            if (isVisualizing) return;
            isVisualizing = true;
            StartCoroutine(UpdateSpectrum());
            onVisualizationStart?.Invoke();
        }

        public void StopVisualization()
        {
            if (!NRSettings.config.enableAudioVisualization) return;
            onVisualizationEnd?.Invoke();
            StopAllCoroutines();
            isVisualizing = false;
        }

        private IEnumerator UpdateSpectrum()
        {
            if (!NRSettings.config.enableAudioVisualization) yield break;
            while (true)
            {
                GetSpectrum();
                OnVolumeChanged();
                MakeFrequencyBands();
                //MakeFrequencyBands64();
                BandBuffer();
                //BandBuffer64();
                CreateAudioBands();
                //CreateAudioBands64();
                GetAmplitude();
                yield return null;
            }
        }

        private void GetSpectrum()
        {
            source.GetSpectrumData(samplesLeft, 0, FFTWindow.Blackman);
            source.GetSpectrumData(samplesRight, 1, FFTWindow.Blackman);
            if (source.volume != previousVolume) volumeChanged = true;
            previousVolume = source.volume;
        }

        private void OnVolumeChanged()
        {
            if (volumeChanged)
            {
                volumeChanged = false;
                AudioProfile(0);
                AudioProfile64(0);
            }
        }

        private void AudioProfile(float audioProfile)
        {
            for(int i = 0; i < 8; i++)
            {
                freqBandHighest[i] = audioProfile;
            }
        }

        private void AudioProfile64(float audioProfile)
        {
            for (int i = 0; i < 64; i++)
            {
                freqBandHighest64[i] = audioProfile;
            }
        }

        private void GetAmplitude()
        {
            float currentAmplitude = 0;
            float currentAmplitudeBuffer = 0;
            for(int i = 0; i < 8; i++)
            {
                currentAmplitude += audioBand[i];
                currentAmplitudeBuffer += audioBandBuffer[i];
            }
            if(currentAmplitude > amplitudeHighest)
            {
                amplitudeHighest = currentAmplitude;
            }
            amplitude = currentAmplitude / amplitudeHighest;
            amplitudeBuffer = currentAmplitudeBuffer / amplitudeHighest;
        }

        private void CreateAudioBands()
        {
            for(int i = 0; i < 8; i++)
            {
                if(freqBand[i] > freqBandHighest[i])
                {
                    freqBandHighest[i] = freqBand[i];
                }
                audioBand[i] = freqBand[i] / freqBandHighest[i];
                audioBandBuffer[i] = bandBuffer[i] / freqBandHighest[i];
            }
        }

        private void CreateAudioBands64()
        {
            for (int i = 0; i < 64; i++)
            {
                if (freqBand64[i] > freqBandHighest64[i])
                {
                    freqBandHighest64[i] = freqBand64[i];
                }
                audioBand64[i] = freqBand64[i] / freqBandHighest64[i];
                audioBandBuffer64[i] = bandBuffer64[i] / freqBandHighest64[i];
            }
        }

        private void MakeFrequencyBands()
        {
            int count = 0;

            for(int i = 0; i < 8; i++)
            {
                float average = 0;
                int sampleCount = (int)Mathf.Pow(2, i) * 2;
                if(i == 7)
                {
                    sampleCount += 2;
                }
                for(int j = 0; j < sampleCount; j++)
                {
                    if(channel == Channel.Stereo)
                    {
                        average += samplesLeft[count] + samplesRight[count] * (count + 1);
                    }
                    else if (channel == Channel.Left)
                    {
                        average += samplesLeft[count] * (count + 1);
                    }
                    else if(channel == Channel.Right)
                    {
                        average += samplesRight[count] * (count + 1);
                    }
                    count++;
                }

                average /= count;
                freqBand[i] = average * 10f;

            }
        }

        private void MakeFrequencyBands64()
        {
            int count = 0;
            int sampleCount = 1;
            int power = 0;

            for (int i = 0; i < 64; i++)
            {
                float average = 0;
                if (i == 16 || i == 32 || i == 40 || i == 48 || i == 56)
                {
                    power++;
                    sampleCount = (int)Mathf.Pow(2, power);
                    if(power == 3)
                    {
                        sampleCount -= 2;
                    }
                }
                for (int j = 0; j < sampleCount; j++)
                {
                    if (channel == Channel.Stereo)
                    {
                        average += samplesLeft[count] + samplesRight[count] * (count + 1);
                    }
                    else if (channel == Channel.Left)
                    {
                        average += samplesLeft[count] * (count + 1);
                    }
                    else if (channel == Channel.Right)
                    {
                        average += samplesRight[count] * (count + 1);
                    }
                    count++;
                }

                average /= count;
                freqBand64[i] = average * 80f;

            }
        }

        private void BandBuffer()
        {
            for(int i = 0; i < 8; i++)
            {
                if(freqBand[i] > bandBuffer[i])
                {
                    bandBuffer[i] = freqBand[i];
                    bufferDecrease[i] = .005f;
                }
                else if(freqBand[i] < bandBuffer[i])
                {
                    bandBuffer[i] -= bufferDecrease[i];
                    bufferDecrease[i] *= 1.2f;
                }
            }
        }

        private void BandBuffer64()
        {
            for (int i = 0; i < 64; i++)
            {
                if (freqBand64[i] > bandBuffer64[i])
                {
                    bandBuffer64[i] = freqBand64[i];
                    bufferDecrease64[i] = .005f;
                }
                else if (freqBand64[i] < bandBuffer64[i])
                {
                    bandBuffer64[i] -= bufferDecrease64[i];
                    bufferDecrease64[i] *= 1.2f;
                }
            }
        }    
}
}
