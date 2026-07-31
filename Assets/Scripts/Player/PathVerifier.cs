using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoBehaviour orquestrador da pipeline de verificação do caminho.
/// Também é responsável por desenhar o feedback visual (linhas sólidas e tracejadas).
///
/// Pipeline ao finalizar a fase:
///   SignalPath.FinalizePath()
///       ↓ List&lt;Vector3&gt; playerPath
///   PathChecker.Evaluate(correctCorners, playerPath)
///       ↓ PathCheckResult (IsCorrect, CoveredSegments, GabaritoTotal, MissedCorners)
///   DrawFeedback(playerPath, correctCorners)
///       ↓ GameObjects de LineRenderer (efeito visual)
///   ScoreController.ReportResult(coveredSegments, gabaritoTotal, isCorrect)
///   ResultScreenController.Show(isCorrect)
/// </summary>
public class PathVerifier : MonoBehaviour
{
    #region Fields

    [Header("Output Settings")]
    [Tooltip("Y no mundo para o nível lógico LOW (0).")]
    [SerializeField] private float lowY = -2.5f;
    [Tooltip("Y no mundo para o nível lógico HIGH (1).")]
    [SerializeField] private float highY = 1.25f;

    [Header("Corner Answer Key (Auto-Generated)")]
    [SerializeField] private List<Vector3> correctCorners;

    [Header("Referências")]
    [SerializeField] private SignalPath signalPath;
    [SerializeField] private float cornerTolerance = 1.0f;

    [Header("Visual Feedback")]
    [Tooltip("Cor da linha quando o segmento é correto.")]
    [SerializeField] private Color successColor = Color.green;
    [Tooltip("Cor da linha quando o segmento é incorreto.")]
    [SerializeField] private Color failureColor = Color.red;
    [Tooltip("Prefab de LineRenderer instanciado para cada segmento de feedback.")]
    [SerializeField] private LineRenderer linePrefab;
    [Tooltip("Transform pai que agrupa os GameObjects de feedback.")]
    [SerializeField] private Transform feedbackLinesParent;

    [Header("Dashed Line Settings")]
    [SerializeField] private float dashLength = 0.15f;
    [SerializeField] private float dashGap = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showMissedCornersInGame = false;
    [SerializeField] private bool realtimeFeedback = false;

    // ── Módulos delegados (POCOs) ─────────────────────────────────────────────
    private GabaritoGenerator gabaritoGenerator;
    private PathChecker pathChecker;

    // Estado de debug (Gizmos)
    private List<Vector3> missedCorners = new();
    private LineRenderer signalLineRenderer;

    #endregion

    #region Tipo compartilhado

    /// <summary>
    /// Representa uma transição de saída do flip-flop em uma posição X do mundo.
    /// </summary>
    public struct SignalEvent
    {
        /// <summary>Posição X no mundo onde ocorre a transição.</summary>
        public float x;
        /// <summary>Novo valor lógico da saída após a transição.</summary>
        public bool value;

        public SignalEvent(float x, bool value) { this.x = x; this.value = value; }
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        gabaritoGenerator = new GabaritoGenerator(lowY, highY);
        gabaritoGenerator.Generate();
        correctCorners = gabaritoGenerator.CorrectCorners;

        pathChecker = new PathChecker(cornerTolerance, enableDebugLogs);
    }

    private void Start()
    {
        if (signalPath != null)
            signalLineRenderer = signalPath.GetComponent<LineRenderer>();

        SignalColorManager.OnColorsChanged += SyncFeedbackColors;
        SyncFeedbackColors();
    }

    private void OnDestroy()
    {
        SignalColorManager.OnColorsChanged -= SyncFeedbackColors;
    }

    private void Update()
    {
        if (!realtimeFeedback ||
            signalPath == null ||
            correctCorners == null ||
            signalPath.PathPoints.Count < 2)
            return;

        DrawFeedback(signalPath.PathPoints);
    }

    private void OnDrawGizmos()
    {
        if (correctCorners == null || correctCorners.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < correctCorners.Count - 1; i++)
        {
            Gizmos.DrawSphere(correctCorners[i], 0.2f);
            Gizmos.DrawLine(correctCorners[i], correctCorners[i + 1]);
        }
        if (correctCorners.Count > 0)
            Gizmos.DrawSphere(correctCorners[correctCorners.Count - 1], 0.2f);

        if (showMissedCornersInGame && Application.isPlaying && missedCorners?.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (var corner in missedCorners)
            {
                Gizmos.DrawWireSphere(corner, cornerTolerance);
                Gizmos.DrawSphere(corner, 0.3f);
            }
        }
    }

