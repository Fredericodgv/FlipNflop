using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Encapsulates all logic for the "Audio" tab in the settings menu.
/// Interacts with <see cref="AudioManager"/> and UI Toolkit elements (<see cref="VisualElement"/>, <see cref="Slider"/>, <see cref="Button"/>).
/// </summary>
public class AudioSettingsTab : ISettingsTab
{
    #region Private Fields

    /// <summary>
    /// UI Toolkit Slider control for the Master volume setting.
    /// </summary>
    private Slider sliderMaster;

    /// <summary>
    /// UI Toolkit Slider control for the Sound Effects (SFX) volume setting.
    /// </summary>
    private Slider sliderSons;

    /// <summary>
    /// UI Toolkit Slider control for the Music volume setting.
    /// </summary>
    private Slider sliderMusica;

    /// <summary>
    /// UI Toolkit Button control for resetting audio settings to default values.
    /// </summary>
    private Button btnResetAudio;

    #endregion

    #region ISettingsTab Implementation

    /// <summary>
    /// Caches UI Toolkit elements from the root hierarchy and initializes slider values.
    /// Queries elements: "SliderMaster", "SliderSons", "SliderMusica", and "BtnResetAudio".
    /// Interacts with <see cref="AudioManager"/>.
    /// </summary>
    /// <param name="root">The root <see cref="VisualElement"/> container of the options menu.</param>
    public void Init(VisualElement root)
    {
        sliderMaster = root.Q<Slider>("SliderMaster");
        sliderSons = root.Q<Slider>("SliderSons");
        sliderMusica = root.Q<Slider>("SliderMusica");
        btnResetAudio = root.Q<Button>("BtnResetAudio");

        InitSliders();
    }

    /// <summary>
    /// Registers event callbacks for UI Toolkit buttons.
    /// </summary>
    public void RegisterCallbacks()
    {
        if (btnResetAudio != null) btnResetAudio.clicked += ResetDefaultAudio;
    }

    /// <summary>
    /// Unregisters event callbacks for UI Toolkit buttons and volume sliders to prevent memory leaks.
    /// </summary>
    public void UnregisterCallbacks()
    {
        if (btnResetAudio != null) btnResetAudio.clicked -= ResetDefaultAudio;

        sliderMaster?.UnregisterValueChangedCallback(OnMasterVolumeChanged);
        sliderSons?.UnregisterValueChangedCallback(OnSFXVolumeChanged);
        sliderMusica?.UnregisterValueChangedCallback(OnMusicVolumeChanged);
    }

    /// <summary>
    /// Called when the active localization locale changes.
    /// Audio tab contains no localized text labels directly, so no operation is performed.
    /// </summary>
    public void OnLocaleChanged() { }

    #endregion

    #region Slider Initialization & Event Handlers

    /// <summary>
    /// Synchronizes UI sliders with stored volume values retrieved from <see cref="GameSettings.Instance"/>
    /// and registers value change callbacks.
    /// Interacts with <see cref="GameSettings"/> and <see cref="AudioManager"/>.
    /// </summary>
    private void InitSliders()
    {
        if (sliderMaster != null)
        {
            sliderMaster.SetValueWithoutNotify(GameSettings.Instance.VolumeMaster);
            sliderMaster.RegisterValueChangedCallback(OnMasterVolumeChanged);
        }

        if (sliderSons != null)
        {
            sliderSons.SetValueWithoutNotify(GameSettings.Instance.VolumeSFX);
            sliderSons.RegisterValueChangedCallback(OnSFXVolumeChanged);
        }

        if (sliderMusica != null)
        {
            sliderMusica.SetValueWithoutNotify(GameSettings.Instance.VolumeMusic);
            sliderMusica.RegisterValueChangedCallback(OnMusicVolumeChanged);
        }
    }

    /// <summary>
    /// Handles Master volume slider value changes, updating <see cref="GameSettings.Instance"/> and <see cref="AudioManager.Instance"/> if present.
    /// </summary>
    /// <param name="evt">The UI Toolkit value change event containing the new float volume value.</param>
    private void OnMasterVolumeChanged(ChangeEvent<float> evt)
    {
        GameSettings.Instance.VolumeMaster = evt.newValue;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(evt.newValue);
        else
        {
            PlayerPrefs.SetFloat("AudioMaster", evt.newValue);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Handles SFX volume slider value changes, updating <see cref="GameSettings.Instance"/> and <see cref="AudioManager.Instance"/> if present.
    /// </summary>
    /// <param name="evt">The UI Toolkit value change event containing the new float volume value.</param>
    private void OnSFXVolumeChanged(ChangeEvent<float> evt)
    {
        GameSettings.Instance.VolumeSFX = evt.newValue;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(evt.newValue);
        else
        {
            PlayerPrefs.SetFloat("AudioSons", evt.newValue);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Handles Music volume slider value changes, updating <see cref="GameSettings.Instance"/> and <see cref="AudioManager.Instance"/> if present.
    /// </summary>
    /// <param name="evt">The UI Toolkit value change event containing the new float volume value.</param>
    private void OnMusicVolumeChanged(ChangeEvent<float> evt)
    {
        GameSettings.Instance.VolumeMusic = evt.newValue;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(evt.newValue);
        else
        {
            PlayerPrefs.SetFloat("AudioMusica", evt.newValue);
            PlayerPrefs.Save();
        }
    }

    #endregion

    #region Reset Operations

    /// <summary>
    /// Resets all volume sliders and audio settings to default constants, then saves preferences to <see cref="PlayerPrefs"/>.
    /// Interacts with <see cref="GameSettings"/> and <see cref="AudioManager"/>.
    /// </summary>
    private void ResetDefaultAudio()
    {
        sliderMaster?.SetValueWithoutNotify(GameSettings.DEFAULT_MASTER);
        sliderMusica?.SetValueWithoutNotify(GameSettings.DEFAULT_MUSIC);
        sliderSons?.SetValueWithoutNotify(GameSettings.DEFAULT_SFX);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RestoreDefaultVolumes();
        }
        else
        {
            GameSettings.Instance.RestoreDefaultAudio();
        }

        PlayerPrefs.Save();
    }

    #endregion
}
