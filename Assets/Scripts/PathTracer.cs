using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathTracer : MonoBehaviour
{
    [Header("Espaçamento entre pontos")]
    public float pointSpacing = 0.1f;

    [Header("Alturas fixas")]
    public float groundY = -2.5f;    // Y quando jumpForce for positivo
    public float ceilingY = 1.25f;   // Y quando jumpForce for negativo

    // Referência ao PlayerController para ler jumpForce
    private PlayerController playerController;

    private LineRenderer lineRenderer;
    private Vector3 lastPoint;
    private int pointCount = 0;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        playerController = GetComponent<PlayerController>();

        // Define ponto inicial conforme jumpForce
        float startY = playerController.jumpForce > 0 ? groundY : ceilingY;
        Vector3 startPos = new Vector3(transform.position.x, startY, transform.position.z);

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPos);
        lastPoint = startPos;
    }

    void Update()
    {
        // Escolhe Y conforme sinal de jumpForce
        float fixedY = playerController.jumpForce > 0 ? groundY : ceilingY;

        // Apenas distância em X para adicionar ponto
        float deltaX = Mathf.Abs(transform.position.x - lastPoint.x);
        if (deltaX >= pointSpacing)
        {
            pointCount++;
            lineRenderer.positionCount = pointCount + 1;

            Vector3 newPoint = new Vector3(transform.position.x, fixedY, transform.position.z);
            lineRenderer.SetPosition(pointCount, newPoint);
            lastPoint = newPoint;
        }
    }
}
