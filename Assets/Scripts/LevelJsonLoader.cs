using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Loads level JSON, configures scene, renders diagrams/terrain, and spawns obstacles.
/// </summary>
public class LevelJsonLoader : MonoBehaviour
{
    #region Fields
    [Header("Tilemaps")]
    [SerializeField] private Tilemap inputTilemap;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Tilemap clockTilemap;

    [Header("Level JSON")]
    [Tooltip("JSON containing jSignal, kSignal, floor, ceiling, levelTiles and clockTiles")]
    [SerializeField] private TextAsset levelFile;

    [Header("Tiles - Diagrams")]
    [Tooltip("Tiles for 3-bit neighborhood patterns (prev,curr,next) as binary index: 0=000,1=001,2=010,3=011,4=100,5=101,6=110,7=111")]
    [SerializeField] private TileBase[] diagramTiles = new TileBase[8];


    [Header("Tiles - Terrain")]
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase ceilingTile;
    [SerializeField] private bool flipCeilingY = true;
    [SerializeField] private TileBase wallTile;

    [Header("Placement Settings")]
    [SerializeField] private int startX = 0;
    [SerializeField] private int j_YRow = 8;
    [SerializeField] private int k_YRow = 5;
    [SerializeField] private int preset_YRow = 10;
    [SerializeField] private int clear_YRow = 2;
    [SerializeField] private int clock_YRow = 14;
    [SerializeField] private int floorYRow = 0;
    [SerializeField] private int ceilingYRow = 12;

    [Header("Obstacles")]
    [Tooltip("If true, obstacle startTileY is relative to floorYRow; otherwise it's absolute tile Y.")]
    [SerializeField] private bool obstacleYRelativeToFloor = true;
    [Tooltip("Parent transform for spawned obstacles (optional)")]
    [SerializeField] private Transform obstaclesParent;
    [Tooltip("Map JSON obstacle 'type' to prefab to spawn")]
    [SerializeField] private List<ObstaclePrefabEntry> obstaclePrefabs = new List<ObstaclePrefabEntry>();

    [Header("Debug")]
    [Tooltip("If true, logs the computed output vector (0/1) used by PathVerifier.")]
    [SerializeField] private bool debugLogOutputVector = false;
    [Tooltip("String view of the computed output vector (0/1). Read-only, for inspector visualization.")]
    [SerializeField]
    [TextArea]
    private string debugOutputVector;

    #endregion

    #region Parsed Signals

    public bool[] ParsedJSignal { get; private set; }
    public bool[] ParsedKSignal { get; private set; }
    public bool[] ParsedPresetSignal { get; private set; }
    public bool[] ParsedClearSignal { get; private set; }
    // Per-tile output timeline (async preset/clear immediate; JK at clock edges)
    public bool[] OutputTimeline { get; private set; }
    // Per-tile operation description (e.g., keep, preset_async, clear_async, set_sync, reset_sync, switch_sync, combined)
    [SerializeField]
    private string[] outputOpsPerTile;

    #endregion


    #region Output: Timeline
    /// <summary>
    /// Per-tile timeline: JK at edge tile, async immediately; async suppresses next edge only.
    /// </summary>
    public bool[] ComputeOutputTimelineFromParsedSignals()
    {
        int diagramLen = Mathf.RoundToInt(LevelManager.Instance != null ? LevelManager.Instance.diagramEndX : 0f);
        if (diagramLen <= 0)
        {
            diagramLen = MaxLen(ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal);
            if (diagramLen <= 0) return null;
        }
        int totalLength = diagramLen + 1; // inclui o tile imediatamente após a última borda de clock

        GetClockSamplingParameters(out int step, out int _);
        var timeline = new bool[totalLength];
        bool q = false;
        bool asyncSincePrevEdge = false;
        for (int i = 0; i < totalLength; i++)
        {
            bool hasPreset = GetAt(ParsedPresetSignal, i);
            bool hasClear = GetAt(ParsedClearSignal, i);
            bool isEdge = (step > 0 && i > 0 && (i % step) == 0);
            bool hasAsyncCurrent = (hasPreset || hasClear);


            // 1) Apply JK at the tile AFTER the edge, sampling J/K at i-1,
            //    but only if no async happened strictly before this edge (tiles < i).
            if (isEdge)
            {
                // Allow sync even if asyncSincePrevEdge was true IF an async also occurs now (to produce combined op)
                bool suppressEdge = asyncSincePrevEdge && !hasAsyncCurrent;
                if (!suppressEdge)
                {
                    bool j = GetAt(ParsedJSignal, i - 1);
                    bool k = GetAt(ParsedKSignal, i - 1);
                    if (j && !k) q = true;
                    else if (!j && k) q = false;
                    else if (j && k) q = !q;
                }
                // Reset at the edge to begin tracking async for the next cycle
                asyncSincePrevEdge = false;
            }

            // 2) Apply asynchronous preset/clear immediately for this tile (after JK at the edge)
            if (hasPreset && hasClear)
            {
                q = false; // Clear priority
                asyncSincePrevEdge = true; // counts towards suppression of the NEXT edge
            }
            else if (hasClear)
            {
                q = false;
                asyncSincePrevEdge = true;
            }
            else if (hasPreset)
            {
                q = true;
                asyncSincePrevEdge = true;
            }

            timeline[i] = q;
        }
        return timeline;
    }

