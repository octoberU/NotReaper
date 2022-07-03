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
    }
}

