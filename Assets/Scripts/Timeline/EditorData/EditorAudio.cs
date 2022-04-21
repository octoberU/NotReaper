using NotReaper;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotReaper.Audio;
namespace NotReaper
{
    /// <summary>
    /// Responsible for handling audio levels and playback speed.
    /// </summary>
    public static class EditorAudio
    {
        /// <summary>
        /// The songs volume.
        /// </summary>
        public static float SongVolume { get; private set; }
        /// <summary>
        /// The volume of hitsounds.
        /// </summary>
        public static float HitsoundVolume { get; private set; }
        /// <summary>
        /// The volume of sustains.
        /// </summary>
        public static float SustainVolume { get; private set; }
        /// <summary>
        /// The volume of UI sounds.
        /// </summary>
        public static float UIVolume { get; private set; }
        /// <summary>
        /// The song's playback speed.
        /// </summary>
        public static float PlaybackSpeed { get; private set; } = 1f;
        /// <summary>
        /// The percentage we're currently at in the song.
        /// </summary>
        public static float SongPercentage => GetPercentagePlayed(EditorTime.Time);
        /// <summary>
        /// The last tick of the song.
        /// </summary>
        public static QNT_Timestamp SongEndTime => QNT_Timestamp.ShiftTick(playback.song.Length);
        /// <summary>
        /// Indicates if the song is currently playing.
        /// </summary>
        public static bool IsPlaying { get; private set; }
        /// <summary>
        /// Raised when <see cref="SongVolume"/> changes.
        /// </summary>
        public static event OnValueChanged onSongVolumeChanged;
        /// <summary>
        /// Raised when <see cref="HitsoundVolume"/> changes.
        /// </summary>
        public static event OnValueChanged onHitsoundVolumeChanged;
        /// <summary>
        /// Raised when <see cref="SustainVolume"/> changes.
        /// </summary>
        public static event OnValueChanged onSustainVolumeChanged;
        /// <summary>
        /// Raised when <see cref="UIVolume"/> changes.
        /// </summary>
        public static event OnValueChanged onUIVolumeChanged;
        /// <summary>
        /// Raised when <see cref="PlaybackSpeed"/> changes.
        /// </summary>
        public static event OnValueChanged onPlaybackSpeedChanged;
        public delegate void OnValueChanged(float volume);

        public static event OnPlaybackToggled onPlaybackToggled;
        public delegate void OnPlaybackToggled(bool isPlaying);

        private static PrecisePlayback playback;
        private static EditorAudioPlayer player;

        static EditorAudio()
        {
            OnNRStart.OnStart(() =>
            {
                playback = PrecisePlayback.Instance;
                player = NRDependencyInjector.Get<EditorAudioPlayer>();
                SetSongVolume(NRSettings.config.mainVol);
                SetHitsoundVolume(NRSettings.config.noteVol);
                SetSustainVolume(NRSettings.config.sustainVol);
                UIVolume = NRSettings.config.soundEffectsVol;
            });

            NRSettings.OnLoad(() =>
            {
                var configuration = AudioSettings.GetConfiguration();
                configuration.dspBufferSize = NRSettings.config.audioDSP;
                AudioSettings.Reset(configuration);
            });

        }

        /// <summary>
        /// Updates song volume and saves it to config.
        /// </summary>
        /// <param name="volume">The new volume.</param>
        public static void SetSongVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (SongVolume == volume)
                return;

            SongVolume = volume;

            NRSettings.config.mainVol = volume;
            NRSettings.SaveSettingsJson();
            playback.volume = volume;
            onSongVolumeChanged?.Invoke(volume);
        }
        /// <summary>
        /// Updates hitsound volume and saves it to config.
        /// </summary>
        /// <param name="volume">The new volume.</param>
        public static void SetHitsoundVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (HitsoundVolume == volume)
                return;

            HitsoundVolume = volume;