    /// <summary>
    /// Timeline + op labels per tile (keep, set/reset/switch_sync, preset/clear_async, combined).
    /// </summary>
    public bool[] ComputeOutputTimelineWithOps(out string[] ops)
    {
        ops = null;
        int diagramLen = Mathf.RoundToInt(LevelManager.Instance != null ? LevelManager.Instance.diagramEndX : 0f);
        if (diagramLen <= 0)
        {
            diagramLen = MaxLen(ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal);
            if (diagramLen <= 0) return null;
        }
        int totalLength = diagramLen + 1; // inclui o tile logo após a última borda de clock

        GetClockSamplingParameters(out int step, out int _);
        var timeline = new bool[totalLength];
        var opArr = new string[totalLength];
        bool q = false;
        bool asyncSincePrevEdge = false;
        for (int i = 0; i < totalLength; i++)
        {
            bool prevQ = q;
            bool hasPreset = GetAt(ParsedPresetSignal, i);
            bool hasClear = GetAt(ParsedClearSignal, i);
            bool isEdge = (step > 0 && i > 0 && (i % step) == 0);
            bool hasAsyncCurrent = (hasPreset || hasClear);

            bool syncApplied = false;
            string syncToken = null;

            // 1) Apply JK at the edge (sampling J/K at i-1) only if no async happened strictly before this edge
            if (isEdge)
            {
                bool suppressEdge = asyncSincePrevEdge && !hasAsyncCurrent;
                if (!suppressEdge)
                {
                    bool j = GetAt(ParsedJSignal, i - 1);
                    bool k = GetAt(ParsedKSignal, i - 1);
                    bool beforeSyncQ = q;
                    if (j && !k) { q = true; syncApplied = true; syncToken = beforeSyncQ != q ? "set_sync" : "hold_sync"; }
                    else if (!j && k) { q = false; syncApplied = true; syncToken = beforeSyncQ != q ? "reset_sync" : "hold_sync"; }
                    else if (j && k) { q = !q; syncApplied = true; syncToken = "switch_sync"; }
                    else { syncApplied = true; syncToken = "hold_sync"; }
                }
                else
                {
                    // JK ignorado devido a evento assíncrono ocorrido antes deste edge
                    syncApplied = false;
                    syncToken = "sync_ignored";
                }
                // Edge concluída: começa novo ciclo de contagem de async
                asyncSincePrevEdge = false;
            }

            // 2) Apply async immediately for this tile, after edge processing
            string asyncToken = null;
            if (hasPreset && hasClear)
            {
                bool changed = q != false;
                q = false; // Clear priority
                asyncToken = changed ? "clear_async" : "clear_async_noop";
                asyncSincePrevEdge = true; // conta para o próximo edge
            }
            else if (hasClear)
            {
                bool changed = q != false;
                q = false;
                asyncToken = changed ? "clear_async" : "clear_async_noop";
                asyncSincePrevEdge = true;
            }
            else if (hasPreset)
            {
                bool changed = q != true;
                q = true;
                asyncToken = changed ? "preset_async" : "preset_async_noop";
                asyncSincePrevEdge = true;
            }

            timeline[i] = q;

            // 3) Decide final token.
            // Anchor at the edge when it changed Q; only combine if async changed too.
            string finalToken;
            bool asyncIsNoop = (asyncToken != null && asyncToken.EndsWith("_noop"));
            if (syncApplied && syncToken != null && syncToken != "hold_sync")
            {
                finalToken = (!asyncIsNoop && asyncToken != null) ? (syncToken + "_then_" + asyncToken) : syncToken;
            }
            else if (asyncToken != null)
            {
                finalToken = asyncToken;
            }
            else
            {
                finalToken = (prevQ == q) ? "keep" : (q ? "set_initial" : "reset_initial");
            }
            opArr[i] = finalToken;
        }
        ops = opArr;
        return timeline;
    }