    #endregion

    #region API pública

    /// <summary>
    /// Finaliza o caminho do jogador e executa a pipeline completa.
    /// Chamado pelo PlayerController na morte ou ao completar a fase.
    ///
    /// ENTRADA: endX — limite direito do caminho a avaliar; null usa LevelManager.phaseEndX
    /// SAÍDA  : feedback visual + ScoreController + ResultScreenController atualizados
    /// </summary>
    public void FinalizeAndCheckPath(float? endX = null)
    {
        if (signalPath == null)
        {
            Debug.LogError("[PathVerifier] Referência ao SignalPath não está definida!");
            return;
        }

        float finalX = endX ?? LevelManager.Instance.phaseEndX;
        signalPath.FinalizePath(finalX);

        RunPipeline();
    }

    /// <summary>
    /// Reconstrói o gabarito a partir de eventos externos (testes / editor tools).
    /// </summary>
    public void BuildCorrectCornersFromSignalEvents(
        List<SignalEvent> events,
        float initialX = 0f,
        bool initialState = false)
    {
        gabaritoGenerator.BuildFromEvents(events, initialX, initialState);
        correctCorners = gabaritoGenerator.CorrectCorners;
    }

    #endregion

    #region Pipeline

    private void RunPipeline()
    {
        SyncFeedbackColors();

        if (signalPath == null || signalPath.PathPoints.Count < 2)
        {
            Debug.LogError("[PathVerifier] Caminho do jogador inválido!");
            ResultScreenController.Instance?.Show(false);
            return;
        }

        if (signalLineRenderer != null)
            signalLineRenderer.enabled = false;

        List<Vector3> playerPath = signalPath.PathPoints;

        // ── Etapa 1: Verificação ──────────────────────────────────────────────
        // ENTRADA: correctCorners, playerPath
        // SAÍDA  : PathCheckResult
        PathCheckResult result = pathChecker.Evaluate(correctCorners, playerPath);
        missedCorners = new List<Vector3>(result.MissedCorners);

        // ── Etapa 2: Feedback visual ──────────────────────────────────────────
        // ENTRADA: playerPath, correctCorners, cores, prefab, parent
        // SAÍDA  : GameObjects de LineRenderer criados em feedbackLinesParent
        DrawFeedback(playerPath);

        // ── Etapa 3: Resultado ────────────────────────────────────────────────
        // ENTRADA: coveredSegments, gabaritoTotal, isCorrect
        // SAÍDA  : ScoreController e ResultScreenController
        ScoreController.Instance?.ReportResult(result.CoveredSegments, result.GabaritoTotal, result.IsCorrect);
        ResultScreenController.Instance?.Show(result.IsCorrect);
    }

    #endregion

    #region Desenho de Feedback

    /// <summary>
    /// Limpa o feedback anterior e redesenha as linhas sólidas (acerto) e tracejadas (erro).
    ///
    /// ENTRADA: playerPath — pontos do caminho a colorir
    /// SAÍDA  : GameObjects de LineRenderer em feedbackLinesParent (efeito visual)
    /// </summary>
    private void DrawFeedback(List<Vector3> playerPath)
    {
        if (linePrefab == null || feedbackLinesParent == null) return;

        ClearFeedbackLines();

        float accumulator = 0f;
        bool isDrawingDash = true;

        for (int i = 0; i < playerPath.Count - 1; i++)
        {
            Vector3 segStart = playerPath[i];
            Vector3 segEnd = playerPath[i + 1];

            if (IsSegmentValid(segStart, segEnd))
            {
                DrawSolidLine(segStart, segEnd, successColor);
                accumulator = 0f;
                isDrawingDash = true;
            }
            else
            {
                DrawDashedLine(segStart, segEnd, ref accumulator, ref isDrawingDash);
            }
        }
    }

    private void ClearFeedbackLines()
    {
        foreach (Transform child in feedbackLinesParent)
            Destroy(child.gameObject);
    }

