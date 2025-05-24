using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathTracer : MonoBehaviour
{
    public float pointSpacing = 0.1f; // Distância entre os pontos da linha
    private LineRenderer lineRenderer;
    private Vector3 lastPosition;
    private int pointCount = 0;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 1; // Inicia com 1 ponto
        lineRenderer.SetPosition(0, transform.position); // Define a posição inicial
        lastPosition = transform.position;
    }

    void Update()
    {
        // Verifica se o personagem se moveu o suficiente para adicionar um novo ponto
        if (Vector3.Distance(transform.position, lastPosition) > pointSpacing)
        {
            pointCount++;
            lineRenderer.positionCount = pointCount;
            lineRenderer.SetPosition(pointCount - 1, transform.position);
            lastPosition = transform.position;
        }
    }
}
