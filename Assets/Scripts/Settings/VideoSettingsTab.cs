using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization.Settings;

/// <summary>
/// Encapsula toda a lógica da aba "Vídeo" do menu de configurações.
/// </summary>
public class VideoSettingsTab : ISettingsTab
{
    private const string ContrastSaveKey = "ContrastValue";

    private readonly SpriteRenderer contrastOverlaySprite;

    private Slider sliderContrast;
    private Button btnResetContrast;
    private Button btnToggleLanguage;

    public VideoSettingsTab(SpriteRenderer contrastOverlay)
    {
        contrastOverlaySprite = contrastOverlay;
    }

    public void Init(VisualElement root)
    {
        sliderContrast = root.Q<Slider>("SliderContrast");
        btnResetContrast = root.Q<Button>("BtnResetContrast");
        btnToggleLanguage = root.Q<Button>("BtnToggleLanguage");

        InitContrast();
    }

    public void RegisterCallbacks()
    {
        if (btnResetContrast != null) btnResetContrast.clicked += ResetContrast;
        if (btnToggleLanguage != null) btnToggleLanguage.clicked += ToggleLanguage;
    }

    public void UnregisterCallbacks()
    {
        if (btnResetContrast != null) btnResetContrast.clicked -= ResetContrast;
        if (btnToggleLanguage != null) btnToggleLanguage.clicked -= ToggleLanguage;

        sliderContrast?.UnregisterValueChangedCallback(OnContrastChanged);
    }

    public void OnLocaleChanged() { }

    private void InitContrast()
    {
        if (sliderContrast == null)
            return;

        float savedContrast = PlayerPrefs.GetFloat(ContrastSaveKey, 0f);

        sliderContrast.SetValueWithoutNotify(savedContrast);
        ApplyContrast(savedContrast);

        sliderContrast.RegisterValueChangedCallback(OnContrastChanged);
    }

    private void OnContrastChanged(ChangeEvent<float> evt) => ApplyContrast(evt.newValue);

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

        PlayerPrefs.SetFloat(ContrastSaveKey, value);
    }

    private void ResetContrast()
    {
        if (sliderContrast != null)
            sliderContrast.value = 0f;
    }

    private void ToggleLanguage()
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;

        int currentIndex = locales.IndexOf(LocalizationSettings.SelectedLocale);
        int nextIndex = (currentIndex + 1) % locales.Count;

        LocalizationSettings.SelectedLocale = locales[nextIndex];
    }
}
