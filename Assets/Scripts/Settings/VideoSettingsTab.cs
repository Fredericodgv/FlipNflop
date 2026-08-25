using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization.Settings;

/// <summary>
/// Encapsulates all logic for the "Video" tab in the settings menu, including contrast adjustment overlay and language toggling.
/// Reads and writes contrast preferences through <see cref="PlayerPrefs"/>.
/// Interacts with <see cref="SpriteRenderer"/>, <see cref="LocalizationSettings"/>, and UI Toolkit elements.
/// </summary>
public class VideoSettingsTab : ISettingsTab
{
    #region Constants & Fields

    private const string KEY_CONTRAST = "ContrastValue";
    private const float DEFAULT_CONTRAST = 0f;

    private readonly SpriteRenderer contrastOverlaySprite;

    private Slider sliderContrast;
    private Button btnResetContrast;
    private Button btnToggleLanguage;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="VideoSettingsTab"/> with a contrast overlay sprite reference.
    /// </summary>
    /// <param name="contrastOverlay">Sprite renderer overlay for visual contrast adjustments.</param>
    public VideoSettingsTab(SpriteRenderer contrastOverlay)
    {
        contrastOverlaySprite = contrastOverlay;
    }

    #endregion

    #region ISettingsTab Implementation

    /// <summary>
    /// Caches UI Toolkit elements from the root hierarchy and initializes contrast settings.
    /// Queries elements: "SliderContrast", "BtnResetContrast", and "BtnToggleLanguage".
    /// </summary>
    /// <param name="root">The root <see cref="VisualElement"/> container of the options menu.</param>
    public void Init(VisualElement root)
    {
        sliderContrast = root.Q<Slider>("SliderContrast");
        btnResetContrast = root.Q<Button>("BtnResetContrast");
        btnToggleLanguage = root.Q<Button>("BtnToggleLanguage");

        InitContrast();
    }

    /// <summary>
    /// Registers event callbacks for reset contrast and language toggle buttons.
    /// </summary>
    public void RegisterCallbacks()
    {
        if (btnResetContrast != null) btnResetContrast.clicked += ResetContrast;
        if (btnToggleLanguage != null) btnToggleLanguage.clicked += ToggleLanguage;
    }

    /// <summary>
    /// Unregisters event callbacks for buttons and sliders to prevent memory leaks.
    /// </summary>
    public void UnregisterCallbacks()
    {
        if (btnResetContrast != null) btnResetContrast.clicked -= ResetContrast;
        if (btnToggleLanguage != null) btnToggleLanguage.clicked -= ToggleLanguage;

        sliderContrast?.UnregisterValueChangedCallback(OnContrastChanged);
    }

    /// <summary>
    /// Called when the active localization locale changes.
    /// Video tab controls require no specific locale change re-initialization.
    /// </summary>
    public void OnLocaleChanged() { }

    #endregion

    #region Contrast Control

    /// <summary>
    /// Loads saved contrast value from <see cref="PlayerPrefs"/>, initializes slider control, applies contrast to overlay sprite, and registers slider callback.
    /// </summary>
    private void InitContrast()
    {
        if (sliderContrast == null)
            return;

        float savedContrast = PlayerPrefs.GetFloat(KEY_CONTRAST, DEFAULT_CONTRAST);

        sliderContrast.SetValueWithoutNotify(savedContrast);
        ApplyContrast(savedContrast);

        sliderContrast.RegisterValueChangedCallback(OnContrastChanged);
    }

    /// <summary>
    /// Handles contrast slider value change events and updates contrast overlay.
    /// </summary>
    /// <param name="evt">The UI Toolkit value change event.</param>
    private void OnContrastChanged(ChangeEvent<float> evt) => ApplyContrast(evt.newValue);

    /// <summary>
    /// Applies visual contrast color/alpha to <see cref="contrastOverlaySprite"/> and saves value to <see cref="PlayerPrefs"/>.
    /// Positive values apply white overlay with alpha, negative values apply black overlay with alpha.
    /// </summary>
    /// <param name="value">Contrast level float between -1.0 and 1.0.</param>
    private void ApplyContrast(float value)
    {
        if (contrastOverlaySprite == null)
            return;

        Color overlayColor = value switch
        {
            > 0f => new Color(1f, 1f, 1f, value),
            < 0f => new Color(0f, 0f, 0f, -value),
            _ => Color.clear
        };

        contrastOverlaySprite.color = overlayColor;

        PlayerPrefs.SetFloat(KEY_CONTRAST, value);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Resets contrast slider value to 0 (default transparent contrast).
    /// </summary>
    private void ResetContrast()
    {
        if (sliderContrast != null)
            sliderContrast.value = 0f;
    }

    #endregion

    #region Localization Control

    /// <summary>
    /// Cycles through available localization locales and updates <see cref="LocalizationSettings.SelectedLocale"/>.
    /// Interacts with Unity Localization system.
    /// </summary>
    private void ToggleLanguage()
    {
        var locales = LocalizationSettings.AvailableLocales?.Locales;
        if (locales == null || locales.Count == 0) return;

        int currentIndex = locales.IndexOf(LocalizationSettings.SelectedLocale);
        int nextIndex = (currentIndex + 1) % locales.Count;

        LocalizationSettings.SelectedLocale = locales[nextIndex];
    }

    #endregion
}
