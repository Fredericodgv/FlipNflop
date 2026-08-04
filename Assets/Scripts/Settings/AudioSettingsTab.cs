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
    /// Synchronizes UI sliders with stored volume values retrieved from <see cref="AudioManager.Instance"/>
    /// and registers value change callbacks.
    /// </summary>
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

    /// <summary>
    /// Handles Master volume slider value changes and updates <see cref="AudioManager.Instance"/>.
    /// </summary>
    /// <param name="evt">The UI Toolkit value change event containing the new float volume value.</param>
    private void OnMasterVolumeChanged(ChangeEvent<float> evt) => AudioManager.Instance?.SetMasterVolume(evt.newValue);

    /// <summary>
    /// Handles SFX volume slider value changes and updates <see cref="AudioManager.Instance"/>.
    /// </summary>
    /// <param name="evt">The UI Toolkit value change event containing the new float volume value.</param>
    private void OnSFXVolumeChanged(ChangeEvent<float> evt) => AudioManager.Instance?.SetSFXVolume(evt.newValue);

    /// <summary>
    /// Handles Music volume slider value changes and updates <see cref="AudioManager.Instance"/>.
    /// </summary>
    /// <param name="evt">The UI Toolkit value change event containing the new float volume value.</param>
    private void OnMusicVolumeChanged(ChangeEvent<float> evt) => AudioManager.Instance?.SetMusicVolume(evt.newValue);

    #endregion

    #region Reset Operations

    /// <summary>
    /// Resets all volume sliders and <see cref="AudioManager"/> settings to default volume constants, then saves preferences to <see cref="PlayerPrefs"/>.
    /// </summary>
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

    #endregion
}
