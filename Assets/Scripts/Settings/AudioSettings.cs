using System;
using UnityEngine;

/// <summary>
/// Scene-independent data manager for game audio preferences.
/// Encapsulates volume state (Master, Music, SFX), defaults, PlayerPrefs persistence, and change notifications.
/// Interacted with by <see cref="AudioSettingsTab"/> for UI and <see cref="AudioManager"/> for AudioMixer synchronization.
/// </summary>
public class AudioSettings
{
    #region Singleton

    private static AudioSettings _instance;

    /// <summary>
    /// Gets the singleton instance, auto-initializing on first access.
    /// </summary>
    public static AudioSettings Instance
    {
        get
        {
            if (_instance == null)
                Initialize();
            return _instance;
        }
    }

    /// <summary>
    /// Creates the singleton instance and loads audio preferences from <see cref="PlayerPrefs"/>.
    /// </summary>
    public static void Initialize()
    {
        if (_instance == null)
        {
            _instance = new AudioSettings();
            _instance.Load();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event invoked whenever audio volume settings are changed.
    /// Listened to by <see cref="AudioManager"/> to synchronize the <see cref="UnityEngine.Audio.AudioMixer"/>.
    /// </summary>
    public static event Action OnAudioChanged;

    #endregion

    #region Constants & Defaults

    public const float DEFAULT_MASTER = 1f;
    public const float DEFAULT_MUSIC = 0.25f;
    public const float DEFAULT_SFX = 1f;

    private const string KEY_MASTER = "AudioMaster";
    private const string KEY_MUSIC = "AudioMusica";
    private const string KEY_SFX = "AudioSons";

    #endregion

    #region Properties

    public float VolumeMaster { get; set; }
    public float VolumeMusic { get; set; }
    public float VolumeSFX { get; set; }

    #endregion

    #region Load & Save

    /// <summary>
    /// Loads stored volume preferences from <see cref="PlayerPrefs"/>.
    /// </summary>
    public void Load()
    {
        VolumeMaster = PlayerPrefs.GetFloat(KEY_MASTER, DEFAULT_MASTER);
        VolumeMusic = PlayerPrefs.GetFloat(KEY_MUSIC, DEFAULT_MUSIC);
        VolumeSFX = PlayerPrefs.GetFloat(KEY_SFX, DEFAULT_SFX);
    }

    /// <summary>
    /// Saves current volume values to <see cref="PlayerPrefs"/> and notifies listeners via <see cref="OnAudioChanged"/>.
    /// </summary>
    public void SaveAndNotify()
    {
        PlayerPrefs.SetFloat(KEY_MASTER, VolumeMaster);
        PlayerPrefs.SetFloat(KEY_MUSIC, VolumeMusic);
        PlayerPrefs.SetFloat(KEY_SFX, VolumeSFX);
        PlayerPrefs.Save();

        OnAudioChanged?.Invoke();
    }

    /// <summary>
    /// Restores all volume levels to default constants, saves to <see cref="PlayerPrefs"/>, and notifies listeners.
    /// </summary>
    public void RestoreDefaults()
    {
        VolumeMaster = DEFAULT_MASTER;
        VolumeMusic = DEFAULT_MUSIC;
        VolumeSFX = DEFAULT_SFX;

        SaveAndNotify();
    }

    #endregion
}

