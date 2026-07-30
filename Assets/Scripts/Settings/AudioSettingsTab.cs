using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Encapsula toda a lógica da aba "Áudio" do menu de configurações.
/// </summary>
public class AudioSettingsTab : ISettingsTab
{
    private Slider sliderMaster;
    private Slider sliderSons;
    private Slider sliderMusica;
    private Button btnResetAudio;

    public void Init(VisualElement root)
    {
        sliderMaster = root.Q<Slider>("SliderMaster");
        sliderSons = root.Q<Slider>("SliderSons");
        sliderMusica = root.Q<Slider>("SliderMusica");
        btnResetAudio = root.Q<Button>("BtnResetAudio");

        InitSliders();
    }

    public void RegisterCallbacks()
    {
        if (btnResetAudio != null) btnResetAudio.clicked += ResetDefaultAudio;
    }

    public void UnregisterCallbacks()
    {
        if (btnResetAudio != null) btnResetAudio.clicked -= ResetDefaultAudio;

        sliderMaster?.UnregisterValueChangedCallback(OnMasterVolumeChanged);
        sliderSons?.UnregisterValueChangedCallback(OnSFXVolumeChanged);
        sliderMusica?.UnregisterValueChangedCallback(OnMusicVolumeChanged);
    }

    public void OnLocaleChanged() { }

    private void InitSliders()
    {
        if (AudioManager.Instance == null)
            return;

        if (sliderMaster != null)
        {
            sliderMaster.SetValueWithoutNotify(AudioManager.Instance.GetMasterVolume());
            sliderMaster.RegisterValueChangedCallback(OnMasterVolumeChanged);
        }

        if (sliderSons != null)
        {
            sliderSons.SetValueWithoutNotify(AudioManager.Instance.GetSFXVolume());
            sliderSons.RegisterValueChangedCallback(OnSFXVolumeChanged);
        }

        if (sliderMusica != null)
        {
            sliderMusica.SetValueWithoutNotify(AudioManager.Instance.GetMusicVolume());
            sliderMusica.RegisterValueChangedCallback(OnMusicVolumeChanged);
        }
    }

    private void OnMasterVolumeChanged(ChangeEvent<float> evt) => AudioManager.Instance?.SetMasterVolume(evt.newValue);
    private void OnSFXVolumeChanged(ChangeEvent<float> evt) => AudioManager.Instance?.SetSFXVolume(evt.newValue);
    private void OnMusicVolumeChanged(ChangeEvent<float> evt) => AudioManager.Instance?.SetMusicVolume(evt.newValue);

    private void ResetDefaultAudio()
    {
        if (AudioManager.Instance == null) return;

        sliderMaster?.SetValueWithoutNotify(AudioManager.DEFAULT_MASTER);
        sliderMusica?.SetValueWithoutNotify(AudioManager.DEFAULT_MUSIC);
        sliderSons?.SetValueWithoutNotify(AudioManager.DEFAULT_SFX);

        AudioManager.Instance.SetMasterVolume(AudioManager.DEFAULT_MASTER);
        AudioManager.Instance.SetMusicVolume(AudioManager.DEFAULT_MUSIC);
        AudioManager.Instance.SetSFXVolume(AudioManager.DEFAULT_SFX);

        PlayerPrefs.Save();
    }
}
