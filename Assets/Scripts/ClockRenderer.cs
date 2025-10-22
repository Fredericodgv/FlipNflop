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

    [Header("Gizmos de Edição")]
    [Tooltip("Desenha gizmos para TODAS as posições de clock ao longo do nível, para facilitar montagem de fase.")]
    public bool drawFullLevelGizmos = true;
    [Tooltip("X inicial do nível (primeira linha potencial). Alinhe este valor à grade desejada.")]
    public float levelStartX = 0f;
    [Tooltip("Cor das linhas de gizmo do nível.")]
    public Color fullLevelGizmoColor = new Color(0f, 0.9f, 1f, 0.6f);
    [Tooltip("Desenha também um índice pequeno (gizmos) a cada N linhas (0 = não desenha). Apenas visual.")]
    public int labelEveryN = 0;
    [Tooltip("Alinha automaticamente o início (levelStartX) ao spawn do player")] public bool autoAlignToPlayer = false;
    [Tooltip("Transform do spawn do player (usado se autoAlignToPlayer=true)")] public Transform playerSpawn;
    [Tooltip("Linhas extras à esquerda do início alinhado (apenas gizmo)")] public int extraLinesBeforeStart = 0;

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

    #region Gizmos
    void OnDrawGizmos()
    {
        if (!drawFullLevelGizmos) return;
        if (spacing <= 0f) return;

        // Obter levelEndX do LevelManager (em edição ou play)
        float levelEndX = 0f;
        LevelManager lm = null;
        if (Application.isPlaying && LevelManager.Instance != null)
        {
            lm = LevelManager.Instance;
        }
        else
        {
            lm = FindFirstObjectByType<LevelManager>();
        }

        if (lm == null)
        {
            // Sem LevelManager não dá para saber o fim; desenha nada.
            return;
        }
        levelEndX = lm.levelEndX;

        float minX = Mathf.Min(levelStartX, levelEndX);
        float maxX = Mathf.Max(levelStartX, levelEndX);

        // Ajuste opcional de alinhamento ao player
        if (autoAlignToPlayer && playerSpawn != null)
        {
            levelStartX = playerSpawn.position.x;
            minX = Mathf.Min(levelStartX, levelEndX);
            maxX = Mathf.Max(levelStartX, levelEndX);
        }

        float firstX = Mathf.Ceil(minX / spacing) * spacing;
        if (autoAlignToPlayer && playerSpawn != null)
        {
            // Recalcula firstX baseado no spawn (usar floor para incluir a linha na posição ou antes)
            float aligned = Mathf.Floor(playerSpawn.position.x / spacing) * spacing;
            firstX = aligned - extraLinesBeforeStart * spacing;
        }
        int count = Mathf.FloorToInt((maxX - firstX) / spacing) + 1;
        if (count < 0) return;

        Gizmos.color = fullLevelGizmoColor;
        for (int i = 0; i < count; i++)
        {
            float xPos = firstX + i * spacing;
            Vector3 p0 = new Vector3(xPos, -lineLength / 2f, 0f);
            Vector3 p1 = new Vector3(xPos, lineLength / 2f, 0f);
            Gizmos.DrawLine(p0, p1);

#if UNITY_EDITOR
            if (labelEveryN > 0 && (i % labelEveryN == 0))
            {
                // Desenha um pequeno rótulo (Editor somente)
                UnityEditor.Handles.color = fullLevelGizmoColor;
                UnityEditor.Handles.Label(p1 + Vector3.up * 0.2f, i.ToString());
            }
#endif
        }
    }
    #endregion
}