    #endregion

    #region Output: Events
    /// <summary>
    /// Builds events from per-tile sim: sync at X=i, async at X=i+0.5.
    /// </summary>
    public List<PathVerifier.SignalEvent> ComputeOutputEventsFromParsedSignals()
    {
        // Re-simulate per-tile with edge-then-async ordering to capture double transitions
        int diagramLen = Mathf.RoundToInt(LevelManager.Instance != null ? LevelManager.Instance.diagramEndX : 0f);
        if (diagramLen <= 0)
        {
            diagramLen = MaxLen(ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal);
            if (diagramLen <= 0) return null;
        }
        int totalLength = diagramLen + 1;

        GetClockSamplingParameters(out int step, out int _);
        var events = new List<PathVerifier.SignalEvent>();

        bool q = false;        // state carried across tiles
        bool prev = q;         // last emitted baseline
        bool asyncSincePrevEdge = false; // set only when an async actually changed Q since last edge

        for (int i = 0; i < totalLength; i++)
        {
            bool isEdge = (step > 0 && i > 0 && (i % step) == 0);
            bool hasPreset = GetAt(ParsedPresetSignal, i);
            bool hasClear = GetAt(ParsedClearSignal, i);
            bool hasAsyncCurrent = (hasPreset || hasClear);

            // 1) Synchronous edge effect at integer X=i
            if (isEdge)
            {
                // Suppress edge ONLY if a prior async changed Q and there is no async this tile
                bool suppressEdge = asyncSincePrevEdge && !hasAsyncCurrent;
                if (!suppressEdge)
                {
                    bool j = GetAt(ParsedJSignal, i - 1);
                    bool k = GetAt(ParsedKSignal, i - 1);
                    bool qSync = q;
                    if (j && !k) qSync = true;
                    else if (!j && k) qSync = false;
                    else if (j && k) qSync = !qSync;

                    if (qSync != prev)
                    {
                        events.Add(new PathVerifier.SignalEvent(i, qSync));
                        prev = qSync;
                    }
                    q = qSync;
                }
                // reset tracker; it may be re-set by async below if that async changes Q
                asyncSincePrevEdge = false;
            }

            // 2) Asynchronous immediate effect at half tile X=i+0.5
            if (hasPreset || hasClear)
            {
                bool qBefore = q;
                // Clear priority if both
                q = hasClear ? false : true;
                bool asyncChanged = (q != qBefore);
                if (asyncChanged)
                {
                    float xPos = i + 0.5f;
                    events.Add(new PathVerifier.SignalEvent(xPos, q));
                    prev = q;
                }
                // Only mark for next-edge suppression if the async actually changed Q
                asyncSincePrevEdge = asyncChanged;
            }
        }
        return events;
    }

    #endregion

    #region Helpers
    /// <summary>
    /// Gets clock step/startOffset (first edge at X=step).
    /// </summary>
    private void GetClockSamplingParameters(out int step, out int startOffset)
    {
        float stepF = (LevelManager.Instance != null && LevelManager.Instance.clockStepX > 0f)
            ? LevelManager.Instance.clockStepX
            : 1f;
        step = Mathf.Max(1, Mathf.RoundToInt(stepF));
        startOffset = step;
    }

    private static bool GetAt(bool[] arr, int idx)
    {
        return arr != null && idx >= 0 && idx < arr.Length && arr[idx];
    }

    private static bool[] InvertBits(bool[] arr)
    {
        if (arr == null) return null;
        var res = new bool[arr.Length];
        for (int i = 0; i < arr.Length; i++) res[i] = !arr[i];
        return res;
    }

