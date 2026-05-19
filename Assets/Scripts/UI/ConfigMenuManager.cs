using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ConfigMenuManager : MonoBehaviour
{
    public static ConfigMenuManager Instance { get; private set; }

    [Header("Configurações Gerais")]
    [SerializeField] private string nomeMenuInicial;
    [SerializeField] private InputActionAsset inputActionAsset;

    // --- REFERÊNCIAS DO UI TOOLKIT ---
    private UIDocument _uiDocument;
    private VisualElement _mainConfigMenu; // O contêiner pai de tudo (se houver)

    // Painéis
    private VisualElement _mainPanel;
    private VisualElement _optionsPanel;
    private VisualElement _colorPanel;
    private VisualElement _audioPanel;
    private VisualElement _videoPanel;
    private VisualElement _controlsPanel;

    // Botões do PanelMain
    private Button _btnContinue;
    private Button _btnRetry;
    private Button _btnOptions;
    private Button _btnExit;

    #region Inicialização

    // --- FUNÇÕES PÚBLICAS PARA OUTROS SCRIPTS ---

    // Verifica se a raiz do UI Toolkit está visível
    public bool IsMenuOpen => _uiDocument != null && _uiDocument.rootVisualElement.style.display == DisplayStyle.Flex;

    // Função chamada pelo seu outro botão na tela do jogo para abrir as configurações
    public void OpenMenuConfig()
    {
        Time.timeScale = 0f;
        // Mostra o contêiner principal do UI Toolkit
        _uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        // Garante que a primeira aba a aparecer seja o painel principal
        ShowPage(_mainPanel);
    }

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

    private void OnEnable()
    {
        // 1. Pegar o UIDocument
        _uiDocument = GetComponent<UIDocument>();
        var root = _uiDocument.rootVisualElement;

        // 2. Buscar os Painéis pelo nome dado no UI Builder (com '#' não é necessário, apenas o nome)
        _mainPanel = root.Q<VisualElement>("PanelMain");
        _optionsPanel = root.Q<VisualElement>("PanelOptions");
        _videoPanel = root.Q<VisualElement>("PanelVideo");
        // Adicione a busca para colorPanel, audioPanel e controlsPanel quando criá-los...

        // 3. Buscar os botões da tela principal
        _btnContinue = root.Q<Button>("Continue");
        _btnRetry = root.Q<Button>("Retry");
        _btnOptions = root.Q<Button>("Options");
        _btnExit = root.Q<Button>("Exit");

        // 4. Inscrever os eventos de clique
        if (_btnContinue != null) _btnContinue.clicked += ContinueGame;
        if (_btnRetry != null) _btnRetry.clicked += RestartLevel;
        if (_btnOptions != null) _btnOptions.clicked += OpenOptions;
        if (_btnExit != null) _btnExit.clicked += BackInitialMenu; // Ou Application.Quit()

        // 5. Inicializar sistemas antigos (Deixe comentado até criarmos a UI deles)
        // InitAudioSliders();
        // InitDropdownPaleta();
        // InitVideoSettings();
        // LoadBindings();

        // 6. Começar mostrando apenas o painel principal
        ShowPage(_mainPanel);
    }

    private void OnDisable()
    {
        // Limpar eventos para evitar memory leaks
        if (_btnContinue != null) _btnContinue.clicked -= ContinueGame;
        if (_btnRetry != null) _btnRetry.clicked -= RestartLevel;
        if (_btnOptions != null) _btnOptions.clicked -= OpenOptions;
        if (_btnExit != null) _btnExit.clicked -= BackInitialMenu;
    }

    #endregion

    #region Navegação entre Painéis (Versão UI Toolkit)

    private void ShowPage(VisualElement page)
    {
        // Esconde todos
        if (_mainPanel != null) _mainPanel.style.display = DisplayStyle.None;
        if (_optionsPanel != null) _optionsPanel.style.display = DisplayStyle.None;
        if (_colorPanel != null) _colorPanel.style.display = DisplayStyle.None;
        if (_audioPanel != null) _audioPanel.style.display = DisplayStyle.None;
        if (_videoPanel != null) _videoPanel.style.display = DisplayStyle.None;
        if (_controlsPanel != null) _controlsPanel.style.display = DisplayStyle.None;

        // Mostra apenas o que foi solicitado
        if (page != null) page.style.display = DisplayStyle.Flex;
    }

    public void OpenOptions() => ShowPage(_optionsPanel);
    public void BackToMain() => ShowPage(_mainPanel);

    // As outras funções (OpenAudio, OpenVideo, etc) farão exatamente a mesma coisa usando ShowPage()

    #endregion

    #region PanelMain (Lógica do Jogo)

    public void ContinueGame()
    {
        Time.timeScale = 1f;
        // Esconde o menu de configurações
        _uiDocument.rootVisualElement.style.display = DisplayStyle.None;

        // BUSCA A HUD NA CENA E MANDA ELA REAPARECER
        // (Substituindo o antigo buttonConfig.SetActive(true))
        var hud = FindAnyObjectByType<ButtonConfigMenuManager>();
        if (hud != null)
        {
            hud.ShowHUD();
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackInitialMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeMenuInicial);
    }

    #endregion
}
// ==============================================================================
// O RESTO DO SEU CÓDIGO (CORES, ÁUDIO, VÍDEO, CONTROLES) CONTINUA AQUI PARA BAIXO.
// MAS MANTENHA AS FUNÇÕES DE INIT COMENTADAS LÁ EM CIMA POR ENQUANTO.
// ==============================================================================


