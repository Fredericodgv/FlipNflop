using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathTracer : MonoBehaviour
{
    [Header("Espaçamento entre pontos")]
    public float pointSpacing = 0.1f;

    [Header("Alturas fixas")]
    public float groundY = -2.5f;
    public float ceilingY = 1.25f;

    private PlayerController playerController;
    private LineRenderer lineRenderer;

    private List<Vector3> pathPoints = new List<Vector3>();
    private Vector3 lastPoint;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        playerController = GetComponent<PlayerController>();

        float startY = playerController.jumpForce > 0 ? groundY : ceilingY;
        Vector3 startPoint = new Vector3(transform.position.x, startY, transform.position.z);

        pathPoints.Add(startPoint);
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPoint);
        lastPoint = startPoint;
    }

    void Update()
    {
        float fixedY = playerController.jumpForce > 0 ? groundY : ceilingY;
        Vector3 currentPos = new Vector3(transform.position.x, fixedY, transform.position.z);

        float deltaX = Mathf.Abs(currentPos.x - lastPoint.x);
        float deltaY = Mathf.Abs(currentPos.y - lastPoint.y);

        // ➤ Se andou para a direita (X) ou mudou em Y (ex: subiu ou desceu com gravidade)
        if ((currentPos.x > lastPoint.x && deltaX >= pointSpacing) || deltaY >= pointSpacing)
        {
            pathPoints.Add(currentPos);
            lastPoint = currentPos;
            UpdateLineRenderer();
        }

        // ➤ Andou para a esquerda → remover pontos à frente
        if (currentPos.x < lastPoint.x)
        {
            pathPoints.RemoveAll(p => p.x > currentPos.x + 0.01f);
            lastPoint = currentPos;
            UpdateLineRenderer();
        }
    }

    void UpdateLineRenderer()
    {
        lineRenderer.positionCount = pathPoints.Count;
        for (int i = 0; i < pathPoints.Count; i++)
        {
            lineRenderer.SetPosition(i, pathPoints[i]);
        }
    }
}
