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
    [Header("Tilemaps - Signals")]
    [SerializeField] private Tilemap jInputTilemap;
    [SerializeField] private Tilemap kInputTilemap;

    [Header("Tilemaps - Terrain")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap ceilingTilemap;

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
        public string floor;
        public string ceiling;
        public List<ObstacleData> obstacles;
    }

    // Exposed parsed signals so other systems (like PathVerifier) can compute the expected outputs
    public bool[] ParsedJSignal { get; private set; }
    public bool[] ParsedKSignal { get; private set; }

    /// <summary>
    /// Compute the sequence of JK outputs sampled on the clock. This uses the parsed J/K arrays
    /// and samples them at indices (clockStep-1), (2*clockStep-1), ... until one of the arrays ends.
    /// The JK behavior follows: J=1,K=0 => Q=1; J=0,K=1 => Q=0; J=1,K=1 => toggle; J=0,K=0 => hold.
    /// Returns null if inputs are invalid.
    /// </summary>
    public bool[] ComputeOutputSamplesFromParsedSignals()
    {
        var samples = BuildOutputSequenceFromSignals(this.ParsedJSignal, this.ParsedKSignal,
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

        // start at index 0 so the first J/K value is considered
        for (int idx = 0; idx < maxIndex; idx += clockStep)
        {
            bool j = jSignal[idx];
            bool k = kSignal[idx];
            if (j && !k) qState = true;
            else if (!j && k) qState = false;
            else if (j && k) qState = !qState;
            // j==0 && k==0 => hold (qState unchanged)
            outputs.Add(qState);
        }

        var outArr = outputs.Count > 0 ? outputs.ToArray() : null;
        Debug.Log($"LevelJsonLoader: BuildOutputSequenceFromSignals(clockStep={clockStep}, jLen={(jSignal == null ? 0 : jSignal.Length)}, kLen={(kSignal == null ? 0 : kSignal.Length)}) -> outputs len={(outArr == null ? 0 : outArr.Length)} value={BoolArrayToString(outArr)}");
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
            float x = (i + 1) * step;
            events.Add(new PathVerifier.SignalEvent(x, samples[i]));
        }
        Debug.Log($"LevelJsonLoader: ComputeOutputEventsFromParsedSignals -> {EventsToString(events)}");
        return events;
    }

    // debug helpers
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
        // store parsed signals for external consumers (e.g. PathVerifier)
        this.ParsedJSignal = jSignal;
        this.ParsedKSignal = kSignal;
        var floorBand = ParseInputString(data.floor);
        var ceilingBand = ParseInputString(data.ceiling);

        ClearAllTilemaps();
        GenerateDiagram(jInputTilemap, jSignal, j_YRow);
        GenerateDiagram(kInputTilemap, kSignal, k_YRow);
        GenerateBand(floorTilemap, floorBand, floorYRow, floorTile, false);
        GenerateBand(ceilingTilemap, ceilingBand, ceilingYRow, ceilingTile, flipCeilingY);

        CompleteStaticScenery();

        if (data.obstacles != null && data.obstacles.Count > 0)
        {
            SpawnObstacles(data.obstacles);
        }
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
            if (data.clockTiles > 0) LevelManager.Instance.clockStepX = data.clockTiles;
            if (data.levelTiles > 0) LevelManager.Instance.levelEndX = data.levelTiles;
        }
    }



    /// <summary>
    /// Renders a signal diagram onto a tilemap using rising/falling/flat tiles.
    /// </summary>
    private void GenerateDiagram(Tilemap targetMap, bool[] signal, int yRow)
    {
        if (targetMap == null || signal == null) return;
        for (int i = 0; i < signal.Length; i++)
        {
            TileBase tileToPlace = null;
            bool current = signal[i];
            bool prev = (i > 0) ? signal[i - 1] : current;
            bool next = (i < signal.Length - 1) ? signal[i + 1] : current;

            if (current)
            {
                // Current is 1
                if (!prev && next)
                {
                    // 0 -> 1 (rising), this is the first 1
                    tileToPlace = tile_1_Rising;
                }
                else if (prev && !next)
                {
                    // 1 -> 0 (falling), this is the last 1
                    tileToPlace = tile_1_Falling;
                }
                else if (!prev && !next)
                {
                    // isolated single 1 (0->1->0)
                    tileToPlace = tile_1_RisingFalling;
                }
                else
                {
                    // steady 1
                    tileToPlace = tile_1_Standard;
                }
            }
            else
            {
                // Current is 0
                if (prev && !next)
                {
                    // 1 -> 0 (falling), first 0 after transition
                    tileToPlace = tile_0_Falling;
                }
                else if (!prev && next)
                {
                    // 0 -> 1 (rising), last 0 before transition
                    tileToPlace = tile_0_Rising;
                }
                else if (prev && next)
                {
                    // isolated single 0 (1->0->1)
                    tileToPlace = tile_0_FallingRising;
                }
                else
                {
                    // steady 0
                    tileToPlace = tile_0_Standard;
                }
            }

            if (tileToPlace != null)
                targetMap.SetTile(new Vector3Int(startX + i, yRow, 0), tileToPlace);
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
        if (floorTilemap != null && wallTile != null)
        {
            int yMin = Mathf.Min(floorYRow, ceilingYRow);
            int yMax = Mathf.Max(floorYRow, ceilingYRow);
            if (yMin <= yMax)
            {
                int xWall = startX - 2;
                for (int y = yMin; y <= yMax; y++)
                {
                    floorTilemap.SetTile(new Vector3Int(xWall, y, 0), wallTile);
                }
            }
        }

        if (floorTilemap != null && floorTile != null)
        {
            int xFloor = startX - 1;
            floorTilemap.SetTile(new Vector3Int(xFloor, floorYRow, 0), floorTile);
        }

        if (ceilingTilemap != null && ceilingTile != null)
        {
            int xCeil = startX - 1;
            var pos = new Vector3Int(xCeil, ceilingYRow, 0);
            ceilingTilemap.SetTile(pos, ceilingTile);
            if (flipCeilingY)
            {
                ceilingTilemap.SetTileFlags(pos, TileFlags.None);
                ceilingTilemap.SetTransformMatrix(pos, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f)));
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
        if (jInputTilemap == null || kInputTilemap == null || floorTilemap == null || ceilingTilemap == null)
        {
            Debug.LogError("LevelJsonLoader: One or more Tilemap references are missing.");
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
        jInputTilemap.ClearAllTiles();
        kInputTilemap.ClearAllTiles();
        floorTilemap.ClearAllTiles();
        ceilingTilemap.ClearAllTiles();
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
            var worldPos = floorTilemap.GetCellCenterWorld(cell);

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
        // Prefer to configure known obstacle controllers directly (avoid adding extra components)
        var mace = go.GetComponent<MaceController>();
        if (mace != null)
        {
            // compute cell size for unit conversion
            var cellSize = floorTilemap != null && floorTilemap.layoutGrid != null
                ? floorTilemap.layoutGrid.cellSize
                : Vector3.one;
            float speedUnits = o.speed * Mathf.Abs(cellSize.x);
            float horizUnits = o.horizontalDistance * Mathf.Abs(cellSize.x);
            float vertUnits = o.verticalDistance * Mathf.Abs(cellSize.y);
            mace.ApplyObstacleData(o.startX, o.startY, speedUnits, horizUnits, vertUnits, o.starterCorner, o.clockwise, startX, floorYRow, obstacleYRelativeToFloor, floorTilemap);
            return;
        }
    }
}
