using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Música de Fundo")]
    [SerializeField] private AudioSource musicSource; // AudioSource com Output → grupo Musica do Mixer

    private const string PARAM_MASTER = "VolumeMaster";
    private const string PARAM_MUSIC = "VolumeMusica";
    private const string PARAM_SFX = "VolumeSons";

    private const string KEY_MASTER = "AudioMaster";
    private const string KEY_MUSIC = "AudioMusica";
    private const string KEY_SFX = "AudioSons";

    public const float DEFAULT_MASTER = 1f;    // 100%
    public const float DEFAULT_SFX = 1f; // 100%
    public const float DEFAULT_MUSIC = 0.25f; // 25%

    public static AudioManager Instance { get; private set; }

    #region Inicialização

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        LoadVolumes();

        if (musicSource != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    #endregion

    #region Controle de Volume

    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat(PARAM_MASTER, LinearToDecibel(value));
        PlayerPrefs.SetFloat(KEY_MASTER, value);
    }

    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat(PARAM_MUSIC, LinearToDecibel(value));
        PlayerPrefs.SetFloat(KEY_MUSIC, value);
    }

    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat(PARAM_SFX, LinearToDecibel(value));
        PlayerPrefs.SetFloat(KEY_SFX, value);
    }

    public float GetMasterVolume() => PlayerPrefs.GetFloat(KEY_MASTER, DEFAULT_MASTER);
    public float GetMusicVolume() => PlayerPrefs.GetFloat(KEY_MUSIC, DEFAULT_MUSIC);
    public float GetSFXVolume() => PlayerPrefs.GetFloat(KEY_SFX, DEFAULT_SFX);

    #endregion

    #region Salvar / Carregar / Resetar

    public void LoadVolumes()
    {
        SetMasterVolume(GetMasterVolume());
        SetMusicVolume(GetMusicVolume());
        SetSFXVolume(GetSFXVolume());
    }

    public void RestoreDefaultVolumes()
    {
        PlayerPrefs.DeleteKey(KEY_MASTER);
        PlayerPrefs.DeleteKey(KEY_MUSIC);
        PlayerPrefs.DeleteKey(KEY_SFX);
        LoadVolumes();
    }

    #endregion

    #region Utilitário

    private float LinearToDecibel(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }

    #endregion
}