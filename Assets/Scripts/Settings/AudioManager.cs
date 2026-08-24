using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Scene runtime audio player and AudioMixer synchronizer.
/// Applies volume configurations from <see cref="AudioSettings"/> to the Unity <see cref="AudioMixer"/>, and controls background music.
/// Interacts with <see cref="AudioSettings"/> for volume data and events, <see cref="AudioMixer"/> for output mixing, and <see cref="AudioSource"/> for playback.
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region Inspector Fields

    [Header("Audio Mixer")]
    [Tooltip("Main AudioMixer controlling Master, Music, and SFX channels.")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Background Music")]
    [Tooltip("AudioSource component used to play background music.")]
    [SerializeField] private AudioSource musicSource;

    #endregion

    #region Constants & Fields

    private const string PARAM_MASTER = "VolumeMaster";
    private const string PARAM_MUSIC = "VolumeMusica";
    private const string PARAM_SFX = "VolumeSons";

    public const float DEFAULT_MASTER = AudioSettings.DEFAULT_MASTER;
    public const float DEFAULT_SFX = AudioSettings.DEFAULT_SFX;
    public const float DEFAULT_MUSIC = AudioSettings.DEFAULT_MUSIC;

    /// <summary>
    /// Singleton instance for scene audio playback access.
    /// </summary>
    public static AudioManager Instance { get; private set; }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        AudioSettings.OnAudioChanged += ApplyMixerVolumes;
    }

    private void OnDisable()
    {
        AudioSettings.OnAudioChanged -= ApplyMixerVolumes;
    }

    private void Start()
    {
        ApplyMixerVolumes();

        if (musicSource != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    #endregion

    #region Volume Application

    /// <summary>
    /// Synchronizes all <see cref="AudioMixer"/> group decibel levels with values stored in <see cref="AudioSettings.Instance"/>.
    /// </summary>
    public void ApplyMixerVolumes()
    {
        if (audioMixer == null)
            return;

        audioMixer.SetFloat(PARAM_MASTER, LinearToDecibel(AudioSettings.Instance.VolumeMaster));
        audioMixer.SetFloat(PARAM_MUSIC, LinearToDecibel(AudioSettings.Instance.VolumeMusic));
        audioMixer.SetFloat(PARAM_SFX, LinearToDecibel(AudioSettings.Instance.VolumeSFX));
    }

    /// <summary>
    /// Updates Master volume in <see cref="AudioSettings"/> and applies it to the mixer.
    /// </summary>
    /// <param name="value">Linear volume scale (0.0001 to 1.0).</param>
    public void SetMasterVolume(float value)
    {
        AudioSettings.Instance.VolumeMaster = value;
        AudioSettings.Instance.SaveAndNotify();
    }

    /// <summary>
    /// Updates Music volume in <see cref="AudioSettings"/> and applies it to the mixer.
    /// </summary>
    /// <param name="value">Linear volume scale (0.0001 to 1.0).</param>
    public void SetMusicVolume(float value)
    {
        AudioSettings.Instance.VolumeMusic = value;
        AudioSettings.Instance.SaveAndNotify();
    }

    /// <summary>
    /// Updates SFX volume in <see cref="AudioSettings"/> and applies it to the mixer.
    /// </summary>
    /// <param name="value">Linear volume scale (0.0001 to 1.0).</param>
    public void SetSFXVolume(float value)
    {
        AudioSettings.Instance.VolumeSFX = value;
        AudioSettings.Instance.SaveAndNotify();
    }

    public float GetMasterVolume() => AudioSettings.Instance.VolumeMaster;
    public float GetMusicVolume() => AudioSettings.Instance.VolumeMusic;
    public float GetSFXVolume() => AudioSettings.Instance.VolumeSFX;

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