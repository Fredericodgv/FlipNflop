using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    // --- Lógica do Singleton ---
    public static LevelManager Instance { get; private set; }

    [Header("Configurações da Fase")]
    [Tooltip("Comprimento lógico do diagrama (última borda de clock / fim dos sinais).")]
    public float diagramEndX = 25f;
    [Tooltip("Folga extra após o diagrama para permitir execução da última transição antes de encerrar.")]
    public float phaseSlackTiles = 1f;
    [Tooltip("Extensão jogável total (diagramEndX + phaseSlackTiles). Use nas verificações de fim de fase.")]
    public float phaseEndX = 26f;
    [Tooltip("Valor legado: representa o fim lógico do diagrama (sem slack). Usado por sistemas que não precisam da folga.")]
    public float levelEndX = 25f; // agora igual a diagramEndX; phaseEndX mantém a folga separada
    [Tooltip("Passo do clock em X (distância entre linhas de clock / amostragem).")]
    public float clockStepX = 6f; // Centralizado aqui para uso global

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

        // Garante coerência inicial entre diagramEndX e phaseEndX
        phaseEndX = diagramEndX + phaseSlackTiles;
        levelEndX = diagramEndX; // legado aponta para fim lógico somente
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