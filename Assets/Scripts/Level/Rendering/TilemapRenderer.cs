using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles all tilemap rendering operations for diagrams, terrain, and clock patterns.
/// </summary>
public class TilemapRenderer
{
    private readonly Tilemap inputTilemap;
    private readonly Tilemap terrainTilemap;
    private readonly Tilemap clockTilemap;
    private readonly TileBase[] diagramTiles;
    private readonly TileBase floorTile;
    private readonly TileBase ceilingTile;
    private readonly TileBase wallTile;
    private readonly bool flipCeilingY;
    private readonly int startX;

    public TilemapRenderer(
        Tilemap inputTilemap,
        Tilemap terrainTilemap,
        Tilemap clockTilemap,
        TileBase[] diagramTiles,
        TileBase floorTile,
        TileBase ceilingTile,
        TileBase wallTile,
        bool flipCeilingY,
        int startX)
    {
        this.inputTilemap = inputTilemap;
        this.terrainTilemap = terrainTilemap;
        this.clockTilemap = clockTilemap;
        this.diagramTiles = diagramTiles;
        this.floorTile = floorTile;
        this.ceilingTile = ceilingTile;
        this.wallTile = wallTile;
        this.flipCeilingY = flipCeilingY;
        this.startX = startX;
    }

    /// <summary>
    /// Clears all configured tilemaps.
    /// </summary>
    public void ClearAllTilemaps()
    {
        inputTilemap?.ClearAllTiles();
        clockTilemap?.ClearAllTiles();
        terrainTilemap?.ClearAllTiles();
    }

    /// <summary>
    /// Renders a signal diagram on the input tilemap.
    /// </summary>
    public void RenderDiagram(bool[] signal, int yRow, Color color)
    {
        if (inputTilemap == null || signal == null) return;

        var pattern = new int[signal.Length];
        for (int i = 0; i < signal.Length; i++)
        {
            pattern[i] = signal[i] ? 1 : 0;
        }

        DrawPattern(inputTilemap, pattern, yRow, startX);
        ColorRow(inputTilemap, pattern.Length, yRow, startX, color);
    }

    /// <summary>
    /// Renders the clock pattern on the clock tilemap.
    /// </summary>
    public void RenderClock(int[] clockPattern, int yRow, Color clockColor)
    {
        if (clockTilemap == null || clockPattern == null) return;

        DrawPattern(clockTilemap, clockPattern, yRow, startX);
        ColorRow(clockTilemap, clockPattern.Length, yRow, startX, clockColor);
    }

    /// <summary>
    /// Renders floor and ceiling bands on the terrain tilemap.
    /// </summary>
    public void RenderTerrain(bool[] floorBand, bool[] ceilingBand, int floorYRow, int ceilingYRow, int extendRight = 3)
    {
        if (terrainTilemap == null) return;

        if (floorBand != null && floorTile != null)
        {
            RenderBand(floorBand, floorYRow, floorTile, false, extendRight);
        }

        if (ceilingBand != null && ceilingTile != null)
        {
            RenderBand(ceilingBand, ceilingYRow, ceilingTile, flipCeilingY, extendRight);
        }
    }

    /// <summary>
    /// Completes static scenery (left wall and edge tiles).
    /// </summary>
    public void CompleteStaticScenery(int floorYRow, int ceilingYRow)
    {
        if (terrainTilemap == null) return;

        // Render left wall
        if (wallTile != null)
        {
            int yMin = Mathf.Min(floorYRow, ceilingYRow);
            int yMax = Mathf.Max(floorYRow, ceilingYRow);
            if (yMin <= yMax)
            {
                int xWall = startX - 2;
                for (int y = yMin; y <= yMax; y++)
                {
                    SetTileWithFlip(terrainTilemap, new Vector3Int(xWall, y, 0), wallTile, false);
                }
            }
        }

        // Render edge floor tile
        if (floorTile != null)
        {
            int xFloor = startX - 1;
            SetTileWithFlip(terrainTilemap, new Vector3Int(xFloor, floorYRow, 0), floorTile, false);
        }

        // Render edge ceiling tile
        if (ceilingTile != null)
        {
            int xCeil = startX - 1;
            var pos = new Vector3Int(xCeil, ceilingYRow, 0);
            SetTileWithFlip(terrainTilemap, pos, ceilingTile, flipCeilingY);
        }
    }

    #region Private Rendering Methods

    /// <summary>
    /// Renders a 0/1 band at the given row with optional extension beyond the last '1'.
    /// </summary>
    private void RenderBand(bool[] band, int yRow, TileBase tile, bool flipY, int extendRight)
    {
        if (band == null || tile == null) return;

        // Render the band from the signal array
        for (int i = 0; i < band.Length; i++)
        {
            if (!band[i]) continue;
            var pos = new Vector3Int(startX + i, yRow, 0);
            SetTileWithFlip(terrainTilemap, pos, tile, flipY);
        }

        // Extend band a few tiles after the last '1'
        if (extendRight > 0)
        {
            int lastIdx = -1;
            for (int i = band.Length - 1; i >= 0; i--)
            {
                if (band[i])
                {
                    lastIdx = i;
                    break;
                }
            }

            if (lastIdx >= 0)
            {
                for (int off = 1; off <= extendRight; off++)
                {
                    var posExt = new Vector3Int(startX + lastIdx + off, yRow, 0);
                    SetTileWithFlip(terrainTilemap, posExt, tile, flipY);
                }
            }
        }
    }

    /// <summary>
    /// Generalized pattern renderer for both inputs and clock.
    /// Pattern codes: 0 = low, 1 = high.
    /// </summary>
    private void DrawPattern(Tilemap map, int[] pattern, int yRow, int xStart)
    {
        if (map == null || pattern == null || pattern.Length == 0) return;

        for (int i = 0; i < pattern.Length; i++)
        {
            int curr = pattern[i];
            int prev = (i > 0) ? pattern[i - 1] : curr;
            int next = (i < pattern.Length - 1) ? pattern[i + 1] : curr;
            int idx = ((prev != 0 ? 1 : 0) << 2) | ((curr != 0 ? 1 : 0) << 1) | (next != 0 ? 1 : 0);
            var tile = SafeTile(idx);
            if (tile != null)
            {
                map.SetTile(new Vector3Int(xStart + i, yRow, 0), tile);
            }
        }
    }

    /// <summary>
    /// Sets a tile at the specified position with optional Y-axis flip.
    /// </summary>
    private void SetTileWithFlip(Tilemap targetMap, Vector3Int position, TileBase tile, bool flipY)
    {
        if (targetMap == null || tile == null) return;

        targetMap.SetTile(position, tile);
        if (flipY)
        {
            targetMap.SetTileFlags(position, TileFlags.None);
            targetMap.SetTransformMatrix(position, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f)));
        }
    }

    /// <summary>
    /// Applies color to a row of tiles.
    /// </summary>
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

    /// <summary>
    /// Safely retrieves a tile from the diagram tiles array.
    /// </summary>
    private TileBase SafeTile(int idx)
    {
        if (diagramTiles == null || idx < 0 || idx >= diagramTiles.Length)
        {
            return null;
        }
        return diagramTiles[idx];
    }

    #endregion
}
