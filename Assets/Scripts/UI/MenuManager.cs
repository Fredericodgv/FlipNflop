using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;


public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelSobre;
    [SerializeField] private GameObject painelLevelSelect;
    [Header("Referências de HUD")]
    [Tooltip("Painel de sucesso (por exemplo: SuccessPanel / successUI). Será ocultado pelo HideHUD, enquanto o botão de esconder permanecerá visível se atribuído.")]
    [SerializeField] private GameObject successPanel;
    [Tooltip("Painel de falha (Game Over). Será ocultado/desocultado como o de sucesso.")]
    [SerializeField] private GameObject failurePanel;
    [Tooltip("Nome do botão dentro do painel que deve permanecer visível ao ocultar o conteúdo.")]
    [SerializeField] private string hideButtonName = "HideButton";
    [SerializeField] private string menu;
    [SerializeField] private string nomeDoLevel;
    [SerializeField] private string nextLevel;
    [SerializeField] private string levelAtual;
    [SerializeField] private string customLevelName = "Custom";
    public static string LevelToLoadJSON = "";

    public void Jogar()
    {
        SceneManager.LoadScene(nomeDoLevel);
    }

    public void Upload()
    {
        SceneManager.LoadScene("LevelUpload");
    }

    public void AbrirSobre()
    {
        painelMenuInicial.SetActive(false);
        painelSobre.SetActive(true);
    }

    public void FecharSobre()
    {
        painelMenuInicial.SetActive(true);
        painelSobre.SetActive(false);
    }

    public void Sair()
    {
        Application.Quit();
    }

    // Level Manager
    public void NextLevel()
    {
        SceneManager.LoadScene(nextLevel);
    }

    public void Menu()
    {
        SceneManager.LoadScene(menu);
    }

    public void Restart()
    {
        SceneManager.LoadScene(levelAtual);
    }

    public void AbrirSelecaoDeNiveis()
    {
        painelMenuInicial.SetActive(false);
        painelLevelSelect.SetActive(true);
    }

    // Método para o botão "Voltar" da tela de seleção de níveis:
    public void FecharSelecaoDeNiveis()
    {
        painelLevelSelect.SetActive(false);
        painelMenuInicial.SetActive(true);
    }

    #region Hide HUD Button

    public void HideHUD()
    {
        // Usa o botão clicado para encontrar o painel (pai imediato) e alternar o conteúdo
        var clicked = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (clicked != null)
        {
            var parent = clicked.transform != null ? clicked.transform.parent : null;
            if (parent != null)
            {
                TogglePanelContentKeepButton(parent.gameObject);
                return;
            }
        }

        // Fallback: cena de menu — oculta painéis do menu/sobre
        if (painelMenuInicial != null) painelMenuInicial.SetActive(false);
        if (painelSobre != null) painelSobre.SetActive(false);
    }

    public void LoadMenuPrincipal()
    {
        SceneManager.LoadScene(menu);
    }

    public void SelectLevelAndLoad(string levelJsonName)
    {
        if (string.IsNullOrEmpty(levelJsonName))
        {
            Debug.LogError("Nome do arquivo JSON não pode ser nulo ou vazio");
            return;
        }

        LevelToLoadJSON = levelJsonName;
        Debug.Log("JSON Selecionado: " + levelJsonName);

        SceneManager.LoadScene(customLevelName);
    }

    private void TogglePanelContentKeepButton(GameObject panelRoot)
    {
        if (panelRoot == null) return;
        if (!panelRoot.activeSelf) panelRoot.SetActive(true);

        Transform keepVisible = null;
        if (!string.IsNullOrEmpty(hideButtonName))
            keepVisible = panelRoot.transform.Find(hideButtonName);

        bool anyContentVisible = false;
        foreach (Transform child in panelRoot.transform)
        {
            if (child == null) continue;
            if (keepVisible != null && child == keepVisible) continue;
            if (child.gameObject.activeSelf) { anyContentVisible = true; break; }
        }

        bool showContent = !anyContentVisible;

        foreach (Transform child in panelRoot.transform)
        {
            if (child == null) continue;

            // HideButton sempre visível
            if (keepVisible != null && child == keepVisible)
            {
                if (!child.gameObject.activeSelf) child.gameObject.SetActive(true);
                continue;
            }

            if (showContent)
            {
                // Ao reexibir, restaura apenas quem tinha estado salvo como ativo
                string key = $"hide_{panelRoot.name}_{child.name}";
                bool wasActive = PlayerPrefs.GetInt(key, 0) == 1;
                child.gameObject.SetActive(wasActive);
            }
            else
            {
                // Ao ocultar, salva o estado atual antes de desativar
                string key = $"hide_{panelRoot.name}_{child.name}";
                PlayerPrefs.SetInt(key, child.gameObject.activeSelf ? 1 : 0);
                child.gameObject.SetActive(false);
            }
        }

        var img = panelRoot.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.enabled = showContent;
        var raw = panelRoot.GetComponent<UnityEngine.UI.RawImage>();
        if (raw != null) raw.enabled = showContent;
    }
    #endregion
}
