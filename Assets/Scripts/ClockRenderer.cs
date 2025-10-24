using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClockRenderer : MonoBehaviour
{
    [Header("Configurações")]
    // O espaçamento do clock é centralizado em LevelManager.clockStepX
    public float lineLength = 10f;
    public float lineWidth = 0.1f;
    public Material dottedLineMaterial;
    public int linesPerChunk = 10;          // Quantas linhas são geradas de cada vez

    [Header("Gizmos de Edição")]
    [Tooltip("Desenha gizmos para TODAS as posições de clock ao longo do nível, para facilitar montagem de fase.")]
    public bool drawFullLevelGizmos = true;
    [Tooltip("Cor das linhas de gizmo do nível.")]
    public Color fullLevelGizmoColor = new Color(0f, 0.9f, 1f, 0.6f);
    [Tooltip("Desenha também um índice pequeno (gizmos) a cada N linhas (0 = não desenha). Apenas visual.")]
    public int labelEveryN = 0;

    private Transform cameraTransform;
    private float lastCameraX;
    private const float DefaultStep = 5f; // fallback não-serializado

    private float GetStep(LevelManager lmContext = null)
    {
        // Em play, usa o LevelManager singleton; em edição (gizmos), usa o LevelManager encontrado
        if (Application.isPlaying && LevelManager.Instance != null)
        {
            return Mathf.Max(0.0001f, LevelManager.Instance.clockStepX);
        }
        if (!Application.isPlaying && lmContext != null)
        {
            return Mathf.Max(0.0001f, lmContext.clockStepX);
        }
        // Fallback para constante local (não aparece no Inspector)
        return Mathf.Max(0.0001f, DefaultStep);
    }

    void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraX = cameraTransform.position.x;
        GenerateLinesAroundCamera();
    }

    void Update()
    {
        // Se a câmera se moveu o suficiente, gera novas linhas
        float step = GetStep();
        if (Mathf.Abs(cameraTransform.position.x - lastCameraX) >= step)
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
        float step = GetStep();
        // Âncora fixa em 0: linhas alinhadas ao X=0 do mundo
        float startX = Mathf.Floor(cameraX / step) * step - (linesPerChunk / 2f * step);

        for (int i = 0; i < linesPerChunk; i++)
        {
            float xPos = startX + i * step;
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

    #region Gizmos
    void OnDrawGizmos()
    {
        if (!drawFullLevelGizmos) return;

        // Obter levelEndX do LevelManager (em edição ou play)
        LevelManager lm = Application.isPlaying ? LevelManager.Instance : FindFirstObjectByType<LevelManager>();
        if (lm == null) return;
        float levelEndX = lm.levelEndX;

        float step = GetStep(lm);
        if (step <= 0f) return;

        // Âncora fixa em 0 para gizmos
        float minX = Mathf.Min(0f, levelEndX);
        float maxX = Mathf.Max(0f, levelEndX);
        float firstX = Mathf.Ceil(minX / step) * step;
        int count = Mathf.FloorToInt((maxX - firstX) / step) + 1;
        if (count < 0) return;

        Gizmos.color = fullLevelGizmoColor;
        for (int i = 0; i < count; i++)
        {
            float xPos = firstX + i * step;
            Vector3 p0 = new Vector3(xPos, -lineLength / 2f, 0f);
            Vector3 p1 = new Vector3(xPos, lineLength / 2f, 0f);
            Gizmos.DrawLine(p0, p1);

#if UNITY_EDITOR
            if (labelEveryN > 0)
            {
                int idxFromWorldZero = Mathf.RoundToInt(xPos / step);
                if (idxFromWorldZero % labelEveryN == 0)
                {
                    UnityEditor.Handles.color = fullLevelGizmoColor;
                    UnityEditor.Handles.Label(p1 + Vector3.up * 0.2f, idxFromWorldZero.ToString());
                }
            }
#endif
        }
    }
    #endregion
}