using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(UIDocument))]
public class ConfigManager : MonoBehaviour
{
    private const string ContrastSaveKey = "ContrastValue";

    private enum SignalType
    {
        J,
        K,
        CLK,
        Preset,
        Clear,
        FeedbackSuccess,
        FeedbackFailure
    }

    [Header("Vídeo")]
    [Tooltip("Overlay usado para aplicar contraste visual.")]
    [SerializeField] private SpriteRenderer contrastOverlaySprite;

    private UIDocument uiDocument;
    private VisualElement root;

    // Navegação
    private Button btnTabColors;
    private Button btnTabAudio;
    private Button btnTabVideo;
    private Button btnTabControls;
    private Button btnBack;

    private VisualElement panelColor;
    private VisualElement panelAudio;
    private VisualElement panelVideo;
    private VisualElement panelControls;

    private readonly List<VisualElement> panels = new();

    // Áudio
    private Slider sliderMaster;
    private Slider sliderSons;
    private Slider sliderMusica;
    private Button btnResetAudio;

    // Vídeo
    private Slider sliderContrast;
    private Button btnResetContrast;
    private Button btnToggleLanguage;

    // Cores — sinais
    private DropdownField dropdownPaleta;
    private Button btnResetColors;
    private bool isApplyingPreset = false;

    private VisualElement previewJ;
    private VisualElement previewK;
    private VisualElement previewCLK;
    private VisualElement previewPreset;
    private VisualElement previewClear;
    private VisualElement previewFeedbackSuccess;
    private VisualElement previewFeedbackFailure;

    private VisualElement containerSwatchesJ;
    private VisualElement containerSwatchesK;
    private VisualElement containerSwatchesCLK;
    private VisualElement containerSwatchesPreset;
    private VisualElement containerSwatchesClear;
    private VisualElement containerSwatchesFeedbackSuccess;
    private VisualElement containerSwatchesFeedbackFailure;

    private Button btnCustomJ;
    private Button btnCustomK;
    private Button btnCustomCLK;
    private Button btnCustomPreset;
    private Button btnCustomClear;
    private Button btnCustomFeedbackSuccess;
    private Button btnCustomFeedbackFailure;

    // RGB Overlay
    private VisualElement rgbOverlay;
    private Label rgbTitle;
    private VisualElement rgbPreview;

    private TextField inputHex;

    private Slider sliderR;
    private Slider sliderG;
    private Slider sliderB;

    private Button btnConfirmRGB;
    private Button btnCancelRGB;

    private SignalType activeCustomSignal = SignalType.J;

    private readonly List<Button> swatchesJ = new();
    private readonly List<Button> swatchesK = new();
    private readonly List<Button> swatchesCLK = new();
    private readonly List<Button> swatchesPreset = new();
    private readonly List<Button> swatchesClear = new();
    private readonly List<Button> swatchesFeedbackSuccess = new();
    private readonly List<Button> swatchesFeedbackFailure = new();

