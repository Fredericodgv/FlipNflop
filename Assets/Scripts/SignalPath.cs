using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(PlayerController))]
public class SignalPath : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("A distância mínima que o jogador deve se mover para um novo ponto ser adicionado.")]
    [SerializeField] private float pointSpacing = 0.1f;

    [Header("World Constraints")]
    [Tooltip("A coordenada Y do chão.")]
    [SerializeField] private float groundY = -2.5f;
    [Tooltip("A coordenada Y do teto.")]
    [SerializeField] private float ceilingY = 1.5f;
    
    private LineRenderer lineRenderer;
    private PlayerController playerController;
    private List<Vector3> pathPoints = new List<Vector3>();
    private Vector3 lastPointPosition;

    public List<Vector3> PathPoints => pathPoints;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        InitializePath();
    }

    private void Update()
    {
        float targetY = playerController.IsGravityInverted ? ceilingY : groundY;
        Vector3 currentTargetPosition = new Vector3(transform.position.x, targetY, 0);

        if (currentTargetPosition.x < lastPointPosition.x)
        {
            RemovePointsAfter(currentTargetPosition.x);
        }

        if (Vector3.Distance(currentTargetPosition, lastPointPosition) > pointSpacing)
        {
            AddPointToPath(currentTargetPosition);
        }
    }

    /// <summary>
    /// Limpa a trilha e adiciona o primeiro ponto na posição inicial.
    /// </summary>
    private void InitializePath()
    {
        pathPoints.Clear();
        float startY = playerController.IsGravityInverted ? ceilingY : groundY;
        Vector3 startPoint = new Vector3(transform.position.x, startY, 0);
        AddPointToPath(startPoint);
    }

    /// <summary>
    /// Adiciona um novo ponto à trilha e atualiza o LineRenderer.
    /// </summary>
    private void AddPointToPath(Vector3 point)
    {
        pathPoints.Add(point);
        lastPointPosition = point;
        lineRenderer.positionCount = pathPoints.Count;
        lineRenderer.SetPosition(pathPoints.Count - 1, point);
    }

    /// <summary>
    /// Remove todos os pontos da trilha à frente da posição X atual do jogador.
    /// </summary>
    private void RemovePointsAfter(float currentX)
    {
        int removalIndex = pathPoints.FindIndex(p => p.x > currentX);

        if (removalIndex != -1)
        {
            int removeCount = pathPoints.Count - removalIndex;
            pathPoints.RemoveRange(removalIndex, removeCount);
            lineRenderer.positionCount = pathPoints.Count;

            if (pathPoints.Count > 0)
            {
                lastPointPosition = pathPoints[pathPoints.Count - 1];
            }
            else
            {
                InitializePath();
            }
        }
    }
}