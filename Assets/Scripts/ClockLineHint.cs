using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders vertical dotted lines for clock edges and async preset/clear transitions.
/// Input is handled externally by HintManager — this script only manages rendering.
/// </summary>
public class ClockLineHint : MonoBehaviour
{
    public enum HintMode
    {
        Off,
        ClockOnly,
        ClockAndAsync
    }

    [Header("Line Appearance")]
    [SerializeField] private float lineLength = 10f;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Material dottedLineMaterial;

    [Header("Clock Lines")]
    [Tooltip("Fixed clock step (spacing between clock edges).")]
    [SerializeField] private float clockStep = 6f;
    [Tooltip("Color for clock edge lines.")]
    [SerializeField] private Color clockLineColor = new Color(0f, 0.9f, 1f, 0.6f);

    [Header("Hint Mode")]
    [Tooltip("Current hint display mode (Off, ClockOnly, ClockAndAsync). Default: ClockOnly")]
    [SerializeField] private HintMode hintMode = HintMode.ClockOnly;

    [Header("Async Transition Hints")]
    [Tooltip("Color for async transition hint lines.")]
    [SerializeField] private Color asyncHintColor = new Color(1f, 0.5f, 0f, 0.6f);
    [Tooltip("Reference to LevelJsonLoader to access preset/clear signals.")]
    [SerializeField] private LevelJsonLoader levelJsonLoader;

    [Header("Rendering Optimization")]
    [Tooltip("Number of extra lines to render beyond visible camera bounds.")]
    [SerializeField] private int bufferLines = 5;

    [Header("Gizmos")]
    [Tooltip("Draw all clock lines as gizmos in Scene view for level design.")]
    [SerializeField] private bool drawClockGizmos = true;
    [Tooltip("Draw async transition positions as gizmos in Scene view.")]
    [SerializeField] private bool drawAsyncGizmos = true;
    [Tooltip("Draw index labels every N clock lines (0 = disabled).")]
    [SerializeField] private int labelEveryN = 0;

    private Transform cameraTransform;
    private float lastCameraX;
    private readonly List<GameObject> activeLines = new List<GameObject>();
    private HintMode lastHintMode;

