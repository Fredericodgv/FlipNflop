using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;
using Newtonsoft.Json;

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
    [SerializeField] private TextAsset levelFile;

    [Header("Tiles - Diagrams")]
    [Tooltip("Tiles for 3-bit neighborhood patterns (prev,curr,next) as binary index: 0=000..7=111")]
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
    [Tooltip("If true, obstacle startTileY is relative to floorYRow; otherwise absolute tile Y.")]
    [SerializeField] private bool obstacleYRelativeToFloor = true;
    [SerializeField] private Transform obstaclesParent;
    [SerializeField] private List<ObstacleSpawner.ObstaclePrefabEntry> obstaclePrefabs = new List<ObstacleSpawner.ObstaclePrefabEntry>();

    [Header("HUD Labels")]
    [SerializeField] private SignalLabelRenderer signalLabelRenderer;

    [Header("Debug")]
    [SerializeField] private bool debugLogOutputVector = false;
    [SerializeField][TextArea] private string debugOutputVector;

    #endregion

    #region Components & Cache
    private TilemapRenderer tilemapRenderer;
    private ObstacleSpawner obstacleSpawner;

    // Cacheados no RenderLevel para uso no ApplySignalColors
    private int _cachedLevelLength;
    private int _cachedJ_Y;
    private int _cachedK_Y;
    private int _cachedClock_Y;
    private int _cachedPresetY;
    private int _cachedClearY;
    private bool _hasPreset;
    private bool _hasClear;
    #endregion

    #region Parsed Signals
    public bool[] ParsedJSignal { get; private set; }
    public bool[] ParsedKSignal { get; private set; }
    public bool[] ParsedPresetSignal { get; private set; }
    public bool[] ParsedClearSignal { get; private set; }
    public bool[] OutputTimeline { get; private set; }
    public bool asyncActiveHigh { get; private set; }

    [SerializeField] private string[] outputOpsPerTile;
    #endregion

    #region Lifecycle & Events

    private void OnEnable()
    {
        // Inscreve no evento da branch develop para mudar cores em tempo real
        SignalColorManager.OnColorsChanged += ApplySignalColors;
    }

    private void OnDisable()
    {
        SignalColorManager.OnColorsChanged -= ApplySignalColors;
    }

    private void Awake()
    {
        string json = ResolveJsonSource();
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("LevelJsonLoader: nenhuma fonte de JSON disponível.");
            return;
        }

        LevelData data = ParseJson(json);
        if (data == null) return;

#if UNITY_EDITOR
        if (!ValidateAll(data)) return;
