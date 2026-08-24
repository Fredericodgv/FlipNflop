using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Manages runtime audio playback and applies volume settings from <see cref="GameSettings"/> to the Unity <see cref="AudioMixer"/>.
/// Acts as a bridge between the scene-independent <see cref="GameSettings"/> data store and the scene-bound <see cref="AudioMixer"/>/<see cref="AudioSource"/>.
/// Persists as a singleton MonoBehaviour via <see cref="Object.DontDestroyOnLoad(Object)"/>.
/// Interacts with <see cref="GameSettings"/> for volume data, <see cref="AudioMixer"/> for runtime mixing, <see cref="AudioSource"/> for music playback.
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region Inspector Fields

    /// <summary>
    /// Reference to the main <see cref="AudioMixer"/> controlling audio groups (Master, Music, SFX).
    /// </summary>
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    /// <summary>
    /// Background music <see cref="AudioSource"/> routed to the Music audio group in the <see cref="AudioMixer"/>.
    /// </summary>
    [Header("Background Music")]
    [SerializeField] private AudioSource musicSource;

    #endregion

    #region Constants & Fields

    /// <summary>
    /// Audio Mixer parameter name for the Master volume control.
    /// </summary>
    private const string PARAM_MASTER = "VolumeMaster";

    /// <summary>
    /// Audio Mixer parameter name for the Music volume control.
    /// </summary>
    private const string PARAM_MUSIC = "VolumeMusica";

    /// <summary>
    /// Audio Mixer parameter name for the SFX volume control.
    /// </summary>
    private const string PARAM_SFX = "VolumeSons";

    /// <summary>
    /// Default Master volume setting (100% / 1.0). Delegates to <see cref="GameSettings.DEFAULT_MASTER"/>.
    /// </summary>
    public const float DEFAULT_MASTER = GameSettings.DEFAULT_MASTER;

    /// <summary>
    /// Default SFX volume setting (100% / 1.0). Delegates to <see cref="GameSettings.DEFAULT_SFX"/>.
    /// </summary>
    public const float DEFAULT_SFX = GameSettings.DEFAULT_SFX;

    /// <summary>
    /// Default Music volume setting (25% / 0.25). Delegates to <see cref="GameSettings.DEFAULT_MUSIC"/>.
    /// </summary>
    public const float DEFAULT_MUSIC = GameSettings.DEFAULT_MUSIC;

    /// <summary>
    /// Singleton instance for global access to <see cref="AudioManager"/>.
    /// </summary>
    public static AudioManager Instance { get; private set; }

    #endregion

    #region Initialization

    /// <summary>
    /// Configures the singleton instance and enforces persistence across scene loads using <see cref="Object.DontDestroyOnLoad(Object)"/>.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Loads saved volume preferences from <see cref="GameSettings"/> via <see cref="LoadVolumes"/> and starts background music playback if assigned.
    /// </summary>
    private void Start()
    {
        LoadVolumes();

        if (musicSource != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    #endregion

    #region Volume Control

    /// <summary>
    /// Sets the Master volume, updates the <see cref="AudioMixer"/>, and saves the value to <see cref="GameSettings"/>.
    /// </summary>
    /// <param name="value">Linear volume scale between 0.0001 and 1.0.</param>
    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat(PARAM_MASTER, LinearToDecibel(value));
        GameSettings.Instance.VolumeMaster = value;
        PlayerPrefs.SetFloat("AudioMaster", value);
    }

    /// <summary>
    /// Sets the Music volume, updates the <see cref="AudioMixer"/>, and saves the value to <see cref="GameSettings"/>.
    /// </summary>
    /// <param name="value">Linear volume scale between 0.0001 and 1.0.</param>
    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat(PARAM_MUSIC, LinearToDecibel(value));
        GameSettings.Instance.VolumeMusic = value;
        PlayerPrefs.SetFloat("AudioMusica", value);
    }

    /// <summary>
    /// Sets the SFX volume, updates the <see cref="AudioMixer"/>, and saves the value to <see cref="GameSettings"/>.
    /// </summary>
    /// <param name="value">Linear volume scale between 0.0001 and 1.0.</param>
    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat(PARAM_SFX, LinearToDecibel(value));
        GameSettings.Instance.VolumeSFX = value;
        PlayerPrefs.SetFloat("AudioSons", value);
    }

    /// <summary>
    /// Retrieves the stored Master volume setting from <see cref="GameSettings"/>.
    /// </summary>
    /// <returns>Stored Master volume float value.</returns>
    public float GetMasterVolume() => GameSettings.Instance.VolumeMaster;

    /// <summary>
    /// Retrieves the stored Music volume setting from <see cref="GameSettings"/>.
    /// </summary>
    /// <returns>Stored Music volume float value.</returns>
    public float GetMusicVolume() => GameSettings.Instance.VolumeMusic;

    /// <summary>
    /// Retrieves the stored SFX volume setting from <see cref="GameSettings"/>.
    /// </summary>
    /// <returns>Stored SFX volume float value.</returns>
    public float GetSFXVolume() => GameSettings.Instance.VolumeSFX;

    #endregion

    #region Save / Load / Reset

    /// <summary>
    /// Loads all volume preferences from <see cref="GameSettings"/> and applies them to the <see cref="AudioMixer"/>.
    /// </summary>
    public void LoadVolumes()
    {
        SetMasterVolume(GetMasterVolume());
        SetMusicVolume(GetMusicVolume());
        SetSFXVolume(GetSFXVolume());
    }

    /// <summary>
    /// Restores all volumes to defaults via <see cref="GameSettings.RestoreDefaultAudio"/> and reloads them into the <see cref="AudioMixer"/>.
    /// </summary>
    public void RestoreDefaultVolumes()
    {
        GameSettings.Instance.RestoreDefaultAudio();
        LoadVolumes();
    }

    #endregion

    #region Utility

    /// <summary>
    /// Converts a linear volume value (0.0 to 1.0) into logarithmic decibels (-80 dB to 0 dB) for Unity <see cref="AudioMixer"/>.
    /// </summary>
    /// <param name="linear">Linear volume value.</param>
    /// <returns>Volume in decibels.</returns>
    private float LinearToDecibel(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }

    #endregion
}