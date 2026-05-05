// /Assets/Scripts/PathVerifier.cs

using System.Collections.Generic;
using UnityEngine;

public class PathVerifier : MonoBehaviour
{
    #region Fields

    [Header("Configuração da Saída")]
    [Tooltip("A posição Y para o nível lógico BAIXO (0) da saída.")]
    [SerializeField] private float lowY = -2.5f;
    [Tooltip("A posição Y para o nível lógico ALTO (1) da saída.")]
    [SerializeField] private float highY = 1.25f;

    [Header("Gabarito das Quinas (Gerado Automaticamente)")]
    public List<Vector3> correctCorners;

    [Header("Referências Adicionais")]
    [SerializeField] private SignalPath signalPath;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject retryButton;
    [SerializeField] private float cornerTolerance = 1.0f;

    [Header("Configuração de Feedback Visual")]
    [Tooltip("A cor da linha quando o caminho está correto.")]
    [SerializeField] private Color successColor = Color.green;
    [Tooltip("A cor da linha quando o caminho está incorreto.")]
    [SerializeField] private Color failureColor = Color.red;

    [Tooltip("O Prefab da linha a ser instanciado para o feedback.")]
    [SerializeField] private LineRenderer linePrefab;

    [Tooltip("O objeto pai que agrupará as linhas de feedback.")]
    [SerializeField] private Transform feedbackLinesParent;

    [Header("Configuração da Linha Tracejada (Matemática)")]
    [Tooltip("O tamanho de cada tracinho vermelho.")]
    [SerializeField] private float dashLength = 0.15f;
    [Tooltip("O tamanho do espaço vazio entre os tracinhos.")]
    [SerializeField] private float dashGap = 0.1f;

    [Header("Debug")]
    [Tooltip("Se ativado, mostra logs detalhados sobre a validação do caminho.")]
    [SerializeField] private bool enableDebugLogs = false;
    [Tooltip("Se ativado, mostra gizmos para quinas não atingidas durante o jogo.")]
    [SerializeField] private bool showMissedCornersInGame = false;
    [Tooltip("Se ativado, colore a linha do jogador em tempo real durante o jogo.")]
    [SerializeField] private bool realtimeFeedback = false;

    private List<Vector3> missedCorners = new List<Vector3>();
    private LineRenderer signalLineRenderer;

    #endregion

    #region Unity Methods

    /// <summary>
    /// Called when the script instance is being loaded. Triggers generation of the reference path (gabarito).
    /// </summary>
    private void Awake()
    {
        GenerateCorrectPath();
    }

