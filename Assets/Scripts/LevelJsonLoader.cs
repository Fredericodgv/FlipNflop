using System;
using System.Collections.Generic;
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
    [Tooltip(".json contendo jSignalTiles, kSignalTiles, floorTiles, ceilingTiles, levelTiles e clockTiles")]
    [SerializeField] private TextAsset levelFile;

    [Header("Tiles - Diagrams")]
    [SerializeField] private TileBase tile_0_Normal;
    [SerializeField] private TileBase tile_1_Rising;
    [SerializeField] private TileBase tile_2_Falling;
    [SerializeField] private TileBase tile_3_High;

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
        public string jSignalTiles;
        public string kSignalTiles;
        public string floorTiles;
        public string ceilingTiles;
        public List<ObstacleData> obstacles;
    }

    [Serializable]
    private class ObstacleData
    {
        public string type;
        public int startTileX;
        public int startTileY;
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

        var jSignal = ParseInputString(data.jSignalTiles);
        var kSignal = ParseInputString(data.kSignalTiles);
        var floorBand = ParseInputString(data.floorTiles);
        var ceilingBand = ParseInputString(data.ceilingTiles);

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
            Debug.LogError("LevelJsonLoader: JSON asset vazio ou ausente.");
            return null;
        }
        try
        {
            return JsonUtility.FromJson<LevelData>(jsonAsset.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"LevelJsonLoader: Erro ao ler JSON: {ex.Message}");
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
            TileBase tileToPlace;
            bool current = signal[i];
            if (current)
            {
                tileToPlace = tile_3_High;
            }
            else
            {
                bool afterHigh = (i > 0 && signal[i - 1]);
                bool beforeHigh = (i < signal.Length - 1 && signal[i + 1]);
                if (beforeHigh) tileToPlace = tile_1_Rising;
                else if (afterHigh) tileToPlace = tile_2_Falling;
                else tileToPlace = tile_0_Normal;
            }
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
            Debug.LogError("LevelJsonLoader: Tilemaps ausentes.");
            return false;
        }
        if (tile_0_Normal == null || tile_1_Rising == null || tile_2_Falling == null || tile_3_High == null)
        {
            Debug.LogError("LevelJsonLoader: Tiles de diagrama ausentes.");
            return false;
        }
        if (floorTile == null || ceilingTile == null)
        {
            Debug.LogError("LevelJsonLoader: Tiles de terreno ausentes.");
            return false;
        }
        if (levelFile == null)
        {
            Debug.LogError("LevelJsonLoader: JSON de nível não definido.");
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
                Debug.LogWarning($"LevelJsonLoader: Nenhum prefab mapeado para obstacle type='{o.type}'.");
                continue;
            }

            int cellX = startX + o.startTileX;
            int cellY = obstacleYRelativeToFloor ? (floorYRow + o.startTileY) : o.startTileY;
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
            mace.ApplyObstacleData(o.startTileX, o.startTileY, speedUnits, horizUnits, vertUnits, o.starterCorner, o.clockwise, startX, floorYRow, obstacleYRelativeToFloor, floorTilemap);
            return;
        }

        // Fallback: attach ObstacleConfigData for other obstacle types
        var cfg = go.AddComponent<ObstacleConfigData>();
        var cs = floorTilemap != null && floorTilemap.layoutGrid != null ? floorTilemap.layoutGrid.cellSize : Vector3.one;
        cfg.speedTilesPerSec = o.speed;
        cfg.horizontalTiles = o.horizontalDistance;
        cfg.verticalTiles = o.verticalDistance;
        cfg.speedUnitsPerSec = o.speed * Mathf.Abs(cs.x);
        cfg.horizontalUnits = o.horizontalDistance * Mathf.Abs(cs.x);
        cfg.verticalUnits = o.verticalDistance * Mathf.Abs(cs.y);
        cfg.starterCorner = o.starterCorner;
        cfg.clockwise = o.clockwise;
    }
}
