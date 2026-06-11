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
    private UploadMenuManager uploadManager;
    private Button backBtn;
    private VisualElement activeSubmenu;
    private readonly List<VisualElement> submenus = new();

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        uploadManager = GetComponent<UploadMenuManager>(); // Puxa o componente
        var root = uiDocument.rootVisualElement;

        // 1. Buscando os painéis
        painelMenuInicial = root.Q<VisualElement>("MainMenu");
        painelSobre = root.Q<VisualElement>("About");
        painelLevelSelect = root.Q<VisualElement>("LevelSelect");
        painelTutorial = root.Q<VisualElement>("Tutorial"); // Use o ID exato do seu painel
        backBtn = root.Q<Button>("BackButton");

        submenus.Clear();
        submenus.Add(painelSobre);
        submenus.Add(painelLevelSelect);
        submenus.Add(painelTutorial);

        if (backBtn != null)
            backBtn.clicked += FecharSubmenuAtual;

        // 2. Botões do Menu Principal
        Button btnPlay = root.Q<Button>("PlayButton");
        if (btnPlay != null) btnPlay.clicked += AbrirSelecaoDeNiveis;

        Button btnUpload = root.Q<Button>("UploadButton");
        if (btnUpload != null && uploadManager != null)
        {
            // Chama a função do seu script de WebGL!
            btnUpload.clicked += uploadManager.OnClickUpload;
        }

        // Botão que abre o tutorial no Menu Principal
        Button btnTutorial = root.Q<Button>("TutorialButton");
        if (btnTutorial != null) btnTutorial.clicked += AbrirTutorial;

        Button btnAbout = root.Q<Button>("AboutButton");
        if (btnAbout != null) btnAbout.clicked += AbrirSobre;

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
        OpenSubmenu(painelSobre);
    }

    public void FecharSobre()
    {
        CloseSubmenu(painelSobre);
    }

    public void AbrirSelecaoDeNiveis()
    {
        OpenSubmenu(painelLevelSelect);
    }

    public void FecharSelecaoDeNiveis()
    {
        CloseSubmenu(painelLevelSelect);
    }

    public void AbrirTutorial()
    {
        OpenSubmenu(painelTutorial);
    }

    public void FecharTutorial()
    {
        CloseSubmenu(painelTutorial);
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

    #region Carregamento JSON

    public void SelectLevelAndLoad(string levelJsonName)
    {
        LevelToLoadJSON = levelJsonName;
        Debug.Log("JSON Selecionado com sucesso: " + levelJsonName);

        SceneManager.LoadScene(customLevelName);
    }

    #endregion
}
