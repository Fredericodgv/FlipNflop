// /Assets/Scripts/PathVerifier.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Text;

public class PathVerifier : MonoBehaviour
{
    #region Fields

    private struct DrawingPoint
    {
        public Vector3 position;
        public bool isCorrect;
        public override string ToString() => $"Pos: {position}, Correct: {isCorrect}";
    }

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
    [Tooltip("A posição X onde a verificação termina.")]
    [SerializeField] private float endX = 25f;
    [Tooltip("O intervalo em X para cada verificação (o 'pulso do clock').")]
    [SerializeField] private float clockStepX = 5f;

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

    /// <summary>
    /// Ponto de entrada principal para finalizar e verificar o caminho do jogador.
    /// </summary>
    public void FinalizeAndCheckPath()
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
    /// Orquestra o processo de verificação do caminho desenhado pelo jogador.
    /// </summary>
    private void CheckPlayerPath()
    {
        if (signalPath == null)
        {
            Debug.LogError("Referência ao SignalPath não definida!");
            return;
        }

        Debug.ClearDeveloperConsole();

        List<Vector3> playerCorners = ExtractCorners(signalPath.PathPoints);
        List<bool> cornerChecks = EvaluateCorrectCorners(signalPath.PathPoints);
        bool isPathCorrectOverall = !cornerChecks.Contains(false) && playerCorners.Count <= correctCorners.Count;

        PrintVectorListToDebug("--- Quinas Corretas (Gabarito) ---", correctCorners);
        PrintVectorListToDebug("--- Quinas do Jogador ---", playerCorners);
        
        signalPath.GetComponent<LineRenderer>().enabled = false;

        List<DrawingPoint> drawingPath = BuildDrawingPath(playerCorners, cornerChecks);
        PrintDrawingPathToDebug("--- Caminho de Desenho Final (com pontos de erro injetados) ---", drawingPath);

        DrawFeedbackLines(drawingPath);

        if (isPathCorrectOverall)
        {
            Debug.Log("<color=green>VERIFICAÇÃO BEM-SUCEDIDA! O caminho está correto.</color>");
            if (successUI != null) successUI.SetActive(true);
        }
        else
        {
            Debug.LogError("VERIFICAÇÃO FALHOU!");
            if (failureUI != null) failureUI.SetActive(true);
        }
    }

