using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathVerifier : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Arraste aqui o GameObject do jogador que contém o script SignalPath.")]
    [SerializeField] private SignalPath playerCircuit;

    [Header("Gabarito das Quinas")]
    [Tooltip("Defina aqui APENAS os pontos de quina que representam o caminho correto.")]
    public List<Vector3> correctCorners;
    
    [Tooltip("Uma margem de erro para a comparação das posições das quinas.")]
    [SerializeField] private float cornerTolerance = 0.5f;

    [Header("Telas de Feedback")]
    [Tooltip("A tela que aparece quando o caminho está CORRETO.")]
    [SerializeField] private GameObject successUI;

    [Tooltip("A tela que aparece quando o caminho está INCORRETO.")]
    [SerializeField] private GameObject failureUI;

    private void Start()
    {
        if (successUI != null) successUI.SetActive(false);
        if (failureUI != null) failureUI.SetActive(false);
    }

    /// <summary>
    /// Verifica o caminho do jogador e ativa a UI correspondente.
    /// </summary>
    public void CheckPlayerPath()
    {
        if (playerCircuit == null)
        {
            Debug.LogError("Referência ao SignalPath não definida!");
            return;
        }

        List<Vector3> playerCorners = ExtractCorners(playerCircuit.PathPoints);

        if (playerCorners.Count != correctCorners.Count)
        {
            Debug.Log($"Falha: O jogador fez {playerCorners.Count} quinas, mas o esperado eram {correctCorners.Count}.");
            if (failureUI != null) failureUI.SetActive(true);
            return;
        }

        for (int i = 0; i < correctCorners.Count; i++)
        {
            if (Vector3.Distance(playerCorners[i], correctCorners[i]) > cornerTolerance)
            {
                Debug.Log($"Falha na quina {i}: Posição do jogador {playerCorners[i]}, esperado {correctCorners[i]}.");
                if (failureUI != null) failureUI.SetActive(true);
                return;
            }
        }
        
        Debug.Log("Sucesso! O caminho do circuito está correto.");
        if (successUI != null) successUI.SetActive(true);
    }

    private List<Vector3> ExtractCorners(List<Vector3> allPoints)
    {
        if (allPoints == null || allPoints.Count < 2) return new List<Vector3>();

        List<Vector3> corners = new List<Vector3>();
        corners.Add(allPoints[0]);

        for (int i = 1; i < allPoints.Count - 1; i++)
        {
            Vector3 previousDirection = (allPoints[i] - allPoints[i - 1]).normalized;
            Vector3 nextDirection = (allPoints[i + 1] - allPoints[i]).normalized;
            float dotProduct = Vector3.Dot(previousDirection, nextDirection);

            if (Mathf.Abs(dotProduct) < 0.1f)
            {
                corners.Add(allPoints[i]);
            }
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
        Gizmos.DrawSphere(correctCorners[correctCorners.Count - 1], 0.1f);
    }
}