using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathVerifier : MonoBehaviour
{
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
    [SerializeField] private float cornerTolerance = 0.5f;

    private void Awake()
    {
        GenerateCorrectPath();
    }

    private void Start()
    {
        if (successUI != null) successUI.SetActive(false);
        if (failureUI != null) failureUI.SetActive(false);
    }
    

    private void GenerateCorrectPath()
    {
        if (j_InputTilemap == null || k_InputTilemap == null || highSignalTile == null)
        {
            correctCorners = new List<Vector3>();
            return;
        }

        correctCorners = new List<Vector3>();
        bool outputState = false; // Estado Q inicial é 0 (baixo)

        // Itera pelos PONTOS DE TRANSIÇÃO (as bordas do clock)
        for (float x = startX; x <= endX; x += clockStepX)
        {
            // Pega a altura Y da linha ANTES deste ponto de transição 'x'
            float previousY = outputState ? highY : lowY;

            // Adiciona o ponto que finaliza o segmento horizontal anterior
            correctCorners.Add(new Vector3(x, previousY, 0));

            // Se já chegamos ao final do percurso, não precisamos calcular o próximo estado
            if (x >= endX)
            {
                break;
            }

            // Lê as entradas no início do segmento (em 'x') para determinar o PRÓXIMO estado
            bool j_input = IsSignalHigh(j_InputTilemap, x, j_InputCheckY);
            bool k_input = IsSignalHigh(k_InputTilemap, x, k_InputCheckY);
            
            // Aplica a tabela verdade do Flip-Flop JK para definir o novo estado da saída
            if (j_input && !k_input) outputState = true;
            else if (!j_input && k_input) outputState = false;
            else if (j_input && k_input) outputState = !outputState;
            else if (!j_input && !k_input) continue; // Estado permanece o mesmo

            float currentY = outputState ? highY : lowY;

            // Se o estado mudou, precisamos de uma linha vertical.
            // Para isso, adicionamos um novo ponto na MESMA posição X, mas na NOVA altura Y.
            if (!Mathf.Approximately(currentY, previousY))
            {
                correctCorners.Add(new Vector3(x, currentY, 0));
            }
        }


        for (int i = correctCorners.Count - 1; i > 0; i--)
        {
            if (Vector3.Distance(correctCorners[i], correctCorners[i - 1]) < 0.01f)
            {
                correctCorners.RemoveAt(i);
            }
            if (i == 0 || i == correctCorners.Count - 1) continue;
            // Remove quinas desnecessárias (se a quina não muda a altura)
            if (correctCorners[i - 1].y == correctCorners[i].y && correctCorners[i + 1].y == correctCorners[i].y)
            {
                correctCorners.RemoveAt(i);
            }
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


    private void CheckPlayerPath()
    {
        if (signalPath == null) { Debug.LogError("Referência ao SignalPath não definida!"); return; }
        bool isPathCorrect = VerifyCorners(signalPath.PathPoints);
        if (isPathCorrect) { if (successUI != null) successUI.SetActive(true); }
        else { if (failureUI != null) failureUI.SetActive(true); }
    }

 private bool VerifyCorners(List<Vector3> playerPath)
    {
        List<Vector3> playerCorners = ExtractCorners(playerPath);

        if (playerCorners.Count > 0)
        {
            Vector3 firstCorner = playerCorners[0];
            firstCorner.x = this.startX; // "this.startX" é o -5 do nosso gabarito
            playerCorners[0] = firstCorner;
        }

        if (playerCorners.Count != correctCorners.Count) 
        { 
            Debug.Log($"Contagem de quinas incorreta. Esperado: {correctCorners.Count}, Jogador: {playerCorners.Count}"); 
            return false; 
        }

        for (int i = 0; i < correctCorners.Count; i++)
        {
            if (Vector3.Distance(playerCorners[i], correctCorners[i]) > cornerTolerance) 
            { 
                Debug.Log($"Posição da quina {i} incorreta. Player: {playerCorners[i]} | Correta: {correctCorners[i]}"); 
                return false; 
            }
        }
        
        Debug.Log("VERIFICAÇÃO BEM-SUCEDIDA! O caminho está correto.");
        return true;
    }

    private List<Vector3> ExtractCorners(List<Vector3> allPoints)
    {
        if (allPoints == null || allPoints.Count < 2) return new List<Vector3>();
        List<Vector3> corners = new List<Vector3>();
        corners.Add(allPoints[0]);
        for (int i = 1; i < allPoints.Count - 1; i++)
        {
            Vector3 pDir = (allPoints[i] - allPoints[i - 1]).normalized;
            Vector3 nDir = (allPoints[i + 1] - allPoints[i]).normalized;
            if (Mathf.Abs(Vector3.Dot(pDir, nDir)) < 0.1f) { corners.Add(allPoints[i]); }
        }
        corners.Add(allPoints[allPoints.Count - 1]);
        return corners;
    }

    private void OnDrawGizmos()
    {
        if (correctCorners == null || correctCorners.Count < 2) return;
        Gizmos.color = Color.green;
        for (int i = 0; i < correctCorners.Count - 1; i++)
        {
            Gizmos.DrawSphere(correctCorners[i], 0.1f);
            Gizmos.DrawLine(correctCorners[i], correctCorners[i + 1]);
        }
        if (correctCorners.Count > 0)
        {
            Gizmos.DrawSphere(correctCorners[correctCorners.Count - 1], 0.1f);
        }
    }
}