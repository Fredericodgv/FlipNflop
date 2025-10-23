// /Assets/Scripts/PathVerifier.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Text;

public class PathVerifier : MonoBehaviour
{
    #region Fields

    [Header("Referências de Entrada")]
    [Tooltip("O Tilemap que representa a entrada J.")]
    [SerializeField] private Tilemap j_InputTilemap;
    [Tooltip("A coordenada Y no mundo onde a linha de entrada J será verificada.")]
    [SerializeField] private float j_InputCheckY = 8.5f;

    [Tooltip("O Tilemap que representa a entrada K.")]
    [SerializeField] private Tilemap k_InputTilemap;
    [Tooltip("A coordenada Y no mundo onde a linha de entrada K será verificada.")]
    [SerializeField] private float k_InputCheckY = 5.5f;

    [Tooltip("O asset de Tile que representa um sinal em nível ALTO (1).")]
    [SerializeField] private TileBase highSignalTile;

    [Header("Configuração da Saída")]
    [Tooltip("A posição Y para o nível lógico BAIXO (0) da saída.")]
    [SerializeField] private float lowY = -2.5f;
    [Tooltip("A posição Y para o nível lógico ALTO (1) da saída.")]
    [SerializeField] private float highY = 1.25f;
    [Tooltip("A posição X onde a verificação começa.")]
    [SerializeField] private float startX = -5f;

    // ClockStepX agora é centralizado no LevelManager.

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

    private void Awake()
    {
        GenerateCorrectPath();
    }

    private void Start()
    {
        if (successUI != null) successUI.SetActive(false);
        if (failureUI != null) failureUI.SetActive(false);
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
        {
            Gizmos.DrawSphere(correctCorners[correctCorners.Count - 1], 0.2f);
        }
    }

    #endregion

    #region Public Methods

    public void FinalizeAndCheckPath()
    {
        if (signalPath != null)
        {
            signalPath.FinalizePath(LevelManager.Instance.levelEndX);
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

    private void CheckPlayerPath()
    {
        if (signalPath == null || signalPath.PathPoints.Count < 2)
        {
            Debug.LogError("Caminho do jogador inválido ou não definido!");
            if (failureUI != null) failureUI.SetActive(true);
            return;
        }

        signalPath.GetComponent<LineRenderer>().enabled = false;

        DrawFeedbackLines(signalPath.PathPoints);

        // A checagem de sucesso geral ainda se baseia em o jogador ter passado por todas as quinas.
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
    /// Desenha o feedback iterando sobre cada pequeno segmento do caminho do jogador.
    /// </summary>
    private void DrawFeedbackLines(List<Vector3> playerPath)
    {
        if (linePrefab == null || feedbackLinesParent == null) return;
        foreach (Transform child in feedbackLinesParent) Destroy(child.gameObject);

        // Itera sobre cada pequeno segmento que o jogador desenhou.
        for (int i = 0; i < playerPath.Count - 1; i++)
        {
            Vector3 p_start = playerPath[i];
            Vector3 p_end = playerPath[i + 1];

            // Verifica se o ponto de início do segmento está na trajetória correta.
            Vector3 closestToStart = FindClosestPointOnFullPath(p_start, correctCorners);
            bool startIsOnPath = Vector3.Distance(p_start, closestToStart) <= cornerTolerance;

            // Verifica se o ponto de fim do segmento está na trajetória correta.
            Vector3 closestToEnd = FindClosestPointOnFullPath(p_end, correctCorners);
            bool endIsOnPath = Vector3.Distance(p_end, closestToEnd) <= cornerTolerance;

            // O segmento só é verde se AMBOS os pontos estiverem na trajetória.
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

    // Ativa o objeto alvo e garante que seu pai (container comum) também esteja ativo.
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

    private List<Vector3> ExtractCorners(List<Vector3> allPoints)
    {
        if (allPoints == null || allPoints.Count < 2) return new List<Vector3>();
        var corners = new List<Vector3> { allPoints[0] };
        for (int i = 1; i < allPoints.Count - 1; i++)
        {
            Vector3 pDir = (allPoints[i] - allPoints[i - 1]).normalized;
            Vector3 nDir = (allPoints[i + 1] - allPoints[i]).normalized;
            if (Mathf.Abs(Vector3.Dot(pDir, nDir)) < 0.1f) { corners.Add(allPoints[i]); }
        }
        corners.Add(allPoints[allPoints.Count - 1]);
        return corners;
    }

    #endregion


    #region Gabarito Generation

    private void GenerateCorrectPath()
    {
        if (j_InputTilemap == null || k_InputTilemap == null || highSignalTile == null) { correctCorners = new List<Vector3>(); return; }
        correctCorners = new List<Vector3>();
        bool outputState = false;
        float step = (LevelManager.Instance != null && LevelManager.Instance.clockStepX > 0f)
            ? LevelManager.Instance.clockStepX
            : 5f; // fallback não-serializado
        if (step <= 0f) step = 0.0001f;
        for (float x = startX; x <= LevelManager.Instance.levelEndX; x += step)
        {
            float previousY = outputState ? highY : lowY;
            correctCorners.Add(new Vector3(x, previousY, 0));
            if (x >= LevelManager.Instance.levelEndX) break;
            bool j_input = IsSignalHigh(j_InputTilemap, x, j_InputCheckY);
            bool k_input = IsSignalHigh(k_InputTilemap, x, k_InputCheckY);
            if (j_input && !k_input) outputState = true;
            else if (!j_input && k_input) outputState = false;
            else if (j_input && k_input) outputState = !outputState;
            else if (!j_input && !k_input) continue;
            float currentY = outputState ? highY : lowY;
            if (!Mathf.Approximately(currentY, previousY)) { correctCorners.Add(new Vector3(x, currentY, 0)); }
        }
        for (int i = correctCorners.Count - 1; i > 0; i--)
        {
            if (Vector3.Distance(correctCorners[i], correctCorners[i - 1]) < 0.01f) { correctCorners.RemoveAt(i); }
            if (i == 0 || i >= correctCorners.Count - 1) continue;
            if (correctCorners[i - 1].y == correctCorners[i].y && correctCorners[i + 1].y == correctCorners[i].y) { correctCorners.RemoveAt(i); }
        }
    }

    private bool IsSignalHigh(Tilemap tilemap, float x, float checkY)
    {
        if (tilemap == null) return false;
        Vector3 worldCheckPos = new Vector3(x + 0.1f, checkY, 0);
        Vector3Int cellPos = tilemap.WorldToCell(worldCheckPos);
        TileBase tile = tilemap.GetTile(cellPos);
        return tile == highSignalTile;
    }

    #endregion

    #region Debug Functions

    private void PrintVectorListToDebug(string header, List<Vector3> list)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color=yellow>{header}</color>");
        if (list.Count == 0) sb.AppendLine(" (lista vazia)");
        else
        {
            for (int i = 0; i < list.Count; i++) sb.AppendLine($"  [{i}]: {list[i]}");
        }
        Debug.Log(sb.ToString());
    }

    #endregion
}