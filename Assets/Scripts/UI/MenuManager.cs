using UnityEngine;
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
    private UploadMenuManager uploadManager;

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        uploadManager = GetComponent<UploadMenuManager>(); // Puxa o componente
        var root = uiDocument.rootVisualElement;

        // 1. Buscando os painéis
        painelMenuInicial = root.Q<VisualElement>("MainMenu");
        painelSobre = root.Q<VisualElement>("About");
        painelLevelSelect = root.Q<VisualElement>("LevelSelect");

        // 2. Botões do Menu Principal
        Button btnPlay = root.Q<Button>("PlayButton");
        if (btnPlay != null) btnPlay.clicked += AbrirSelecaoDeNiveis;

        Button btnUpload = root.Q<Button>("UploadButton");
        if (btnUpload != null && uploadManager != null)
        {
            // Chama a função do seu script de WebGL!
            btnUpload.clicked += uploadManager.OnClickUpload;
        }


        painelTutorial = root.Q<VisualElement>("Tutorial"); // Use o ID exato do seu painel

        // Botão que abre o tutorial no Menu Principal
        Button btnTutorial = root.Q<Button>("TutorialButton");
        if (btnTutorial != null) btnTutorial.clicked += AbrirTutorial;

        // Botão de voltar do Tutorial
        Button btnVoltarTutorial =
        painelTutorial?.Q<Button>("BackButton");
        if (btnVoltarTutorial != null) btnVoltarTutorial.clicked += FecharTutorial;

        Button btnAbout = root.Q<Button>("AboutButton");
        if (btnAbout != null) btnAbout.clicked += AbrirSobre;

        // 3. Botões de Voltar
        Button btnVoltarLevel = painelLevelSelect?.Q<Button>("BackButton");
        if (btnVoltarLevel != null) btnVoltarLevel.clicked += FecharSelecaoDeNiveis;

        Button btnVoltarSobre = painelSobre?.Q<Button>("BackButton");
        if (btnVoltarSobre != null) btnVoltarSobre.clicked += FecharSobre;

        // 4. Conectando os 9 botões usando TextAsset
        for (int i = 0; i < arquivosDasFases.Length; i++)
        {
            TextAsset arquivoJson = arquivosDasFases[i];

            // Só tenta conectar se você tiver arrastado um arquivo para o slot
            if (arquivoJson != null)
            {
                string nomeDoJson = arquivoJson.name; // Pega o nome do arquivo sem a extensão .json
                string idDoBotao = $"Level{i + 1}";

                ConfigurarBotaoNivel(root, idDoBotao, nomeDoJson);
            }
        }
    }

    private void ConfigurarBotaoNivel(VisualElement root, string btnId, string jsonName)
    {
        Button btn = root.Q<Button>(btnId);
        if (btn != null)
        {
            btn.clicked += () => SelectLevelAndLoad(jsonName);
        }
        else
        {
            Debug.LogWarning($"Botão {btnId} não encontrado no UI Builder!");
        }
    }

    #region Controle de Telas (Alternando os Displays)

    public void AbrirSobre()
    {
        if (painelMenuInicial != null) painelMenuInicial.style.display = DisplayStyle.None;
        if (painelSobre != null) painelSobre.style.display = DisplayStyle.Flex;
    }

    public void FecharSobre()
    {
        if (painelSobre != null) painelSobre.style.display = DisplayStyle.None;
        if (painelMenuInicial != null) painelMenuInicial.style.display = DisplayStyle.Flex;
    }

    public void AbrirSelecaoDeNiveis()
    {
        if (painelMenuInicial != null) painelMenuInicial.style.display = DisplayStyle.None;
        if (painelLevelSelect != null) painelLevelSelect.style.display = DisplayStyle.Flex;
    }

    public void FecharSelecaoDeNiveis()
    {
        if (painelLevelSelect != null) painelLevelSelect.style.display = DisplayStyle.None;
        if (painelMenuInicial != null) painelMenuInicial.style.display = DisplayStyle.Flex;
    }

    public void AbrirTutorial()
    {
        if (painelMenuInicial != null) painelMenuInicial.style.display = DisplayStyle.None;
        if (painelTutorial != null) painelTutorial.style.display = DisplayStyle.Flex;
    }

    public void FecharTutorial()
    {
        if (painelTutorial != null) painelTutorial.style.display = DisplayStyle.None;
        if (painelMenuInicial != null) painelMenuInicial.style.display = DisplayStyle.Flex;
    }

    #endregion

    #region Carregamento JSON

    public void SelectLevelAndLoad(string levelJsonName)
    {
        LevelToLoadJSON = levelJsonName;
        Debug.Log("JSON Selecionado com sucesso: " + levelJsonName);

        SceneManager.LoadScene(customLevelName);
    }

    #endregion
}