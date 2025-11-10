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
    [Header("Tilemaps")]
    [SerializeField] private Tilemap inputTilemap;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Tilemap clockTilemap;

    [Header("Level JSON")]
    [Tooltip("JSON containing jSignal, kSignal, floor, ceiling, levelTiles and clockTiles")]
    [SerializeField] private TextAsset levelFile;

    [Header("Tiles - Diagrams")]
    [Tooltip("Tile used for steady (flat) signal 1")]
    [SerializeField] private TileBase tile_1_Standard;
    [Tooltip("Tile used for the '1' side of a rising edge (first 1 after a 0->1 transition)")]
    [SerializeField] private TileBase tile_1_Rising;
    [Tooltip("Tile used for the '1' side of a falling edge (last 1 before a 1->0 transition)")]
    [SerializeField] private TileBase tile_1_Falling;
    [Tooltip("Tile used when a single '1' is both rising and falling (0->1->0 short pulse)")]
    [SerializeField] private TileBase tile_1_RisingFalling;

    [Tooltip("Tile used for steady (flat) signal 0")]
    [SerializeField] private TileBase tile_0_Standard;
    [Tooltip("Tile used for the '0' side of a rising edge (last 0 before a 0->1 transition)")]
    [SerializeField] private TileBase tile_0_Rising;
    [Tooltip("Tile used for the '0' side of a falling edge (first 0 after a 1->0 transition)")]
    [SerializeField] private TileBase tile_0_Falling;
    [Tooltip("Tile used when a single '0' is both falling and rising (1->0->1 short dip)")]
    [SerializeField] private TileBase tile_0_FallingRising;

    [Header("Tiles - Terrain")]
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase ceilingTile;
    [SerializeField] private bool flipCeilingY = true;
    [SerializeField] private TileBase wallTile;

    [Header("Placement Settings")]
    [SerializeField] private int startX = 0;
    [SerializeField] private int j_YRow = 8;
    [SerializeField] private int k_YRow = 5;
    [SerializeField] private int preset_YRow = 10; // optional row for preset diagram
    [SerializeField] private int clear_YRow = 2;   // optional row for clear diagram
    [SerializeField] private int clock_YRow = 14;  // row for clock diagram
    [SerializeField] private int floorYRow = 0;
    [SerializeField] private int ceilingYRow = 12;

    [Header("Obstacles")]
    [Tooltip("If true, obstacle startTileY is relative to floorYRow; otherwise it's absolute tile Y.")]
    [SerializeField] private bool obstacleYRelativeToFloor = true;
    [Tooltip("Parent transform for spawned obstacles (optional)")]
    [SerializeField] private Transform obstaclesParent;
    [Tooltip("Map JSON obstacle 'type' to prefab to spawn")]
    [SerializeField] private List<ObstaclePrefabEntry> obstaclePrefabs = new List<ObstaclePrefabEntry>();

    /// <summary>
    /// JSON data model for level configuration.
    /// </summary>
    [Serializable]
    private class LevelData
    {
        public string levelName;
        public int levelTiles;
        public int clockTiles;
        public string jSignal;
        public string kSignal;
        // Optional asynchronous inputs: when present, override the output at that instant
        // presetSignal -> forces Q=1; clearSignal -> forces Q=0. If absent, ignored.
        public string presetSignal;
        public string clearSignal;
        public string floor;
        public string ceiling;
        public List<ObstacleData> obstacles;
    }

    public bool[] ParsedJSignal { get; private set; }
    public bool[] ParsedKSignal { get; private set; }
    public bool[] ParsedPresetSignal { get; private set; }
    public bool[] ParsedClearSignal { get; private set; }

    /// <summary>
    /// Compute the sequence of JK outputs sampled on the clock. This uses the parsed J/K arrays
    /// and samples them at indices (clockStep-1), (2*clockStep-1), ... until one of the arrays ends.
    /// The JK behavior follows: J=1,K=0 => Q=1; J=0,K=1 => Q=0; J=1,K=1 => toggle; J=0,K=0 => hold.
    /// Returns null if inputs are invalid.
    /// </summary>
    public bool[] ComputeOutputSamplesFromParsedSignals()
    {
        var samples = BuildOutputSequenceFromSignalsWithAsync(this.ParsedJSignal, this.ParsedKSignal,
            this.ParsedPresetSignal, this.ParsedClearSignal,
            (LevelManager.Instance != null && LevelManager.Instance.clockStepX > 0f)
                ? Mathf.Max(1, Mathf.RoundToInt(LevelManager.Instance.clockStepX))
                : 1);
        Debug.Log($"LevelJsonLoader: ComputeOutputSamplesFromParsedSignals -> samples length={(samples == null ? 0 : samples.Length)} value={BoolArrayToString(samples)}");
        return samples;
    }

    /// <summary>
    /// Static helper: build an output sequence from raw J/K boolean arrays sampled every clockStep items.
    /// </summary>
    public static bool[] BuildOutputSequenceFromSignals(bool[] jSignal, bool[] kSignal, int clockStep)
    {
        if (jSignal == null || kSignal == null || clockStep <= 0) return null;
        int maxIndex = Math.Min(jSignal.Length, kSignal.Length);
        if (maxIndex == 0) return null;

        var outputs = new List<bool>();
        bool qState = false; // initial output assumed LOW

        for (int idx = 0; idx < maxIndex; idx += clockStep)
        {
            bool j = jSignal[idx];
            bool k = kSignal[idx];
            if (j && !k) qState = true;
            else if (!j && k) qState = false;
            else if (j && k) qState = !qState;
            outputs.Add(qState);
        }

        var outArr = outputs.Count > 0 ? outputs.ToArray() : null;
        Debug.Log($"LevelJsonLoader: BuildOutputSequenceFromSignals(clockStep={clockStep}, jLen={(jSignal == null ? 0 : jSignal.Length)}, kLen={(kSignal == null ? 0 : kSignal.Length)}) -> outputs len={(outArr == null ? 0 : outArr.Length)} value={BoolArrayToString(outArr)}");
        return outArr;
    }

    /// <summary>
    /// Static helper: build an output sequence from raw J/K boolean arrays sampled every clockStep items,
    /// honoring optional asynchronous Preset (force 1) and Clear (force 0). If both are true at the same
    /// sampled index, Clear wins and a warning is logged.
    /// </summary>
    public static bool[] BuildOutputSequenceFromSignalsWithAsync(bool[] jSignal, bool[] kSignal,
        bool[] presetSignal, bool[] clearSignal, int clockStep)
    {
        if (jSignal == null || kSignal == null || clockStep <= 0) return null;
        int maxIndex = Math.Min(jSignal.Length, kSignal.Length);
        if (maxIndex == 0) return null;

        var outputs = new List<bool>();
        bool qState = false; 

        for (int idx = 0; idx < maxIndex; idx += clockStep)
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
                // else hold
            }

            outputs.Add(qState);
        }

        var outArr = outputs.Count > 0 ? outputs.ToArray() : null;
        Debug.Log($"LevelJsonLoader: BuildOutputSequenceFromSignalsWithAsync(clockStep={clockStep}, jLen={(jSignal == null ? 0 : jSignal.Length)}, kLen={(kSignal == null ? 0 : kSignal.Length)}, presetLen={(presetSignal == null ? 0 : presetSignal.Length)}, clearLen={(clearSignal == null ? 0 : clearSignal.Length)}) -> outputs len={(outArr == null ? 0 : outArr.Length)} value={BoolArrayToString(outArr)}");
        return outArr;
    }

    /// <summary>
    /// Builds an ordered list of PathVerifier.SignalEvent from the clock-aligned output samples.
    /// Each sample i maps to X = (i+1) * step (step is taken from LevelManager.clockStepX when available).
    /// Returns null if there are no samples.
    /// </summary>
    public List<PathVerifier.SignalEvent> ComputeOutputEventsFromParsedSignals()
    {
        var samples = ComputeOutputSamplesFromParsedSignals();
        if (samples == null || samples.Length == 0) return null;

        float step = (LevelManager.Instance != null && LevelManager.Instance.clockStepX > 0f)
            ? LevelManager.Instance.clockStepX
            : 1f;

        var events = new List<PathVerifier.SignalEvent>(samples.Length);
        for (int i = 0; i < samples.Length; i++)
        {
            float x = i * step;
            events.Add(new PathVerifier.SignalEvent(x, samples[i]));
        }
        Debug.Log($"LevelJsonLoader: ComputeOutputEventsFromParsedSignals -> {EventsToString(events)}");
        return events;
    }

    private static string BoolArrayToString(bool[] arr)
    {
        if (arr == null) return "null";
        var sb = new StringBuilder(arr.Length);
        for (int i = 0; i < arr.Length; i++) sb.Append(arr[i] ? '1' : '0');
        return sb.ToString();
    }

    private static string EventsToString(List<PathVerifier.SignalEvent> events)
    {
        if (events == null) return "null";
        var sb = new StringBuilder();
        sb.Append("events[");
        for (int i = 0; i < events.Count; i++)
        {
            var e = events[i];
            sb.AppendFormat("(x={0:0.##},v={1})", e.x, e.value ? 1 : 0);
            if (i < events.Count - 1) sb.Append(",");
        }
        sb.Append("]");
        return sb.ToString();
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

    /// <summary>
    /// Initializes tilemaps from the provided JSON and updates LevelManager settings.
    /// </summary>
    private void Awake()
    {
        if (!ValidateReferences()) return;
        var data = LoadLevelData(levelFile);
        if (data == null) return;

        ApplyLevelConfig(data);

        var jSignal = ParseInputString(data.jSignal);
        var kSignal = ParseInputString(data.kSignal);
        var presetSignal = ParseInputString(data.presetSignal);
        var clearSignal = ParseInputString(data.clearSignal);
        // store parsed signals for external consumers (e.g. PathVerifier)
        this.ParsedJSignal = jSignal;
        this.ParsedKSignal = kSignal;
        this.ParsedPresetSignal = presetSignal;
        this.ParsedClearSignal = clearSignal;
        Debug.Log($"LevelJsonLoader: raw jSignal string='{data.jSignal}' parsed({(jSignal == null ? 0 : jSignal.Length)}): {BoolArrayToString(jSignal)}");
        Debug.Log($"LevelJsonLoader: raw kSignal string='{data.kSignal}' parsed({(kSignal == null ? 0 : kSignal.Length)}): {BoolArrayToString(kSignal)}");
        if (data.presetSignal != null)
            Debug.Log($"LevelJsonLoader: raw presetSignal string='{data.presetSignal}' parsed({(presetSignal == null ? 0 : presetSignal.Length)}): {BoolArrayToString(presetSignal)}");
        if (data.clearSignal != null)
            Debug.Log($"LevelJsonLoader: raw clearSignal string='{data.clearSignal}' parsed({(clearSignal == null ? 0 : clearSignal.Length)}): {BoolArrayToString(clearSignal)}");
        var floorBand = ParseInputString(data.floor);
        var ceilingBand = ParseInputString(data.ceiling);

        ClearAllTilemaps();
        GenerateDiagram(inputTilemap, jSignal, j_YRow);
        GenerateDiagram(inputTilemap, kSignal, k_YRow);
        if (presetSignal != null)
            GenerateDiagram(inputTilemap, presetSignal, preset_YRow);
        if (clearSignal != null)
            GenerateDiagram(inputTilemap, clearSignal, clear_YRow);

        int clockStep = data.clockTiles > 0 ? data.clockTiles : 1;
        if (data.clockTiles <= 0)
            Debug.LogWarning($"LevelJsonLoader: clockTiles <= 0 in JSON; defaulting clock step to 1.");
        int levelLength = data.levelTiles > 0
            ? data.levelTiles
            : Mathf.Max(
                jSignal != null ? jSignal.Length : 0,
                Mathf.Max(
                    kSignal != null ? kSignal.Length : 0,
                    Mathf.Max(
                        floorBand != null ? floorBand.Length : 0,
                        ceilingBand != null ? ceilingBand.Length : 0)));

        if (levelLength > 0)
        {
            int halfStep = Mathf.Max(1, clockStep / 2);
            // Start clock half-period earlier so a full cycle appears before x=startX
            // We extend the generated length by halfStep to preserve the right boundary
            var clock = BuildClockSignal(levelLength + halfStep, halfStep, false);
            Debug.Log($"LevelJsonLoader: built clock signal (len={clock.Length}, halfStep={halfStep}, clockTiles={clockStep}): {BoolArrayToString(clock)}");
            GenerateDiagram(clockTilemap, clock, clock_YRow, startX - halfStep);
        }
        else
        {
            Debug.LogError("LevelJsonLoader: Cannot build clock signal because determined level length is 0.");
        }
        GenerateBand(terrainTilemap, floorBand, floorYRow, floorTile, false);
        GenerateBand(terrainTilemap, ceilingBand, ceilingYRow, ceilingTile, flipCeilingY);

        CompleteStaticScenery();

        if (data.obstacles != null && data.obstacles.Count > 0)
        {
            SpawnObstacles(data.obstacles);
        }
    }

    /// <summary>
    /// Builds a clock boolean array of totalLength, toggling every 'step' tiles. Starts LOW unless startHigh=true.
    /// Example (step=5, startHigh=false): 00000 11111 00000 11111 ...
    /// </summary>
    public static bool[] BuildClockSignal(int totalLength, int step, bool startHigh = false)
    {
        if (totalLength <= 0 || step <= 0) return null;
        var arr = new bool[totalLength];
        bool state = startHigh;
        int count = 0;
        for (int i = 0; i < totalLength; i++)
        {
            arr[i] = state;
            count++;
            if (count >= step)
            {
                state = !state;
                count = 0;
            }
        }
        return arr;
    }

    /// <summary>
    /// Loads and parses the JSON asset into a LevelData instance.
    /// </summary>
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

    /// <summary>
    /// Applies levelTiles and clockTiles to LevelManager (as levelEndX and clockStepX) if available.
    /// </summary>
    private void ApplyLevelConfig(LevelData data)
    {
        if (LevelManager.Instance != null)
        {
            // Clock mandatory: default to 1 if invalid
            LevelManager.Instance.clockStepX = data.clockTiles > 0 ? data.clockTiles : 1;
            if (data.levelTiles > 0) LevelManager.Instance.levelEndX = data.levelTiles;
        }
    }



    /// <summary>
    /// Renders a signal diagram onto a tilemap using rising/falling/flat tiles.
    /// </summary>
    private void GenerateDiagram(Tilemap targetMap, bool[] signal, int yRow)
    {
        GenerateDiagram(targetMap, signal, yRow, startX);
    }

    private void GenerateDiagram(Tilemap targetMap, bool[] signal, int yRow, int baseX)
    {
        if (targetMap == null || signal == null) return;
        // Extend one cell to the left replicating only the signal value (flat), not the transition style
        if (signal.Length > 0)
        {
            TileBase leftTile = signal[0] ? tile_1_Standard : tile_0_Standard;
            if (leftTile != null)
                targetMap.SetTile(new Vector3Int(baseX - 1, yRow, 0), leftTile);
        }

        for (int i = 0; i < signal.Length; i++)
        {
            TileBase tileToPlace = ResolveTileForIndex(signal, i);
            if (tileToPlace != null)
                targetMap.SetTile(new Vector3Int(baseX + i, yRow, 0), tileToPlace);
        }
    }

    /// <summary>
    /// Determines which tile should be placed at a given index considering transitions.
    /// </summary>
    private TileBase ResolveTileForIndex(bool[] signal, int i)
    {
        bool current = signal[i];
        bool prev = (i > 0) ? signal[i - 1] : current;
        bool next = (i < signal.Length - 1) ? signal[i + 1] : current;

        if (current)
        {
            if (!prev && next) return tile_1_Rising;          // 0 -> 1 first 1
            if (prev && !next) return tile_1_Falling;         // 1 -> 0 last 1
            if (!prev && !next) return tile_1_RisingFalling;  // isolated 1
            return tile_1_Standard;                           // steady 1
        }
        else
        {
            if (prev && !next) return tile_0_Falling;         // 1 -> 0 first 0
            if (!prev && next) return tile_0_Rising;          // 0 -> 1 last 0
            if (prev && next) return tile_0_FallingRising;    // isolated 0
            return tile_0_Standard;                           // steady 0
        }
    }

    /// <summary>
    /// Renders a 0/1 band onto a tilemap at the given Y row.
    /// </summary>
    private void GenerateBand(Tilemap targetMap, bool[] band, int yRow, TileBase tile, bool flipY = false)
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
    private bool ValidateReferences()
    {
        if (inputTilemap == null || terrainTilemap == null || clockTilemap == null)
        {
            Debug.LogError("LevelJsonLoader: One or more required Tilemap references are missing (input/terrain/clock).");
            return false;
        }
        if (tile_1_Standard == null || tile_1_Rising == null || tile_1_Falling == null || tile_1_RisingFalling == null
            || tile_0_Standard == null || tile_0_Rising == null || tile_0_Falling == null || tile_0_FallingRising == null)
        {
            Debug.LogError("LevelJsonLoader: One or more diagram tiles are not assigned.");
            return false;
        }
        if (floorTile == null || ceilingTile == null)
        {
            Debug.LogError("LevelJsonLoader: Floor or ceiling tile is not assigned.");
            return false;
        }
        if (levelFile == null)
        {
            Debug.LogError("LevelJsonLoader: Level JSON asset is not set.");
            return false;
        }
        return true;
    }

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
    /// Returns the first non-empty string between two options.
    /// </summary>
    private string FirstNonEmpty(string a, string b)
    {
        if (!string.IsNullOrEmpty(a)) return a;
        if (!string.IsNullOrEmpty(b)) return b;
        return null;
    }

    /// <summary>
    /// Spawns obstacle prefabs based on JSON data at cell-aligned positions.
    /// </summary>
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
}
