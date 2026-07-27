using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Localization.Settings; // <-- Biblioteca de Localização!

[RequireComponent(typeof(UIDocument))]
public class ResultScreenController : MonoBehaviour
{
    public static ResultScreenController Instance { get; private set; }

    [Header("Navegação")]
    [Tooltip("Nome da cena que carrega a fase (a mesma usada por MenuManager.SelectLevelAndLoad).")]
    [SerializeField] private string customLevelName = "Custom";
    [Tooltip("Nome da cena do menu principal.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // O nome da tabela onde estão os nossos textos
    private const string TableName = "UIStrings";

    private VisualElement resultScreen;
    private VisualElement panelResult;
    private Label resultText;
    private Label scoreText;
    private Label timeText;
    private Label hitsText;
    private Label missesText;

    private Button continuButton;
    private Button tryAgainButton;
    private Button menuButton;
    private Button hideButton;

    private bool contentVisible = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        // Paineis
        resultScreen = root.Q<VisualElement>("ResultScreen");
        panelResult = root.Q<VisualElement>("PanelResult");

        // Textos
        resultText = root.Q<Label>("ResultText");
        scoreText = root.Q<Label>("ScoreText");
        timeText = root.Q<Label>("TimeText");
        hitsText = root.Q<Label>("HitsText");
        missesText = root.Q<Label>("MissesText");

        // Botões
        continuButton = root.Q<Button>("ContinuButton");
        tryAgainButton = root.Q<Button>("TryAgainButton");
        menuButton = root.Q<Button>("MenuButton");
        hideButton = root.Q<Button>("HideButton");

        // Callbacks
        if (continuButton != null) continuButton.clicked += OnContinueClicked;
        if (tryAgainButton != null) tryAgainButton.clicked += OnTryAgainClicked;
        if (menuButton != null) menuButton.clicked += OnMenuClicked;
        if (hideButton != null) hideButton.clicked += OnHideClicked;

        Hide();
    }

    private void OnDisable()
    {
        if (continuButton != null) continuButton.clicked -= OnContinueClicked;
        if (tryAgainButton != null) tryAgainButton.clicked -= OnTryAgainClicked;
        if (menuButton != null) menuButton.clicked -= OnMenuClicked;
        if (hideButton != null) hideButton.clicked -= OnHideClicked;
    }

    #region Public API

    public void Show(bool success)
    {
        resultScreen.RemoveFromClassList("hidden");
        contentVisible = true;
        panelResult.style.display = DisplayStyle.Flex;

        // Busca a palavra "Ver Desenho" no idioma atual
        if (hideButton != null)
            hideButton.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "btn_hide_panel");

        bool hasNext = LevelSequenceManager.Levels != null && LevelSequenceManager.HasNextLevel();

        if (continuButton != null) continuButton.style.display = (success && hasNext) ? DisplayStyle.Flex : DisplayStyle.None;
        if (tryAgainButton != null) tryAgainButton.style.display = success ? DisplayStyle.None : DisplayStyle.Flex;

        // Título dinâmico e traduzido
        if (resultText != null)
        {
            string key = success ? "result_correct" : "result_incorrect";
            resultText.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key);
        }
    }

    public void SetResultData(string score, string time)
    {
        string strScore = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "lbl_score");
        string strTime = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, "lbl_time");

        if (scoreText != null) scoreText.text = $"{strScore} {score}";
        if (timeText != null) timeText.text = $"{strTime} {time}";
    }

    public void Hide()
    {
        resultScreen.AddToClassList("hidden");
    }

    #endregion

    #region Button Handlers

    private void OnContinueClicked()
    {
        Time.timeScale = 1f;

        if (LevelSequenceManager.Levels == null || !LevelSequenceManager.HasNextLevel())
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        LevelSequenceManager.CurrentLevelIndex++;
        MenuManager.LevelToLoadJSON = LevelSequenceManager.Levels[LevelSequenceManager.CurrentLevelIndex];

        SceneManager.LoadScene(customLevelName);
    }

    private void OnTryAgainClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(customLevelName);
    }

    private void OnMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnHideClicked()
    {
        contentVisible = !contentVisible;
        panelResult.style.display = contentVisible ? DisplayStyle.Flex : DisplayStyle.None;

        // Alterna entre "Ver Desenho" e "Mostrar Menu" com base no idioma!
        string btnKey = contentVisible ? "btn_hide_panel" : "btn_show_panel";
        if (hideButton != null)
            hideButton.text = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, btnKey);
    }

    #endregion
}