#endif

        InitComponents();
        ApplyLevelConfig(data);
        LoadSignals(data);
        RenderLevel(data);
    }

    #endregion

    #region Initialization Helpers

    private string ResolveJsonSource()
    {
        if (!string.IsNullOrEmpty(UploadedLevelJson.Content))
        {
            string content = UploadedLevelJson.Content;
            UploadedLevelJson.Content = null;
            return content;
        }

        if (!string.IsNullOrEmpty(MenuManager.LevelToLoadJSON))
        {
            var asset = Resources.Load<TextAsset>("Levels/" + MenuManager.LevelToLoadJSON);
            if (asset != null) return asset.text;
            Debug.LogError($"LevelJsonLoader: arquivo '{MenuManager.LevelToLoadJSON}' não encontrado em Resources/Levels/.");
        }

        if (levelFile != null && !string.IsNullOrWhiteSpace(levelFile.text))
            return levelFile.text;

        return null;
    }

    private LevelData ParseJson(string json)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
            };
            return JsonConvert.DeserializeObject<LevelData>(json, settings);
        }
        catch (JsonException ex)
        {
            Debug.LogError($"LevelJsonLoader: erro ao parsear JSON — {ex.Message}");
            return null;
        }
    }

    private void InitComponents()
    {
        tilemapRenderer = new TilemapRenderer(
            inputTilemap, terrainTilemap, clockTilemap,
            diagramTiles, floorTile, ceilingTile, wallTile,
            flipCeilingY, startX);

        obstacleSpawner = new ObstacleSpawner(
            obstaclesParent, obstaclePrefabs, terrainTilemap,
            startX, floorYRow, obstacleYRelativeToFloor);
    }

    private void LoadSignals(LevelData data)
    {
        ParsedJSignal = data.JSignal;
        ParsedKSignal = data.KSignal;
        ParsedPresetSignal = data.PresetSignal;
        ParsedClearSignal = data.ClearSignal;

        asyncActiveHigh = data.AsyncActive != 0;

        GetClockSamplingParameters(out int clockStep, out _);
        int diagramLen = GetDiagramLength();

        try
        {
            OutputTimeline = FlipFlopSimulator.SimulateJK(
                ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal,
                clockStep, diagramLen, out outputOpsPerTile, out _, asyncActiveHigh);
        }
        catch (InvalidOperationException ex)
        {
            Debug.LogError($"LevelJsonLoader: erro ao simular flip-flop — {ex.Message}");
            OutputTimeline = null;
            outputOpsPerTile = null;
            return;
        }

        UpdateDebugOutputVectorString();

        if (debugLogOutputVector)
            Debug.Log($"LevelJsonLoader: Output timeline ({OutputTimeline?.Length ?? 0}): {debugOutputVector}");
    }

    private void RenderLevel(LevelData data)
    {
        tilemapRenderer.ClearAllTilemaps();

        bool hasAsync = ParsedPresetSignal != null || ParsedClearSignal != null;
        int jY = 12;
        int kY = hasAsync ? 10 : 8;
        int presetY = 8;
        int clearY = 6;
        int clockY = 4;

        // Dentro de RenderLevel, após definir presetY e clearY:
        _hasPreset = ParsedPresetSignal != null;
        _hasClear = ParsedClearSignal != null;
        _cachedPresetY = presetY;
        _cachedClearY = clearY;

        // Verifica se o SignalColorManager (develop) existe, senão usa a cor do JSON (Refactor)
        Color jColor = SignalColorManager.Instance != null ? SignalColorManager.Instance.ColorJ : data.JSignalColor;
        Color kColor = SignalColorManager.Instance != null ? SignalColorManager.Instance.ColorK : data.KSignalColor;
        Color presetColor = SignalColorManager.Instance != null ? SignalColorManager.Instance.ColorPreset : data.PresetSignalColor;
        Color clearColor = SignalColorManager.Instance != null ? SignalColorManager.Instance.ColorClear : data.ClearSignalColor;
        Color clockColor = SignalColorManager.Instance != null ? SignalColorManager.Instance.ColorCLK : data.ClockSignalColor;

        tilemapRenderer.RenderDiagram(ParsedJSignal, jY, jColor);
        tilemapRenderer.RenderDiagram(ParsedKSignal, kY, kColor);
        if (ParsedPresetSignal != null) tilemapRenderer.RenderDiagram(ParsedPresetSignal, presetY, presetColor);
        if (ParsedClearSignal != null) tilemapRenderer.RenderDiagram(ParsedClearSignal, clearY, clearColor);

        GetClockSamplingParameters(out int clockStep, out _);
        int levelLength = LevelManager.Instance != null
            ? Mathf.RoundToInt(LevelManager.Instance.levelEndX)
            : 6 * data.ClockCycles;

        // Atualiza o cache para uso futuro pelo ApplySignalColors
        _cachedLevelLength = levelLength + clockStep;
        _cachedJ_Y = jY;
        _cachedK_Y = kY;
        _cachedClock_Y = clockY;

        bool risingEdge = string.Equals(data.ActiveClockEdge, "rising", StringComparison.OrdinalIgnoreCase);
        var clockPattern = BuildClockPattern(levelLength, clockStep, risingEdge);

        tilemapRenderer.RenderClock(clockPattern, clockY, clockColor);
        tilemapRenderer.RenderTerrain(data.Floor, data.Ceiling, floorYRow, ceilingYRow, 3);
        tilemapRenderer.CompleteStaticScenery(floorYRow, ceilingYRow);

        if (data.Obstacles?.Count > 0)
            obstacleSpawner.SpawnObstacles(data.Obstacles);

        if (signalLabelRenderer != null)
        {
            bool asyncIsHigh = data.AsyncActive == 1;

            // O risingEdge já foi calculado ali em cima! É só passá-lo direto.
            signalLabelRenderer.GenerateLabels(
                jY, kY, presetY, clearY, clockY,
                ParsedPresetSignal != null,
                ParsedClearSignal != null,
                asyncIsHigh,  // Passa para o Preset e Clear
                risingEdge    // Passa para o Clock (já existia no seu código)
            );
        }
    }

    #endregion

    #region Real-time Color Update (from develop)

    /// <summary>
    /// Reapplies signal line colors to tilemaps when user changes
    /// the colors in the menu. Called via the SignalColorManager.OnColorsChanged event.
    /// </summary>
    private void ApplySignalColors()
    {
        if (tilemapRenderer == null) return;
        if (SignalColorManager.Instance == null) return;
        if (_cachedLevelLength <= 0) return;

        Color jColor = SignalColorManager.Instance.ColorJ;
        Color kColor = SignalColorManager.Instance.ColorK;
        Color clkColor = SignalColorManager.Instance.ColorCLK;

        // J, K e CLK (como antes)
        ColorRow(inputTilemap, _cachedLevelLength, _cachedJ_Y, startX, jColor);
        ColorRow(inputTilemap, _cachedLevelLength, _cachedK_Y, startX, kColor);
        ColorRow(clockTilemap, _cachedLevelLength, _cachedClock_Y, startX, clkColor);

        // Preset e Clear (novas adições)
        if (_hasPreset)
        {
            Color presetColor = SignalColorManager.Instance.ColorPreset;
            ColorRow(inputTilemap, _cachedLevelLength, _cachedPresetY, startX, presetColor);
        }

        if (_hasClear)
        {
            Color clearColor = SignalColorManager.Instance.ColorClear;
            ColorRow(inputTilemap, _cachedLevelLength, _cachedClearY, startX, clearColor);
        }
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

    #region Public API

    public List<PathVerifier.SignalEvent> ComputeOutputEventsFromParsedSignals()
    {
        int diagramLen = GetDiagramLength();
        if (diagramLen <= 0) return null;

        GetClockSamplingParameters(out int step, out _);
        FlipFlopSimulator.SimulateJK(
            ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal,
            step, diagramLen, out _, out var events, asyncActiveHigh);

        var pathEvents = new List<PathVerifier.SignalEvent>();
        if (events != null)
            foreach (var evt in events)
                pathEvents.Add(new PathVerifier.SignalEvent(evt.x, evt.value));

        return pathEvents;
    }

    #endregion

    #region Helpers & Validations

    private void GetClockSamplingParameters(out int step, out int startOffset)
    {
        float stepF = (LevelManager.Instance != null && LevelManager.Instance.clockStepX > 0f)
            ? LevelManager.Instance.clockStepX
            : 1f;
        step = Mathf.Max(1, Mathf.RoundToInt(stepF));
        startOffset = step;
    }

    private int GetDiagramLength()
    {
        int len = LevelManager.Instance != null
            ? Mathf.RoundToInt(LevelManager.Instance.diagramEndX)
            : 0;
        return len > 0
            ? len
            : FlipFlopSimulator.MaxLen(ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal);
    }

    private void ApplyLevelConfig(LevelData data)
    {
        if (LevelManager.Instance == null) return;

        if (data.ClockCycles <= 0)
        {
            Debug.LogWarning($"LevelJsonLoader: clockCycles={data.ClockCycles} inválido. Usando 10.");
            data.ClockCycles = 10;
        }

        const int step = 6;
        LevelManager.Instance.clockStepX = step;
        LevelManager.Instance.diagramEndX = data.ClockCycles * step;
        LevelManager.Instance.phaseEndX = LevelManager.Instance.diagramEndX + LevelManager.Instance.phaseSlackTiles;
        LevelManager.Instance.levelEndX = LevelManager.Instance.diagramEndX;
    }

    private bool ValidateAll(LevelData data)
    {
        if (data == null)
        { Debug.LogError("LevelJsonLoader: LevelData nulo (falha no parse do JSON)."); return false; }

        if (inputTilemap == null || terrainTilemap == null || clockTilemap == null)
        { Debug.LogError("LevelJsonLoader: uma ou mais referências de Tilemap ausentes."); return false; }

        if (diagramTiles == null || diagramTiles.Length < 8)
        { Debug.LogError("LevelJsonLoader: diagramTiles precisa de 8 entradas (0..7)."); return false; }

        for (int i = 0; i < 8; i++)
            if (diagramTiles[i] == null)
            { Debug.LogError($"LevelJsonLoader: diagramTiles[{i}] não atribuído."); return false; }

        if (floorTile == null || ceilingTile == null)
        { Debug.LogError("LevelJsonLoader: floorTile ou ceilingTile não atribuído."); return false; }

        return true;
    }

    private void UpdateDebugOutputVectorString()
    {
        if (OutputTimeline == null) { debugOutputVector = "(null)"; return; }
        var sb = new StringBuilder(OutputTimeline.Length);
        foreach (bool b in OutputTimeline) sb.Append(b ? '1' : '0');
        debugOutputVector = sb.ToString();
    }

    /// <summary>
    /// Builds clock signal pattern.
    /// Falling edge: 000111 (transition 1→0)
    /// Rising edge: 111000 (transition 0→1)
    /// </summary>
    public static int[] BuildClockPattern(int totalLength, int step, bool risingEdge = false)
    {
        if (totalLength <= 0 || step <= 0) return null;

        int half = step / 2;
        int a = risingEdge ? 1 : 0;
        int b = risingEdge ? 0 : 1;
        var period = new int[step];

        for (int i = 0; i < half; i++) period[i] = a;
        for (int i = half; i < step; i++) period[i] = b;

        var arr = new int[totalLength + step];
        for (int i = 0; i < arr.Length; i++) arr[i] = period[i % step];
        return arr;
    }

    #endregion

    #region Context Menu Debug

    [ContextMenu("Log Output Vector (0/1)")]
    private void LogOutputVectorContext()
    {
        if (OutputTimeline == null)
        {
            GetClockSamplingParameters(out int clockStep, out _);
            OutputTimeline = FlipFlopSimulator.SimulateJK(
                ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal,
                clockStep, GetDiagramLength(), out outputOpsPerTile, out _, asyncActiveHigh);
            UpdateDebugOutputVectorString();
        }
        Debug.Log($"LevelJsonLoader: Output timeline ({OutputTimeline?.Length ?? 0}): {debugOutputVector}");
    }

    [ContextMenu("Log Output Ops")]
    private void LogOutputOpsContext()
    {
        if (outputOpsPerTile == null)
        {
            GetClockSamplingParameters(out int clockStep, out _);
            OutputTimeline = FlipFlopSimulator.SimulateJK(
                ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal,
                clockStep, GetDiagramLength(), out outputOpsPerTile, out _, asyncActiveHigh);
        }
        var sb = new StringBuilder(outputOpsPerTile.Length * 6);
        for (int i = 0; i < outputOpsPerTile.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(outputOpsPerTile[i]);
        }
        Debug.Log($"LevelJsonLoader: Ops per tile ({outputOpsPerTile.Length}): {sb}");
    }

    #endregion
}