//     #region PanelColors — Cores

//     [Header("PanelColors — Paleta de Acessibilidade")]
//     [SerializeField] private TMP_Dropdown dropdownPaleta;

//     [Header("PanelColors — Swatches de Cor")]
//     [SerializeField] private CanvasButton[] swatchesJ;     // <-- Mudei de Button para CanvasButton
//     [SerializeField] private CanvasButton[] swatchesK;     // <-- Mudei de Button para CanvasButton
//     [SerializeField] private CanvasButton[] swatchesCLK;   // <-- Mudei de Button para CanvasButton

//     // E não se esqueça dos botões customizados logo abaixo:
//     [SerializeField] private CanvasButton btnCustomJ;      // <-- Mudei de Button para CanvasButton
//     [SerializeField] private CanvasButton btnCustomK;      // <-- Mudei de Button para CanvasButton
//     [SerializeField] private CanvasButton btnCustomCLK;    // <-- Mudei de Button para CanvasButton
//     [SerializeField] private float swatchSelectedBorder = 3f;

//     [Header("PanelColors — Cor Customizada (Overlay RGB)")]
//     [Tooltip("Botão '+' ao lado dos swatches de J")]
//     [SerializeField] private Button btnCustomJ;
//     [Tooltip("Botão '+' ao lado dos swatches de K")]
//     [SerializeField] private Button btnCustomK;
//     [Tooltip("Botão '+' ao lado dos swatches de CLK")]
//     [SerializeField] private Button btnCustomCLK;

//     [Tooltip("Overlay escuro que aparece por cima do painel")]
//     [SerializeField] private GameObject rgbOverlay;
//     [Tooltip("Texto do título do overlay (ex: 'Cor personalizada J')")]
//     [SerializeField] private TMP_Text rgbOverlayTitle;
//     [Tooltip("Slider do canal R (0–255)")]
//     [SerializeField] private Slider sliderR;
//     [Tooltip("Slider do canal G (0–255)")]
//     [SerializeField] private Slider sliderG;
//     [Tooltip("Slider do canal B (0–255)")]
//     [SerializeField] private Slider sliderB;
//     [Tooltip("Texto que exibe o valor do canal R")]
//     [SerializeField] private TMP_Text textR;
//     [Tooltip("Texto que exibe o valor do canal G")]
//     [SerializeField] private TMP_Text textG;
//     [Tooltip("Texto que exibe o valor do canal B")]
//     [SerializeField] private TMP_Text textB;
//     [Tooltip("Image de prévia da cor construída pelos sliders")]
//     [SerializeField] private Image rgbPreviewImage;
//     [Tooltip("Texto que exibe o HEX da cor atual")]
//     [SerializeField] private TMP_Text rgbHexText;
//     [Tooltip("Campo de texto para digitar/colar HEX da cor")]
//     [SerializeField] private TMP_InputField inputHex;

