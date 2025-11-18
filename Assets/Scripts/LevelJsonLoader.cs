using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Loads level configuration from JSON (signals, floor/ceiling by tiles, total tiles and clock tiles),
/// applies LevelManager settings, renders input diagrams and terrain bands, and spawns obstacles (prefabs).
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

    /// <summary>
    /// Compute the sequence of JK outputs sampled on the clock.
    /// Sampling rule:
    /// - Clock edges occur at exact multiples of step: X = step, 2*step, 3*step, ...
    /// - The inputs J/K are sampled at the tile just BEFORE the edge: index = X - 1
    ///   i.e., indices: step-1, 2*step-1, 3*step-1, ...
    /// The JK behavior follows: J=1,K=0 => Q=1; J=0,K=1 => Q=0; J=1,K=1 => toggle; J=0,K=0 => hold.
    /// Returns null if inputs are invalid.
    /// </summary>
    #region Output Computation
    public bool[] ComputeOutputSamplesFromParsedSignals()
    {
        GetClockSamplingParameters(out int step, out int startOffset);
        // Sample at indices just before each edge: step-1, 2*step-1, ...
        int sampleStartOffset = Mathf.Max(0, startOffset - 1);
        var samples = BuildOutputSequenceFromSignalsWithAsync(this.ParsedJSignal, this.ParsedKSignal,
            this.ParsedPresetSignal, this.ParsedClearSignal,
            step, sampleStartOffset);
        return samples;
    }

    /// <summary>
    /// Computes a per-tile output timeline applying asynchronous preset/clear immediately at their tile indices,
    /// and JK logic starting on the tile AFTER each clock edge, using J/K sampled from the tile just before the edge (index = edgeX - 1).
    /// Example: for edge at X = m*step, JK is applied at tile index i = m*step, sampling J/K at i-1.
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

            // 1) Apply JK at the tile AFTER the edge, sampling J/K at i-1,
            //    but only if no async happened strictly before this edge (tiles < i).
            if (isEdge)
            {
                if (!asyncSincePrevEdge)
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
    /// Computes per-tile timeline plus an operation label per tile describing what affected Q at that tile.
    /// Operation tokens:
    ///  - keep : no change
    ///  - preset_async / clear_async : async signals present and applied (value may or may not change)
    ///  - preset_async_noop / clear_async_noop : async present but value already same
    ///  - set_sync / reset_sync / switch_sync : JK logic at clock edge caused change
    ///  - hold_sync : JK evaluated (edge) but no change
    ///  - combined: async changed then JK changed again same tile -> token 'preset_async+switch_sync' etc.
    /// Priority order: apply async first (clear overrides preset), then JK at clock edge.
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

            bool syncApplied = false;
            string syncToken = null;

            // 1) Apply JK at the edge (sampling J/K at i-1) only if no async happened strictly before this edge
            if (isEdge)
            {
                if (!asyncSincePrevEdge)
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

            // 3) Decide final token. If both occurred in the same tile and sync actually changed Q,
            //    expose as a combined token: "{sync}_then_{async}". Otherwise prefer async, then sync.
            string finalToken;
            if (asyncToken != null && syncApplied && syncToken != null && syncToken != "hold_sync")
            {
                finalToken = syncToken + "_then_" + asyncToken; // e.g., switch_sync_then_clear_async
            }
            else if (asyncToken != null)
            {
                finalToken = asyncToken;
            }
            else if (syncApplied)
            {
                finalToken = syncToken;
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



    /// <summary>
    /// Static helper: build an output sequence from raw J/K boolean arrays sampled every clockStep items,
    /// honoring optional asynchronous Preset (force 1) and Clear (force 0). If both are true at the same
    /// sampled index, Clear wins and a warning is logged. Sampling starts at startOffset and continues with step size clockStep.
    /// </summary>
    public static bool[] BuildOutputSequenceFromSignalsWithAsync(bool[] jSignal, bool[] kSignal,
        bool[] presetSignal, bool[] clearSignal, int clockStep, int startOffset = 0)
    {
        if (jSignal == null || kSignal == null || clockStep <= 0) return null;
        int maxIndex = Math.Min(jSignal.Length, kSignal.Length);
        if (maxIndex == 0) return null;

        var outputs = new List<bool>();
        bool qState = false;

        int startIdx = Mathf.Clamp(startOffset, 0, Math.Max(0, maxIndex - 1));
        for (int idx = startIdx; idx < maxIndex; idx += clockStep)
        {
            bool hasPreset = (presetSignal != null && idx < presetSignal.Length) ? presetSignal[idx] : false;
            bool hasClear = (clearSignal != null && idx < clearSignal.Length) ? clearSignal[idx] : false;

            if (hasPreset && hasClear)
            {
                Debug.LogWarning($"LevelJsonLoader: Preset and Clear are both 1 at index {idx}. Applying Clear priority (Q=0).");
                qState = false;
            }
            else if (hasClear)
            {
                qState = false;
            }
            else if (hasPreset)
            {
                qState = true;
            }
            else
            {
                bool j = jSignal[idx];
                bool k = kSignal[idx];
                if (j && !k) qState = true;
                else if (!j && k) qState = false;
                else if (j && k) qState = !qState;
            }

            outputs.Add(qState);
        }

        var outArr = outputs.Count > 0 ? outputs.ToArray() : null;
        return outArr;
    }

    private static bool GetAt(bool[] arr, int idx)
    {
        return arr != null && idx >= 0 && idx < arr.Length && arr[idx];
    }

    private static int MaxLen(params bool[][] arrays)
    {
        int max = 0;
        if (arrays == null) return 0;
        for (int i = 0; i < arrays.Length; i++)
        {
            if (arrays[i] != null && arrays[i].Length > max) max = arrays[i].Length;
        }
        return max;
    }

    /// <summary>
    /// Builds an ordered list of PathVerifier.SignalEvent from the clock-aligned output samples.
    /// Event X positions occur at exact multiples of the clock step: X_n = (n+1) * step.
    /// Note: Input sampling uses the tile just before the edge (index = X_n - 1), handled separately in ComputeOutputSamplesFromParsedSignals.
    /// Returns null if there are no samples.
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
        bool asyncSincePrevEdge = false;

        for (int i = 0; i < totalLength; i++)
        {
            bool isEdge = (step > 0 && i > 0 && (i % step) == 0);

            // 1) Synchronous edge effect at integer X=i
            if (isEdge)
            {
                if (!asyncSincePrevEdge)
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
                // reset cycle async tracker at the edge
                asyncSincePrevEdge = false;
            }

            // 2) Asynchronous immediate effect at half tile X=i+0.5
            bool hasPreset = GetAt(ParsedPresetSignal, i);
            bool hasClear = GetAt(ParsedClearSignal, i);
            if (hasPreset || hasClear)
            {
                bool qBefore = q;
                // Clear priority if both
                q = hasClear ? false : true;
                if (q != qBefore)
                {
                    float xPos = i + 0.5f;
                    events.Add(new PathVerifier.SignalEvent(xPos, q));
                    prev = q;
                }
                asyncSincePrevEdge = true; // counts for next edge suppression
            }
        }
        return events;
    }

    /// <summary>
    /// Determines clock sampling parameters (step in tiles and startOffset) based on LevelManager.clockStepX.
    /// startOffset rule (edge positions): first edge at X=step (i.e., startOffset = step).
    /// Input sampling uses X - 1; event X uses exact multiples.
    /// </summary>
    private void GetClockSamplingParameters(out int step, out int startOffset)
    {
        float stepF = (LevelManager.Instance != null && LevelManager.Instance.clockStepX > 0f)
            ? LevelManager.Instance.clockStepX
            : 1f;
        step = Mathf.Max(1, Mathf.RoundToInt(stepF));
        startOffset = step;
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
        GenerateDiagram(inputTilemap, jSignal, j_YRow);
        GenerateDiagram(inputTilemap, kSignal, k_YRow);
        if (presetSignal != null)
            GenerateDiagram(inputTilemap, presetSignal, preset_YRow);
        if (clearSignal != null)
            GenerateDiagram(inputTilemap, clearSignal, clear_YRow);

        // Use values already defined in ApplyLevelConfig (no redundant checks here)
        int clockStep = Mathf.RoundToInt(LevelManager.Instance != null ? LevelManager.Instance.clockStepX : data.clockTiles);
        int levelLength = Mathf.RoundToInt(LevelManager.Instance != null ? LevelManager.Instance.levelEndX : (data.clockTiles * data.clockCicles));

        var clockPattern = BuildClockPattern(levelLength, clockStep, false);
        DrawPattern(clockTilemap, clockPattern, clock_YRow, startX);
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
        int half = step / 2; // floor division for odd steps
        var period = new int[step];
        int firstVal = startHigh ? 1 : 0;
        int secondVal = startHigh ? 0 : 1;
        for (int i = 0; i < half; i++) period[i] = firstVal;
        for (int i = half; i < step; i++) period[i] = secondVal; // odd steps: extra tile in second half
        for (int i = 0; i < totalLength + step; i++)
            arr[i] = period[i % step];
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

            int prevBit = prev != 0 ? 1 : 0;
            int currBit = curr != 0 ? 1 : 0;
            int nextBit = next != 0 ? 1 : 0;
            int idx = (prevBit << 2) | (currBit << 1) | nextBit;

            TileBase tile = SafeTile(idx);
            if (tile != null)
                map.SetTile(new Vector3Int(startX + i, yRow, 0), tile);
        }
    }

    private TileBase SafeTile(int idx)
    {
        if (diagramTiles == null || diagramTiles.Length <= idx || idx < 0) return null;
        return diagramTiles[idx];
    }

    /// <summary>
    /// Loads and parses the JSON asset into a LevelData instance.
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
    /// Single validation entry point: verifies required scene references and basic JSON presence.
    /// Does not enforce value ranges or parity; assumes JSON is trusted for semantics.
    /// </summary>
    private bool ValidateAll(LevelData data)
    {
        // JSON must be parsed successfully
        if (data == null)
        {
            Debug.LogError("LevelJsonLoader: LevelData is null (JSON parsing failed).");
            return false;
        }

        // Required tilemaps
        if (inputTilemap == null || terrainTilemap == null || clockTilemap == null)
        {
            Debug.LogError("LevelJsonLoader: One or more required Tilemap references are missing (input/terrain/clock).");
            return false;
        }

        // Diagram tiles mapping (8 variants 000..111)
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

        // Terrain tiles
        if (floorTile == null || ceilingTile == null)
        {
            Debug.LogError("LevelJsonLoader: Floor or ceiling tile is not assigned.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies clock configuration to LevelManager: step (tiles, no parity coercion) and total length (cycles * step).
    /// </summary>
    private void ApplyLevelConfig(LevelData data)
    {
        if (LevelManager.Instance != null)
        {
            int step = data.clockTiles;
            LevelManager.Instance.clockStepX = step;
            LevelManager.Instance.diagramEndX = data.clockCicles * step;
            LevelManager.Instance.phaseEndX = LevelManager.Instance.diagramEndX + LevelManager.Instance.phaseSlackTiles;
            LevelManager.Instance.levelEndX = LevelManager.Instance.diagramEndX; // legado permanece no fim lógico
        }
    }



    /// <summary>
    /// Renders a signal diagram onto a tilemap using rising/falling/flat tiles.
    /// </summary>
    #endregion

    #region Diagram Rendering
    private void GenerateDiagram(Tilemap targetMap, bool[] signal, int yRow)
    {
        GenerateDiagram(targetMap, signal, yRow, startX);
    }

    private void GenerateDiagram(Tilemap targetMap, bool[] signal, int yRow, int baseX)
    {
        if (targetMap == null || signal == null) return;
        var pattern = new int[signal.Length];
        for (int i = 0; i < signal.Length; i++) pattern[i] = signal[i] ? 1 : 0;
        DrawPattern(targetMap, pattern, yRow, baseX);
    }

    /// <summary>
    /// Renders a 0/1 band onto a tilemap at the given Y row.
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

        // Extend the band by painting extra tiles to the right of the last '1'
        if (extendRight > 0)
        {
            int lastIdx = -1;
            for (int i = band.Length - 1; i >= 0; i--)
            {
                if (band[i]) { lastIdx = i; break; }
            }
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
    /// Completes static scenery independent from level JSON: left wall and extra left floor tile.
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
                {
                    terrainTilemap.SetTile(new Vector3Int(xWall, y, 0), wallTile);
                }
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
    /// Parses a 0/1 string into a boolean array.
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
    /// Spawns obstacle prefabs based on JSON data at cell-aligned positions.
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
    /// Resolves a prefab by obstacle type string.
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
    /// Attaches a simple config component with JSON-provided parameters for the obstacle controller to consume.
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
        public int clockTiles;
        public string jSignal;
        public string kSignal;
        public string presetSignal;
        public string clearSignal;
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
