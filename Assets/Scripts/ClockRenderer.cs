using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClockRenderer : MonoBehaviour
{
    [Header("Configurações")]
    public float spacing = 5f;              // Distância entre linhas (em unidades de mundo)
    public float lineLength = 10f;          
    public float lineWidth = 0.1f;
    public Material dottedLineMaterial;
    public int linesPerChunk = 10;          // Quantas linhas são geradas de cada vez

    private Transform cameraTransform;
    private float lastCameraX;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraX = cameraTransform.position.x;
        GenerateLinesAroundCamera();
    }

    void Update()
    {
        // Se a câmera se moveu o suficiente, gera novas linhas
        if (Mathf.Abs(cameraTransform.position.x - lastCameraX) >= spacing)
        {
            GenerateLinesAroundCamera();
            lastCameraX = cameraTransform.position.x;
        }
    }

    void GenerateLinesAroundCamera()
    {
        // Remove linhas antigas (opcional)
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // Gera novas linhas ao redor da câmera
        float cameraX = cameraTransform.position.x;
        float startX = Mathf.Floor(cameraX / spacing) * spacing - (linesPerChunk / 2f * spacing);

        for (int i = 0; i < linesPerChunk; i++)
        {
            float xPos = startX + i * spacing;
            GameObject lineObj = new GameObject($"DottedLine_{xPos}");
            lineObj.transform.SetParent(transform);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = dottedLineMaterial;
            lr.textureMode = LineTextureMode.Tile;
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;

            lr.SetPosition(0, new Vector3(xPos, -lineLength / 2f, 0));
            lr.SetPosition(1, new Vector3(xPos, lineLength / 2f, 0));
        }
    }
}