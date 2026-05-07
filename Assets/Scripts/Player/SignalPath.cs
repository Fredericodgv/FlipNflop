// /Assets/Scripts/SignalPath.cs

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
    private bool isDrawing = true;
    private bool lastGravityInverted;

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
        if (!isDrawing) return;

        float currentX = transform.position.x;

        // Se o player recuou para x <= 0, limpa tudo e reinicia
        if (currentX <= 0)
        {
            ResetPath();
            return;
        }

        float targetY = playerController.IsGravityInverted ? ceilingY : groundY;

        bool gravityChanged = playerController.IsGravityInverted != lastGravityInverted;
        if (gravityChanged)
        {
            lastGravityInverted = playerController.IsGravityInverted;
            float oldY = playerController.IsGravityInverted ? groundY : ceilingY;
            AddPointToPath(new Vector3(currentX, oldY, 0));
            AddPointToPath(new Vector3(currentX, targetY, 0));
            return;
        }

        Vector3 currentTargetPosition = new Vector3(currentX, targetY, 0);

        // Remove pontos se o player voltou para a esquerda
        if (currentX < lastPointPosition.x)
        {
            RemovePointsAfter(currentX);
        }

        if (Vector3.Distance(currentTargetPosition, lastPointPosition) > pointSpacing)
        {
            AddPointToPath(currentTargetPosition);
        }
    }

    private void InitializePath()
    {
        pathPoints.Clear();
        float startX = Mathf.Max(transform.position.x, 0f);
        float startY = playerController.IsGravityInverted ? ceilingY : groundY;
        lastPointPosition = new Vector3(startX, startY, 0);
    }

    /// <summary>
    /// Limpa o caminho completamente e reinicia o estado (chamado quando x <= 0).
    /// </summary>
    private void ResetPath()
    {
        pathPoints.Clear();
        lineRenderer.positionCount = 0;
        lastGravityInverted = playerController.IsGravityInverted;
        float startY = lastGravityInverted ? ceilingY : groundY;
        lastPointPosition = new Vector3(0f, startY, 0);
    }


    private void AddPointToPath(Vector3 point)
    {
        pathPoints.Add(point);
        lastPointPosition = point;
        lineRenderer.positionCount = pathPoints.Count;
        lineRenderer.SetPosition(pathPoints.Count - 1, point);
    }

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

    public void FinalizePath(float finalX)
    {
        if (pathPoints.Count == 0) return;

        Vector3 lastPoint = pathPoints[pathPoints.Count - 1];
        Vector3 finalPoint = new Vector3(finalX, lastPoint.y, 0);
        AddPointToPath(finalPoint);
    }

    /// <summary>
    /// Altera a cor de toda a linha para uma única cor sólida.
    /// </summary>
    public void SetTrailColor(Color newColor)
    {
        if (lineRenderer == null) return;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(newColor, 0.0f), new GradientColorKey(newColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        lineRenderer.colorGradient = gradient;
    }

    /// <summary>
    /// Altera a cor da linha com base em um gradiente para colorir segmentos.
    /// </summary>
    public void SetTrailColor(Gradient newGradient)
    {
        if (lineRenderer != null)
        {
            lineRenderer.colorGradient = newGradient;
        }
    }
}