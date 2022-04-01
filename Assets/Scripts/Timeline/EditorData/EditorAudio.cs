using NotReaper;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public static float PlaybackSpeed { get; private set; }
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

    private static PrecisePlayback playback;

    static EditorAudio()
    {
        NRSettings.OnLoad(() =>
        {
            playback = PrecisePlayback.Instance;
            SetSongVolume(NRSettings.config.mainVol);
            SetHitsoundVolume(NRSettings.config.noteVol);
            SetSustainVolume(NRSettings.config.sustainVol);
            SetUIVolume(NRSettings.config.soundEffectsVol);
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
}
