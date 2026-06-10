using System.Collections.Generic;
using UnityEngine;

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

    [Header("Dashed Line Settings")]
    [SerializeField] private LineRenderer linePrefab;
    [SerializeField] private float dashLength = 0.15f;
    [SerializeField] private float dashGap = 0.1f;

    [Header("Clock Lines")]
    [SerializeField] private float clockStep = 6f;
    [SerializeField] private Color clockLineColor = new Color(0f, 0.9f, 1f, 0.6f);

    [Header("Hint Mode")]
    [SerializeField] private HintMode hintMode = HintMode.ClockOnly;

    [Header("Async Transition Hints")]
    [SerializeField] private Color asyncHintColor = new Color(1f, 0.5f, 0f, 0.6f);
    [SerializeField] private LevelJsonLoader levelJsonLoader;

    [Header("Rendering Optimization")]
    [SerializeField] private int bufferLines = 5;

    [Header("Gizmos")]
    [SerializeField] private bool drawClockGizmos = true;
    [SerializeField] private bool drawAsyncGizmos = true;
    [SerializeField] private int labelEveryN = 0;

    private Transform cameraTransform;
    private float lastCameraX;
    private readonly List<GameObject> activeLines = new List<GameObject>();
    private HintMode lastHintMode;

    private void Start()
    {
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;

        lastCameraX = cameraTransform != null ? cameraTransform.position.x : 0f;

        hintMode = HasAsyncSignals() ? HintMode.ClockAndAsync : HintMode.ClockOnly;

        lastHintMode = hintMode;
        GenerateVisibleLines();
    }

    private void Update()
    {
        bool cameraMovedEnough = Mathf.Abs(cameraTransform.position.x - lastCameraX) >= clockStep;
        bool modeChanged = hintMode != lastHintMode;

        if (cameraMovedEnough) lastCameraX = cameraTransform.position.x;
        if (modeChanged) lastHintMode = hintMode;
        if (cameraMovedEnough || modeChanged) GenerateVisibleLines();
    }

    /// <summary>
    /// Cycles through hint modes: Off -> ClockOnly -> ClockAndAsync (if async signals exist) -> Off.
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

    /// <summary>
    /// Clears and regenerates all visible hint lines based on the current hint mode and camera position.
    /// </summary>
    private void GenerateVisibleLines()
    {
        ClearAllLines();
        if (hintMode == HintMode.Off) return;

        float cameraX = cameraTransform.position.x;
        float levelEndX = LevelManager.Instance != null ? LevelManager.Instance.levelEndX : 100f;

        GenerateClockLines(cameraX, levelEndX);

        if (hintMode == HintMode.ClockAndAsync)
            GenerateAsyncHintLines(cameraX, levelEndX);
    }

    /// <summary>
    /// Generates dashed vertical lines at each clock edge within the visible range.
    /// </summary>
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
            CreateDashedLine(xPos, clockLineColor, $"ClockLine_{i}");
        }
    }

    /// <summary>
    /// Generates dashed vertical lines at async preset/clear transition positions within the visible range.
    /// </summary>
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
                CreateDashedLine(xPos, asyncHintColor, $"AsyncHint_{i}_{label}");
            }
        }
    }

    /// <summary>
    /// Creates a dashed vertical line at the given X position by spawning multiple solid dash segments under a shared parent.
    /// </summary>
    private void CreateDashedLine(float xPos, Color color, string name)
    {
        GameObject lineRoot = new GameObject(name);
        lineRoot.transform.SetParent(transform);
        activeLines.Add(lineRoot);

        Vector3 start = new Vector3(xPos, -lineLength / 2f, 0);
        Vector3 end = new Vector3(xPos, lineLength / 2f, 0);

        float length = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;
        float traveled = 0f;
        float accumulator = 0f;
        bool isDrawingDash = true;

        while (traveled < length - 0.001f)
        {
            float target = isDrawingDash ? dashLength : dashGap;
            float step = Mathf.Min(target - accumulator, length - traveled);

            if (isDrawingDash)
            {
                Vector3 segStart = start + direction * traveled;
                Vector3 segEnd = segStart + direction * step;
                DrawDashSegment(segStart, segEnd, color, lineRoot.transform);
            }

            traveled += step;
            accumulator += step;

            if (accumulator >= target - 0.001f)
            {
                accumulator = 0f;
                isDrawingDash = !isDrawingDash;
            }
        }
    }

    /// <summary>
    /// Instantiates a single solid LineRenderer segment between two points.
    /// </summary>
    private void DrawDashSegment(Vector3 start, Vector3 end, Color color, Transform parent)
    {
        LineRenderer lr = Instantiate(linePrefab, parent);
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    /// <summary>
    /// Destroys all currently active hint line GameObjects.
    /// </summary>
    private void ClearAllLines()
    {
        foreach (var line in activeLines)
            if (line != null) Destroy(line);
        activeLines.Clear();
    }

    /// <summary>
    /// Returns true if any preset or clear signal contains at least one transition.
    /// </summary>
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

    private void OnDrawGizmos()
    {
        DrawClockLineGizmos();
        DrawAsyncHintGizmos();
    }

    /// <summary>
    /// Draws clock edge lines in the Scene view for level design reference.
    /// </summary>
    private void DrawClockLineGizmos()
    {
        if (!drawClockGizmos) return;
        LevelManager lm = Application.isPlaying ? LevelManager.Instance : FindAnyObjectByType<LevelManager>();
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

    /// <summary>
    /// Draws async transition positions in the Scene view for level design reference.
    /// </summary>
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