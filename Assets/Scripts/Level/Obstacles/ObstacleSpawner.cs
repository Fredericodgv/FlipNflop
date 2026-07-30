using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles spawning and configuration of obstacles from level data.
/// </summary>
public class ObstacleSpawner
{
    private readonly Transform obstaclesParent;
    private readonly List<ObstaclePrefabEntry> obstaclePrefabs;
    private readonly Tilemap terrainTilemap;
    private readonly int startX;
    private readonly int floorYRow;
    private readonly bool obstacleYRelativeToFloor;

    public ObstacleSpawner(
        Transform obstaclesParent,
        List<ObstaclePrefabEntry> obstaclePrefabs,
        Tilemap terrainTilemap,
        int startX,
        int floorYRow,
        bool obstacleYRelativeToFloor)
    {
        this.obstaclesParent = obstaclesParent;
        this.obstaclePrefabs = obstaclePrefabs;
        this.terrainTilemap = terrainTilemap;
        this.startX = startX;
        this.floorYRow = floorYRow;
        this.obstacleYRelativeToFloor = obstacleYRelativeToFloor;
    }

    /// <summary>
    /// Spawns all obstacles from the provided obstacle data list.
    /// </summary>
    public void SpawnObstacles(List<ObstacleData> obstacles)
    {
        if (obstacles == null || obstacles.Count == 0) return;

        foreach (var obstacle in obstacles)
        {
            SpawnSingleObstacle(obstacle);
        }
    }

    /// <summary>
    /// Spawns a single obstacle at the specified position.
    /// </summary>
    private void SpawnSingleObstacle(ObstacleData obstacle)
    {
        var prefab = ResolveObstaclePrefab(obstacle.obstacleName);
        if (prefab == null)
        {
            Debug.LogWarning($"ObstacleSpawner: No prefab mapped for obstacle type='{obstacle.obstacleName}'.");
            return;
        }

        int cellX = startX + obstacle.startX;
        int cellY = obstacleYRelativeToFloor ? (floorYRow + obstacle.startY) : obstacle.startY;
        var cell = new Vector3Int(cellX, cellY, 0);
        var worldPos = terrainTilemap.GetCellCenterWorld(cell);

        var instance = UnityEngine.Object.Instantiate(prefab, worldPos, Quaternion.identity, obstaclesParent);
        AttachObstacleConfig(instance, obstacle);
    }

    /// <summary>
    /// Resolves a prefab by obstacle type string.
    /// </summary>
    private GameObject ResolveObstaclePrefab(string type)
    {
        if (string.IsNullOrEmpty(type)) return null;

        foreach (var entry in obstaclePrefabs)
        {
            if (entry != null && entry.prefab != null && entry.obstacleName == type)
            {
                return entry.prefab;
            }
        }

        return null;
    }

    /// <summary>
    /// Applies obstacle configuration data to the spawned instance.
    /// </summary>
    private void AttachObstacleConfig(GameObject instance, ObstacleData data)
    {
        var mace = instance.GetComponent<MaceController>();
        if (mace != null)
        {
            var cellSize = terrainTilemap != null && terrainTilemap.layoutGrid != null
                ? terrainTilemap.layoutGrid.cellSize
                : Vector3.one;

            float speedUnits = data.speed * Mathf.Abs(cellSize.x);
            float horizUnits = data.horizontalDistance * Mathf.Abs(cellSize.x);
            float vertUnits = data.verticalDistance * Mathf.Abs(cellSize.y);

            mace.ApplyObstacleData(
                data.startX,
                data.startY,
                speedUnits,
                horizUnits,
                vertUnits,
                data.starterCorner,
                data.clockwise,
                startX,
                floorYRow,
                obstacleYRelativeToFloor,
                terrainTilemap);
        }
    }

    #region Data Classes

    /// <summary>
    /// Obstacle data structure matching JSON format.
    /// </summary>
    [Serializable]
    public class ObstacleData
    {
        public string obstacleName;
        public int startX;
        public int startY;
        public float speed;
        public int horizontalDistance;
        public int verticalDistance;
        public string starterCorner;
        public bool clockwise;
    }

    /// <summary>
    /// Maps obstacle type string to prefab GameObject.
    /// </summary>
    [Serializable]
    public class ObstaclePrefabEntry
    {
        public string obstacleName;
        public GameObject prefab;
    }

    #endregion
}
