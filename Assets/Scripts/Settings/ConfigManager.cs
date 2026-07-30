using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Orquestra as abas do menu de configurações.
/// Responsável apenas por: cachear os 4 painéis, trocar de aba (ShowTab),
/// voltar ao menu e delegar toda a lógica específica para os módulos de settings.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class ConfigManager : MonoBehaviour
{
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

    // Módulos delegados
    private readonly List<ISettingsTab> tabs = new();
    private AudioSettingsTab audioTab;
    private VideoSettingsTab videoTab;
    private ColorSettingsTab colorTab;

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

        CachePanels();
        InitTabs();
        RegisterCallbacks();

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
        foreach (var tab in tabs)
            tab.OnLocaleChanged();
    }

    /// <summary>
    /// Cacheia apenas os elementos de navegação (botões de aba e painéis).
    /// </summary>
    private void CachePanels()
    {
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
    }

    /// <summary>
    /// Cria e inicializa os módulos delegados.
    /// </summary>
    private void InitTabs()
    {
        audioTab = new AudioSettingsTab();
        videoTab = new VideoSettingsTab(contrastOverlaySprite);
        colorTab = new ColorSettingsTab();

        tabs.Clear();
        tabs.Add(audioTab);
        tabs.Add(videoTab);
        tabs.Add(colorTab);

        foreach (var tab in tabs)
            tab.Init(root);
    }

    /// <summary>
    /// Registra callbacks de navegação e delega para os módulos.
    /// </summary>
    private void RegisterCallbacks()
    {
        if (btnTabColors != null) btnTabColors.clicked += ShowColorsTab;
        if (btnTabAudio != null) btnTabAudio.clicked += ShowAudioTab;
        if (btnTabVideo != null) btnTabVideo.clicked += ShowVideoTab;
        if (btnTabControls != null) btnTabControls.clicked += ShowControlsTab;
        if (btnBack != null) btnBack.clicked += OnBackClicked;

        foreach (var tab in tabs)
            tab.RegisterCallbacks();
    }

    /// <summary>
    /// Remove callbacks de navegação e dos módulos.
    /// </summary>
    private void UnregisterCallbacks()
    {
        if (btnTabColors != null) btnTabColors.clicked -= ShowColorsTab;
        if (btnTabAudio != null) btnTabAudio.clicked -= ShowAudioTab;
        if (btnTabVideo != null) btnTabVideo.clicked -= ShowVideoTab;
        if (btnTabControls != null) btnTabControls.clicked -= ShowControlsTab;
        if (btnBack != null) btnBack.clicked -= OnBackClicked;

        foreach (var tab in tabs)
            tab.UnregisterCallbacks();
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

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}