//     private string _activeCustomSignal = "J";

//     public void ChangeLanguage(int index)
//     {
//         string idioma = index == 0 ? "Português" : "Inglês";
//         PlayerPrefs.SetString("Idioma", idioma);
//     }

//     // ── Dropdown ─────────────────────────────────────────────────────────────

//     private void InitDropdownPaleta()
//     {
//         if (dropdownPaleta == null) return;
//         dropdownPaleta.ClearOptions();
//         dropdownPaleta.AddOptions(new List<string>(SignalColorManager.PaletteNames));
//         dropdownPaleta.SetValueWithoutNotify(0);
//         dropdownPaleta.onValueChanged.AddListener(OnDropdownPaletaChanged);
//     }

//     private void OnDropdownPaletaChanged(int index)
//     {
//         SignalColorManager.Instance?.ApplyPalette(index);
//         SyncSwatchSelection();
//     }

//     private void SyncDropdownPaleta()
//     {
//         if (dropdownPaleta == null || SignalColorManager.Instance == null) return;
//         int j = SignalColorManager.Instance.IndexJ;
//         int k = SignalColorManager.Instance.IndexK;
//         int clk = SignalColorManager.Instance.IndexCLK;
//         for (int i = 0; i < SignalColorManager.Palettes.Length; i++)
//         {
//             var p = SignalColorManager.Palettes[i];
//             if (p[0] == j && p[1] == k && p[2] == clk)
//             {
//                 dropdownPaleta.SetValueWithoutNotify(i);
//                 return;
//             }
//         }
//     }

//     public void ApplyColorPalette(int index)
//     {
//         SignalColorManager.Instance?.ApplyPalette(index);
//         SyncSwatchSelection();
//         SyncDropdownPaleta();
//     }

//     public void RestoreDefaultColors()
//     {
//         SignalColorManager.Instance?.RestoreDefaultColors();
//         SyncSwatchSelection();
//         SyncDropdownPaleta();
//     }

//     // ── Swatches ─────────────────────────────────────────────────────────────

//     private void InitSwatches()
//     {
//         if (SignalColorManager.Instance == null) return;
//         RegisterSwatchGroup(swatchesJ, "J");
//         RegisterSwatchGroup(swatchesK, "K");
//         RegisterSwatchGroup(swatchesCLK, "CLK");
//         PaintSwatchButtons(swatchesJ);
//         PaintSwatchButtons(swatchesK);
//         PaintSwatchButtons(swatchesCLK);
//         SyncSwatchSelection();
//     }

//     private void RegisterSwatchGroup(Button[] swatches, string signal)
//     {
//         if (swatches == null) return;
//         for (int i = 0; i < swatches.Length; i++)
//         {
//             if (swatches[i] == null) continue;
//             int capturedIndex = i;
//             swatches[i].onClick.RemoveAllListeners();
//             swatches[i].onClick.AddListener(() =>
//             {
//                 SignalColorManager.Instance?.SetAndNotify(signal, capturedIndex);
//                 SyncSwatchSelection();
//                 SyncDropdownPaleta();
//             });
//         }
//     }

