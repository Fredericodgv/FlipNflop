using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Gerencia o menu de pausa do jogo.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class GameMenuManager : MonoBehaviour
{
    [Header("Configurações da Cena")]
    [Tooltip("Nome da cena carregada ao sair do jogo.")]
    [SerializeField] private string mainMenuSceneName = "MenuWeb";

    [Header("Controles do Jogador")]
    [Tooltip("PlayerInput utilizado para bloquear ações durante a pausa.")]
    [SerializeField] private PlayerInput playerInput;

    [Tooltip("Ação utilizada para abrir e fechar o menu.")]
    [SerializeField] private InputActionReference pauseAction;

    private UIDocument uiDocument;

    private VisualElement gameMenuOverlay;
    private VisualElement panelMain;
    private VisualElement panelOptions;

    private Button menuHudButton;
    private Button continueButton;
    private Button retryButton;
    private Button optionsButton;
    private Button exitButton;

    /// <summary>
    /// Indica se o menu está aberto.
    /// </summary>
    public bool IsMenuOpen =>
        gameMenuOverlay != null &&
        gameMenuOverlay.style.display == DisplayStyle.Flex;

    /// <summary>
    /// Inicializa referências do componente.
    /// </summary>
    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    /// <summary>
    /// Inicializa a interface e registra callbacks.
    /// </summary>
    private void OnEnable()
    {
        if (uiDocument == null)
            return;

        CacheUIElements();
        ConfigureInitialState();
        RegisterCallbacks();
    }

    /// <summary>
    /// Remove callbacks registrados.
    /// </summary>
    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    /// <summary>
    /// Busca elementos da interface.
    /// </summary>
    private void CacheUIElements()
    {
        VisualElement root = uiDocument.rootVisualElement;

        menuHudButton = root.Q<Button>("MenuButton");

        gameMenuOverlay = root.Q<VisualElement>("GameMenu");
        panelMain = root.Q<VisualElement>("PanelMain");
        panelOptions = root.Q<VisualElement>("PanelOptions");

        continueButton = root.Q<Button>("Continue");
        retryButton = root.Q<Button>("Retry");
        optionsButton = root.Q<Button>("Options");
        exitButton = root.Q<Button>("Exit");
    }

    /// <summary>
    /// Configura o estado inicial da interface.
    /// </summary>
    private void ConfigureInitialState()
    {
        if (gameMenuOverlay != null)
            gameMenuOverlay.style.display = DisplayStyle.None;

        if (panelOptions != null)
            panelOptions.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Registra callbacks da interface e input.
    /// </summary>
    private void RegisterCallbacks()
    {
        if (menuHudButton != null)
            menuHudButton.clicked += OpenMenu;

        if (continueButton != null)
            continueButton.clicked += ContinueGame;

        if (retryButton != null)
            retryButton.clicked += RestartLevel;

        if (optionsButton != null)
            optionsButton.clicked += OpenOptions;

        if (exitButton != null)
            exitButton.clicked += ExitToMainMenu;

        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
        }
    }

    /// <summary>
    /// Remove callbacks registrados.
    /// </summary>
    private void UnregisterCallbacks()
    {
        if (menuHudButton != null)
            menuHudButton.clicked -= OpenMenu;

        if (continueButton != null)
            continueButton.clicked -= ContinueGame;

        if (retryButton != null)
            retryButton.clicked -= RestartLevel;

        if (optionsButton != null)
            optionsButton.clicked -= OpenOptions;

        if (exitButton != null)
            exitButton.clicked -= ExitToMainMenu;

        if (pauseAction != null && pauseAction.action != null)
            pauseAction.action.performed -= OnPausePerformed;
    }

    /// <summary>
    /// Alterna entre pausa e continuação do jogo.
    /// </summary>
    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (IsMenuOpen)
            ContinueGame();
        else
            OpenMenu();
    }

    /// <summary>
    /// Abre o menu de pausa.
    /// </summary>
    public void OpenMenu()
    {
        Time.timeScale = 0f;

        DisablePlayerInput();

        if (menuHudButton != null)
            menuHudButton.style.display = DisplayStyle.None;

        if (gameMenuOverlay != null)
            gameMenuOverlay.style.display = DisplayStyle.Flex;

        if (panelMain != null)
            panelMain.style.display = DisplayStyle.Flex;

        if (panelOptions != null)
            panelOptions.style.display = DisplayStyle.None;

        if (continueButton != null)
            continueButton.Focus();
    }

    /// <summary>
    /// Fecha o menu e retorna ao jogo.
    /// </summary>
    public void ContinueGame()
    {
        Time.timeScale = 1f;

        EnablePlayerInput();

        if (gameMenuOverlay != null)
            gameMenuOverlay.style.display = DisplayStyle.None;

        if (menuHudButton != null)
            menuHudButton.style.display = DisplayStyle.Flex;

        if (gameMenuOverlay != null)
            gameMenuOverlay.Blur();
    }

    /// <summary>
    /// Reinicia a cena atual.
    /// </summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;

        EnablePlayerInput();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Retorna ao menu principal.
    /// </summary>
    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;

        EnablePlayerInput();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Exibe o painel de configurações.
    /// </summary>
    public void OpenOptions()
    {
        if (panelMain != null)
            panelMain.style.display = DisplayStyle.None;

        if (panelOptions != null)
            panelOptions.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Desativa inputs do jogador.
    /// </summary>
    private void DisablePlayerInput()
    {
        if (playerInput == null || playerInput.actions == null)
            return;

        InputActionMap playerMap = playerInput.actions.FindActionMap("Player");

        if (playerMap != null)
            playerMap.Disable();
    }

    /// <summary>
    /// Reativa inputs do jogador.
    /// </summary>
    private void EnablePlayerInput()
    {
        if (playerInput == null || playerInput.actions == null)
            return;

        InputActionMap playerMap = playerInput.actions.FindActionMap("Player");

        if (playerMap != null)
            playerMap.Enable();
    }
}