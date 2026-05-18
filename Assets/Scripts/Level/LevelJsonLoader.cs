using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    #region Components

    private TilemapRenderer tilemapRenderer;
    private ObstacleSpawner obstacleSpawner;

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

    #region Data Class

    /// <summary>
    /// Represents the deserialized level JSON. JsonProperty maps camelCase JSON keys
    /// to readable C# names and also handles typos in existing JSON (e.g. "clockCicles").
    /// </summary>
    private class LevelData
    {
        [JsonProperty("levelName")]
        public string LevelName { get; set; }

        // Aceita tanto "clockCycles" (correto) quanto "clockCicles" (legado com erro de digitação)
        [JsonProperty("clockCycles")]
        public int ClockCycles { get; set; }

        [JsonProperty("clockCicles")]   // fallback para JSONs antigos
        private int ClockCiclesLegacy { set => ClockCycles = ClockCycles > 0 ? ClockCycles : value; }

        [JsonProperty("asyncActive")]
        public int AsyncActive { get; set; } = 1;

        [JsonProperty("activeClockEdge")]
        public string ActiveClockEdge { get; set; } = "falling";

        [JsonProperty("jSignal")]
        public string JSignal { get; set; }

        [JsonProperty("kSignal")]
        public string KSignal { get; set; }

        [JsonProperty("presetSignal")]
        public string PresetSignal { get; set; }

        [JsonProperty("clearSignal")]
        public string ClearSignal { get; set; }

        [JsonProperty("jSignalColor")]
        public string JSignalColor { get; set; }

        [JsonProperty("kSignalColor")]
        public string KSignalColor { get; set; }

        [JsonProperty("presetSignalColor")]
        public string PresetSignalColor { get; set; }

        [JsonProperty("clearSignalColor")]
        public string ClearSignalColor { get; set; }

        [JsonProperty("clockSignalColor")]
        public string ClockSignalColor { get; set; }

        [JsonProperty("floor")]
        public string Floor { get; set; }

        [JsonProperty("ceiling")]
        public string Ceiling { get; set; }

        [JsonProperty("obstacles")]
        public List<ObstacleSpawner.ObstacleData> Obstacles { get; set; }
    }

    #endregion

    #region Lifecycle

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

    /// <summary>
    /// Resolves JSON string from: uploaded content → Resources → Inspector asset.
    /// </summary>
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
        ParsedJSignal = ParseSignalString(data.JSignal);
        ParsedKSignal = ParseSignalString(data.KSignal);
        ParsedPresetSignal = ParseSignalString(data.PresetSignal);
        ParsedClearSignal = ParseSignalString(data.ClearSignal);

        asyncActiveHigh = data.AsyncActive != 0;

        GetClockSamplingParameters(out int clockStep, out _);
        int diagramLen = GetDiagramLength();

        OutputTimeline = FlipFlopSimulator.SimulateJK(
            ParsedJSignal, ParsedKSignal, ParsedPresetSignal, ParsedClearSignal,
            clockStep, diagramLen, out outputOpsPerTile, out _, asyncActiveHigh);

        UpdateDebugOutputVectorString();

        if (debugLogOutputVector)
            Debug.Log($"LevelJsonLoader: Output timeline ({OutputTimeline?.Length ?? 0}): {debugOutputVector}");
    }

    private void RenderLevel(LevelData data)
    {
        tilemapRenderer.ClearAllTilemaps();

        Color jColor = ParseColor(data.JSignalColor, Color.white);
        Color kColor = ParseColor(data.KSignalColor, Color.white);
        Color presetColor = ParseColor(data.PresetSignalColor, Color.white);
        Color clearColor = ParseColor(data.ClearSignalColor, Color.white);
        Color clockColor = ParseColor(data.ClockSignalColor, Color.white);

        bool hasAsync = ParsedPresetSignal != null || ParsedClearSignal != null;
        int jY = 12;
        int kY = hasAsync ? 10 : 8;
        int presetY = 8;
        int clearY = 6;
        int clockY = 4;

        tilemapRenderer.RenderDiagram(ParsedJSignal, jY, jColor);
        tilemapRenderer.RenderDiagram(ParsedKSignal, kY, kColor);
        if (ParsedPresetSignal != null) tilemapRenderer.RenderDiagram(ParsedPresetSignal, presetY, presetColor);
        if (ParsedClearSignal != null) tilemapRenderer.RenderDiagram(ParsedClearSignal, clearY, clearColor);

        GetClockSamplingParameters(out int clockStep, out _);
        int levelLength = LevelManager.Instance != null
            ? Mathf.RoundToInt(LevelManager.Instance.levelEndX)
            : 6 * data.ClockCycles;

        bool risingEdge = string.Equals(data.ActiveClockEdge, "rising", StringComparison.OrdinalIgnoreCase);
        var clockPattern = BuildClockPattern(levelLength, clockStep, risingEdge);

        tilemapRenderer.RenderClock(clockPattern, clockY, clockColor);
        tilemapRenderer.RenderTerrain(ParseSignalString(data.Floor), ParseSignalString(data.Ceiling), floorYRow, ceilingYRow, 3);
        tilemapRenderer.CompleteStaticScenery(floorYRow, ceilingYRow);

        if (data.Obstacles?.Count > 0)
            obstacleSpawner.SpawnObstacles(data.Obstacles);

        signalLabelRenderer?.GenerateLabels();
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

    #region Clock Pattern

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

    #region Helpers

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

    private static Color ParseColor(string hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        string s = hex.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = "#" + s[2..];
        else if (!s.StartsWith("#")) s = "#" + s;
        return ColorUtility.TryParseHtmlString(s, out var c) ? c : fallback;
    }

    /// <summary>
    /// Converts a string of '0' and '1' characters into a bool array.
    /// Returns null if the string is null/empty or contains no valid bits.
    /// </summary>
    private static bool[] ParseSignalString(string data)
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