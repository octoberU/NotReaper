using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;
using NotReaper.Audio;
using NotReaper.MapPreview;

namespace NotReaper.UI.Volume
{
    public class VolumeOverlay : NRMenu
    {
        [Header("Slider")]
        [SerializeField] private Slider musicVolume;
        [SerializeField] private Slider hitsoundVolume;
        [SerializeField] private Slider sustainVolume;
        [SerializeField] private Slider effectsVolume;

        [NRInject] private Timeline timeline;
        [NRInject] private SoundEffects sounds;
        private CanvasGroup volumeButton;
        private CanvasGroup previewVolumeButton;
        private CanvasGroup canvas;
        private RectTransform rect;
        private Vector2 startSize;

        protected override void Awake()
        {
            base.Awake();
            EditorAudio.onSongVolumeChanged += musicVolume.SetValueWithoutNotify;
            EditorAudio.onHitsoundVolumeChanged += hitsoundVolume.SetValueWithoutNotify;
            EditorAudio.onSustainVolumeChanged += sustainVolume.SetValueWithoutNotify;
            EditorAudio.onUIVolumeChanged += effectsVolume.SetValueWithoutNotify;

            musicVolume.onValueChanged.AddListener(EditorAudio.SetSongVolume);
            hitsoundVolume.onValueChanged.AddListener(EditorAudio.SetHitsoundVolume);
            sustainVolume.onValueChanged.AddListener(EditorAudio.SetSustainVolume);
            effectsVolume.onValueChanged.AddListener(EditorAudio.SetUIVolume);
        }

        private void Start()
        {
            canvas = GetComponent<CanvasGroup>();
            rect = GetComponent<RectTransform>();
            volumeButton = NRDependencyInjector.Get<UIModeSelect>().volumeButton;
            previewVolumeButton = NRDependencyInjector.Get<Preview3DManager>().volumeButton;
            startSize = rect.sizeDelta;

            var size = startSize;
            size.x = 0f;
            rect.sizeDelta = size;
            canvas.alpha = 0f;

            NRSettings.OnLoad(() =>
            {
                musicVolume.SetValueWithoutNotify(NRSettings.config.mainVol);
                hitsoundVolume.SetValueWithoutNotify(NRSettings.config.noteVol);
                sustainVolume.SetValueWithoutNotify(NRSettings.config.sustainVol);
                effectsVolume.SetValueWithoutNotify(NRSettings.config.soundEffectsVol);
                gameObject.SetActive(false);
            });
        }

       /* public void OnMusicVolumeChanged()
        {
            float vol = musicVolume.value;
            NRSettings.config.mainVol = vol;
            timeline.musicVolume = vol;
        }

        public void OnHitsoundVolumeChanged()
        {
            float vol = hitsoundVolume.value;
            NRSettings.config.noteVol = vol;
            timeline.hitsoundVolume = vol;
        }
        public void OnSustainVolumeChanged()
        {
            float vol = sustainVolume.value;
            NRSettings.config.sustainVol = vol;
            timeline.sustainVolume = vol;
        }

        public void OnSoundEffectsVolumeChanged()
        {
            float vol = effectsVolume.value;
            NRSettings.config.soundEffectsVol = vol;
            SoundEffects.Instance.SetVolume(vol);
        }*/

        public override void Show()
        {
            OnActivated();
            sounds.PlaySound(SoundEffects.Sound.Open);
            volumeButton.DOFade(0f, .3f);
            previewVolumeButton.DOFade(0f, .3f);
            var sequence = DOTween.Sequence();
            sequence.Append(canvas.DOFade(1f, .3f));
            sequence.Join(rect.DOSizeDelta(startSize, .3f));
            sequence.SetEase(Ease.OutQuart);
        }

        public override void Hide()
        {
            NRSettings.SaveSettingsJson();
            var size = startSize;
            size.x = 0f;
            volumeButton.DOFade(1f, .3f);
            previewVolumeButton.DOFade(1f, .3f);
            sounds.PlaySound(SoundEffects.Sound.Close);
            var sequence = DOTween.Sequence();
            sequence.Append(canvas.DOFade(0f, .3f));
            sequence.Join(rect.DOSizeDelta(size, .3f));
            sequence.SetEase(Ease.InQuart);
            sequence.OnComplete(() =>
            {
                OnDeactivated();
            });
        }

        public override void ShowHelp() { }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }
    }
}