//     private void PaintSwatchButtons(Button[] swatches)
//     {
//         if (swatches == null) return;
//         for (int i = 0; i < swatches.Length && i < SignalColorManager.PresetColors.Length; i++)
//         {
//             if (swatches[i] == null) continue;
//             var img = swatches[i].GetComponent<Image>();
//             if (img != null) img.color = SignalColorManager.PresetColors[i];
//         }
//     }

//     private void SyncSwatchSelection()
//     {
//         if (SignalColorManager.Instance == null) return;
//         UpdateSwatchOutlines(swatchesJ, SignalColorManager.Instance.IndexJ);
//         UpdateSwatchOutlines(swatchesK, SignalColorManager.Instance.IndexK);
//         UpdateSwatchOutlines(swatchesCLK, SignalColorManager.Instance.IndexCLK);
//         if (previewJ != null) previewJ.color = SignalColorManager.Instance.ColorJ;
//         if (previewK != null) previewK.color = SignalColorManager.Instance.ColorK;
//         if (previewCLK != null) previewCLK.color = SignalColorManager.Instance.ColorCLK;
//     }

//     private void UpdateSwatchOutlines(Button[] swatches, int selectedIndex)
//     {
//         if (swatches == null) return;
//         for (int i = 0; i < swatches.Length; i++)
//         {
//             if (swatches[i] == null) continue;
//             var outline = swatches[i].GetComponent<Outline>();
//             if (outline == null) continue;
//             outline.enabled = (i == selectedIndex);
//             outline.effectDistance = new Vector2(swatchSelectedBorder, -swatchSelectedBorder);
//             outline.effectColor = Color.white;
//         }
//     }

//     // ── Botões de cor customizada ─────────────────────────────────────────────

//     private void InitCustomColorButtons()
//     {
//         if (inputHex != null)
//             inputHex.onEndEdit.AddListener(OnHexInputChanged);
//         if (btnCustomJ != null) { btnCustomJ.onClick.RemoveAllListeners(); btnCustomJ.onClick.AddListener(() => OpenRGBOverlay("J")); }
//         if (btnCustomK != null) { btnCustomK.onClick.RemoveAllListeners(); btnCustomK.onClick.AddListener(() => OpenRGBOverlay("K")); }
//         if (btnCustomCLK != null) { btnCustomCLK.onClick.RemoveAllListeners(); btnCustomCLK.onClick.AddListener(() => OpenRGBOverlay("CLK")); }

//         // Configura range dos sliders
//         foreach (var sl in new[] { sliderR, sliderG, sliderB })
//         {
//             if (sl == null) continue;
//             sl.minValue = 0; sl.maxValue = 255; sl.wholeNumbers = true;
//         }

//         if (sliderR != null) sliderR.onValueChanged.AddListener(_ => OnRGBSliderChanged());
//         if (sliderG != null) sliderG.onValueChanged.AddListener(_ => OnRGBSliderChanged());
//         if (sliderB != null) sliderB.onValueChanged.AddListener(_ => OnRGBSliderChanged());

//         if (rgbOverlay != null) rgbOverlay.SetActive(false);
//     }

//     public void OpenRGBOverlay(string signal)
//     {
//         _activeCustomSignal = signal;

//         if (rgbOverlayTitle != null)
//             rgbOverlayTitle.text = $"Cor personalizada {signal}";

//         // Pré-carrega a cor atual do sinal
//         Color current = signal switch
//         {
//             "J" => SignalColorManager.Instance?.ColorJ ?? Color.white,
//             "K" => SignalColorManager.Instance?.ColorK ?? Color.white,
//             "CLK" => SignalColorManager.Instance?.ColorCLK ?? Color.white,
//             _ => Color.white
//         };

//         sliderR?.SetValueWithoutNotify(Mathf.RoundToInt(current.r * 255f));
//         sliderG?.SetValueWithoutNotify(Mathf.RoundToInt(current.g * 255f));
//         sliderB?.SetValueWithoutNotify(Mathf.RoundToInt(current.b * 255f));

