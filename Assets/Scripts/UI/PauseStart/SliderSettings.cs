using NotReaper.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.UI.Settings
{
    public class SliderSettings : MonoBehaviour
    {
        [SerializeField] private Slider musicVolume;
        [SerializeField] private Slider hitsoundVolume;
        [SerializeField] private Slider sustainVolume;
        [SerializeField] private Slider soundEffectVolume;

        private void Awake()
        {
            EditorAudio.onSongVolumeChanged += musicVolume.SetValueWithoutNotify;
            EditorAudio.onHitsoundVolumeChanged += hitsoundVolume.SetValueWithoutNotify;
            EditorAudio.onSustainVolumeChanged += sustainVolume.SetValueWithoutNotify;
            EditorAudio.onUIVolumeChanged += soundEffectVolume.SetValueWithoutNotify;

            musicVolume.onValueChanged.AddListener(EditorAudio.SetSongVolume);
            hitsoundVolume.onValueChanged.AddListener(EditorAudio.SetHitsoundVolume);
            sustainVolume.onValueChanged.AddListener(EditorAudio.SetSustainVolume);
            soundEffectVolume.onValueChanged.AddListener(EditorAudio.SetUIVolume);
        }

        private void Start()
        {
            /*NRSettings.OnLoad(() =>
            {
                musicVolume.SetValueWithoutNotify(NRSettings.config.mainVol);
                hitsoundVolume.SetValueWithoutNotify(NRSettings.config.noteVol);
                sustainVolume.SetValueWithoutNotify(NRSettings.config.sustainVol);
                soundEffectVolume.SetValueWithoutNotify(NRSettings.config.soundEffectsVol);

            });*/
        }

        /*public void OnMusicVolumeChanged()
        {
            float vol = musicVolume.value;
            Timeline.Instance.musicVolume = vol;
            NRSettings.config.mainVol = vol;
            NRSettings.SaveSettingsJson();
        }
        public void OnHitsoundVolumeChanged()
        {
            float vol = hitsoundVolume.value;
            Timeline.Instance.hitsoundVolume = vol;
            NRSettings.config.noteVol = vol;
            NRSettings.SaveSettingsJson();
        }
        public void OnSustainVolumeChanged()
        {
            float vol = sustainVolume.value;
            Timeline.Instance.sustainVolume = vol;
            NRSettings.config.sustainVol = vol;
            NRSettings.SaveSettingsJson();
        }
        public void OnSoundEffectVolumeChanged()
        {
            float vol = soundEffectVolume.value;
            SoundEffects.Instance.PreviewVolume(vol);
            NRSettings.config.soundEffectsVol = vol;
            NRSettings.SaveSettingsJson();
        }*/
    }
}

