// /Assets/Scripts/PathVerifier.cs

using System.Collections.Generic;
using UnityEngine;
// (removed) using System.Text; - not needed in this file

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
    [SerializeField] private GameObject successUI;
    [SerializeField] private GameObject failureUI;
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
        if (successUI != null) successUI.SetActive(false);
        if (failureUI != null) failureUI.SetActive(false);
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
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Finalizes the player's drawn path up to the level end X and runs the verification routine.
    /// </summary>
    public void FinalizeAndCheckPath()
    {
        if (signalPath != null)
        {
            signalPath.FinalizePath(LevelManager.Instance != null ? LevelManager.Instance.phaseEndX : (correctCorners != null && correctCorners.Count > 0 ? correctCorners[correctCorners.Count - 1].x : 0f));
        }
        else
        {
            Debug.LogError("Referência ao SignalPath não está definida no PathVerifier!");
            return;
        }
        CheckPlayerPath();
    }

    /// <summary>
    /// Finaliza e avalia o caminho até um X específico (ex.: posição de morte do jogador).
    /// </summary>
    /// <param name="endX">Coordenada X limite do traçado.</param>
    public void FinalizeAndCheckPathUntil(float endX)
    {
        if (signalPath != null)
        {
            signalPath.FinalizePath(endX);
        }
        else
        {
            Debug.LogError("Referência ao SignalPath não está definida no PathVerifier!");
            return;
        }
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
            if (failureUI != null) failureUI.SetActive(true);
            return;
        }

        signalPath.GetComponent<LineRenderer>().enabled = false;
        // Gabarito já inclui phaseEndX (diagramEndX + slack). Sem necessidade de ajuste dinâmico.

        DrawFeedbackLines(signalPath.PathPoints);

        List<bool> cornerChecks = EvaluateCorrectCorners(signalPath.PathPoints);
        bool isPathCorrectOverall = !cornerChecks.Contains(false);

        if (isPathCorrectOverall)
        {
            if (successUI != null)
            {
                ActivateWithParent(successUI);
                if (failureUI != null) failureUI.SetActive(false);
            }
        }
        else
        {
            if (failureUI != null)
            {
                ActivateWithParent(failureUI);
                if (successUI != null) successUI.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Se o último X do caminho do jogador excede o último X das quinas corretas, adiciona uma quina extra
    /// (mesmo nível lógico) para cobrir o deslocamento final.
    /// </summary>
    // Removido TryAppendTailCorner: fase estendida tratada na geração do gabarito.

    /// <summary>
    /// Desenha o feedback iterando sobre cada pequeno segmento do caminho do jogador.
    /// </summary>
    private void DrawFeedbackLines(List<Vector3> playerPath)
    {
        if (linePrefab == null || feedbackLinesParent == null) return;
        foreach (Transform child in feedbackLinesParent) Destroy(child.gameObject);

        for (int i = 0; i < playerPath.Count - 1; i++)
        {
            Vector3 p_start = playerPath[i];
            Vector3 p_end = playerPath[i + 1];

            Vector3 closestToStart = FindClosestPointOnFullPath(p_start, correctCorners);
            bool startIsOnPath = Vector3.Distance(p_start, closestToStart) <= cornerTolerance;

            Vector3 closestToEnd = FindClosestPointOnFullPath(p_end, correctCorners);
            bool endIsOnPath = Vector3.Distance(p_end, closestToEnd) <= cornerTolerance;

            bool isSegmentCorrect = startIsOnPath && endIsOnPath;

            Color segmentColor = isSegmentCorrect ? successColor : failureColor;

            LineRenderer lineSegment = Instantiate(linePrefab, feedbackLinesParent);
            lineSegment.SetPosition(0, p_start);
            lineSegment.SetPosition(1, p_end);
            lineSegment.startColor = segmentColor;
            lineSegment.endColor = segmentColor;
        }
    }

    #endregion

    #region Helper Functions

    /// <summary>
    /// Activates the given GameObject and ensures its parent container is active.
    /// </summary>
    private void ActivateWithParent(GameObject target)
    {
        if (target == null) return;
        Transform parent = target.transform.parent;
        if (parent != null && !parent.gameObject.activeSelf)
        {
            parent.gameObject.SetActive(true);
        }
        if (!target.activeSelf) target.SetActive(true);
    }

    /// <summary>
    /// Avalia se a linha completa do jogador passa perto de cada quina do gabarito. Usado para o resultado final (sucesso/falha).
    /// </summary>
    private List<bool> EvaluateCorrectCorners(List<Vector3> playerPath)
    {
        var checks = new List<bool>();
        foreach (Vector3 correctCorner in correctCorners)
        {
            bool cornerWasHit = false;
            for (int i = 0; i < playerPath.Count - 1; i++)
            {
                Vector3 closestPoint = FindClosestPointOnLineSegment(correctCorner, playerPath[i], playerPath[i + 1]);
                if (Vector3.Distance(closestPoint, correctCorner) <= cornerTolerance)
                {
                    cornerWasHit = true;
                    break;
                }
            }
            checks.Add(cornerWasHit);
        }
        return checks;
    }

    /// <summary>
    /// Finds the closest point on the polyline defined by 'path' to the given target point.
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
    /// Returns the closest point on the line segment [lineStart, lineEnd] to the specified point.
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

    /// <summary>
    /// Removes intermediate colinear points from a dense polyline and returns only the corner points.
    /// </summary>
    // ExtractCorners removed — unused helper

    #endregion


    #region Gabarito Generation

    /// <summary>
    /// Generate the reference path (correctCorners) using the LevelJsonLoader.
    /// This method assumes a LevelJsonLoader exists in the scene. If it's missing, an error is logged.
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
        if (events == null || events.Count == 0)
        {
            correctCorners = new List<Vector3>();
            return;
        }

        BuildCorrectCornersFromSignalEvents(events, 0f, false);
    }

    /// <summary>
    /// Build the correctCorners list from an array of output samples (one per clock tick).
    /// The initial state is assumed LOW at x=0; the first sample corresponds to x = step.
    /// </summary>
    public struct SignalEvent
    {
        // X position (world or tile units) where this sample/event occurs
        public float x;
        public bool value;

        public SignalEvent(float x, bool value)
        {
            this.x = x;
            this.value = value;
        }
    }

    /// <summary>
    /// Build the correctCorners list from an ordered list of signal events. Each event defines the
    /// sampled output level at position X. This is generic and can represent clock-aligned samples
    /// or asynchronous inputs (preset/clear) — the loader should prepare the event list.
    /// The initial state is assumed LOW at x=initialX unless initialState is set to true.
    /// </summary>
    public void BuildCorrectCornersFromSignalEvents(List<SignalEvent> events, float initialX = 0f, bool initialState = false)
    {
        if (events == null || events.Count == 0)
        {
            correctCorners = new List<Vector3>();
            return;
        }

        events.Sort((a, b) => a.x.CompareTo(b.x));

        correctCorners = new List<Vector3>();
        bool qState = initialState;
        correctCorners.Add(new Vector3(initialX, qState ? highY : lowY, 0f));

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

        // Ensure the final horizontal segment is represented up to the level end X
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