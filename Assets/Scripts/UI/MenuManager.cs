using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class MenuManager : MonoBehaviour
{
    [Header("Navegação de Cenas")]
    [SerializeField] private string customLevelName = "Custom";

    [Header("Arquivos das Fases (JSON)")]
    [SerializeField] private TextAsset[] arquivosDasFases = new TextAsset[9];

    public static string LevelToLoadJSON = "";

    private UIDocument uiDocument;
    private VisualElement painelMenuInicial;
    private VisualElement painelSobre;
    private VisualElement painelLevelSelect;
    private VisualElement painelTutorial;
    private VisualElement painelUpload;

    private ScrollView scrollFasesApi;

    // Objetos já convertidos (para exibir na lista) e o JSON bruto de cada um (para carregar de fato)
    private readonly List<LevelData> fasesApiCarregadas = new();
    private readonly List<string> fasesApiJsonBruto = new();

    private ApiManager apiManager;
    private UploadMenuManager uploadManager;
    private Button backBtn;
    private VisualElement activeSubmenu;
    private readonly List<VisualElement> submenus = new();

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        uploadManager = GetComponent<UploadMenuManager>();
        apiManager = GetComponent<ApiManager>();
        var root = uiDocument.rootVisualElement;

        painelMenuInicial = root.Q<VisualElement>("MainMenu");
        painelSobre = root.Q<VisualElement>("About");
        painelLevelSelect = root.Q<VisualElement>("LevelSelect");
        painelTutorial = root.Q<VisualElement>("Tutorial");
        painelUpload = root.Q<VisualElement>("PanelUpload");

        scrollFasesApi = root.Q<ScrollView>("LevelListScrollView");

        backBtn = root.Q<Button>("BackButton");

        submenus.Clear();
        submenus.Add(painelSobre);
        submenus.Add(painelLevelSelect);
        submenus.Add(painelTutorial);
        submenus.Add(painelUpload);

        if (backBtn != null)
            backBtn.clicked += FecharSubmenuAtual;

        Button btnPlay = root.Q<Button>("PlayButton");
        if (btnPlay != null) btnPlay.clicked += AbrirSelecaoDeNiveis;

        Button btnUpload = root.Q<Button>("UploadButton");
        if (btnUpload != null) btnUpload.clicked += AbrirUpload;

        Button btnTutorial = root.Q<Button>("TutorialButton");
        if (btnTutorial != null) btnTutorial.clicked += AbrirTutorial;

        Button btnAbout = root.Q<Button>("AboutButton");
        if (btnAbout != null) btnAbout.clicked += AbrirSobre;

        Button btnFetchLevels = root.Q<Button>("BtnFetchLevels");
        if (btnFetchLevels != null) btnFetchLevels.clicked += AtualizarFasesDaAPI;

        Button btnPostLevel = root.Q<Button>("BtnPostLevel");
        if (btnPostLevel != null) btnPostLevel.clicked += EnviarNovaFaseParaAPI;

        for (int i = 0; i < arquivosDasFases.Length; i++)
        {
            TextAsset arquivoJson = arquivosDasFases[i];
            if (arquivoJson != null)
            {
                string nomeDoJson = arquivoJson.name;
                string idDoBotao = $"Level{i + 1}";
                ConfigurarBotaoNivel(root, idDoBotao, nomeDoJson, i);
            }
        }
    }

    private void OnDisable()
    {
        if (backBtn != null)
            backBtn.clicked -= FecharSubmenuAtual;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            FecharSubmenuAtual();
    }

    private void ConfigurarBotaoNivel(VisualElement root, string btnId, string jsonName, int levelIndex)
    {
        Button btn = root.Q<Button>(btnId);
        if (btn != null)
            btn.clicked += () => SelectLevelAndLoad(jsonName, levelIndex);
        else
            Debug.LogWarning($"Botão {btnId} não encontrado no UI Builder!");
    }

    #region Controle de Telas

    public void AbrirSobre() => OpenSubmenu(painelSobre);
    public void AbrirSelecaoDeNiveis() => OpenSubmenu(painelLevelSelect);
    public void AbrirTutorial() => OpenSubmenu(painelTutorial);
    public void AbrirUpload() => OpenSubmenu(painelUpload);

    private void FecharSubmenuAtual()
    {
        if (activeSubmenu != null)
            CloseSubmenu(activeSubmenu);
    }

    private void OpenSubmenu(VisualElement submenu)
    {
        if (submenu == null) return;

        if (painelMenuInicial != null)
            painelMenuInicial.AddToClassList("hidden");

        foreach (VisualElement item in submenus)
            if (item != null) item.AddToClassList("hidden");

        submenu.RemoveFromClassList("hidden");
        activeSubmenu = submenu;

        if (backBtn != null)
            backBtn.RemoveFromClassList("hidden");
    }

    private void CloseSubmenu(VisualElement submenu)
    {
        if (submenu == null) return;

        submenu.AddToClassList("hidden");
        activeSubmenu = null;

        if (backBtn != null)
            backBtn.AddToClassList("hidden");

        if (painelMenuInicial != null)
            painelMenuInicial.RemoveFromClassList("hidden");
    }

    #endregion

    #region Carregamento JSON Local

    public void SelectLevelAndLoad(string levelJsonName, int levelIndex)
    {
        LevelSequenceManager.CurrentLevelIndex = levelIndex;
        LevelToLoadJSON = levelJsonName;
        Debug.Log($"JSON Selecionado com sucesso: {levelJsonName} | Índice: {levelIndex}");
        SceneManager.LoadScene(customLevelName);
    }

    #endregion

    #region Conexão com a API

    private void AtualizarFasesDaAPI()
    {
        Debug.Log("Iniciando requisição para buscar fases na API...");

        apiManager.FetchAllLevels(
            onSuccess: (jsonResposta) =>
            {
                Debug.Log($"Sucesso! Fases baixadas: {jsonResposta}");
                PreencherListaDeFases(jsonResposta);
            },
            onError: (mensagemErro) =>
            {
                Debug.LogError($"Falha ao buscar fases: {mensagemErro}");
            }
        );
    }

    private void PreencherListaDeFases(string jsonResposta)
    {
        if (scrollFasesApi != null)
            scrollFasesApi.Clear();

        fasesApiCarregadas.Clear();
        fasesApiJsonBruto.Clear();

        if (string.IsNullOrWhiteSpace(jsonResposta) || jsonResposta.Trim() == "[]")
        {
            Label lblVazio = new Label("Nenhuma fase customizada encontrada na API.");
            lblVazio.style.color = Color.white;
            lblVazio.style.unityTextAlign = TextAnchor.MiddleCenter;
            scrollFasesApi?.Add(lblVazio);
            return;
        }

        JArray array;
        try
        {
            array = JArray.Parse(jsonResposta);
        }
        catch (JsonException ex)
        {
            Debug.LogError($"MenuManager: erro ao parsear fases da API — {ex.Message}");
            return;
        }

        var settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
        };
        var serializer = JsonSerializer.Create(settings);

        foreach (JToken token in array)
        {
            // Guarda o fragmento JSON exatamente como veio da API — é isso que será
            // passado para o LevelJsonLoader depois, evitando re-serializar LevelData
            // (os conversores dele não suportam WriteJson).
            string rawJson = token.ToString(Formatting.None);

            LevelData data;
            try
            {
                data = token.ToObject<LevelData>(serializer);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"MenuManager: erro ao interpretar uma fase da API — {ex.Message}");
                continue;
            }

            if (data == null) continue;

            fasesApiCarregadas.Add(data);
            fasesApiJsonBruto.Add(rawJson);
        }

        for (int i = 0; i < fasesApiCarregadas.Count; i++)
        {
            LevelData fase = fasesApiCarregadas[i];
            int indexCapturado = i;

            Button btnFase = new Button();
            btnFase.text = $"{fase.LevelName}\n<size=12>{fase.ClockCycles} ciclos de clock</size>";

            btnFase.AddToClassList("btn");
            btnFase.style.marginBottom = 10;
            btnFase.style.whiteSpace = WhiteSpace.Normal;

            btnFase.clicked += () => SelecionarFaseDaApi(indexCapturado);

            scrollFasesApi?.Add(btnFase);
        }
    }

    /// <summary>
    /// Carrega a fase escolhida da API, usando o mesmo mecanismo do UploadMenuManager:
    /// joga o JSON bruto em UploadedLevelJson.Content e carrega a cena "Custom".
    /// O LevelJsonLoader prioriza UploadedLevelJson.Content sobre MenuManager.LevelToLoadJSON.
    /// </summary>
    private void SelecionarFaseDaApi(int index)
    {
        if (index < 0 || index >= fasesApiJsonBruto.Count)
        {
            Debug.LogError($"MenuManager: índice de fase da API inválido ({index}).");
            return;
        }

        LevelData fase = fasesApiCarregadas[index];
        string rawJson = fasesApiJsonBruto[index];

        Debug.Log($"Carregando fase customizada da API: {fase.LevelName}");

        UploadedLevelJson.Content = rawJson;
        LevelToLoadJSON = ""; // garante que o loader use Content, não Resources

        SceneManager.LoadScene(customLevelName);
    }

    private void EnviarNovaFaseParaAPI()
    {
        Debug.Log("Iniciando requisição POST para salvar nova fase...");
    }

    #endregion
}