    /// <summary>
    /// Avalia se a linha completa do jogador passa perto de cada quina do gabarito.
    /// </summary>
    private List<bool> EvaluateCorrectCorners(List<Vector3> playerPath)
    {
        List<bool> checks = new List<bool>();
        foreach (Vector3 correctCorner in correctCorners)
        {
            bool cornerWasHit = false;
            for (int i = 0; i < playerPath.Count - 1; i++)
            {
                Vector3 p1 = playerPath[i];
                Vector3 p2 = playerPath[i + 1];
                Vector3 closestPoint = FindClosestPointOnLineSegment(correctCorner, p1, p2);

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
    /// Constrói a lista de pontos para desenhar, injetando pontos de falha no caminho do jogador.
    /// </summary>
    private List<DrawingPoint> BuildDrawingPath(List<Vector3> playerCorners, List<bool> cornerChecks)
    {
        var path = new List<DrawingPoint>();
        if (playerCorners.Count == 0) return path;

        for (int i = 0; i < playerCorners.Count; i++)
        {
            Vector3 p_current = playerCorners[i];
            int currentCorrectIndex = FindClosestCorrectCornerIndex(p_current);
            bool isCurrentCorrect = currentCorrectIndex != -1 && cornerChecks[currentCorrectIndex];
            path.Add(new DrawingPoint { position = p_current, isCorrect = isCurrentCorrect });

            if (i < playerCorners.Count - 1)
            {
                Vector3 p_next = playerCorners[i + 1];
                int nextCorrectIndex = FindClosestCorrectCornerIndex(p_next);

                if (currentCorrectIndex != -1 && nextCorrectIndex != -1 && currentCorrectIndex < nextCorrectIndex)
                {
                    var missedPointsToInject = new List<DrawingPoint>();
                    for (int j = currentCorrectIndex + 1; j < nextCorrectIndex; j++)
                    {
                        if (!cornerChecks[j])
                        {
                            Vector3 missedCorrectPos = correctCorners[j];
                            Vector3 p1 = p_current;
                            Vector3 p2 = p_next;
                            float t = (p2.x - p1.x) == 0 ? 0 : (missedCorrectPos.x - p1.x) / (p2.x - p1.x);
                            Vector3 projectedPos = Vector3.Lerp(p1, p2, t);
                            missedPointsToInject.Add(new DrawingPoint { position = projectedPos, isCorrect = false });
                        }
                    }
                    missedPointsToInject.Sort((a, b) => a.position.x.CompareTo(b.position.x));
                    path.AddRange(missedPointsToInject);
                }
            }
        }
        return path;
    }

    /// <summary>
    /// Desenha os segmentos de linha coloridos com base no caminho processado.
    /// </summary>
    private void DrawFeedbackLines(List<DrawingPoint> drawingPath)
    {
        if (linePrefab == null || feedbackLinesParent == null) return;
        foreach (Transform child in feedbackLinesParent) Destroy(child.gameObject);

        Debug.Log("--- Desenhando Linhas de Feedback ---");

        for (int i = 0; i < drawingPath.Count - 1; i++)
        {
            DrawingPoint startPoint = drawingPath[i];
            DrawingPoint endPoint = drawingPath[i + 1];
            
            Color segmentColor = startPoint.isCorrect ? successColor : failureColor;
            string colorName = startPoint.isCorrect ? "VERDE" : "VERMELHO";
            Debug.Log($"Desenhando segmento {i}: De {startPoint} para {endPoint} | COR: {colorName}");

            LineRenderer lineSegment = Instantiate(linePrefab, feedbackLinesParent);
            lineSegment.SetPosition(0, startPoint.position);
            lineSegment.SetPosition(1, endPoint.position);
            lineSegment.startColor = segmentColor;
            lineSegment.endColor = segmentColor;
        }
    }
    
    #endregion

    #region Helper Functions
    
    private int FindClosestCorrectCornerIndex(Vector3 playerCorner)
    {
        for (int i = 0; i < correctCorners.Count; i++)
        {
            if (Vector3.Distance(playerCorner, correctCorners[i]) <= cornerTolerance)
            {
                return i;
            }
        }
        return -1;
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
        for (float x = startX; x <= endX; x += clockStepX)
        {
            float previousY = outputState ? highY : lowY;
            correctCorners.Add(new Vector3(x, previousY, 0));
            if (x >= endX) break;
            bool j_input = IsSignalHigh(j_InputTilemap, x, j_InputCheckY);
            bool k_input = IsSignalHigh(k_InputTilemap, x, k_InputCheckY);
            if (j_input && !k_input) outputState = true;
            else if (!j_input && k_input) outputState = false;
            else if (j_input && k_input) outputState = !outputState;
            else if (!j_input && !k_input) continue;
            float currentY = outputState ? highY : lowY;
            if (!Mathf.Approximately(currentY, previousY)) { correctCorners.Add(new Vector3(x, currentY, 0)); }
        }
        for (int i = correctCorners.Count - 1; i > 0; i--) { if (Vector3.Distance(correctCorners[i], correctCorners[i - 1]) < 0.01f) { correctCorners.RemoveAt(i); } if (i == 0 || i >= correctCorners.Count - 1) continue; if (correctCorners[i - 1].y == correctCorners[i].y && correctCorners[i + 1].y == correctCorners[i].y) { correctCorners.RemoveAt(i); } }
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
        if (list.Count == 0)
        {
            sb.AppendLine(" (lista vazia)");
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
            {
                sb.AppendLine($"  [{i}]: {list[i]}");
            }
        }
        Debug.Log(sb.ToString());
    }

    private void PrintDrawingPathToDebug(string header, List<DrawingPoint> list)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color=cyan>{header}</color>");
        if (list.Count == 0)
        {
            sb.AppendLine(" (lista vazia)");
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
            {
                sb.AppendLine($"  [{i}]: {list[i].ToString()}");
            }
        }
        Debug.Log(sb.ToString());
    }

    #endregion
}