//         UpdateRGBPreview();

//         if (rgbOverlay != null) rgbOverlay.SetActive(true);
//     }

//     private void OnRGBSliderChanged() => UpdateRGBPreview();

//     private void OnHexInputChanged(string hex)
//     {
//         string cleaned = hex.Trim();
//         if (!cleaned.StartsWith("#")) cleaned = "#" + cleaned;

//         if (ColorUtility.TryParseHtmlString(cleaned, out Color c))
//         {
//             sliderR?.SetValueWithoutNotify(Mathf.RoundToInt(c.r * 255f));
//             sliderG?.SetValueWithoutNotify(Mathf.RoundToInt(c.g * 255f));
//             sliderB?.SetValueWithoutNotify(Mathf.RoundToInt(c.b * 255f));
//             UpdateRGBPreview();
//         }
//         else
//         {
//             // HEX inválido — restaura o texto com a cor atual
//             if (rgbHexText != null) inputHex.SetTextWithoutNotify(rgbHexText.text);
//         }
//     }

//     private void UpdateRGBPreview()
//     {
//         float r = (sliderR?.value ?? 255f) / 255f;
//         float g = (sliderG?.value ?? 255f) / 255f;
//         float b = (sliderB?.value ?? 255f) / 255f;

//         Color c = new Color(r, g, b);

//         if (rgbPreviewImage != null) rgbPreviewImage.color = c;

//         if (textR != null) textR.text = Mathf.RoundToInt(r * 255f).ToString();
//         if (textG != null) textG.text = Mathf.RoundToInt(g * 255f).ToString();
//         if (textB != null) textB.text = Mathf.RoundToInt(b * 255f).ToString();

//         if (rgbHexText != null)
//         {
//             string hex = "#" + ColorUtility.ToHtmlStringRGB(c);
//             rgbHexText.text = hex;
//             inputHex?.SetTextWithoutNotify(hex);
//         }
//     }

//     public void ConfirmRGBColor()
//     {
//         float r = (sliderR?.value ?? 255f) / 255f;
//         float g = (sliderG?.value ?? 255f) / 255f;
//         float b = (sliderB?.value ?? 255f) / 255f;

//         SignalColorManager.Instance?.SetCustomColor(_activeCustomSignal, new Color(r, g, b));
//         SyncSwatchSelection();
//         SyncDropdownPaleta();

//         if (rgbOverlay != null) rgbOverlay.SetActive(false);
//     }

//     public void CancelRGBColor()
//     {
//         if (rgbOverlay != null) rgbOverlay.SetActive(false);
//     }

//     #endregion

//     #region PanelAudio

//     [Header("PanelAudio — Sliders")]
//     [SerializeField] private Slider sliderMaster;
//     [SerializeField] private Slider sliderSons;
//     [SerializeField] private Slider sliderMusica;

//     private void InitAudioSliders()
//     {
//         if (AudioManager.Instance == null) return;
//         if (sliderMaster != null) { sliderMaster.SetValueWithoutNotify(AudioManager.Instance.GetMasterVolume()); sliderMaster.onValueChanged.AddListener(val => AudioManager.Instance.SetMasterVolume(val)); }
//         if (sliderSons != null) { sliderSons.SetValueWithoutNotify(AudioManager.Instance.GetSFXVolume()); sliderSons.onValueChanged.AddListener(val => AudioManager.Instance.SetSFXVolume(val)); }
//         if (sliderMusica != null) { sliderMusica.SetValueWithoutNotify(AudioManager.Instance.GetMusicVolume()); sliderMusica.onValueChanged.AddListener(val => AudioManager.Instance.SetMusicVolume(val)); }
//     }

//     private void SyncAudioSliders()
//     {
//         if (AudioManager.Instance == null) return;
//         sliderMaster?.SetValueWithoutNotify(AudioManager.Instance.GetMasterVolume());
//         sliderSons?.SetValueWithoutNotify(AudioManager.Instance.GetSFXVolume());
//         sliderMusica?.SetValueWithoutNotify(AudioManager.Instance.GetMusicVolume());
//     }

