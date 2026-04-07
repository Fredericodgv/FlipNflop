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
    [SerializeField] private int floorYRow = 0;
    [SerializeField] private int ceilingYRow = 12;

    [Header("Obstacles")]
    [Tooltip("If true, obstacle startTileY is relative to floorYRow; otherwise it's absolute tile Y.")]
    [SerializeField] private bool obstacleYRelativeToFloor = true;
    [Tooltip("Parent transform for spawned obstacles (optional)")]
    [SerializeField] private Transform obstaclesParent;
    [Tooltip("Map JSON obstacle 'type' to prefab to spawn")]
    [SerializeField] private List<ObstacleSpawner.ObstaclePrefabEntry> obstaclePrefabs = new List<ObstacleSpawner.ObstaclePrefabEntry>();

    [Header("HUD Labels")]
    [Tooltip("Optional: SignalLabelRenderer to position HUD labels at signal Y positions")]
    [SerializeField] private SignalLabelRenderer signalLabelRenderer;

    [Header("Debug")]
    [Tooltip("If true, logs the computed output vector (0/1) used by PathVerifier.")]
    [SerializeField] private bool debugLogOutputVector = false;
    [Tooltip("String view of the computed output vector (0/1). Read-only, for inspector visualization.")]
    [SerializeField]
    [TextArea]
    private string debugOutputVector;

    #endregion

    #region Components

    private TilemapRenderer tilemapRenderer;
    private ObstacleSpawner obstacleSpawner;

    #endregion

    #region Parsed Signals

    public bool[] ParsedJSignal { get; private set; }
    public bool[] ParsedKSignal { get; private set; }
    public bool[] ParsedPresetSignal { get; private set; }
    public bool[] ParsedClearSignal { get; private set; }
    // Per-tile output timeline (async preset/clear immediate; JK at clock edges)
    public bool[] OutputTimeline { get; private set; }
    // Per-tile operation description (e.g., keep, preset_async, clear_async, set_sync, reset_sync, switch_sync, combined)
    public bool asyncActiveHigh { get; private set; }
    [SerializeField]
    private string[] outputOpsPerTile;

    // Async active mode: true = active-high (1), false = active-low (0)

    #endregion

    #region Output Computation
    /// <summary>
    /// Computes output events from parsed signals using FlipFlopSimulator.
    /// </summary>
    public List<PathVerifier.SignalEvent> ComputeOutputEventsFromParsedSignals()
    {
        int diagramLen = GetDiagramLength();
        if (diagramLen <= 0) return null;

        GetClockSamplingParameters(out int step, out int _);

        // Use consolidated method (discards timeline and ops, we only need events)
        FlipFlopSimulator.SimulateJK(ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal,
            step, diagramLen, out _, out var events, asyncActiveHigh);

        // Convert FlipFlopSimulator.SignalEvent to PathVerifier.SignalEvent
        var pathEvents = new List<PathVerifier.SignalEvent>();
        if (events != null)
        {
            foreach (var evt in events)
            {
                pathEvents.Add(new PathVerifier.SignalEvent(evt.x, evt.value));
            }
        }
        return pathEvents;
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

    /// <summary>
    /// Gets diagram length from LevelManager or signal arrays.
    /// </summary>
    private int GetDiagramLength()
    {
        int diagramLen = Mathf.RoundToInt(LevelManager.Instance != null ? LevelManager.Instance.diagramEndX : 0f);
        if (diagramLen <= 0)
        {
            diagramLen = FlipFlopSimulator.MaxLen(ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal);
        }
        return diagramLen;
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
        TextAsset jsonAssetToLoad = levelFile;
        string jsonFileNameFromMenu = MenuManager.LevelToLoadJSON;

        if (!string.IsNullOrEmpty(jsonFileNameFromMenu))
        {
            Debug.Log($"[LOADER] Tentando carregar recurso pelo caminho: '{jsonFileNameFromMenu}'");

            jsonAssetToLoad = Resources.Load<TextAsset>("Levels/" + jsonFileNameFromMenu);

            if (jsonAssetToLoad != null)
                Debug.Log($"[LOADER] SUCESSO: TextAsset '{jsonFileNameFromMenu}' carregado!");
            else
                Debug.LogError($"[LOADER] FALHA: Arquivo '{jsonFileNameFromMenu}' não encontrado na pasta Resources. Usando TextAsset do Inspector.");
        }
        else
        {
            Debug.Log("[LOADER] Variável estática vazia. Usando TextAsset do Inspector (Fallback).");
        }

        var data = LoadLevelData(jsonAssetToLoad);

#if UNITY_EDITOR
        if (!ValidateAll(data)) return;
#endif

        // Initialize rendering components
        tilemapRenderer = new TilemapRenderer(
            inputTilemap,
            terrainTilemap,
            clockTilemap,
            diagramTiles,
            floorTile,
            ceilingTile,
            wallTile,
            flipCeilingY,
            startX);

        obstacleSpawner = new ObstacleSpawner(
            obstaclesParent,
            obstaclePrefabs,
            terrainTilemap,
            startX,
            floorYRow,
            obstacleYRelativeToFloor);

        ApplyLevelConfig(data);

        var jSignal = ParseInputString(data.jSignal);
        var kSignal = ParseInputString(data.kSignal);
        var presetSignal = ParseInputString(data.presetSignal);
        var clearSignal = ParseInputString(data.clearSignal);

        // Store parsed signals without modification (visual diagram shows JSON as-is)
        this.ParsedJSignal = jSignal;
        this.ParsedKSignal = kSignal;
        this.ParsedPresetSignal = presetSignal;
        this.ParsedClearSignal = clearSignal;

        var floorBand = ParseInputString(data.floor);
        var ceilingBand = ParseInputString(data.ceiling);

        int asyncActiveMode = (data != null ? data.asyncActive : 1);
        bool asyncActiveHigh = asyncActiveMode == 1;
        this.asyncActiveHigh = asyncActiveHigh;

        GetClockSamplingParameters(out int clockStep, out int _);
        int diagramLen = GetDiagramLength();
        this.OutputTimeline = FlipFlopSimulator.SimulateJK(ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal, clockStep, diagramLen, out outputOpsPerTile, out _, asyncActiveHigh);
        this.asyncActiveHigh = asyncActiveHigh;
        UpdateDebugOutputVectorString();
        if (debugLogOutputVector)
        {
            Debug.Log($"LevelJsonLoader: Output timeline ({(OutputTimeline != null ? OutputTimeline.Length : 0)}): {debugOutputVector}");
        }

        tilemapRenderer.ClearAllTilemaps();

        Color jColor = ParseColor(data?.jSignalColor, Color.white);
        Color kColor = ParseColor(data?.kSignalColor, Color.white);
        Color presetColor = ParseColor(data?.presetSignalColor, Color.white);
        Color clearColor = ParseColor(data?.clearSignalColor, Color.white);
        Color clockColor = ParseColor(data?.clockSignalColor, Color.white);

        // Calculate Y positions: J=12, Clock=4, others distributed between
        bool hasAsync = (presetSignal != null || clearSignal != null);
        int j_Y = 12;
        int k_Y = hasAsync ? 10 : 8;
        int preset_Y = 8;
        int clear_Y = 6;
        int clock_Y = 4;

        tilemapRenderer.RenderDiagram(jSignal, j_Y, jColor);
        tilemapRenderer.RenderDiagram(kSignal, k_Y, kColor);
        if (presetSignal != null)
            tilemapRenderer.RenderDiagram(presetSignal, preset_Y, presetColor);
        if (clearSignal != null)
            tilemapRenderer.RenderDiagram(clearSignal, clear_Y, clearColor);

        int levelLength = Mathf.RoundToInt(LevelManager.Instance != null ? LevelManager.Instance.levelEndX : (6 * data.clockCicles));

        // Parse active clock edge ("rising" or "falling")
        bool isRisingEdge = data.activeClockEdge != null && data.activeClockEdge.ToLower() == "rising";
        var clockPattern = BuildClockPattern(levelLength, clockStep, isRisingEdge);
        tilemapRenderer.RenderClock(clockPattern, clock_Y, clockColor);
        tilemapRenderer.RenderTerrain(floorBand, ceilingBand, floorYRow, ceilingYRow, 3);

        tilemapRenderer.CompleteStaticScenery(floorYRow, ceilingYRow);

        if (data.obstacles != null && data.obstacles.Count > 0)
        {
            obstacleSpawner.SpawnObstacles(data.obstacles);
        }

        // Generate HUD signal labels at correct Y positions
        if (signalLabelRenderer != null)
        {
            signalLabelRenderer.GenerateLabels();
        }
    }

    #endregion

    #region Clock Pattern

    /// <summary>
    /// Builds clock signal pattern.
    /// Falling edge: 000111 (transition 1→0)
    /// Rising edge: 111000 (transition 0→1)
    /// </summary>
    public static int[] BuildClockPattern(int totalLength, int step, bool risingEdge = false)
    {
        if (totalLength <= 0 || step <= 0) return null;
        var arr = new int[totalLength + step];
        int half = step / 2;
        var period = new int[step];
        int a = risingEdge ? 1 : 0;
        int b = risingEdge ? 0 : 1;
        for (int i = 0; i < half; i++) period[i] = a;
        for (int i = half; i < step; i++) period[i] = b;
        for (int i = 0; i < totalLength + step; i++) arr[i] = period[i % step];
        return arr;
    }

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
            GetClockSamplingParameters(out int clockStep, out int _);
            int diagramLen = GetDiagramLength();
            OutputTimeline = FlipFlopSimulator.SimulateJK(ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal, clockStep, diagramLen, out outputOpsPerTile, out _, asyncActiveHigh);
            UpdateDebugOutputVectorString();
        }
        Debug.Log($"LevelJsonLoader: Output timeline ({(OutputTimeline != null ? OutputTimeline.Length : 0)}): {debugOutputVector}");
    }

    [ContextMenu("Log Output Ops")]
    private void LogOutputOpsContext()
    {
        if (outputOpsPerTile == null || OutputTimeline == null)
        {
            GetClockSamplingParameters(out int clockStep, out int _);
            int diagramLen = GetDiagramLength();
            OutputTimeline = FlipFlopSimulator.SimulateJK(ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal, clockStep, diagramLen, out outputOpsPerTile, out _, asyncActiveHigh);
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
    /// Sets fixed clock step (6) and derived lengths from level data.
    /// </summary>
    private void ApplyLevelConfig(LevelData data)
    {
        if (LevelManager.Instance != null)
        {
            if (data.clockCicles <= 0)
            {
                Debug.LogWarning($"LevelJsonLoader: Invalid clockCicles={data.clockCicles}. Using default value of 10.");
                data.clockCicles = 10;
            }

            int step = 6;
            LevelManager.Instance.clockStepX = step;
            LevelManager.Instance.diagramEndX = data.clockCicles * step;
            LevelManager.Instance.phaseEndX = LevelManager.Instance.diagramEndX + LevelManager.Instance.phaseSlackTiles;
            LevelManager.Instance.levelEndX = LevelManager.Instance.diagramEndX; // legado permanece no fim lógico
        }
    }



    #endregion

    #region Clock Patternion
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

    #endregion

    #region Data Classes

    [Serializable]
    private class LevelData
    {
        public string levelName;
        public int clockCicles;
        public int asyncActive = 1;
        public string activeClockEdge = "falling"; // "rising" or "falling"
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
        public List<ObstacleSpawner.ObstacleData> obstacles;
    }

    #endregion
}