    /// <summary>
    /// ENTRADA: dois extremos de um segmento do jogador
    /// SAÍDA  : true se o segmento está dentro dos limites ortogonais de algum segmento do gabarito
    /// </summary>
    private bool IsSegmentValid(Vector3 pStart, Vector3 pEnd)
    {
        if ((pEnd - pStart).sqrMagnitude < 0.0001f) return true;

        for (int i = 0; i < correctCorners.Count - 1; i++)
        {
            Vector3 c1 = correctCorners[i];
            Vector3 c2 = correctCorners[i + 1];
            bool isHorizontal = Mathf.Abs(c1.y - c2.y) <= 0.1f;

            if (isHorizontal)
            {
                if (Mathf.Abs(pStart.y - c1.y) <= cornerTolerance &&
                    Mathf.Abs(pEnd.y - c1.y) <= cornerTolerance)
                {
                    float minX = Mathf.Min(c1.x, c2.x) - 0.1f;
                    float maxX = Mathf.Max(c1.x, c2.x) + 0.1f;
                    if (pStart.x >= minX && pStart.x <= maxX &&
                        pEnd.x >= minX && pEnd.x <= maxX)
                        return true;
                }
            }
            else
            {
                if (Mathf.Abs(pStart.x - c1.x) <= cornerTolerance &&
                    Mathf.Abs(pEnd.x - c1.x) <= cornerTolerance)
                {
                    float minY = Mathf.Min(c1.y, c2.y) - 0.1f;
                    float maxY = Mathf.Max(c1.y, c2.y) + 0.1f;
                    if (pStart.y >= minY && pStart.y <= maxY &&
                        pEnd.y >= minY && pEnd.y <= maxY)
                        return true;
                }
            }
        }
        return false;
    }

    private void DrawSolidLine(Vector3 start, Vector3 end, Color color)
    {
        LineRenderer line = Instantiate(linePrefab, feedbackLinesParent);
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startColor = color;
        line.endColor = color;
    }

    private void DrawDashedLine(Vector3 start, Vector3 end, ref float accumulator, ref bool isDrawingDash)
    {
        float length = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;
        float traveled = 0f;

        while (traveled < length - 0.001f)
        {
            float target = isDrawingDash ? dashLength : dashGap;
            float step = Mathf.Min(target - accumulator, length - traveled);

            if (isDrawingDash)
            {
                Vector3 subStart = start + direction * traveled;
                DrawSolidLine(subStart, subStart + direction * step, failureColor);
            }

            traveled += step;
            accumulator += step;

            if (accumulator >= target - 0.001f)
            {
                accumulator = 0f;
                isDrawingDash = !isDrawingDash;
            }
        }
    }

    #endregion

    #region Cores

    private void SyncFeedbackColors()
    {
        if (SignalColorManager.Instance == null) return;
        successColor = SignalColorManager.Instance.ColorFeedbackSuccess;
        failureColor = SignalColorManager.Instance.ColorFeedbackFailure;
    }

    #endregion

    #region Debug

    [ContextMenu("Log Gabarito Info")]
    private void LogGabaritoInfo()
    {
        if (correctCorners == null || correctCorners.Count == 0)
        {
            Debug.LogWarning("[PathVerifier] Gabarito vazio ou não gerado!");
            return;
        }

        Debug.Log("<color=cyan>===== GABARITO INFO =====</color>");
        Debug.Log($"Total de quinas: {correctCorners.Count}");
        Debug.Log($"Primeira quina: {correctCorners[0]}");
        Debug.Log($"Última quina: {correctCorners[correctCorners.Count - 1]}");
        Debug.Log($"Tolerância: {cornerTolerance}");

        for (int i = 0; i < correctCorners.Count; i++)
        {
            string level = Mathf.Approximately(correctCorners[i].y, lowY) ? "LOW" : "HIGH";
            Debug.Log($"  Quina #{i}: X={correctCorners[i].x:F2} Y={correctCorners[i].y:F2} ({level})");
        }
    }

    [ContextMenu("Toggle Debug Logs")]
    private void ToggleDebugLogs()
    {
        enableDebugLogs = !enableDebugLogs;
        pathChecker = new PathChecker(cornerTolerance, enableDebugLogs);
        Debug.Log($"<color=yellow>[PathVerifier] Debug logs {(enableDebugLogs ? "ATIVADOS" : "DESATIVADOS")}</color>");
    }

    #endregion
}