            NRSettings.config.noteVol = volume;
            NRSettings.SaveSettingsJson();
            playback.hitSoundVolume = volume;
            onHitsoundVolumeChanged?.Invoke(volume);
        }
        /// <summary>
        /// Updates sustain volume and saves it to config.
        /// </summary>
        /// <param name="volume">The new volume.</param>
        public static void SetSustainVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (SustainVolume == volume)
                return;

            SustainVolume = volume;

            NRSettings.config.sustainVol = volume;
            NRSettings.SaveSettingsJson();
            onSustainVolumeChanged?.Invoke(volume);
        }
        /// <summary>
        /// Updates UI volume and saves it to config.
        /// </summary>
        /// <param name="volume">The new volume.</param>
        public static void SetUIVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (UIVolume == volume)
                return;

            UIVolume = volume;

            NRSettings.config.soundEffectsVol = volume;
            NRSettings.SaveSettingsJson();
            onUIVolumeChanged?.Invoke(volume);
        }

        public static void SetPlaybackSpeed(float speed)
        {
            if (!EditorFile.IsAudioLoaded)
                return;

            speed = Mathf.Clamp01(speed);

            if (PlaybackSpeed == speed)
                return;

            PlaybackSpeed = speed;
            playback.speed = speed;
            onPlaybackSpeedChanged?.Invoke(speed);
        }
        public static void SetPlaybackSpeedUnclamped(float speed)
        {
            if (!EditorFile.IsAudioLoaded)
                return;

            if (PlaybackSpeed == speed)
                return;

            PlaybackSpeed = speed;
            playback.speed = speed;
            onPlaybackSpeedChanged?.Invoke(speed);
        }
        /// <summary>
        /// Plays a hitsound at the specified time.
        /// </summary>
        /// <param name="time">The time to play the hitsound at.</param>
        public static void PlayHitsound(QNT_Timestamp time) => playback.PlayHitsound(time);
        /// <summary>
        /// Toggles playback of the loaded audio.
        /// </summary>
        /// <param name="metronome">True if the metronome should be started.</param>
        public static void TogglePlay(bool metronome = false)
        {
            IsPlaying = !IsPlaying;
            player.TogglePlay(metronome);
            onPlaybackToggled?.Invoke(IsPlaying);
        }
        /// <summary>
        /// Smoothly jumps the audio to the specified time.
        /// </summary>
        /// <param name="beat">The time to jump to.</param>
        public static void JumpToBeat(float beat) => player.JumpToBeat(beat);
        /// <summary>
        /// Smoothly jumps the audio to the specified time.
        /// </summary>
        /// <param name="time">The time to jump to.</param>
        public static void JumpToTime(QNT_Timestamp time) => player.JumpToTime(time);
        /// <summary>
        /// Jumps the audio to the specified percentage immediately.
        /// </summary>
        /// <param name="percent">The percent to jump to.</param>
        public static void JumpToPercent(float percent) => player.JumpToPercent(percent);
        /// <summary>
        /// Jumps the audio to the specified percentage. Use this if you want to jump while in UI or using a tool.
        /// </summary>
        /// <param name="percent">The percent to jump to.</param>
        public static void ForceJumpToPercent(float percent) => player.JumpToPercent(percent, true);
        /// <summary>
        /// Scrubs the timeline.
        /// </summary>
        /// <param name="forward">True for scrubbing forward, false for backwards.</param>
        /// <param name="byTick">True for scrubbing by tick, false for scrubbing by <see cref="EditorBeatSnap.BeatSnap"/></param>
        public static void ScrubTimeline(bool forward, bool byTick) => player.Scrub(forward, byTick);
        /// <summary>
        /// Applies an offset to audio playback. <see cref="EditorTime.Time"/> is not affected by this offset, only audio playback.
        /// </summary>
        /// <param name="offset">The offset to apply.</param>
        /// <remarks>To properly offset audio, you also need to call <see cref="PrecisePlayback.OffsetPlaybackTime(QNT_Timestamp, QNT_Duration)"/></remarks>
        public static void SetOffset(Relative_QNT offset) => player.SetOffset(offset);
        /// <summary>
        /// Get the percentage at which the supplied time is in the song.
        /// </summary>
        /// <param name="time">The time to get the percentage for.</param>
        /// <returns>The percentage of the song at time.</returns>
        public static float GetPercentagePlayed(QNT_Timestamp time) => GetPercentagePlayed(time.ToSeconds());
        /// <summary>
        /// Get the percentage at which the supplied time is in the song.
        /// </summary>
        /// <param name="time">The time to get the percentage for.</param>
        /// <returns>The percentage of the song at time.</returns>
        public static float GetPercentagePlayed(float seconds) => playback == null || playback.song == null ? 0f : (seconds / playback.song.Length);
    }
}
