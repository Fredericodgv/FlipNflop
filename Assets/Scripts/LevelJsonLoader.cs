using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Loads level configuration from JSON (signals, floor/ceiling, length and step),
/// applies LevelManager settings, and renders input diagrams and terrain bands.
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
    [Tooltip(".json contendo j/k signals, chao, teto, levelEndX e clockStepX")]
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

    /// <summary>
    /// JSON data model for level configuration.
    /// </summary>
    [Serializable]
    private class LevelData
    {
        public string j_signal;
        public string J_signal;
        public string k_signal;
        public string K_signal;
        public string chao;
        public string teto;
        public float levelEndX;
        public float clockStepX;
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

        var jSignal = ParseInputString(FirstNonEmpty(data.J_signal, data.j_signal));
        var kSignal = ParseInputString(FirstNonEmpty(data.K_signal, data.k_signal));
        var floorBand = ParseInputString(data.chao);
        var ceilingBand = ParseInputString(data.teto);

        ClearAllTilemaps();
        GenerateDiagram(jInputTilemap, jSignal, j_YRow);
        GenerateDiagram(kInputTilemap, kSignal, k_YRow);
        GenerateBand(floorTilemap, floorBand, floorYRow, floorTile, false);
        GenerateBand(ceilingTilemap, ceilingBand, ceilingYRow, ceilingTile, flipCeilingY);

        CompleteStaticScenery();
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
    /// Applies levelEndX and clockStepX to LevelManager if available.
    /// </summary>
    private void ApplyLevelConfig(LevelData data)
    {
        if (LevelManager.Instance != null)
        {
            if (data.clockStepX > 0f) LevelManager.Instance.clockStepX = data.clockStepX;
            if (data.levelEndX > 0f) LevelManager.Instance.levelEndX = data.levelEndX;
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
}
