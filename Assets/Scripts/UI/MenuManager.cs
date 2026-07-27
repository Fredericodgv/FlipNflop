using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
    [Header("Navegação de Cenas")]
    [SerializeField] private string customLevelName = "Custom";

    [Header("Arquivos das Fases (JSON)")]
    [Tooltip("Arraste os arquivos .json das fases para cá")]
    [SerializeField] private TextAsset[] arquivosDasFases = new TextAsset[9];

    public static string LevelToLoadJSON = "";

    // Elementos de UI
    private UIDocument uiDocument;
    private VisualElement painelMenuInicial;
    private VisualElement painelSobre;
    private VisualElement painelLevelSelect;
    private VisualElement painelTutorial;
    private VisualElement painelUpload;
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

        // 1. Buscando os painéis
        painelMenuInicial = root.Q<VisualElement>("MainMenu");
        painelSobre = root.Q<VisualElement>("About");
        painelLevelSelect = root.Q<VisualElement>("LevelSelect");
        painelTutorial = root.Q<VisualElement>("Tutorial");
        painelUpload = root.Q<VisualElement>("PanelUpload"); // Encontrando o painel no UXML
        backBtn = root.Q<Button>("BackButton");

        submenus.Clear();
        submenus.Add(painelSobre);
        submenus.Add(painelLevelSelect);
        submenus.Add(painelTutorial);
        submenus.Add(painelUpload); // Adicionando ao controle automático de telas

        if (backBtn != null)
            backBtn.clicked += FecharSubmenuAtual;

        // 2. Botões do Menu Principal
        Button btnPlay = root.Q<Button>("PlayButton");
        if (btnPlay != null) btnPlay.clicked += AbrirSelecaoDeNiveis;

        Button btnUpload = root.Q<Button>("UploadButton");
        if (btnUpload != null)
        {
            // Agora o botão do menu principal abre o painel ao invés de chamar o WebGL direto
            btnUpload.clicked += AbrirUpload;
        }

        Button btnTutorial = root.Q<Button>("TutorialButton");
        if (btnTutorial != null) btnTutorial.clicked += AbrirTutorial;

        Button btnAbout = root.Q<Button>("AboutButton");
        if (btnAbout != null) btnAbout.clicked += AbrirSobre;

        // 3. Botões do Novo Painel de API
        Button btnFetchLevels = root.Q<Button>("BtnFetchLevels");
        if (btnFetchLevels != null) btnFetchLevels.clicked += AtualizarFasesDaAPI;

        Button btnPostLevel = root.Q<Button>("BtnPostLevel");
        if (btnPostLevel != null) btnPostLevel.clicked += EnviarNovaFaseParaAPI;

        // 4. Conectando os 9 botões usando TextAsset
        for (int i = 0; i < arquivosDasFases.Length; i++)
        {
            TextAsset arquivoJson = arquivosDasFases[i];

            // Só tenta conectar se você tiver arrastado um arquivo para o slot
            if (arquivoJson != null)
            {
                string nomeDoJson = arquivoJson.name; // Pega o nome do arquivo sem a extensão .json
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
        {
            btn.clicked += () => SelectLevelAndLoad(jsonName, levelIndex);
        }
        else
        {
            Debug.LogWarning($"Botão {btnId} não encontrado no UI Builder!");
        }
    }

    #region Controle de Telas (Alternando os Displays)

    public void AbrirSobre()
    {
        OpenSubmenu(painelSobre);
    }

    public void AbrirSelecaoDeNiveis()
    {
        OpenSubmenu(painelLevelSelect);
    }

    public void AbrirTutorial()
    {
        OpenSubmenu(painelTutorial);
    }

    // NOVO MÉTODO PARA ABRIR A TELA DA API
    public void AbrirUpload()
    {
        OpenSubmenu(painelUpload);
    }

    private void FecharSubmenuAtual()
    {
        if (activeSubmenu != null)
            CloseSubmenu(activeSubmenu);
    }

    private void OpenSubmenu(VisualElement submenu)
    {
        if (submenu == null)
        {
            Debug.LogError("Elemento de submenu não encontrado!");
            return;
        }

        if (painelMenuInicial != null)
            painelMenuInicial.AddToClassList("hidden");

        foreach (VisualElement item in submenus)
        {
            if (item != null)
                item.AddToClassList("hidden");
        }

        submenu.RemoveFromClassList("hidden");
        activeSubmenu = submenu;

        if (backBtn != null)
            backBtn.RemoveFromClassList("hidden");
    }

    private void CloseSubmenu(VisualElement submenu)
    {
        if (submenu == null)
        {
            Debug.LogError("Elemento de submenu não encontrado!");
            return;
        }

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

        // Chama o método PÚBLICO passando o que fazer em caso de Sucesso ou Erro
        apiManager.FetchAllLevels(
            onSuccess: (jsonResposta) =>
            {
                Debug.Log($"Sucesso! Fases baixadas: {jsonResposta}");
                // TODO: Aqui vamos transformar o JSON recebido na lista de botões!
            },
            onError: (mensagemErro) =>
            {
                Debug.LogError($"Falha ao buscar fases: {mensagemErro}");
            }
        );
    }

    private void EnviarNovaFaseParaAPI()
    {
        // TODO: Pegar o JSON da fase customizada criada pelo jogador e enviar via ApiManager.CreateLevel()
        Debug.Log("Iniciando requisição POST para salvar nova fase...");

        // Se você ainda quiser usar a lógica antiga do WebGL para o envio, 
        // você pode descomentar a linha abaixo:
        // if (uploadManager != null) uploadManager.OnClickUpload();
    }

    #endregion
}