    private static Color ParseColor(string hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        string s = hex.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = "#" + s.Substring(2);
        else if (!s.StartsWith("#")) s = "#" + s;
        if (ColorUtility.TryParseHtmlString(s, out var c)) return c;
        return fallback;
    }

    private void ColorRow(Tilemap map, int length, int yRow, int baseX, Color color)
    {
        if (map == null || length <= 0) return;
        for (int i = 0; i < length; i++)
        {
            var pos = new Vector3Int(baseX + i, yRow, 0);
            map.SetTileFlags(pos, TileFlags.None);
            map.SetColor(pos, color);
        }
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initializes tilemaps from the provided JSON and updates LevelManager settings.
    /// </summary>
    private void Awake()
    {
        var data = LoadLevelData(levelFile);

#if UNITY_EDITOR
            if (!ValidateAll(data)) return;
#endif

        ApplyLevelConfig(data);

        var jSignal = ParseInputString(data.jSignal);
        var kSignal = ParseInputString(data.kSignal);
        var presetSignal = ParseInputString(data.presetSignal);
        var clearSignal = ParseInputString(data.clearSignal);

        // Async active mode: 1 = active-high (default), 0 = active-low (invert preset/clear arrays)
        int asyncActiveMode = (data != null ? data.asyncActive : 1);
        if (asyncActiveMode == 0)
        {
            presetSignal = InvertBits(presetSignal);
            clearSignal = InvertBits(clearSignal);
        }
        this.ParsedJSignal = jSignal;
        this.ParsedKSignal = kSignal;
        this.ParsedPresetSignal = presetSignal;
        this.ParsedClearSignal = clearSignal;

        var floorBand = ParseInputString(data.floor);
        var ceilingBand = ParseInputString(data.ceiling);

        // Compute and expose the per-tile output timeline (0/1) for PathVerifier/reference
        this.OutputTimeline = ComputeOutputTimelineWithOps(out outputOpsPerTile);
        UpdateDebugOutputVectorString();
        if (debugLogOutputVector)
        {
            Debug.Log($"LevelJsonLoader: Output timeline ({(OutputTimeline != null ? OutputTimeline.Length : 0)}): {debugOutputVector}");
        }

        ClearAllTilemaps();
        // Parse colors (accepts 0xRRGGBB, #RRGGBB, RRGGBB)
        Color jColor = ParseColor(data?.jSignalColor, Color.white);
        Color kColor = ParseColor(data?.kSignalColor, Color.white);
        Color presetColor = ParseColor(data?.presetSignalColor, Color.white);
        Color clearColor = ParseColor(data?.clearSignalColor, Color.white);
        Color clockColor = ParseColor(data?.clockSignalColor, Color.white);

        GenerateDiagram(inputTilemap, jSignal, j_YRow, startX, jColor);
        GenerateDiagram(inputTilemap, kSignal, k_YRow, startX, kColor);
        if (presetSignal != null)
            GenerateDiagram(inputTilemap, presetSignal, preset_YRow, startX, presetColor);
        if (clearSignal != null)
            GenerateDiagram(inputTilemap, clearSignal, clear_YRow, startX, clearColor);

        // Use values already defined in ApplyLevelConfig (no redundant checks here)
        int clockStep = Mathf.RoundToInt(LevelManager.Instance != null ? LevelManager.Instance.clockStepX : 6);
        int levelLength = Mathf.RoundToInt(LevelManager.Instance != null ? LevelManager.Instance.levelEndX : (6 * data.clockCicles));

        var clockPattern = BuildClockPattern(levelLength, clockStep, false);
        DrawPattern(clockTilemap, clockPattern, clock_YRow, startX);
        ColorRow(clockTilemap, clockPattern.Length, clock_YRow, startX, clockColor);
        // Extend floor and ceiling by +3 tiles beyond the last '1' defined in JSON
        GenerateBand(terrainTilemap, floorBand, floorYRow, floorTile, false, 3);
        GenerateBand(terrainTilemap, ceilingBand, ceilingYRow, ceilingTile, flipCeilingY, 3);

        CompleteStaticScenery();

        if (data.obstacles != null && data.obstacles.Count > 0)
        {
            SpawnObstacles(data.obstacles);
        }
    }



    /// <summary>
    /// Builds the clock pattern:
    /// Step S: period = S tiles. First floor(S/2) zeros, then remaining tiles ones (or inverted if startHigh).
    /// For odd S this creates a slightly asymmetric duty cycle (ex: S=5 -> 2 zeros, 3 ones).
    /// Returns int[] of length totalLength with values: 0=low, 1=high.
    /// </summary>
    #endregion

    #region Clock Pattern
    public static int[] BuildClockPattern(int totalLength, int step, bool startHigh = false)
    {
        if (totalLength <= 0 || step <= 0) return null;
        var arr = new int[totalLength + step];
        int half = step / 2;
        var period = new int[step];
        int a = startHigh ? 1 : 0;
        int b = startHigh ? 0 : 1;
        for (int i = 0; i < half; i++) period[i] = a;
        for (int i = half; i < step; i++) period[i] = b;
        for (int i = 0; i < totalLength + step; i++) arr[i] = period[i % step];
        return arr;
    }

    /// <summary>
    /// Generalized pattern renderer for both inputs and clock.
    /// pattern codes: 0 = low, 1 = high.
    /// </summary>
    private void DrawPattern(Tilemap map, int[] pattern, int yRow, int startX)
    {
        if (map == null || pattern == null || pattern.Length == 0) return;
        for (int i = 0; i < pattern.Length; i++)
        {
            int curr = pattern[i];
            int prev = (i > 0) ? pattern[i - 1] : curr;
            int next = (i < pattern.Length - 1) ? pattern[i + 1] : curr;
            int idx = ((prev != 0 ? 1 : 0) << 2) | ((curr != 0 ? 1 : 0) << 1) | (next != 0 ? 1 : 0);
            var tile = SafeTile(idx);
            if (tile != null) map.SetTile(new Vector3Int(startX + i, yRow, 0), tile);
        }
    }

    private TileBase SafeTile(int idx)
    {
        if (diagramTiles == null || diagramTiles.Length <= idx || idx < 0) return null;
        return diagramTiles[idx];
    }

    /// <summary>
    /// Loads and parses the JSON asset.
    /// </summary>
    #endregion

    #region JSON & Config
    private LevelData LoadLevelData(TextAsset jsonAsset)
    {
        if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
        {
            Debug.LogError("LevelJsonLoader: JSON asset is empty or missing.");
            return null;
        }
        try
        {
            return JsonUtility.FromJson<LevelData>(jsonAsset.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"LevelJsonLoader: Error reading JSON: {ex.Message}");
            return null;
        }
    }

    [ContextMenu("Log Output Vector (0/1)")]
    private void LogOutputVectorContext()
    {
        if (OutputTimeline == null)
        {
            OutputTimeline = ComputeOutputTimelineWithOps(out outputOpsPerTile);
            UpdateDebugOutputVectorString();
        }
        Debug.Log($"LevelJsonLoader: Output timeline ({(OutputTimeline != null ? OutputTimeline.Length : 0)}): {debugOutputVector}");
    }

    [ContextMenu("Log Output Ops")]
    private void LogOutputOpsContext()
    {
        if (outputOpsPerTile == null || OutputTimeline == null)
        {
            OutputTimeline = ComputeOutputTimelineWithOps(out outputOpsPerTile);
            UpdateDebugOutputVectorString();
        }
        var sb = new StringBuilder(outputOpsPerTile.Length * 6);
        for (int i = 0; i < outputOpsPerTile.Length; i++)
        {
            sb.Append(outputOpsPerTile[i]);
            if (i < outputOpsPerTile.Length - 1) sb.Append(' ');
        }
        Debug.Log($"LevelJsonLoader: Ops per tile ({outputOpsPerTile.Length}): {sb}");
    }

    private void UpdateDebugOutputVectorString()
    {
        if (OutputTimeline == null)
        {
            debugOutputVector = "(null)";
            return;
        }
        var sb = new StringBuilder(OutputTimeline.Length);
        for (int i = 0; i < OutputTimeline.Length; i++) sb.Append(OutputTimeline[i] ? '1' : '0');
        debugOutputVector = sb.ToString();
    }

    /// <summary>
    /// Basic validations for scene refs and JSON presence.
    /// </summary>
    private bool ValidateAll(LevelData data)
    {
        if (data == null)
        {
            Debug.LogError("LevelJsonLoader: LevelData is null (JSON parsing failed).");
            return false;
        }
        if (inputTilemap == null || terrainTilemap == null || clockTilemap == null)
        {
            Debug.LogError("LevelJsonLoader: One or more required Tilemap references are missing (input/terrain/clock).");
            return false;
        }
        if (diagramTiles == null || diagramTiles.Length < 8)
        {
            Debug.LogError("LevelJsonLoader: diagramTiles must have 8 entries (indices 0..7 for 000..111).");
            return false;
        }
        for (int i = 0; i < 8; i++)
        {
            if (diagramTiles[i] == null)
            {
                Debug.LogError($"LevelJsonLoader: diagramTiles[{i}] is not assigned. Expected mapping 0=000,1=001,2=010,3=011,4=100,5=101,6=110,7=111.");
                return false;
            }
        }
        if (floorTile == null || ceilingTile == null)
        {
            Debug.LogError("LevelJsonLoader: Floor or ceiling tile is not assigned.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Sets fixed clock (6) and derived lengths.
    /// </summary>
    private void ApplyLevelConfig(LevelData data)
    {
        if (LevelManager.Instance != null)
        {
            int step = 6;
            LevelManager.Instance.clockStepX = step;
            LevelManager.Instance.diagramEndX = data.clockCicles * step;
            LevelManager.Instance.phaseEndX = LevelManager.Instance.diagramEndX + LevelManager.Instance.phaseSlackTiles;
            LevelManager.Instance.levelEndX = LevelManager.Instance.diagramEndX; // legado permanece no fim lógico
        }
    }



    /// <summary>
    /// Renders a signal diagram on a tilemap.
    /// </summary>
    #endregion

    #region Diagram Rendering

    private void GenerateDiagram(Tilemap targetMap, bool[] signal, int yRow, int baseX, Color color)
    {
        if (targetMap == null || signal == null) return;
        var pattern = new int[signal.Length];
        for (int i = 0; i < signal.Length; i++) pattern[i] = signal[i] ? 1 : 0;
        DrawPattern(targetMap, pattern, yRow, baseX);
        ColorRow(targetMap, pattern.Length, yRow, baseX, color);
    }

    /// <summary>
    /// Renders a 0/1 band at the given row.
    /// </summary>
    private void GenerateBand(Tilemap targetMap, bool[] band, int yRow, TileBase tile, bool flipY = false, int extendRight = 0)
    {
        if (targetMap == null || band == null || tile == null) return;
        for (int i = 0; i < band.Length; i++)
        {
            if (!band[i]) continue;
            var pos = new Vector3Int(startX + i, yRow, 0);
            targetMap.SetTile(pos, tile);
            if (flipY)
            {
                targetMap.SetTileFlags(pos, TileFlags.None);
                targetMap.SetTransformMatrix(pos, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f)));
            }
        }

        // Extend band a few tiles after the last '1'
        if (extendRight > 0)


        {
            int lastIdx = -1;
            for (int i = band.Length - 1; i >= 0; i--) { if (band[i]) { lastIdx = i; break; } }
            if (lastIdx >= 0)
            {
                for (int off = 1; off <= extendRight; off++)
                {
                    var posExt = new Vector3Int(startX + lastIdx + off, yRow, 0);
                    targetMap.SetTile(posExt, tile);
                    if (flipY)
                    {
                        targetMap.SetTileFlags(posExt, TileFlags.None);
                        targetMap.SetTransformMatrix(posExt, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f)));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Completes static scenery (left wall/tiles).
    /// </summary>
    private void CompleteStaticScenery()
    {
        if (terrainTilemap != null && wallTile != null)
        {
            int yMin = Mathf.Min(floorYRow, ceilingYRow);
            int yMax = Mathf.Max(floorYRow, ceilingYRow);
            if (yMin <= yMax)
            {
                int xWall = startX - 2;
                for (int y = yMin; y <= yMax; y++)
                    terrainTilemap.SetTile(new Vector3Int(xWall, y, 0), wallTile);
            }
        }

        if (terrainTilemap != null && floorTile != null)
        {
            int xFloor = startX - 1;
            terrainTilemap.SetTile(new Vector3Int(xFloor, floorYRow, 0), floorTile);
        }

        if (terrainTilemap != null && ceilingTile != null)
        {
            int xCeil = startX - 1;
            var pos = new Vector3Int(xCeil, ceilingYRow, 0);
            terrainTilemap.SetTile(pos, ceilingTile);
            if (flipCeilingY)
            {
                terrainTilemap.SetTileFlags(pos, TileFlags.None);
                terrainTilemap.SetTransformMatrix(pos, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f)));
            }
        }
    }

    /// <summary>
    /// Parses 0/1 into a bool array (ignores other chars).
    /// </summary>
    #endregion

    #region Parsing & Validation
    private bool[] ParseInputString(string data)
    {
        if (string.IsNullOrEmpty(data)) return null;
        var list = new List<bool>(data.Length);
        foreach (char c in data)
        {
            if (c == '1') list.Add(true);
            else if (c == '0') list.Add(false);
        }
        return list.Count > 0 ? list.ToArray() : null;
    }

    /// <summary>
    /// Validates required references.
    /// </summary>
    // Legacy per-section validations were consolidated into ValidateAll(LevelData)

    /// <summary>
    /// Clears all configured tilemaps.
    /// </summary>
    private void ClearAllTilemaps()
    {
        inputTilemap.ClearAllTiles();
        if (clockTilemap != null) clockTilemap.ClearAllTiles();
        terrainTilemap.ClearAllTiles();
    }



    /// <summary>
    /// Spawns obstacles at cell positions.
    /// </summary>
    #endregion

    #region Obstacles
    private void SpawnObstacles(List<ObstacleData> obstacles)
    {
        foreach (var o in obstacles)
        {
            var prefab = ResolveObstaclePrefab(o.type);
            if (prefab == null)
            {
                Debug.LogWarning($"LevelJsonLoader: No prefab mapped for obstacle type='{o.type}'.");
                continue;
            }

            int cellX = startX + o.startX;
            int cellY = obstacleYRelativeToFloor ? (floorYRow + o.startY) : o.startY;
            var cell = new Vector3Int(cellX, cellY, 0);
            var worldPos = terrainTilemap.GetCellCenterWorld(cell);

            var go = Instantiate(prefab, worldPos, Quaternion.identity, obstaclesParent);
            AttachObstacleConfig(go, o);
        }
    }

    /// <summary>
    /// Resolves a prefab by obstacle type.
    /// </summary>
    private GameObject ResolveObstaclePrefab(string type)
    {
        if (string.IsNullOrEmpty(type)) return null;
        for (int i = 0; i < obstaclePrefabs.Count; i++)
        {
            if (obstaclePrefabs[i] != null && obstaclePrefabs[i].prefab != null && obstaclePrefabs[i].type == type)
                return obstaclePrefabs[i].prefab;
        }
        return null;
    }

    /// <summary>
    /// Applies obstacle data to controller if present.
    /// </summary>
    private void AttachObstacleConfig(GameObject go, ObstacleData o)
    {
        var mace = go.GetComponent<MaceController>();
        if (mace != null)
        {
            var cellSize = terrainTilemap != null && terrainTilemap.layoutGrid != null
                ? terrainTilemap.layoutGrid.cellSize
                : Vector3.one;
            float speedUnits = o.speed * Mathf.Abs(cellSize.x);
            float horizUnits = o.horizontalDistance * Mathf.Abs(cellSize.x);
            float vertUnits = o.verticalDistance * Mathf.Abs(cellSize.y);
            mace.ApplyObstacleData(o.startX, o.startY, speedUnits, horizUnits, vertUnits, o.starterCorner, o.clockwise, startX, floorYRow, obstacleYRelativeToFloor, terrainTilemap);
            return;
        }
    }

    #endregion

    #region Data Classes
    [Serializable]
    private class LevelData
    {
        public string levelName;
        public int clockCicles;
        public int asyncActive = 1;
        public string jSignal;
        public string kSignal;
        public string presetSignal;
        public string clearSignal;
        public string jSignalColor;
        public string kSignalColor;
        public string presetSignalColor;
        public string clearSignalColor;
        public string clockSignalColor;
        public string floor;
        public string ceiling;
        public List<ObstacleData> obstacles;
    }

    [Serializable]
    private class ObstacleData
    {
        public string type;
        public int startX;
        public int startY;
        public float speed;
        public int horizontalDistance;
        public int verticalDistance;
        public string starterCorner;
        public bool clockwise;
    }

    [Serializable]
    private class ObstaclePrefabEntry
    {
        public string type;
        public GameObject prefab;
    }
    #endregion
}
