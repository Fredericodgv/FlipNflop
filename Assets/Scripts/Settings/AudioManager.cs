using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Manages global audio volume settings and background music playback.
/// Interacts with Unity <see cref="AudioMixer"/>, <see cref="AudioSource"/>, and persists volume levels using <see cref="PlayerPrefs"/>.
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
    /// <see cref="PlayerPrefs"/> key for storing the Master volume setting.
    /// </summary>
    private const string KEY_MASTER = "AudioMaster";

    /// <summary>
    /// <see cref="PlayerPrefs"/> key for storing the Music volume setting.
    /// </summary>
    private const string KEY_MUSIC = "AudioMusica";

    /// <summary>
    /// <see cref="PlayerPrefs"/> key for storing the SFX volume setting.
    /// </summary>
    private const string KEY_SFX = "AudioSons";

    /// <summary>
    /// Default Master volume setting (100% / 1.0).
    /// </summary>
    public const float DEFAULT_MASTER = 1f;

    /// <summary>
    /// Default SFX volume setting (100% / 1.0).
    /// </summary>
    public const float DEFAULT_SFX = 1f;

    /// <summary>
    /// Default Music volume setting (25% / 0.25).
    /// </summary>
    public const float DEFAULT_MUSIC = 0.25f;

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
    /// Loads saved volume preferences via <see cref="LoadVolumes"/> and starts background music playback if assigned.
    /// Interacts with <see cref="PlayerPrefs"/> and <see cref="AudioSource"/>.
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
    /// Sets the Master volume, updates the <see cref="AudioMixer"/>, and saves the value to <see cref="PlayerPrefs"/>.
    /// </summary>
    /// <param name="value">Linear volume scale between 0.0001 and 1.0.</param>
    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat(PARAM_MASTER, LinearToDecibel(value));
        PlayerPrefs.SetFloat(KEY_MASTER, value);
    }

    /// <summary>
    /// Sets the Music volume, updates the <see cref="AudioMixer"/>, and saves the value to <see cref="PlayerPrefs"/>.
    /// </summary>
    /// <param name="value">Linear volume scale between 0.0001 and 1.0.</param>
    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat(PARAM_MUSIC, LinearToDecibel(value));
        PlayerPrefs.SetFloat(KEY_MUSIC, value);
    }

    /// <summary>
    /// Sets the SFX volume, updates the <see cref="AudioMixer"/>, and saves the value to <see cref="PlayerPrefs"/>.
    /// </summary>
    /// <param name="value">Linear volume scale between 0.0001 and 1.0.</param>
    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat(PARAM_SFX, LinearToDecibel(value));
        PlayerPrefs.SetFloat(KEY_SFX, value);
    }

    /// <summary>
    /// Retrieves the stored Master volume setting from <see cref="PlayerPrefs"/> or returns <see cref="DEFAULT_MASTER"/>.
    /// </summary>
    /// <returns>Stored Master volume float value.</returns>
    public float GetMasterVolume() => PlayerPrefs.GetFloat(KEY_MASTER, DEFAULT_MASTER);

    /// <summary>
    /// Retrieves the stored Music volume setting from <see cref="PlayerPrefs"/> or returns <see cref="DEFAULT_MUSIC"/>.
    /// </summary>
    /// <returns>Stored Music volume float value.</returns>
    public float GetMusicVolume() => PlayerPrefs.GetFloat(KEY_MUSIC, DEFAULT_MUSIC);

    /// <summary>
    /// Retrieves the stored SFX volume setting from <see cref="PlayerPrefs"/> or returns <see cref="DEFAULT_SFX"/>.
    /// </summary>
    /// <returns>Stored SFX volume float value.</returns>
    public float GetSFXVolume() => PlayerPrefs.GetFloat(KEY_SFX, DEFAULT_SFX);

    #endregion

    #region Save / Load / Reset

    /// <summary>
    /// Loads all volume preferences from <see cref="PlayerPrefs"/> and applies them to the <see cref="AudioMixer"/>.
    /// </summary>
    public void LoadVolumes()
    {
        SetMasterVolume(GetMasterVolume());
        SetMusicVolume(GetMusicVolume());
        SetSFXVolume(GetSFXVolume());
    }

    /// <summary>
    /// Deletes saved volume keys from <see cref="PlayerPrefs"/> and reloads default volumes.
    /// </summary>
    public void RestoreDefaultVolumes()
    {
        PlayerPrefs.DeleteKey(KEY_MASTER);
        PlayerPrefs.DeleteKey(KEY_MUSIC);
        PlayerPrefs.DeleteKey(KEY_SFX);
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