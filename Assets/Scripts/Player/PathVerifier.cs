using System.Collections.Generic;
using UnityEngine;

public class PathVerifier : MonoBehaviour
{
    #region Fields

    [Header("Output Settings")]
    [Tooltip("The Y position for the LOW (0) logic level of the output.")]
    [SerializeField] private float lowY = -2.5f;
    [Tooltip("The Y position for the HIGH (1) logic level of the output.")]
    [SerializeField] private float highY = 1.25f;

    [Header("Corner Answer Key (Auto-Generated)")]
    [SerializeField] private List<Vector3> correctCorners;

    [Header("Additional References")]
    [SerializeField] private SignalPath signalPath;
    [SerializeField] private float cornerTolerance = 1.0f;

    [Header("Visual Feedback Settings")]
    [Tooltip("The line color when the path is correct.")]
    [SerializeField] private Color successColor = Color.green;
    [Tooltip("The line color when the path is incorrect.")]
    [SerializeField] private Color failureColor = Color.red;

    [Tooltip("The line Prefab to be instantiated for feedback.")]
    [SerializeField] private LineRenderer linePrefab;

    [Tooltip("The parent object that will group the feedback lines.")]
    [SerializeField] private Transform feedbackLinesParent;

    [Header("Dashed Line Settings (Math)")]
    [Tooltip("The length of each red dash.")]
    [SerializeField] private float dashLength = 0.15f;
    [Tooltip("The length of the empty gap between dashes.")]
    [SerializeField] private float dashGap = 0.1f;

    [Header("Debug")]
    [Tooltip("If enabled, shows detailed logs about the path validation.")]
    [SerializeField] private bool enableDebugLogs = false;
    [Tooltip("If enabled, shows gizmos for missed corners during gameplay.")]
    [SerializeField] private bool showMissedCornersInGame = false;
    [Tooltip("If enabled, colors the player's line in real-time during gameplay.")]
    [SerializeField] private bool realtimeFeedback = false;

    private List<Vector3> missedCorners = new List<Vector3>();
    private LineRenderer signalLineRenderer;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        GenerateCorrectPath();
    }

    private void Start()
    {
        if (signalPath != null)
            signalLineRenderer = signalPath.GetComponent<LineRenderer>();

        // Registra callback para atualizar cores ao vivo caso o jogador mude nas configurações
        SignalColorManager.OnColorsChanged += SyncFeedbackColors;

        // Aplica as cores salvas imediatamente
        SyncFeedbackColors();
    }

    private void OnDestroy()
    {
        SignalColorManager.OnColorsChanged -= SyncFeedbackColors;
    }

    private void Update()
    {
        if (!realtimeFeedback || signalPath == null || correctCorners == null || signalPath.PathPoints.Count < 2)
            return;

        UpdateRealtimeFeedback();
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

    #region Color Sync

    /// <summary>
    /// Sincroniza successColor e failureColor com o SignalColorManager.
    /// Chamado no Start e sempre que OnColorsChanged for disparado.
    /// </summary>
    private void SyncFeedbackColors()
    {
        if (SignalColorManager.Instance == null) return;

        successColor = SignalColorManager.Instance.ColorFeedbackSuccess;
        failureColor = SignalColorManager.Instance.ColorFeedbackFailure;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Finalizes and evaluates the path until a specific X coordinate (e.g., player's death position).
    /// Called when player dies or finishes the level.
    /// </summary>
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

    private void CheckPlayerPath()
    {
        // Garante cores atualizadas no momento da verificação final
        SyncFeedbackColors();

        if (signalPath == null || signalPath.PathPoints.Count < 2)
        {
            Debug.LogError("Caminho do jogador inválido ou não definido!");
            ResultScreenController.Instance?.Show(false);
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

        int gabaritoTotal = correctCorners.Count - 1; ;
        DrawFeedbackLines(signalPath.PathPoints, out int correct, out int total);
        int coveredSegments = CountCoveredGabaritoSegments(signalPath.PathPoints);
        ScoreController.Instance?.ReportResult(coveredSegments, gabaritoTotal, isPathCorrectOverall);

        if (enableDebugLogs)
        {
            int correctCount = cornerChecks.FindAll(x => x).Count;
            int totalCount = cornerChecks.Count;
            Debug.Log($"<color=yellow>[PathVerifier] Resultado: {correctCount}/{totalCount} quinas atingidas</color>");
        }

        ResultScreenController.Instance?.Show(isPathCorrectOverall);
    }

    private int CountCoveredGabaritoSegments(List<Vector3> playerPath)
    {
        int covered = 0;
        for (int i = 0; i < correctCorners.Count - 1; i++)
        {
            Vector3 midpoint = (correctCorners[i] + correctCorners[i + 1]) / 2f;
            Vector3 closest = FindClosestPointOnFullPath(midpoint, playerPath);
            if (Vector3.Distance(midpoint, closest) <= cornerTolerance)
                covered++;
        }
        return covered;
    }
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

            // Usa a nova lógica estrita de bounding box
            bool isSegmentCorrect = IsSegmentStrictlyValid(p_start, p_end);

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

            // Usa a nova lógica estrita de bounding box ao vivo
            bool isSegmentCorrect = IsSegmentStrictlyValid(p_start, p_end);

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
    #endregion

    #region Helper Functions

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
    /// Verifica se um segmento do jogador está dentro dos limites ortogonais da tolerância,
    /// mas corta o excesso longitudinal (evitando que a linha verde invada a vermelha na quina).
    /// </summary>
    private bool IsSegmentStrictlyValid(Vector3 p_start, Vector3 p_end)
    {
        // Se o segmento for microscópico (pontos duplicados), consideramos válido para evitar flickering
        if ((p_end - p_start).sqrMagnitude < 0.0001f) return true;

        for (int i = 0; i < correctCorners.Count - 1; i++)
        {
            Vector3 c1 = correctCorners[i];
            Vector3 c2 = correctCorners[i + 1];
            bool isCHoriz = Mathf.Abs(c1.y - c2.y) <= 0.1f;

            if (isCHoriz)
            {
                // Verifica a distância ortogonal (Y) mantendo a tolerância original
                if (Mathf.Abs(p_start.y - c1.y) <= cornerTolerance &&
                    Mathf.Abs(p_end.y - c1.y) <= cornerTolerance)
                {
                    // Limite estrito longitudinal (X) com uma margem minúscula (0.1f) para não quebrar a quina
                    float minX = Mathf.Min(c1.x, c2.x) - 0.1f;
                    float maxX = Mathf.Max(c1.x, c2.x) + 0.1f;

                    if (p_start.x >= minX && p_start.x <= maxX &&
                        p_end.x >= minX && p_end.x <= maxX)
                    {
                        return true;
                    }
                }
            }
            else
            {
                // Verifica a distância ortogonal (X) mantendo a tolerância original
                if (Mathf.Abs(p_start.x - c1.x) <= cornerTolerance &&
                    Mathf.Abs(p_end.x - c1.x) <= cornerTolerance)
                {
                    // Limite estrito longitudinal (Y) com uma margem minúscula (0.1f)
                    float minY = Mathf.Min(c1.y, c2.y) - 0.1f;
                    float maxY = Mathf.Max(c1.y, c2.y) + 0.1f;

                    if (p_start.y >= minY && p_start.y <= maxY &&
                        p_end.y >= minY && p_end.y <= maxY)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
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

    private void GenerateCorrectPath()
    {
        var loader = UnityEngine.Object.FindAnyObjectByType<LevelJsonLoader>();
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