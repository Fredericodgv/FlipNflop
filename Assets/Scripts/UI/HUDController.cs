using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gestor central da interface durante o gameplay.
/// Controla a navegação entre fases e a visibilidade dos painéis de resultado.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Navegação de Cenas")]
    [Tooltip("Nome da cena do Menu Principal.")]
    [SerializeField] private string menuSceneName = "Menu";

    [Tooltip("Nome da cena da próxima fase.")]
    [SerializeField] private string nextLevelSceneName;

    [Tooltip("Nome da cena atual (para o Restart).")]
    [SerializeField] private string currentLevelSceneName;

    [Header("Configurações de Visibilidade")]
    [Tooltip("Nome exato do GameObject do botão de ocultar (ex: 'HideButton').")]
    [SerializeField] private string hideButtonName = "HideButton";

    #region Navegação de Gameplay

    /// <summary>
    /// Carrega a próxima fase configurada.
    /// </summary>
    public void NextLevel()
    {
        if (LevelSequenceManager.HasNextLevel())
        {
            LevelSequenceManager.CurrentLevelIndex++;
            int newIndex = LevelSequenceManager.CurrentLevelIndex;

            string nextLevel = LevelSequenceManager.Levels[newIndex];

            MenuManager.LevelToLoadJSON = nextLevel;

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MenuWEB");
        }
    }

    /// <summary>
    /// Reinicia a fase atual.
    /// </summary>
    public void Restart()
    {
        if (!string.IsNullOrEmpty(currentLevelSceneName))
            SceneManager.LoadScene(currentLevelSceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Retorna ao Menu Principal.
    /// </summary>
    public void LoadMenuPrincipal()
    {
        if (!string.IsNullOrEmpty(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
    }

    #endregion

    #region Funcionalidade Hide HUD

    /// <summary>
    /// Alterna a visibilidade do painel ao qual o botão clicado pertence, 
    /// mantendo o botão de 'Hide' visível.
    /// </summary>
    public void HideHUD()
    {
        GameObject clickedButton = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        if (clickedButton != null && clickedButton.transform.parent != null)
        {
            GameObject panelRoot = clickedButton.transform.parent.gameObject;
            TogglePanelContentKeepButton(panelRoot);

            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void TogglePanelContentKeepButton(GameObject panelRoot)
    {
        if (panelRoot == null) return;

        Transform keepVisible = !string.IsNullOrEmpty(hideButtonName) ? panelRoot.transform.Find(hideButtonName) : null;

        bool anyContentVisible = false;
        foreach (Transform child in panelRoot.transform)
        {
            if (child == null || child == keepVisible) continue;
            if (child.gameObject.activeSelf) { anyContentVisible = true; break; }
        }

        bool showContent = !anyContentVisible;

        foreach (Transform child in panelRoot.transform)
        {
            if (child == null) continue;

            if (child == keepVisible)
            {
                child.gameObject.SetActive(true);
                continue;
            }

            string prefsKey = $"hide_{panelRoot.name}_{child.name}";

            if (showContent)
            {
                bool wasActive = PlayerPrefs.GetInt(prefsKey, 0) == 1;
                child.gameObject.SetActive(wasActive);
            }
            else
            {
                PlayerPrefs.SetInt(prefsKey, child.gameObject.activeSelf ? 1 : 0);
                child.gameObject.SetActive(false);
            }
        }

        if (panelRoot.TryGetComponent<Image>(out var img)) img.enabled = showContent;
        if (panelRoot.TryGetComponent<RawImage>(out var rawImg)) rawImg.enabled = showContent;
    }

    #endregion
}