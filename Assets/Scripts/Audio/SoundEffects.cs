using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NotReaper.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEffects : Singleton<SoundEffects>
    {

        [Space, Header("Clips")]
        [SerializeField] private AudioClip click;
        [SerializeField] private AudioClip open;
        [SerializeField] private AudioClip close;
        [SerializeField] private AudioClip save;
        [SerializeField] private AudioClip notification;
        [SerializeField] private AudioClip startup;

        private AudioSource source;
        private bool isPreviewing;
        private bool hasPlayedStartupSound = false;

        protected override void Awake()
        {

            base.Awake();
            source = GetComponent<AudioSource>();
            EditorAudio.onUIVolumeChanged += SetVolume;
            
        }

        private void Start()
        {
            NRSettings.OnLoad(() => 
            {
                float vol = NRSettings.config.soundEffectsVol;
                SetVolume(vol);
            
            });
           
        }

        private void SetVolume(float volume)
        {
            if (source != null)
                source.volume = volume * .5f;
        }

        private IEnumerator DoPreviewVolume()
        {
            isPreviewing = true;
            source.clip = notification;

            source.PlayOneShot(notification);
            while (source.isPlaying)
            {
                yield return null;
            }
            isPreviewing = false;
        }

        public void PreviewVolume(float volume)
        {
            if (!hasPlayedStartupSound) return;
            
            SetVolume(volume);
            if (!isPreviewing)
            {
                StartCoroutine(DoPreviewVolume());
            }
        }

        public void PlaySound(Sound type)
        {
            AudioClip clip = null;
            switch (type)
            {
                case Sound.Click:
                    clip = click;
                    break;
                case Sound.Open:
                    clip = open;
                    break;
                case Sound.Close:
                    clip = close;
                    break;
                case Sound.Save:
                    clip = save;
                    break;
                case Sound.Notification:
                    clip = notification;
                    break;
                case Sound.Startup:
                    clip = startup;
                    hasPlayedStartupSound = true;
                    break;
                default:
                    break;
            }
            source.PlayOneShot(clip);
        }

        private void OnApplicationFocus(bool focus)
        {
            if (NRSettings.config == null) return;
            if (focus)
            {
                SetVolume(EditorAudio.UIVolume);
            }
            else
            {
                SetVolume(0f);
            }
        }

        public enum Sound
        {
            Click,
            Open,
            Close,
            Save,
            Notification,
            Startup,
        }
    }
}

