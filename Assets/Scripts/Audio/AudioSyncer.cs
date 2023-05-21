using System;
using NotReaper.Audio.Noise;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Audio
{
    public abstract class AudioSyncer : MonoBehaviour
    {
        public float bias;
        public float timeStep;
        public float timeToBeat;
        public float restSmoothTime;
        public int band;
        public SyncMode sync;

        private float previousAudioValue;
        private float audioValue;
        private float timer;

        protected bool isBeat;

        protected delegate void OnStart();
        protected OnStart onStart;

        protected delegate void OnStop();
        protected OnStop onStop;

        private bool _subscribed;
        
        public enum SyncMode
        {
            Band8,
            Band64,
            BufferedBand8,
            BufferedBand64,
            Amplitude,
            AmplitudeBuffered
        }

        protected virtual void Start()
        {
        }

        private void OnEnable()
        {
            if (_subscribed)
                return;
            
            AudioPeer.onVisualizationStart += StartVisualization;
            AudioPeer.onVisualizationEnd += StopVisualization;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
                return;
            
            AudioPeer.onVisualizationStart -= StartVisualization;
            AudioPeer.onVisualizationEnd -= StopVisualization;
            _subscribed = false;
        }

        private void StartVisualization()
        {
            StartCoroutine(Sync());
        }

        private void StopVisualization()
        {
            StopCoroutine(Sync());
        }


        public virtual void OnBeat()
        {
            timer = 0;
            isBeat = true;
        }

        protected abstract void Visualize();

        private IEnumerator Sync()
        {
            while (true)
            {
                previousAudioValue = audioValue;
                //audioValue = AudioVisualizer.SpectrumValue;
                audioValue = sync == SyncMode.Amplitude ? AudioPeer.amplitude : sync == SyncMode.AmplitudeBuffered ? AudioPeer.amplitudeBuffer :
                    sync == SyncMode.Band8 ? AudioPeer.audioBand[band] : sync == SyncMode.BufferedBand8 ? AudioPeer.audioBandBuffer[band] : 
                    sync == SyncMode.Band64 ? AudioPeer.audioBand64[band] : AudioPeer.audioBandBuffer64[band];

                if (previousAudioValue > bias && audioValue <= bias)
                {
                    if (timer > timeStep)
                    {
                        OnBeat();
                    }
                }

                if (previousAudioValue <= bias && audioValue > bias)
                {
                    if (timer > timeStep)
                    {
                        OnBeat();
                    }
                }

                timer += Time.deltaTime;

                Visualize();

                yield return null;
            }
            
        }

    }
}