    /// <summary>
    /// Unity Start: hides success/failure UI elements initially.
    /// </summary>
    private void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);

        if (signalPath != null)
            signalLineRenderer = signalPath.GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (!realtimeFeedback || signalPath == null || correctCorners == null || signalPath.PathPoints.Count < 2)
            return;

        UpdateRealtimeFeedback();
    }

    /// <summary>
    /// Draws gizmos in the editor to visualize the generated reference corners and connecting segments.
    /// </summary>
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
        {
            Gizmos.DrawSphere(correctCorners[correctCorners.Count - 1], 0.2f);
        }

        // Desenha quinas perdidas em vermelho durante o jogo
        if (showMissedCornersInGame && Application.isPlaying && missedCorners != null && missedCorners.Count > 0)
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

    #region Public Methods

    /// <summary>
    /// Finalizes and evaluates the path until a specific X coordinate (e.g., player's death position).
    /// Called when player dies or finishes the level
    /// </summary>
    /// <param name="endX">The X coordinate limit of the path.</param>
    public void FinalizeAndCheckPath(float? endX = null)
    {
        if (signalPath == null)
        {
            Debug.LogError("Referência ao SignalPath não está definida no PathVerifier!");
            return;
        }

        float finalX = endX ?? LevelManager.Instance.phaseEndX;

        signalPath.FinalizePath(finalX);
        CheckPlayerPath();
    }

    #endregion

    #region Path Verification Core

    /// <summary>
    /// Internal: evaluates the player's finalized path, draws feedback and toggles success/failure UI.
    /// </summary>
    private void CheckPlayerPath()
    {
        if (signalPath == null || signalPath.PathPoints.Count < 2)
        {
            Debug.LogError("Caminho do jogador inválido ou não definido!");
            if (resultPanel != null)
            {
                ActivateWithParent(resultPanel);
                if (continueButton != null) continueButton.SetActive(false);
                if (retryButton != null) retryButton.SetActive(true);
            }
            return;
        }

        missedCorners.Clear();
        signalLineRenderer.enabled = false;

        if (enableDebugLogs)
        {
            Debug.Log($"<color=cyan>[PathVerifier] Iniciando verificação do caminho</color>");
            Debug.Log($"  Pontos do jogador: {signalPath.PathPoints.Count}");
            Debug.Log($"  Quinas do gabarito: {correctCorners.Count}");
            Debug.Log($"  Tolerância: {cornerTolerance}");
        }

        List<bool> cornerChecks = EvaluateCorrectCorners(signalPath.PathPoints);
        bool isPathCorrectOverall = !cornerChecks.Contains(false);

        int gabaritoTotal = CountHorizontalGabaritoSegments();
        DrawFeedbackLines(signalPath.PathPoints, out int correct, out int total);
        int coveredSegments = CountCoveredGabaritoSegments(signalPath.PathPoints);
        ScoreController.Instance?.ReportResult(coveredSegments, gabaritoTotal);

        if (enableDebugLogs)
        {
            int correctCount = cornerChecks.FindAll(x => x).Count;
            int totalCount = cornerChecks.Count;
            Debug.Log($"<color=yellow>[PathVerifier] Resultado: {correctCount}/{totalCount} quinas atingidas</color>");
        }

        if (resultPanel != null)
        {
            ActivateWithParent(resultPanel);
            if (continueButton != null) continueButton.SetActive(isPathCorrectOverall);
            if (retryButton != null) retryButton.SetActive(!isPathCorrectOverall);
        }
    }

    /// <summary>
    /// Counts how many segments of the correct path were covered by the player's path.
    /// A segment gabarito[i]→gabarito[i+1] is considered covered if its midpoint is sufficiently close to the player's path.
    /// </summary>
    private int CountCoveredGabaritoSegments(List<Vector3> playerPath)
    {
        int covered = 0;
        for (int i = 0; i < correctCorners.Count - 1; i++)
        {
            if (Mathf.Abs(correctCorners[i].y - correctCorners[i + 1].y) > 0.1f) continue;

            Vector3 midpoint = (correctCorners[i] + correctCorners[i + 1]) / 2f;
            Vector3 closest = FindClosestPointOnFullPath(midpoint, playerPath);
            if (Vector3.Distance(midpoint, closest) <= cornerTolerance)
                covered++;
        }
        return covered;
    }

    /// <summary>
    /// Counts how many horizontal segments exist in the correct path (gabarito), which serves as the basis for scoring and feedback.
    /// </summary>
    /// <returns></returns>
    private int CountHorizontalGabaritoSegments()
    {
        int count = 0;
        for (int i = 0; i < correctCorners.Count - 1; i++)
        {
            if (Mathf.Abs(correctCorners[i].y - correctCorners[i + 1].y) <= 0.1f)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Draws the feedback by iterating over each small segment of the player's path.
    /// </summary>
    private void DrawFeedbackLines(List<Vector3> playerPath, out int correctSegments, out int totalSegments)
    {
        correctSegments = 0;
        totalSegments = 0;

        if (linePrefab == null || feedbackLinesParent == null) return;

        foreach (Transform child in feedbackLinesParent)
            Destroy(child.gameObject);

        float patternAccumulator = 0f;
        bool isDrawingDash = true;

        for (int i = 0; i < playerPath.Count - 1; i++)
        {
            Vector3 p_start = playerPath[i];
            Vector3 p_end = playerPath[i + 1];

            Vector3 closestStart = FindClosestPointOnFullPath(p_start, correctCorners);
            Vector3 closestEnd = FindClosestPointOnFullPath(p_end, correctCorners);

            bool startIsOnPath = Vector3.Distance(p_start, closestStart) <= cornerTolerance;
            bool endIsOnPath = Vector3.Distance(p_end, closestEnd) <= cornerTolerance;

            bool isSegmentCorrect = startIsOnPath && endIsOnPath;

            bool isVertical = Mathf.Abs(p_start.y - p_end.y) > 0.1f;

            if (isVertical && isSegmentCorrect)
            {
                bool aligned = false;

                foreach (Vector3 corner in correctCorners)
                {
                    if (Mathf.Abs(corner.x - p_start.x) <= cornerTolerance &&
                        Mathf.Abs(corner.x - p_end.x) <= cornerTolerance)
                    {
                        aligned = true;
                        break;
                    }
                }

                if (!aligned)
                    isSegmentCorrect = false;
            }

            totalSegments++;
            if (isSegmentCorrect) correctSegments++;

            if (isSegmentCorrect)
            {
                DrawSolidLine(p_start, p_end, successColor);

                patternAccumulator = 0f;
                isDrawingDash = true;
            }
            else
            {
                DrawDashedLine(p_start, p_end, ref patternAccumulator, ref isDrawingDash);
            }
        }
    }

    /// <summary>
    /// Instaciate a solid LineRenderer between the start and end points, using the specified color.
    /// Used to draw path segments that are correct (on the gabarito).
    /// </summary>
    private void DrawSolidLine(Vector3 start, Vector3 end, Color color)
    {
        LineRenderer line = Instantiate(linePrefab, feedbackLinesParent);
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startColor = color;
        line.endColor = color;
    }

    /// <summary>
    /// Draws a dashed line between the start and end points by instantiating multiple small LineRenderers.
    /// Used to draw path segments that are incorrect (off the gabarito).
    /// </summary>
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
                Vector3 subEnd = subStart + direction * step;
                DrawSolidLine(subStart, subEnd, failureColor);
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

    /// <summary>
    /// Update real-time feedback by spawning LineRenderers for each segment of the player's path.
    /// Each segment is colored green (on the gabarito) or red (off the gabarito), with no interpolation — abrupt color cuts at transition points.
    /// </summary>
    private void UpdateRealtimeFeedback()
    {
        if (linePrefab == null || feedbackLinesParent == null) return;

        foreach (Transform child in feedbackLinesParent)
            Destroy(child.gameObject);

        var points = signalPath.PathPoints;

        float patternAccumulator = 0f;
        bool isDrawingDash = true;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p_start = points[i];
            Vector3 p_end = points[i + 1];
            Vector3 midpoint = (p_start + p_end) / 2f;

            Vector3 closest = FindClosestPointOnFullPath(midpoint, correctCorners);
            bool onPath = Vector3.Distance(midpoint, closest) <= cornerTolerance;

            if (onPath)
            {
                DrawSolidLine(p_start, p_end, successColor);

                patternAccumulator = 0f;
                isDrawingDash = true;
            }
            else
            {
                DrawDashedLine(p_start, p_end, ref patternAccumulator, ref isDrawingDash);
            }
        }
    }

    #endregion

    #region Helper Functions

    /// <summary>
    /// Evaluates if a specific corner from the gabarito was "hit" by the player's path, meaning the player passed sufficiently close to it at some point.
    /// </summary>
    private bool EvaluateCornerHit(Vector3 corner, List<Vector3> playerPath, out float minDistance, out Vector3 closestPoint)
    {
        minDistance = float.MaxValue;
        closestPoint = Vector3.zero;
        bool wasHit = false;

        for (int i = 0; i < playerPath.Count - 1; i++)
        {
            Vector3 closest = FindClosestPointOnLineSegment(corner, playerPath[i], playerPath[i + 1]);
            float distance = Vector3.Distance(closest, corner);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = closest;
            }

            if (distance <= cornerTolerance)
            {
                wasHit = true;
                break;
            }
        }

        return wasHit;
    }

    /// <summary>
    /// Ensures that the target GameObject and its parent (if exists) are active in the scene. Used to show result panels that might be initially hidden.
    /// </summary>
    /// <param name="target"></param>
    private void ActivateWithParent(GameObject target)
    {
        if (target == null) return;
        Transform parent = target.transform.parent;
        if (parent != null && !parent.gameObject.activeSelf)
            parent.gameObject.SetActive(true);
        if (!target.activeSelf) target.SetActive(true);
    }

    /// <summary>
    /// Evaluates if each corner in the gabarito was hit by the player's path and returns a list of booleans indicating the result for each corner.
    /// </summary>
    private List<bool> EvaluateCorrectCorners(List<Vector3> playerPath)
    {
        var checks = new List<bool>();
        int cornerIndex = 0;

        foreach (Vector3 correctCorner in correctCorners)
        {
            bool cornerWasHit = EvaluateCornerHit(correctCorner, playerPath, out float minDistance, out Vector3 closestPlayerPoint);
            checks.Add(cornerWasHit);

            if (!cornerWasHit)
            {
                missedCorners.Add(correctCorner);
                if (enableDebugLogs)
                    Debug.LogWarning($"  <color=red>✗ Quina #{cornerIndex} PERDIDA:</color> Pos={correctCorner} | Distância mínima={minDistance:F2} | Ponto mais próximo={closestPlayerPoint}");
            }
            else if (enableDebugLogs)
                Debug.Log($"  <color=green>✓ Quina #{cornerIndex} OK:</color> Pos={correctCorner} | Distância={minDistance:F2}");

            cornerIndex++;
        }
        return checks;
    }

    /// <summary>
    /// Finds the closest point on the player's path to a given target point (e.g., a corner from the gabarito) by checking each segment of the path.
    /// </summary>
    private Vector3 FindClosestPointOnFullPath(Vector3 targetPoint, List<Vector3> path)
    {
        if (path.Count == 0) return Vector3.zero;
        if (path.Count == 1) return path[0];

        Vector3 bestPoint = path[0];
        float bestDistanceSqr = (bestPoint - targetPoint).sqrMagnitude;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 pointOnSegment = FindClosestPointOnLineSegment(targetPoint, path[i], path[i + 1]);
            float distanceSqr = (pointOnSegment - targetPoint).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                bestPoint = pointOnSegment;
            }
        }
        return bestPoint;
    }

    /// <summary>
    /// Finds the closest point on a line segment defined by lineStart and lineEnd to a given point. Used for precise distance calculations between the player's path and the gabarito corners.
    /// </summary>
    private Vector3 FindClosestPointOnLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLengthSqr = lineDirection.sqrMagnitude;
        if (lineLengthSqr < 0.0001f) return lineStart;

        float t = Vector3.Dot(point - lineStart, lineDirection) / lineLengthSqr;
        t = Mathf.Clamp01(t);

        return lineStart + lineDirection * t;
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Log Gabarito Info")]
    private void LogGabaritoInfo()
    {
        if (correctCorners == null || correctCorners.Count == 0)
        {
            Debug.LogWarning("[PathVerifier] Gabarito vazio ou não gerado!");
            return;
        }

        Debug.Log($"<color=cyan>===== GABARITO INFO =====</color>");
        Debug.Log($"Total de quinas: {correctCorners.Count}");
        Debug.Log($"Primeira quina: {correctCorners[0]}");
        Debug.Log($"Última quina: {correctCorners[correctCorners.Count - 1]}");
        Debug.Log($"Tolerância: {cornerTolerance}");

        for (int i = 0; i < correctCorners.Count; i++)
        {
            string yLevel = Mathf.Approximately(correctCorners[i].y, lowY) ? "LOW" : "HIGH";
            Debug.Log($"  Quina #{i}: X={correctCorners[i].x:F2} Y={correctCorners[i].y:F2} ({yLevel})");
        }
    }

    [ContextMenu("Toggle Debug Logs")]
    private void ToggleDebugLogs()
    {
        enableDebugLogs = !enableDebugLogs;
        Debug.Log($"<color=yellow>[PathVerifier] Debug logs {(enableDebugLogs ? "ATIVADOS" : "DESATIVADOS")}</color>");
    }

    #endregion

    #region Gabarito Generation

    /// <summary>
    /// Generates the reference path (gabarito) by loading signal events from the LevelJsonLoader and building the correct corners based on those events.
    /// </summary>
    private void GenerateCorrectPath()
    {
        var loader = UnityEngine.Object.FindFirstObjectByType<LevelJsonLoader>();
        if (loader == null)
        {
            Debug.LogError("PathVerifier requires a LevelJsonLoader in the scene.");
            correctCorners = new List<Vector3>();
            return;
        }

        var events = loader.ComputeOutputEventsFromParsedSignals();
        BuildCorrectCornersFromSignalEvents(events, 0f, false);
    }

    public struct SignalEvent
    {
        public float x;
        public bool value;

        public SignalEvent(float x, bool value)
        {
            this.x = x;
            this.value = value;
        }
    }

    /// <summary>
    /// Builds the correct corners for the reference path (gabarito) based on a list of signal events.
    /// </summary>
    public void BuildCorrectCornersFromSignalEvents(List<SignalEvent> events, float initialX = 0f, bool initialState = false)
    {
        correctCorners = new List<Vector3>();
        bool qState = initialState;
        float startY = qState ? highY : lowY;

        correctCorners.Add(new Vector3(initialX, startY, 0f));

        if (events == null || events.Count == 0)
        {
            float phaseEndX = (LevelManager.Instance != null) ? LevelManager.Instance.phaseEndX : 0f;
            correctCorners.Add(new Vector3(phaseEndX, startY, 0f));
            return;
        }

        events.Sort((a, b) => a.x.CompareTo(b.x));

        foreach (var ev in events)
        {
            float x = ev.x;
            float previousY = qState ? highY : lowY;
            correctCorners.Add(new Vector3(x, previousY, 0f));

            if (ev.value != qState)
            {
                float currentY = ev.value ? highY : lowY;
                correctCorners.Add(new Vector3(x, currentY, 0f));
                qState = ev.value;
            }
        }

        float endX = (LevelManager.Instance != null) ? LevelManager.Instance.phaseEndX : (events[events.Count - 1].x);
        correctCorners.Add(new Vector3(endX, qState ? highY : lowY, 0f));

        for (int i = correctCorners.Count - 1; i > 0; i--)
        {
            if (Vector3.Distance(correctCorners[i], correctCorners[i - 1]) < 0.01f) { correctCorners.RemoveAt(i); }
            if (i == 0 || i >= correctCorners.Count - 1) continue;
            if (correctCorners[i - 1].y == correctCorners[i].y && correctCorners[i + 1].y == correctCorners[i].y) { correctCorners.RemoveAt(i); }
        }
    }

    #endregion
}