//     public void RestoreDefaultAudio()
//     {
//         AudioManager.Instance?.RestoreDefaultVolumes();
//         SyncAudioSliders();
//     }

//     #endregion

//     #region PanelVideo

//     [Header("PanelVideo — Contraste")]
//     [SerializeField] private Slider sliderContrast;
//     [Tooltip("SpriteRenderer do overlay de contraste (entre fundo e jogo)")]
//     [SerializeField] private SpriteRenderer contrastOverlaySprite;
//     private const string CONTRAST_SAVE_KEY = "ContrastValue";

//     private void InitVideoSettings()
//     {
//         if (sliderContrast == null) return;
//         sliderContrast.minValue = -1f;
//         sliderContrast.maxValue = 1f;
//         sliderContrast.wholeNumbers = false;

//         float saved = PlayerPrefs.GetFloat(CONTRAST_SAVE_KEY, 0f);
//         sliderContrast.SetValueWithoutNotify(saved);
//         ApplyContrast(saved);

//         sliderContrast.onValueChanged.AddListener(ApplyContrast);
//     }

//     private void SyncVideoSliders()
//     {
//         if (sliderContrast == null) return;
//         sliderContrast.SetValueWithoutNotify(PlayerPrefs.GetFloat(CONTRAST_SAVE_KEY, 0f));
//     }

//     public void ApplyContrast(float value)
//     {
//         if (contrastOverlaySprite == null) return;

//         // value > 0 -> overlay branco (clareia o fundo)
//         // value < 0 -> overlay preto (escurece o fundo)
//         // value = 0 -> totalmente transparente
//         if (value > 0f)
//             contrastOverlaySprite.color = new Color(1f, 1f, 1f, value);
//         else if (value < 0f)
//             contrastOverlaySprite.color = new Color(0f, 0f, 0f, -value);
//         else
//             contrastOverlaySprite.color = new Color(0f, 0f, 0f, 0f);

//         PlayerPrefs.SetFloat(CONTRAST_SAVE_KEY, value);
//         PlayerPrefs.Save();
//     }

//     public void RestoreDefaultVideo()
//     {
//         if (sliderContrast != null) sliderContrast.SetValueWithoutNotify(0f);
//         ApplyContrast(0f);
//     }

//     #endregion

//     #region PanelControles

//     private void InitRebindButtons()
//     {
//         for (int i = 0; i < rebindEntries.Length; i++)
//         {
//             int capturedIndex = i;
//             var entry = rebindEntries[i];
//             if (entry.rebindButton != null)
//             {
//                 entry.rebindButton.onClick.RemoveAllListeners();
//                 entry.rebindButton.onClick.AddListener(() => StartRebind(capturedIndex));
//             }
//         }
//     }

//     public void StartRebind(int entryIndex)
//     {
//         if (entryIndex < 0 || entryIndex >= rebindEntries.Length) return;
//         if (_currentRebindOp != null) return;
//         var entry = rebindEntries[entryIndex];
//         var action = entry.actionReference?.action;
//         if (action == null) return;
//         action.Disable();
//         entry.displayText.text = "[ ... ]";
//         SetAllRebindButtonsInteractable(false);
//         _currentRebindOp = action
//             .PerformInteractiveRebinding(entry.bindingIndex)
//             .WithControlsExcluding("<Mouse>/position")
//             .WithControlsExcluding("<Mouse>/delta")
//             .WithControlsExcluding("<Mouse>/scroll")
//             .WithCancelingThrough("<Keyboard>/escape")
//             .OnMatchWaitForAnother(0.1f)
//             .OnComplete(op => OnRebindComplete(entry, op))
//             .OnCancel(op => OnRebindCanceled(entry, op))
//             .Start();
//     }

