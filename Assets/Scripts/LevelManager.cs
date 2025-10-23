using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    // --- Lógica do Singleton ---
    public static LevelManager Instance { get; private set; }

    [Header("Configurações da Fase")]
    [Tooltip("A posição X onde a fase termina.")]
    public float levelEndX = 25f; // Valor padrão unificado
    [Tooltip("A posição X onde a fase começa (âncora das linhas de clock).")]
    public float levelStartX = -10f;
    [Tooltip("Passo do clock em X (distância entre linhas de clock / amostragem).")]
    public float clockStepX = 5f; // Centralizado aqui para uso global

    [Header("Navegação")]
    [SerializeField] private string nextLevel;
    [SerializeField] private string menu;
    [SerializeField] private string levelAtual;

    private void Awake()
    {
        // Garante que apenas uma instância do LevelManager exista
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Você pode adicionar métodos para carregar próximas fases, etc. aqui
    // Ex:
    // public void LoadNextLevel()
    // {
    //     if (!string.IsNullOrEmpty(nextLevel))
    //     {
    //         SceneManager.LoadScene(nextLevel);
    //     }
    // }
}