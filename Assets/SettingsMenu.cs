using System;
using NotReaper.Audio;
using NotReaper.UI;
using NotReaper.UI.Components;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using NotReaper.Notifications;
using UnityEngine.Events;

namespace NotReaper
{
    public class SettingsMenu : MonoBehaviour
    {

        [SerializeField] NRToggle richPresence;
        [SerializeField] NRToggle vsync;
        [SerializeField] NRToggle clearCacheOnStartup;
        [SerializeField] NRToggle enableTraceLines;
        [SerializeField] NRToggle enableDualines;
        [SerializeField] NRToggle useAutoZOffsetWith360;
        [SerializeField] NRToggle useBouncyAnimations;
        [SerializeField] NRToggle playNoteSoundsWhileScrolling;
        [SerializeField] NRToggle autoSongVolume;
        [SerializeField] NRToggle playEndEvent;
        [SerializeField] NRToggle autoSave;
        [SerializeField] NRToggle gridParticles;
        [SerializeField] NRToggle sustainAnimation;
        [SerializeField] NRToggle audioVisualization;
        [SerializeField] NRToggle gridHitsoundIcons;
        [SerializeField] NRToggle nrCursor;
        [SerializeField] NRToggle allowStacks;
        [SerializeField] NRDropdown cycleBehavior;
        [SerializeField] NRIconInputField savedMapperField;
        [SerializeField] NRIconInputField maudicaAccountToken;

        [SerializeField] ColorSlider LeftHand;
        [SerializeField] ColorSlider RightHand;

        [SerializeField] GameObject WarningText;

        [SerializeField] Slider slider;

        [NRInject] private NewPauseMenu pauseMenu;

        private delegate void LoaderDelegate();
        private List<LoaderDelegate> loaders = new();

        public bool IsDirty { get; private set; }

        private void Awake()
        {
            EditorAudio.onUIVolumeChanged += OnVolumeChanged;
        }

        private void Start()
        {
            RegisterListeners();
            NRSettings.OnLoad(UpdateUI);
        }

        private void RegisterListeners()
        {
            Register(richPresence, c => c.useDiscordRichPresence);
            Register(vsync, c => c.vsync);
            Register(clearCacheOnStartup, c => c.clearCacheOnStartup);
            Register(enableTraceLines, c => c.enableTraceLines);
            Register(enableDualines, c => c.enableDualines);
            Register(useAutoZOffsetWith360, c => c.useAutoZOffsetWith360);
            Register(useBouncyAnimations, c => c.useBouncyAnimations);
            Register(playNoteSoundsWhileScrolling, c => c.playNoteSoundsWhileScrolling);
            Register(autoSongVolume, c => c.autoSongVolume);
            Register(playEndEvent, c => c.playEndEvent);
            Register(autoSave, c => c.backups);
            Register(gridParticles, c => c.enableGridParticles);
            Register(sustainAnimation, c => c.enableSustainAnimation);
            Register(audioVisualization, c => c.enableAudioVisualization);
            Register(gridHitsoundIcons, c => c.enableGridHitsoundIcons);
            Register(nrCursor, c => c.useNRCursor);
            Register(allowStacks, c => c.allowStackedNotes);
            Register(LeftHand, c => c.leftColor);
            Register(RightHand, c => c.rightColor);
            Register(cycleBehavior, c => c.cycleMode);
            Register(slider, c => c.soundEffectsVol, EditorAudio.SetUIVolume);
            Register(savedMapperField, c => c.savedMapperName);
            Register(maudicaAccountToken, c => c.maudicaToken);
        }

        /// <summary>
        /// Registers a toggle for saving/loading
        /// </summary>
        /// <param name="toggle"></param>
        /// <param name="selector"></param>
        private void Register(NRToggle toggle, Expression<Func<NRJsonSettings, bool>> selector, Action<bool> onLoadAction = null)
            => Register(toggle.onSelected, selector, onLoadAction ?? (selected => toggle.selected = selected));
        
        /// <summary>
        /// Registers a ColorSlider for saving/loading
        /// </summary>
        /// <param name="slider"></param>
        /// <param name="selector"></param>
        /// <param name="onLoadAction"></param>
        private void Register(ColorSlider slider, Expression<Func<NRJsonSettings, Color>> selector, Action<Color> onLoadAction = null)
            => Register(slider.onColorsSet, selector, onLoadAction ?? (color => slider.SetColor(color, true)));

        /// <summary>
        /// Registers a Slider for saving/loading
        /// </summary>
        /// <param name="slider"></param>
        /// <param name="selector"></param>
        /// <param name="onLoadAction"></param>
        private void Register(Slider slider, Expression<Func<NRJsonSettings, float>> selector, Action<float> onLoadAction = null)
            => Register(slider.onValueChanged, selector, onLoadAction ?? slider.SetValueWithoutNotify);

        /// <summary>
        /// Registers a Dropdown for saving/loading
        /// </summary>
        /// <param name="dropdown"></param>
        /// <param name="selector"></param>
        /// <param name="onloadAction"></param>
        private void Register(NRDropdown dropdown, Expression<Func<NRJsonSettings, int>> selector, Action<int> onloadAction = null)
            => Register(dropdown.onValueChanged, selector, onloadAction ?? dropdown.SetValueWithoutNotify);
        
        /// <summary>
        /// Registers an InputField for saving/loading
        /// </summary>
        /// <param name="inputField"></param>
        /// <param name="selector"></param>
        /// <param name="onloadAction"></param>
        private void Register(NRIconInputField inputField, Expression<Func<NRJsonSettings, string>> selector, Action<string> onloadAction = null)
            => Register(inputField.onValueChanged, selector, onloadAction ?? (text => inputField.text = text));