    private void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraX = cameraTransform.position.x;
        lastHintMode = hintMode;
        GenerateVisibleLines();
    }

    private void Update()
    {
        bool needsUpdate = false;

        if (Mathf.Abs(cameraTransform.position.x - lastCameraX) >= clockStep)
        {
            lastCameraX = cameraTransform.position.x;
            needsUpdate = true;
        }

        if (hintMode != lastHintMode)
        {
            lastHintMode = hintMode;
            needsUpdate = true;
        }

        if (needsUpdate)
            GenerateVisibleLines();
    }

    // -------------------------------------------------------------------------
    // Public API — called by HintManager
    // -------------------------------------------------------------------------

    /// <summary>
    /// Cycles through hint modes: ClockOnly -> ClockAndAsync (if async exists) -> Off -> ClockOnly.
    /// </summary>
    public void ToggleHintMode()
    {
        bool hasAsync = HasAsyncSignals();

        hintMode = hintMode switch
        {
            HintMode.Off => HintMode.ClockOnly,
            HintMode.ClockOnly => hasAsync ? HintMode.ClockAndAsync : HintMode.Off,
            HintMode.ClockAndAsync => HintMode.Off,
            _ => HintMode.ClockOnly
        };

        Debug.Log($"<color=cyan>[ClockLineHint] Hint mode: {hintMode}</color>");
    }

    // -------------------------------------------------------------------------
    // Line generation
    // -------------------------------------------------------------------------

    private void GenerateVisibleLines()
    {
        ClearAllLines();
        if (hintMode == HintMode.Off) return;

        float cameraX = cameraTransform.position.x;
        float levelEndX = LevelManager.Instance != null ? LevelManager.Instance.levelEndX : 100f;

        if (hintMode == HintMode.ClockOnly || hintMode == HintMode.ClockAndAsync)
            GenerateClockLines(cameraX, levelEndX);

        if (hintMode == HintMode.ClockAndAsync)
            GenerateAsyncHintLines(cameraX, levelEndX);
    }

    private void GenerateClockLines(float cameraX, float levelEndX)
    {
        float halfSpan = bufferLines * clockStep;
        float leftEdge = cameraX - halfSpan;
        float rightEdge = Mathf.Min(cameraX + halfSpan, levelEndX);
        int startMultiple = Mathf.Max(1, Mathf.FloorToInt(leftEdge / clockStep));
        int endMultiple = Mathf.FloorToInt(rightEdge / clockStep);

        for (int i = startMultiple; i <= endMultiple; i++)
        {
            float xPos = i * clockStep;
            if (xPos > levelEndX) break;
            CreateLine(xPos, clockLineColor, $"ClockLine_{i}");
        }
    }

    private void GenerateAsyncHintLines(float cameraX, float levelEndX)
    {
        if (levelJsonLoader == null) return;

        var presetSignal = levelJsonLoader.ParsedPresetSignal;
        var clearSignal = levelJsonLoader.ParsedClearSignal;
        if (presetSignal == null && clearSignal == null) return;

        float halfSpan = bufferLines * clockStep;
        float leftEdge = cameraX - halfSpan;
        float rightEdge = Mathf.Min(cameraX + halfSpan, levelEndX);
        int maxLength = Mathf.Max(
            presetSignal != null ? presetSignal.Length : 0,
            clearSignal != null ? clearSignal.Length : 0);

        for (int i = 0; i < maxLength; i++)
        {
            bool presetTransition = presetSignal != null && i < presetSignal.Length &&
                                    presetSignal[i] != (i > 0 ? presetSignal[i - 1] : false);
            bool clearTransition = clearSignal != null && i < clearSignal.Length &&
                                    clearSignal[i] != (i > 0 ? clearSignal[i - 1] : false);

            if (!presetTransition && !clearTransition) continue;

            float xPos = i + 0.5f;
            if (xPos >= leftEdge && xPos <= rightEdge && xPos <= levelEndX)
            {
                string label = presetTransition && clearTransition ? "Preset+Clear"
                             : presetTransition ? "Preset" : "Clear";
                CreateLine(xPos, asyncHintColor, $"AsyncHint_{i}_{label}");
            }
        }
    }

    private void CreateLine(float xPos, Color color, string name)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = dottedLineMaterial;
        lr.textureMode = LineTextureMode.Tile;
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.SetPosition(0, new Vector3(xPos, -lineLength / 2f, 0));
        lr.SetPosition(1, new Vector3(xPos, lineLength / 2f, 0));

        activeLines.Add(lineObj);
    }

    private void ClearAllLines()
    {
        foreach (var line in activeLines)
            if (line != null) Destroy(line);
        activeLines.Clear();
    }

    private bool HasAsyncSignals()
    {
        if (levelJsonLoader == null) return false;

        var presetSignal = levelJsonLoader.ParsedPresetSignal;
        var clearSignal = levelJsonLoader.ParsedClearSignal;

        if (presetSignal != null)
            for (int i = 1; i < presetSignal.Length; i++)
                if (presetSignal[i] != presetSignal[i - 1]) return true;

        if (clearSignal != null)
            for (int i = 1; i < clearSignal.Length; i++)
                if (clearSignal[i] != clearSignal[i - 1]) return true;

        return false;
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        DrawClockLineGizmos();
        DrawAsyncHintGizmos();
    }

    private void DrawClockLineGizmos()
    {
        if (!drawClockGizmos) return;
        LevelManager lm = Application.isPlaying ? LevelManager.Instance : FindFirstObjectByType<LevelManager>();
        if (lm == null || clockStep <= 0f) return;

        int count = Mathf.FloorToInt(lm.levelEndX / clockStep);
        Gizmos.color = clockLineColor;

        for (int i = 1; i <= count; i++)
        {
            float xPos = i * clockStep;
            Vector3 p0 = new Vector3(xPos, -lineLength / 2f, 0f);
            Vector3 p1 = new Vector3(xPos, lineLength / 2f, 0f);
            Gizmos.DrawLine(p0, p1);
#if UNITY_EDITOR
            if (labelEveryN > 0 && i % labelEveryN == 0)
            {
                UnityEditor.Handles.color = clockLineColor;
                UnityEditor.Handles.Label(p1 + Vector3.up * 0.2f, i.ToString());
            }
#endif
        }
    }

    private void DrawAsyncHintGizmos()
    {
        if (!drawAsyncGizmos || levelJsonLoader == null) return;

        var presetSignal = levelJsonLoader.ParsedPresetSignal;
        var clearSignal = levelJsonLoader.ParsedClearSignal;
        if (presetSignal == null && clearSignal == null) return;

        int maxLength = Mathf.Max(
            presetSignal != null ? presetSignal.Length : 0,
            clearSignal != null ? clearSignal.Length : 0);

        Gizmos.color = asyncHintColor;

        for (int i = 0; i < maxLength; i++)
        {
            bool presetTransition = presetSignal != null && i < presetSignal.Length &&
                                    presetSignal[i] != (i > 0 ? presetSignal[i - 1] : false);
            bool clearTransition = clearSignal != null && i < clearSignal.Length &&
                                    clearSignal[i] != (i > 0 ? clearSignal[i - 1] : false);

            if (!presetTransition && !clearTransition) continue;

            float xPos = i + 0.5f;
            Vector3 p0 = new Vector3(xPos, -lineLength / 2f, 0f);
            Vector3 p1 = new Vector3(xPos, lineLength / 2f, 0f);
            Gizmos.DrawLine(p0, p1);
#if UNITY_EDITOR
            if (labelEveryN > 0)
            {
                string label = presetTransition && clearTransition ? "P+C"
                             : presetTransition ? "P" : "C";
                UnityEditor.Handles.color = asyncHintColor;
                UnityEditor.Handles.Label(p1 + Vector3.up * 0.2f, $"{i}.5 {label}");
            }
#endif
        }
    }
}