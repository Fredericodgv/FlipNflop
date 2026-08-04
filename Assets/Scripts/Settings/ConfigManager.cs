using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Orchestrates the settings menu tabs and panel navigation.
/// Responsible for caching navigation UI elements, toggling active tab panels, returning to main menu,
/// and delegating tab-specific logic to <see cref="ISettingsTab"/> implementations (<see cref="AudioSettingsTab"/>, <see cref="VideoSettingsTab"/>, <see cref="ColorSettingsTab"/>).
/// Interacts with <see cref="UIDocument"/> and Unity <see cref="LocalizationSettings"/>.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class ConfigManager : MonoBehaviour
{
    #region Inspector Fields

    /// <summary>
    /// Sprite overlay reference used by <see cref="VideoSettingsTab"/> to adjust visual contrast.
    /// </summary>
    [Header("Video")]
    [Tooltip("Overlay used to apply visual contrast.")]
    [SerializeField] private SpriteRenderer contrastOverlaySprite;

    #endregion

    #region Private Fields

    /// <summary>
    /// Reference to the <see cref="UIDocument"/> component attached to this GameObject.
    /// </summary>
    private UIDocument uiDocument;

    /// <summary>
    /// Root visual element container extracted from <see cref="UIDocument"/>.
    /// </summary>
    private VisualElement root;

    /// <summary>Tab navigation button for Colors panel.</summary>
    private Button btnTabColors;

    /// <summary>Tab navigation button for Audio panel.</summary>
    private Button btnTabAudio;

    /// <summary>Tab navigation button for Video panel.</summary>
    private Button btnTabVideo;

    /// <summary>Tab navigation button for Controls panel.</summary>
    private Button btnTabControls;

    /// <summary>Button to navigate back to the main menu.</summary>
    private Button btnBack;

    /// <summary>Visual element container for the Colors settings panel.</summary>
    private VisualElement panelColor;

    /// <summary>Visual element container for the Audio settings panel.</summary>
    private VisualElement panelAudio;

    /// <summary>Visual element container for the Video settings panel.</summary>
    private VisualElement panelVideo;

    /// <summary>Visual element container for the Controls settings panel.</summary>
    private VisualElement panelControls;

    /// <summary>List of all tab panel visual elements for bulk visibility toggling.</summary>
    private readonly List<VisualElement> panels = new();

    /// <summary>List of active <see cref="ISettingsTab"/> module implementations.</summary>
    private readonly List<ISettingsTab> tabs = new();

    /// <summary>Delegated Audio settings tab handler.</summary>
    private AudioSettingsTab audioTab;

    /// <summary>Delegated Video settings tab handler.</summary>
    private VideoSettingsTab videoTab;

    /// <summary>Delegated Colors settings tab handler.</summary>
    private ColorSettingsTab colorTab;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes required component references (<see cref="UIDocument"/>).
    /// </summary>
    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    /// <summary>
    /// Extracts UI root element, caches panels, initializes tab modules, registers UI callbacks,
    /// displays the default Video tab, and subscribes to locale changes via <see cref="LocalizationSettings.SelectedLocaleChanged"/>.
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

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    /// <summary>
    /// Unregisters UI callbacks and unsubscribes from <see cref="LocalizationSettings.SelectedLocaleChanged"/> event to prevent memory leaks.
    /// </summary>
    private void OnDisable()
    {
        UnregisterCallbacks();

        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    #endregion

    #region Localization Event Handlers

    /// <summary>
    /// Callback triggered when the active localization locale is changed.
    /// Notifies all registered <see cref="ISettingsTab"/> modules.
    /// </summary>
    /// <param name="newLocale">The newly selected <see cref="Locale"/>.</param>
    private void OnLocaleChanged(Locale newLocale)
    {
        foreach (var tab in tabs)
            tab.OnLocaleChanged();
    }

    #endregion

    #region UI Initialization & Caching

    /// <summary>
    /// Queries and caches navigation buttons and panel visual elements from UI root.
    /// Queries: "BtnTabColors", "BtnTabAudio", "BtnTabVideo", "BtnTabControls", "BtnBack",
    /// "PanelColor", "PanelAudio", "PanelVideo", "PanelControls".
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
    /// Instantiates delegated settings tab modules (<see cref="AudioSettingsTab"/>, <see cref="VideoSettingsTab"/>, <see cref="ColorSettingsTab"/>)
    /// and initializes them with the root visual element.
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

    #endregion

    #region Callback Registration

    /// <summary>
    /// Registers click event handlers for tab navigation buttons and delegates callback registration to tab modules.
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
    /// Unregisters click event handlers for tab navigation buttons and delegates callback unregistration to tab modules.
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

    #endregion

    #region Navigation & Tab Display

    /// <summary>Shows the Colors settings panel.</summary>
    private void ShowColorsTab() => ShowTab(panelColor);

    /// <summary>Shows the Audio settings panel.</summary>
    private void ShowAudioTab() => ShowTab(panelAudio);

    /// <summary>Shows the Video settings panel.</summary>
    private void ShowVideoTab() => ShowTab(panelVideo);

    /// <summary>Shows the Controls settings panel.</summary>
    private void ShowControlsTab() => ShowTab(panelControls);

    /// <summary>
    /// Hides all tab panels and displays only the specified active panel element.
    /// </summary>
    /// <param name="activePanel">The <see cref="VisualElement"/> panel to show.</param>
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
    /// Navigates back from options menu to the main menu by toggling panel displays ("PanelOptions" and "PanelMain")
    /// and refocusing the Options button.
    /// </summary>
    private void OnBackClicked()
    {
        var panelOptions = root.Q<VisualElement>("PanelOptions");
        var panelMain = root.Q<VisualElement>("PanelMain");

        if (panelOptions != null) panelOptions.style.display = DisplayStyle.None;
        if (panelMain != null) panelMain.style.display = DisplayStyle.Flex;

        root.Q<Button>("Options")?.Focus();
    }

    #endregion

    #region Application Event Handlers

    /// <summary>
    /// Ensures all unsaved <see cref="PlayerPrefs"/> data is saved when the application quits.
    /// </summary>
    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    #endregion
}