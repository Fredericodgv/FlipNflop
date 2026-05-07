using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Configurações da Fase")]
    [Tooltip("Comprimento lógico do diagrama (última borda de clock / fim dos sinais).")]
    public float diagramEndX = 25f;
    [Tooltip("Folga extra após o diagrama para permitir execução da última transição antes de encerrar.")]
    public float phaseSlackTiles = 1f;
    [Tooltip("Extensão jogável total (diagramEndX + phaseSlackTiles). Use nas verificações de fim de fase.")]
    public float phaseEndX = 26f;
    [Tooltip("Valor legado: representa o fim lógico do diagrama (sem slack). Usado por sistemas relacionaod ao diagrama")]
    public float levelEndX = 25f;
    [Tooltip("Passo do clock em X (distância entre linhas de clock / amostragem).")]
    public float clockStepX = 6f;

    [Header("Navegação")]
    [SerializeField] private string nextLevel;
    [SerializeField] private string menu;
    [SerializeField] private string levelAtual;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        phaseEndX = diagramEndX + phaseSlackTiles;
        levelEndX = diagramEndX;
    }

}