    /// <summary>
    /// Inicializa referências obrigatórias.
    /// </summary>
    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    /// <summary>
    /// Busca elementos da UI e registra callbacks.
    /// </summary>
    private void OnEnable()
    {
        if (uiDocument == null)
            return;

        root = uiDocument.rootVisualElement;

        CacheUIElements();
        RegisterCallbacks();

        InitAudioSliders();
        InitVideoSettings();
        InitColorSettings();

        ShowTab(panelVideo);

        // Se inscreve para escutar quando o idioma mudar
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    /// <summary>
    /// Remove callbacks registrados.
    /// </summary>
    private void OnDisable()
    {
        UnregisterCallbacks();

        // Remove a inscrição para evitar memory leaks
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        RefreshPaletteDropdown();
        SyncColorUI();
    }

    /// <summary>
    /// Busca todos os elementos da interface.
    /// </summary>
    private void CacheUIElements()
    {
        // Navegação
        btnTabColors = root.Q<Button>("BtnTabColors");
        btnTabAudio = root.Q<Button>("BtnTabAudio");
        btnTabVideo = root.Q<Button>("BtnTabVideo");
        btnTabControls = root.Q<Button>("BtnTabControls");
        btnBack = root.Q<Button>("BtnBack");

        panelColor = root.Q<VisualElement>("PanelColor");
        panelAudio = root.Q<VisualElement>("PanelAudio");
        panelVideo = root.Q<VisualElement>("PanelVideo");
        panelControls = root.Q<VisualElement>("PanelControls");

        panels.Clear();
        panels.Add(panelColor);
        panels.Add(panelAudio);
        panels.Add(panelVideo);
        panels.Add(panelControls);

        // Áudio
        sliderMaster = root.Q<Slider>("SliderMaster");
        sliderSons = root.Q<Slider>("SliderSons");
        sliderMusica = root.Q<Slider>("SliderMusica");
        btnResetAudio = root.Q<Button>("BtnResetAudio");

        // Vídeo
        sliderContrast = root.Q<Slider>("SliderContrast");
        btnResetContrast = root.Q<Button>("BtnResetContrast");
        btnToggleLanguage = root.Q<Button>("BtnToggleLanguage");

        // Cores
        dropdownPaleta = root.Q<DropdownField>("DropdownPaleta");
        btnResetColors = root.Q<Button>("BtnResetColors");

        previewJ = root.Q<VisualElement>("PreviewJ");
        previewK = root.Q<VisualElement>("PreviewK");
        previewCLK = root.Q<VisualElement>("PreviewCLK");
        previewPreset = root.Q<VisualElement>("PreviewPreset");
        previewClear = root.Q<VisualElement>("PreviewClear");
        previewFeedbackSuccess = root.Q<VisualElement>("PreviewFeedbackSuccess");
        previewFeedbackFailure = root.Q<VisualElement>("PreviewFeedbackFailure");

        containerSwatchesJ = root.Q<VisualElement>("ContainerSwatchesJ");
        containerSwatchesK = root.Q<VisualElement>("ContainerSwatchesK");
        containerSwatchesCLK = root.Q<VisualElement>("ContainerSwatchesCLK");
        containerSwatchesPreset = root.Q<VisualElement>("ContainerSwatchesPreset");
        containerSwatchesClear = root.Q<VisualElement>("ContainerSwatchesClear");
        containerSwatchesFeedbackSuccess = root.Q<VisualElement>("ContainerSwatchesFeedbackSuccess");
        containerSwatchesFeedbackFailure = root.Q<VisualElement>("ContainerSwatchesFeedbackFailure");

        btnCustomJ = root.Q<Button>("BtnCustomJ");
        btnCustomK = root.Q<Button>("BtnCustomK");
        btnCustomCLK = root.Q<Button>("BtnCustomCLK");
        btnCustomPreset = root.Q<Button>("BtnCustomPreset");
        btnCustomClear = root.Q<Button>("BtnCustomClear");
        btnCustomFeedbackSuccess = root.Q<Button>("BtnCustomFeedbackSuccess");
        btnCustomFeedbackFailure = root.Q<Button>("BtnCustomFeedbackFailure");

        // RGB
        rgbOverlay = root.Q<VisualElement>("RGBOverlay");
        rgbTitle = root.Q<Label>("RGBTitle");
        rgbPreview = root.Q<VisualElement>("RGBPreview");

        inputHex = root.Q<TextField>("InputHex");

        sliderR = root.Q<Slider>("SliderR");
        sliderG = root.Q<Slider>("SliderG");
        sliderB = root.Q<Slider>("SliderB");

        btnConfirmRGB = root.Q<Button>("BtnConfirmRGB");
        btnCancelRGB = root.Q<Button>("BtnCancelRGB");
    }

    /// <summary>
    /// Registra callbacks da interface.
    /// </summary>
    private void RegisterCallbacks()
    {
        if (btnTabColors != null) btnTabColors.clicked += ShowColorsTab;
        if (btnTabAudio != null) btnTabAudio.clicked += ShowAudioTab;
        if (btnTabVideo != null) btnTabVideo.clicked += ShowVideoTab;
        if (btnTabControls != null) btnTabControls.clicked += ShowControlsTab;
        if (btnBack != null) btnBack.clicked += OnBackClicked;

        if (btnResetContrast != null) btnResetContrast.clicked += ResetContrast;
        if (btnToggleLanguage != null) btnToggleLanguage.clicked += ToggleLanguage;

        if (btnCustomJ != null) btnCustomJ.clicked += OpenCustomJ;
        if (btnCustomK != null) btnCustomK.clicked += OpenCustomK;
        if (btnCustomCLK != null) btnCustomCLK.clicked += OpenCustomCLK;
        if (btnCustomPreset != null) btnCustomPreset.clicked += OpenCustomPreset;
        if (btnCustomClear != null) btnCustomClear.clicked += OpenCustomClear;
        if (btnCustomFeedbackSuccess != null) btnCustomFeedbackSuccess.clicked += OpenCustomFeedbackSuccess;
        if (btnCustomFeedbackFailure != null) btnCustomFeedbackFailure.clicked += OpenCustomFeedbackFailure;

        if (btnConfirmRGB != null) btnConfirmRGB.clicked += ConfirmRGBColor;
        if (btnCancelRGB != null) btnCancelRGB.clicked += CloseRGBOverlay;

        if (btnResetColors != null) btnResetColors.clicked += ResetDefaultColors;
        if (btnResetAudio != null) btnResetAudio.clicked += ResetDefaultAudio;

        sliderR?.RegisterValueChangedCallback(OnRGBSliderChanged);
        sliderG?.RegisterValueChangedCallback(OnRGBSliderChanged);
        sliderB?.RegisterValueChangedCallback(OnRGBSliderChanged);

        inputHex?.RegisterValueChangedCallback(OnHexInputChanged);
    }

    /// <summary>
    /// Remove callbacks registrados.
    /// </summary>
    private void UnregisterCallbacks()
    {
        if (btnTabColors != null) btnTabColors.clicked -= ShowColorsTab;
        if (btnTabAudio != null) btnTabAudio.clicked -= ShowAudioTab;
        if (btnTabVideo != null) btnTabVideo.clicked -= ShowVideoTab;
        if (btnTabControls != null) btnTabControls.clicked -= ShowControlsTab;
        if (btnBack != null) btnBack.clicked -= OnBackClicked;

        if (btnResetContrast != null) btnResetContrast.clicked -= ResetContrast;
        if (btnToggleLanguage != null) btnToggleLanguage.clicked -= ToggleLanguage;

        if (btnCustomJ != null) btnCustomJ.clicked -= OpenCustomJ;
        if (btnCustomK != null) btnCustomK.clicked -= OpenCustomK;
        if (btnCustomCLK != null) btnCustomCLK.clicked -= OpenCustomCLK;
        if (btnCustomPreset != null) btnCustomPreset.clicked -= OpenCustomPreset;
        if (btnCustomClear != null) btnCustomClear.clicked -= OpenCustomClear;
        if (btnCustomFeedbackSuccess != null) btnCustomFeedbackSuccess.clicked -= OpenCustomFeedbackSuccess;
        if (btnCustomFeedbackFailure != null) btnCustomFeedbackFailure.clicked -= OpenCustomFeedbackFailure;

        if (btnConfirmRGB != null) btnConfirmRGB.clicked -= ConfirmRGBColor;
        if (btnCancelRGB != null) btnCancelRGB.clicked -= CloseRGBOverlay;

        sliderMaster?.UnregisterValueChangedCallback(OnMasterVolumeChanged);
        sliderSons?.UnregisterValueChangedCallback(OnSFXVolumeChanged);
        sliderMusica?.UnregisterValueChangedCallback(OnMusicVolumeChanged);

        sliderContrast?.UnregisterValueChangedCallback(OnContrastChanged);

        sliderR?.UnregisterValueChangedCallback(OnRGBSliderChanged);
        sliderG?.UnregisterValueChangedCallback(OnRGBSliderChanged);
        sliderB?.UnregisterValueChangedCallback(OnRGBSliderChanged);

        inputHex?.UnregisterValueChangedCallback(OnHexInputChanged);
    }

    private void ShowColorsTab() => ShowTab(panelColor);
    private void ShowAudioTab() => ShowTab(panelAudio);
    private void ShowVideoTab() => ShowTab(panelVideo);
    private void ShowControlsTab() => ShowTab(panelControls);

    /// <summary>
    /// Exibe apenas o painel informado.
    /// </summary>
    private void ShowTab(VisualElement activePanel)
    {
        foreach (var panel in panels)
        {
            if (panel != null)
                panel.style.display = DisplayStyle.None;
        }

        if (activePanel != null)
            activePanel.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Retorna ao menu principal.
    /// </summary>
    private void OnBackClicked()
    {
        var panelOptions = root.Q<VisualElement>("PanelOptions");
        var panelMain = root.Q<VisualElement>("PanelMain");

        if (panelOptions != null) panelOptions.style.display = DisplayStyle.None;
        if (panelMain != null) panelMain.style.display = DisplayStyle.Flex;

        root.Q<Button>("Options")?.Focus();
    }

    #region Audio

    private void InitAudioSliders()
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

        // Busca os valores padrão diretamente do AudioManager
        sliderMaster?.SetValueWithoutNotify(AudioManager.DEFAULT_MASTER);
        sliderMusica?.SetValueWithoutNotify(AudioManager.DEFAULT_MUSIC);
        sliderSons?.SetValueWithoutNotify(AudioManager.DEFAULT_SFX);

        // Aplica no sistema de som
        AudioManager.Instance.SetMasterVolume(AudioManager.DEFAULT_MASTER);
        AudioManager.Instance.SetMusicVolume(AudioManager.DEFAULT_MUSIC);
        AudioManager.Instance.SetSFXVolume(AudioManager.DEFAULT_SFX);

        PlayerPrefs.Save();
    }

    #endregion

    #region Video   

    private void InitVideoSettings()
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

    #endregion

    #region Colors

    private void InitColorSettings()
    {
        if (SignalColorManager.Instance == null)
            return;

        if (dropdownPaleta != null)
        {
            RefreshPaletteDropdown();

            dropdownPaleta.RegisterValueChangedCallback(evt =>
            {
                string localizedCustomName = SignalColorManager.GetLocalizedPaletteName(SignalColorManager.CUSTOM_INDEX);

                if (evt.newValue == localizedCustomName || isApplyingPreset)
                    return;

                int selectedIndex = -1;
                for (int i = 0; i < SignalColorManager.Palettes.Length; i++)
                {
                    if (evt.newValue == SignalColorManager.GetLocalizedPaletteName(i))
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                if (selectedIndex >= 0)
                {
                    isApplyingPreset = true;

                    SignalColorManager.Instance.ApplyPalette(selectedIndex);
                    SyncColorUI();

                    dropdownPaleta.SetValueWithoutNotify(evt.newValue);

                    isApplyingPreset = false;
                }
            });
        }

        GenerateSwatches(containerSwatchesJ, swatchesJ, SignalType.J);
        GenerateSwatches(containerSwatchesK, swatchesK, SignalType.K);
        GenerateSwatches(containerSwatchesCLK, swatchesCLK, SignalType.CLK);
        GenerateSwatches(containerSwatchesPreset, swatchesPreset, SignalType.Preset);
        GenerateSwatches(containerSwatchesClear, swatchesClear, SignalType.Clear);
        GenerateSwatches(containerSwatchesFeedbackSuccess, swatchesFeedbackSuccess, SignalType.FeedbackSuccess);
        GenerateSwatches(containerSwatchesFeedbackFailure, swatchesFeedbackFailure, SignalType.FeedbackFailure);

        SyncColorUI();
    }

    private void RefreshPaletteDropdown()
    {
        if (dropdownPaleta == null || SignalColorManager.Instance == null)
            return;

        string localizedCustomName = SignalColorManager.GetLocalizedPaletteName(SignalColorManager.CUSTOM_INDEX);

        List<string> choices = new List<string>();
        for (int i = 0; i < SignalColorManager.Palettes.Length; i++)
        {
            choices.Add(SignalColorManager.GetLocalizedPaletteName(i));
        }
        choices.Add(localizedCustomName);

        dropdownPaleta.choices = choices;

        int activePaletteIndex = SignalColorManager.Instance.GetCurrentPaletteIndex();
        if (activePaletteIndex >= 0)
        {
            dropdownPaleta.SetValueWithoutNotify(SignalColorManager.GetLocalizedPaletteName(activePaletteIndex));
        }
        else
        {
            dropdownPaleta.SetValueWithoutNotify(localizedCustomName);
        }
    }

    /// <summary>
    /// Gera botões de cores pré-definidas para um sinal.
    /// </summary>
    private void GenerateSwatches(
        VisualElement container,
        List<Button> swatches,
        SignalType signalType)
    {
        if (container == null)
            return;

        container.Clear();
        swatches.Clear();

        for (int i = 0; i < SignalColorManager.PresetColors.Length; i++)
        {
            int colorIndex = i;
            Color color = SignalColorManager.PresetColors[i];

            Button swatch = new();

            swatch.style.width = 30;
            swatch.style.height = 30;
            swatch.style.minWidth = 30;
            swatch.style.minHeight = 30;
            swatch.style.maxWidth = 30;
            swatch.style.maxHeight = 30;
            swatch.style.flexShrink = 0;

            swatch.style.paddingTop = 0;
            swatch.style.paddingRight = 0;
            swatch.style.paddingBottom = 0;
            swatch.style.paddingLeft = 0;

            swatch.style.marginRight = 5;
            swatch.style.marginBottom = 5;

            swatch.style.backgroundColor = color;

            swatch.style.borderTopWidth = 2;
            swatch.style.borderBottomWidth = 2;
            swatch.style.borderLeftWidth = 2;
            swatch.style.borderRightWidth = 2;

            swatch.clicked += () =>
            {
                SignalColorManager.Instance.SetAndNotify(signalType.ToString(), colorIndex);
                SyncColorUI();
            };

            container.Add(swatch);
            swatches.Add(swatch);
        }
    }

    /// <summary>
    /// Sincroniza toda a UI de cores com o estado atual do SignalColorManager.
    /// </summary>
    private void SyncColorUI()
    {
        if (SignalColorManager.Instance == null)
            return;

        // 1. Atualiza os quadradinhos de preview com as cores do Manager
        previewJ.style.backgroundColor = SignalColorManager.Instance.ColorJ;
        previewK.style.backgroundColor = SignalColorManager.Instance.ColorK;
        previewCLK.style.backgroundColor = SignalColorManager.Instance.ColorCLK;
        previewPreset.style.backgroundColor = SignalColorManager.Instance.ColorPreset;
        previewClear.style.backgroundColor = SignalColorManager.Instance.ColorClear;
        previewFeedbackSuccess.style.backgroundColor = SignalColorManager.Instance.ColorFeedbackSuccess;
        previewFeedbackFailure.style.backgroundColor = SignalColorManager.Instance.ColorFeedbackFailure;

        // 2. Sincroniza as bordas brancas dos seletores baseando-se nos índices puros
        UpdateSwatchBorders(swatchesJ, SignalColorManager.Instance.IndexJ);
        UpdateSwatchBorders(swatchesK, SignalColorManager.Instance.IndexK);
        UpdateSwatchBorders(swatchesCLK, SignalColorManager.Instance.IndexCLK);
        UpdateSwatchBorders(swatchesPreset, SignalColorManager.Instance.IndexPreset);
        UpdateSwatchBorders(swatchesClear, SignalColorManager.Instance.IndexClear);
        UpdateSwatchBorders(swatchesFeedbackSuccess, SignalColorManager.Instance.IndexFeedbackSuccess);
        UpdateSwatchBorders(swatchesFeedbackFailure, SignalColorManager.Instance.IndexFeedbackFailure);

        // 3. Obtém qual paleta está ativa diretamente da regra de negócio
        int activePaletteIndex = SignalColorManager.Instance.GetCurrentPaletteIndex();

        if (activePaletteIndex >= 0)
        {
            dropdownPaleta?.SetValueWithoutNotify(SignalColorManager.GetLocalizedPaletteName(activePaletteIndex));
        }
        else if (!isApplyingPreset)
        {
            dropdownPaleta?.SetValueWithoutNotify(SignalColorManager.GetLocalizedPaletteName(SignalColorManager.CUSTOM_INDEX));
        }
    }

    /// <summary>
    /// Atualiza a borda do swatch selecionado.
    /// </summary>
    private void UpdateSwatchBorders(List<Button> swatches, int selectedIndex)
    {
        for (int i = 0; i < swatches.Count; i++)
        {
            Color borderColor = i == selectedIndex ? Color.white : Color.clear;

            swatches[i].style.borderTopColor = borderColor;
            swatches[i].style.borderBottomColor = borderColor;
            swatches[i].style.borderLeftColor = borderColor;
            swatches[i].style.borderRightColor = borderColor;
        }
    }

    private void ResetDefaultColors()
    {
        if (SignalColorManager.Instance == null) return;

        // Aplica a Paleta 0 (geralmente a paleta padrão/original do jogo)
        SignalColorManager.Instance.ApplyPalette(0);

        // Sincroniza as bordas brancas, as cores dos quadradinhos e o dropdown
        SyncColorUI();

        PlayerPrefs.Save(); // Salva a mudança imediatamente
    }

    #endregion

    #region RGB Overlay

    private void OpenCustomJ() => OpenRGBOverlay(SignalType.J);
    private void OpenCustomK() => OpenRGBOverlay(SignalType.K);
    private void OpenCustomCLK() => OpenRGBOverlay(SignalType.CLK);
    private void OpenCustomPreset() => OpenRGBOverlay(SignalType.Preset);
    private void OpenCustomClear() => OpenRGBOverlay(SignalType.Clear);
    private void OpenCustomFeedbackSuccess() => OpenRGBOverlay(SignalType.FeedbackSuccess);
    private void OpenCustomFeedbackFailure() => OpenRGBOverlay(SignalType.FeedbackFailure);

    private void OpenRGBOverlay(SignalType signalType)
    {
        if (SignalColorManager.Instance == null)
            return;

        activeCustomSignal = signalType;

        if (rgbTitle != null)
            rgbTitle.text = $"Cor Personalizada: {signalType}";

        Color currentColor = signalType switch
        {
            SignalType.J => SignalColorManager.Instance.ColorJ,
            SignalType.K => SignalColorManager.Instance.ColorK,
            SignalType.CLK => SignalColorManager.Instance.ColorCLK,
            SignalType.Preset => SignalColorManager.Instance.ColorPreset,
            SignalType.Clear => SignalColorManager.Instance.ColorClear,
            SignalType.FeedbackSuccess => SignalColorManager.Instance.ColorFeedbackSuccess,
            SignalType.FeedbackFailure => SignalColorManager.Instance.ColorFeedbackFailure,
            _ => Color.white
        };

        sliderR?.SetValueWithoutNotify(Mathf.RoundToInt(currentColor.r * 255f));
        sliderG?.SetValueWithoutNotify(Mathf.RoundToInt(currentColor.g * 255f));
        sliderB?.SetValueWithoutNotify(Mathf.RoundToInt(currentColor.b * 255f));

        UpdateRGBPreview();

        if (rgbOverlay != null)
            rgbOverlay.style.display = DisplayStyle.Flex;
    }

    private void CloseRGBOverlay()
    {
        if (rgbOverlay != null)
            rgbOverlay.style.display = DisplayStyle.None;
    }

    private void OnRGBSliderChanged(ChangeEvent<float> evt) => UpdateRGBPreview();

    private void UpdateRGBPreview()
    {
        float r = (sliderR?.value ?? 255f) / 255f;
        float g = (sliderG?.value ?? 255f) / 255f;
        float b = (sliderB?.value ?? 255f) / 255f;

        Color color = new(r, g, b);

        if (rgbPreview != null)
            rgbPreview.style.backgroundColor = color;

        inputHex?.SetValueWithoutNotify("#" + ColorUtility.ToHtmlStringRGB(color));
    }

    private void OnHexInputChanged(ChangeEvent<string> evt)
    {
        string hex = evt.newValue.Trim();

        if (!hex.StartsWith("#"))
            hex = "#" + hex;

        if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            return;

        sliderR?.SetValueWithoutNotify(Mathf.RoundToInt(color.r * 255f));
        sliderG?.SetValueWithoutNotify(Mathf.RoundToInt(color.g * 255f));
        sliderB?.SetValueWithoutNotify(Mathf.RoundToInt(color.b * 255f));

        if (rgbPreview != null)
            rgbPreview.style.backgroundColor = color;
    }

    private void ConfirmRGBColor()
    {
        if (SignalColorManager.Instance == null)
            return;

        float r = (sliderR?.value ?? 255f) / 255f;
        float g = (sliderG?.value ?? 255f) / 255f;
        float b = (sliderB?.value ?? 255f) / 255f;

        SignalColorManager.Instance.SetCustomColor(activeCustomSignal.ToString(), new Color(r, g, b));

        SyncColorUI();
        CloseRGBOverlay();
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}

    #endregion