//     private void OnRebindComplete(RebindEntry entry, InputActionRebindingExtensions.RebindingOperation op)
//     {
//         op.Dispose(); _currentRebindOp = null;
//         string newPath = entry.actionReference.action.bindings[entry.bindingIndex].effectivePath;
//         ResolveConflicts(entry, newPath);
//         entry.actionReference.action.Enable();
//         SaveBindings(); UpdateAllKeyTexts(); SetAllRebindButtonsInteractable(true);
//     }

//     private void OnRebindCanceled(RebindEntry entry, InputActionRebindingExtensions.RebindingOperation op)
//     {
//         op.Dispose(); _currentRebindOp = null;
//         entry.actionReference.action.Enable();
//         UpdateAllKeyTexts(); SetAllRebindButtonsInteractable(true);
//     }

//     private void ResolveConflicts(RebindEntry changedEntry, string newPath)
//     {
//         foreach (var other in rebindEntries)
//         {
//             if (other == changedEntry || other.actionReference?.action == null) continue;
//             var otherBinding = other.actionReference.action.bindings[other.bindingIndex];
//             string otherPath = otherBinding.hasOverrides ? otherBinding.overridePath : otherBinding.path;
//             if (otherPath == newPath) other.actionReference.action.ApplyBindingOverride(other.bindingIndex, string.Empty);
//         }
//     }

//     private void UpdateAllKeyTexts()
//     {
//         foreach (var entry in rebindEntries)
//         {
//             if (entry.displayText == null || entry.actionReference?.action == null) continue;
//             var bindings = entry.actionReference.action.bindings;
//             if (entry.bindingIndex >= bindings.Count) continue;
//             var binding = bindings[entry.bindingIndex];
//             string path = binding.hasOverrides ? binding.overridePath : binding.path;
//             entry.displayText.text = string.IsNullOrEmpty(path)
//                 ? "---"
//                 : InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
//         }
//     }

//     private void SetAllRebindButtonsInteractable(bool interactable)
//     {
//         foreach (var entry in rebindEntries)
//             if (entry.rebindButton != null) entry.rebindButton.interactable = interactable;
//     }

//     public void RestoreDefaultControls()
//     {
//         if (_currentRebindOp != null) { _currentRebindOp.Cancel(); return; }
//         if (inputActionAsset != null) { inputActionAsset.RemoveAllBindingOverrides(); PlayerPrefs.DeleteKey(BINDINGS_SAVE_KEY); }
//         UpdateAllKeyTexts();
//     }

//     #endregion

//     #region Restaurar Padrões (Geral)

//     /// <summary>
//     /// Restore all default patterns
//     /// </summary>
//     public void RestoreDefault()
//     {
//         if (inputActionAsset != null) inputActionAsset.RemoveAllBindingOverrides();
//         SignalColorManager.Instance?.RestoreDefaultColors();
//         AudioManager.Instance?.RestoreDefaultVolumes();
//         RestoreDefaultVideo();
//         PlayerPrefs.DeleteAll();
//         SyncSwatchSelection(); SyncDropdownPaleta(); SyncAudioSliders(); UpdateAllKeyTexts();
//     }

//     #endregion

//     #region Salvar / Carregar Bindings

//     private void SaveBindings()
//     {
//         if (inputActionAsset == null) return;
//         PlayerPrefs.SetString(BINDINGS_SAVE_KEY, inputActionAsset.SaveBindingOverridesAsJson());
//         PlayerPrefs.Save();
//     }

//     private void LoadBindings()
//     {
//         if (inputActionAsset == null) return;
//         string json = PlayerPrefs.GetString(BINDINGS_SAVE_KEY, string.Empty);
//         if (!string.IsNullOrEmpty(json)) inputActionAsset.LoadBindingOverridesFromJson(json);
//         UpdateAllKeyTexts();
//     }

//     #endregion
// }