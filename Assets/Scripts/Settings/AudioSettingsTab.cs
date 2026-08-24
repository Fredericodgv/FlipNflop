using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Encapsulates all logic for the "Audio" tab in the settings menu.
/// Interacts with <see cref="AudioSettings"/> for volume data and UI Toolkit elements (<see cref="VisualElement"/>, <see cref="Slider"/>, <see cref="Button"/>).
/// </summary>
public class AudioSettingsTab : ISettingsTab
{
    #region Private Fields

    private Slider sliderMaster;
    private Slider sliderSons;
    private Slider sliderMusica;
    private Button btnResetAudio;

    #endregion

    #region ISettingsTab Implementation

    /// <summary>
    /// Caches UI Toolkit elements from the root hierarchy and initializes slider values.
    /// Queries elements: "SliderMaster", "SliderSons", "SliderMusica", and "BtnResetAudio".
    /// Interacts with <see cref="AudioSettings"/>.
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
    /// Synchronizes UI sliders with stored volume values retrieved from <see cref="AudioSettings.Instance"/>
    /// and registers value change callbacks.
    /// Interacts with <see cref="AudioSettings"/>.
    /// </summary>
    private void InitSliders()
    {
        if (sliderMaster != null)
        {
            sliderMaster.SetValueWithoutNotify(AudioSettings.Instance.VolumeMaster);
            sliderMaster.RegisterValueChangedCallback(OnMasterVolumeChanged);
        }

        if (sliderSons != null)
        {
            sliderSons.SetValueWithoutNotify(AudioSettings.Instance.VolumeSFX);
            sliderSons.RegisterValueChangedCallback(OnSFXVolumeChanged);
        }

        if (sliderMusica != null)
        {
            sliderMusica.SetValueWithoutNotify(AudioSettings.Instance.VolumeMusic);
            sliderMusica.RegisterValueChangedCallback(OnMusicVolumeChanged);
        }
    }

    /// <summary>
    /// Handles Master volume slider value changes, updating <see cref="AudioSettings.Instance"/>.
    /// </summary>
    /// <param name="evt">The UI Toolkit value change event containing the new float volume value.</param>
    private void OnMasterVolumeChanged(ChangeEvent<float> evt)
    {
        AudioSettings.Instance.VolumeMaster = evt.newValue;
        AudioSettings.Instance.SaveAndNotify();
    }

    /// <summary>
    /// Handles SFX volume slider value changes, updating <see cref="AudioSettings.Instance"/>.
    /// </summary>
    /// <param name="evt">The UI Toolkit value change event containing the new float volume value.</param>
    private void OnSFXVolumeChanged(ChangeEvent<float> evt)
    {
        AudioSettings.Instance.VolumeSFX = evt.newValue;
        AudioSettings.Instance.SaveAndNotify();
    }

    /// <summary>
    /// Handles Music volume slider value changes, updating <see cref="AudioSettings.Instance"/>.
    /// </summary>
    /// <param name="evt">The UI Toolkit value change event containing the new float volume value.</param>
    private void OnMusicVolumeChanged(ChangeEvent<float> evt)
    {
        AudioSettings.Instance.VolumeMusic = evt.newValue;
        AudioSettings.Instance.SaveAndNotify();
    }

    #endregion

    #region Reset Operations

    /// <summary>
    /// Resets all volume sliders and audio settings to default constants via <see cref="AudioSettings.Instance"/>.
    /// </summary>
    private void ResetDefaultAudio()
    {
        sliderMaster?.SetValueWithoutNotify(AudioSettings.DEFAULT_MASTER);
        sliderMusica?.SetValueWithoutNotify(AudioSettings.DEFAULT_MUSIC);
        sliderSons?.SetValueWithoutNotify(AudioSettings.DEFAULT_SFX);

        AudioSettings.Instance.RestoreDefaults();
    }

    #endregion
}
