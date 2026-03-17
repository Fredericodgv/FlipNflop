using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

/// <summary>
/// Displays world-space hint labels at the next clock edge to the right of the player.
/// Input is handled externally by HintManager — call ShowHint() to trigger.
///
/// - J label appears at the J signal row Y
/// - K label appears at the K signal row Y (auto-adjusted based on async presence)
/// - Preset/Clear labels appear at their rows (only if level has those signals)
/// - Operation label appears at outputHighY (fixed)
/// </summary>
public class OperationHint : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player transform used to determine current X position.")]
    [SerializeField] private Transform player;

    [Tooltip("LevelJsonLoader to access parsed signals and clock step.")]
    [SerializeField] private LevelJsonLoader levelJsonLoader;

    [Tooltip("Input tilemap used to convert tile rows to world Y positions.")]
    [SerializeField] private Tilemap inputTilemap;

    [Header("Tile Rows (must match LevelJsonLoader)")]
    [Tooltip("Tile row Y for the J signal line.")]
    [SerializeField] private int jTileRow = 12;

    [Tooltip("Tile row Y for Preset signal line.")]
    [SerializeField] private int presetTileRow = 8;

    [Tooltip("Tile row Y for Clear signal line.")]
    [SerializeField] private int clearTileRow = 6;

    [Header("Output Line Y (world space)")]
    [Tooltip("World Y where the operation label appears (fixed).")]
    [SerializeField] private float outputHighY = 1.25f;

    [Header("Label Appearance")]
    [Tooltip("Font asset for the world-space TMP labels.")]
    [SerializeField] private TMP_FontAsset labelFont;

    [Tooltip("Font size for signal labels (J, K, Preset, Clear).")]
    [SerializeField] private float signalFontSize = 6f;

    [Tooltip("Font size for the operation label.")]
    [SerializeField] private float operationFontSize = 8f;

    [Tooltip("Color for signal value labels.")]
    [SerializeField] private Color signalColor = new Color(0.9f, 0.9f, 0.2f);

    [Tooltip("Color for the operation label.")]
    [SerializeField] private Color operationColor = Color.white;

    [Tooltip("Horizontal offset to the right of the clock line.")]
    [SerializeField] private float xOffset = 10f;

    [Tooltip("Z position for the labels (should be in front of tilemap).")]
    [SerializeField] private float labelZ = -1f;

    [Header("Settings")]
    [Tooltip("How many seconds the hint stays visible before auto-hiding.")]
    [SerializeField] private float hintDuration = 3f;

    // -------------------------------------------------------------------------
    // Runtime
    // -------------------------------------------------------------------------

    private GameObject labelsRoot;
    private Coroutine hideCoroutine;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void OnDestroy() => ClearLabels();

    // -------------------------------------------------------------------------
    // Public API — called by HintManager
    // -------------------------------------------------------------------------

    public void ShowHint()
    {
        if (player == null || levelJsonLoader == null || inputTilemap == null)
        {
            Debug.LogWarning("[OperationHint] Missing references (player, levelJsonLoader or inputTilemap).");
            return;
        }

        float clockStep = LevelManager.Instance != null ? LevelManager.Instance.clockStepX : 6f;
        float playerX = player.position.x;
        int currentClock = Mathf.FloorToInt(playerX / clockStep);
        int nextClock = currentClock + 1;
        int signalIndex = nextClock * (int)clockStep - 1;
        float xPos = (currentClock + 1) * clockStep + xOffset;

        bool j = GetSignalAt(levelJsonLoader.ParsedJSignal, signalIndex);
        bool k = GetSignalAt(levelJsonLoader.ParsedKSignal, signalIndex);
        bool preset = GetSignalAt(levelJsonLoader.ParsedPresetSignal, signalIndex);
        bool clear = GetSignalAt(levelJsonLoader.ParsedClearSignal, signalIndex);

        Debug.Log($"currentClock:{currentClock} nextClock:{nextClock} signalIndex:{signalIndex} J:{j} K:{k}");

        bool hasPreset = levelJsonLoader.ParsedPresetSignal != null;
        bool hasClear = levelJsonLoader.ParsedClearSignal != null;
        bool hasAsync = hasPreset || hasClear;

        bool asyncActiveHigh = levelJsonLoader.asyncActiveHigh;
        string operation = ResolveOperation(j, k, preset, clear, hasPreset, hasClear, asyncActiveHigh);

        ClearLabels();
        labelsRoot = new GameObject("HintLabels");

        // J label
        SpawnLabel(labelsRoot.transform, $"J = {B(j)}", xPos, TileRowToWorldY(jTileRow), signalFontSize, signalColor);

        // K label — row depends on async presence (mirrors LevelJsonLoader logic)
        int kRow = hasAsync ? 10 : 8;
        SpawnLabel(labelsRoot.transform, $"K = {B(k)}", xPos, TileRowToWorldY(kRow), signalFontSize, signalColor);

        // Preset label (only if level has preset)
        if (hasPreset)
            SpawnLabel(labelsRoot.transform, $"Preset = {B(preset)}", xPos, TileRowToWorldY(presetTileRow), signalFontSize, signalColor);

        // Clear label (only if level has clear)
        if (hasClear)
            SpawnLabel(labelsRoot.transform, $"Clear = {B(clear)}", xPos, TileRowToWorldY(clearTileRow), signalFontSize, signalColor);

        // Operation label at fixed output Y
        SpawnLabel(labelsRoot.transform, operation, xPos, outputHighY, operationFontSize, operationColor);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(AutoHide());
    }

    // -------------------------------------------------------------------------
    // Label spawning
    // -------------------------------------------------------------------------

    private void SpawnLabel(Transform parent, string text, float x, float y, float fontSize, Color color)
    {
        var obj = new GameObject($"Label_{text}");
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(x, y, labelZ);

        var tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;

        if (labelFont != null)
            tmp.font = labelFont;
    }

    // -------------------------------------------------------------------------
    // Operation resolution
    // -------------------------------------------------------------------------

    private string ResolveOperation(bool j, bool k, bool preset, bool clear,
                                     bool hasPreset, bool hasClear, bool asyncActiveHigh)
    {
        bool presetActive = hasPreset && (asyncActiveHigh ? preset : !preset);
        bool clearActive = hasClear && (asyncActiveHigh ? clear : !clear);

        if (presetActive) return "Preset";
        if (clearActive) return "Clear";
        if (j && k) return "Switch";
        if (j) return "Set";
        if (k) return "Reset";
        return "Hold";
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private float TileRowToWorldY(int tileRow)
    {
        Vector3 worldPos = inputTilemap.CellToWorld(new Vector3Int(0, tileRow, 0));
        return worldPos.y + inputTilemap.cellSize.y / 2f;
    }

    private static bool GetSignalAt(bool[] signal, int index)
    {
        if (signal == null || index < 0 || index >= signal.Length) return false;
        return signal[index];
    }

    private static string B(bool v) => v ? "1" : "0";

    private void ClearLabels()
    {
        if (labelsRoot != null) { Destroy(labelsRoot); labelsRoot = null; }
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(hintDuration);
        ClearLabels();
        hideCoroutine = null;
    }
}