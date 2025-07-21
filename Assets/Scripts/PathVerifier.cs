using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Renomeie a classe para corresponder ao novo nome do arquivo
public class PathVerifier : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Arraste aqui o GameObject do jogador que contém o PlayerController.")]
    [SerializeField] private PlayerController playerController;

    [Header("Gabarito das Quinas")]
    public List<Vector3> correctCorners;
    [SerializeField] private float cornerTolerance = 0.5f;

    // --- ALTERAÇÃO PRINCIPAL AQUI ---
    [Header("UI de Feedback")]
    [Tooltip("Arraste aqui o CANVAS que contém os painéis de sucesso e falha.")]
    [SerializeField] private GameObject feedbackCanvas; 

    private void Start()
    {
        // Garante que o Canvas e seus filhos comecem no estado correto
        if (feedbackCanvas != null)
        {
            feedbackCanvas.SetActive(true); // O Canvas pode estar ativo
            // Mas os painéis devem começar desativados
            feedbackCanvas.transform.Find("SuccessPanel").gameObject.SetActive(false);
            feedbackCanvas.transform.Find("FailurePanel").gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Verifica o caminho do jogador e ativa o painel correspondente dentro do Canvas.
    /// </summary>
    public void CheckPlayerPath()
    {
        if (feedbackCanvas == null)
        {
            Debug.LogError("Referência ao FeedbackCanvas não definida!");
            return;
        }

        bool isPathCorrect = VerifyCorners();
        
        if (isPathCorrect)
        {
            Debug.Log("Sucesso! O caminho do circuito está correto.");
            // Encontra o painel de sucesso PELO NOME e o ativa
            feedbackCanvas.transform.Find("SuccessPanel").gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("Falha na verificação do caminho.");
            // Encontra o painel de falha PELO NOME e o ativa
            feedbackCanvas.transform.Find("FailurePanel").gameObject.SetActive(true);
        }
    }
    
    // As funções VerifyCorners, ExtractCorners e OnDrawGizmos continuam as mesmas
    private bool VerifyCorners()
    {
        // Esta lógica não precisa de alterações
        if (playerController == null) return false;
        List<Vector3> playerCorners = ExtractCorners(playerController.GetComponent<SignalPath>().PathPoints); 
        if (playerCorners.Count != correctCorners.Count) return false;
        for (int i = 0; i < correctCorners.Count; i++)
        {
            if (Vector3.Distance(playerCorners[i], correctCorners[i]) > cornerTolerance) return false;
        }
        return true;
    }

    private List<Vector3> ExtractCorners(List<Vector3> allPoints)
    {
        // Esta lógica não precisa de alterações
        if (allPoints == null || allPoints.Count < 2) return new List<Vector3>();
        List<Vector3> corners = new List<Vector3>();
        corners.Add(allPoints[0]);
        for (int i = 1; i < allPoints.Count - 1; i++)
        {
            Vector3 previousDirection = (allPoints[i] - allPoints[i - 1]).normalized;
            Vector3 nextDirection = (allPoints[i + 1] - allPoints[i]).normalized;
            if (Mathf.Abs(Vector3.Dot(previousDirection, nextDirection)) < 0.1f)
            {
                corners.Add(allPoints[i]);
            }
        }
        corners.Add(allPoints[allPoints.Count - 1]);
        return corners;
    }

    private void OnDrawGizmos()
    {
        // Esta lógica não precisa de alterações
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