        /// <summary>
        /// Sets up saving/loading of components
        /// </summary>
        /// <param name="setCallback"></param>
        /// <param name="configSelector"></param>
        /// <param name="onLoadAction"></param>
        /// <typeparam name="TValue"></typeparam>
        private void Register<TValue>(UnityEvent<TValue> setCallback, Expression<Func<NRJsonSettings, TValue>> configSelector, Action<TValue> onLoadAction)
        {
            if (configSelector.Body is not MemberExpression configMemberExpr) //selector has to access a field
            {
                LogFailure();
                return;
            }
            
            if (configMemberExpr.Member is not FieldInfo configField) //the accessed member has to be a field
            {
               LogFailure();
               return;
            }
            
            setCallback.AddListener(val =>
            {
                configField.SetValue(NRSettings.config, val); //set value via reflection
                IsDirty = true;
            });

            loaders.Add(LoadDelegate);

            void LoadDelegate()
                => onLoadAction?.Invoke(configSelector.Compile().Invoke(NRSettings.config));
            
            void LogFailure()
                => Debug.LogError($"Something went wrong here, couldn't register config");
        }

        private void OnVolumeChanged(float volume)
            => SoundEffects.Instance.PreviewVolume(volume);

        public void UpdateUI()
        {
            foreach(var loader in loaders)
                loader.Invoke();
            
            /*slider.SetValueWithoutNotify(NRSettings.config.soundEffectsVol);
            richPresence.selected = NRSettings.config.useDiscordRichPresence;
            vsync.selected = NRSettings.config.vsync;
            clearCacheOnStartup.selected = NRSettings.config.clearCacheOnStartup;
            enableTraceLines.selected = NRSettings.config.enableTraceLines;
            enableDualines.selected = NRSettings.config.enableDualines;
            useAutoZOffsetWith360.selected = NRSettings.config.useAutoZOffsetWith360;
            useBouncyAnimations.selected = NRSettings.config.useBouncyAnimations;
            playNoteSoundsWhileScrolling.selected = NRSettings.config.playNoteSoundsWhileScrolling;
            autoSave.selected = NRSettings.config.backups;
            autoSongVolume.selected = NRSettings.config.autoSongVolume;
            playEndEvent.selected = NRSettings.config.playEndEvent;
            LeftHand.SetColor(NRSettings.config.leftColor);
            RightHand.SetColor(NRSettings.config.rightColor);
            savedMapperField.text = NRSettings.config.savedMapperName;
            maudicaAccountToken.text = NRSettings.config.maudicaToken;
            gridParticles.selected = NRSettings.config.enableGridParticles;
            sustainAnimation.selected = NRSettings.config.enableSustainAnimation;
            cycleBehavior.SetValueWithoutNotify(NRSettings.config.cycleMode);
            audioVisualization.selected = NRSettings.config.enableAudioVisualization;
            gridHitsoundIcons.selected = NRSettings.config.enableGridHitsoundIcons;
            nrCursor.selected = NRSettings.config.useNRCursor;
            allowStacks.selected = NRSettings.config.allowStackedNotes;*/
        }

        public void ApplyValues()
        {
            IsDirty = false;
            NotificationCenter.SendNotification("Config saved.", NotificationType.Success);
            NRSettings.SaveSettingsJson();
            ThemeableManager.UpdateColors();
            /*NRSettings.config.soundEffectsVol = slider.value;
            NRSettings.config.useDiscordRichPresence = richPresence.selected;
            NRSettings.config.vsync = vsync.selected;
            NRSettings.config.clearCacheOnStartup = clearCacheOnStartup.selected;
            NRSettings.config.enableTraceLines = enableTraceLines.selected;
            NRSettings.config.enableDualines = enableDualines.selected;
            NRSettings.config.useAutoZOffsetWith360 = useAutoZOffsetWith360.selected;
            NRSettings.config.useBouncyAnimations = useBouncyAnimations.selected;
            NRSettings.config.playNoteSoundsWhileScrolling = playNoteSoundsWhileScrolling.selected;
            NRSettings.config.autoSongVolume = autoSongVolume.selected;
            NRSettings.config.playEndEvent = playEndEvent.selected;
            NRSettings.config.leftColor = LeftHand.color;
            NRSettings.config.rightColor = RightHand.color;
            NRSettings.config.backups = autoSave.selected;
            NRSettings.config.enableGridParticles = gridParticles.selected;
            NRSettings.config.enableSustainAnimation = sustainAnimation.selected;
            NRSettings.config.cycleMode = cycleBehavior.value;
            NRSettings.config.enableAudioVisualization = audioVisualization.selected;
            NRSettings.config.enableGridHitsoundIcons = gridHitsoundIcons.selected;
            NRSettings.config.useNRCursor = nrCursor.selected;
            NRSettings.config.allowStackedNotes = allowStacks.selected;
            ApplyInputFieldValues();
            NotificationCenter.SendNotification("Config saved.", NotificationType.Success);
            NRSettings.SaveSettingsJson();
            ThemeableManager.UpdateColors();*/
        }

        public void ApplyInputFieldValues()
        {
            /*NRSettings.config.savedMapperName = savedMapperField.text;
            NRSettings.config.maudicaToken = maudicaAccountToken.text;*/
        }

        public void ResetColors()
        {
            NRSettings.config.leftColor = new Color(0.44f, 0.78f, 1.0f, 1.0f);
            NRSettings.config.rightColor = new Color(1.0f, 0.63f, 0.45f, 1.0f);
            LeftHand.SetColor(NRSettings.config.leftColor);
            RightHand.SetColor(NRSettings.config.rightColor);
            WarningText.SetActive(true);
            ApplyValues();
        }
    }
}
