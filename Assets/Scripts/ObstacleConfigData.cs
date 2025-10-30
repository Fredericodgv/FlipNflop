using UnityEngine;

/// <summary>
/// Simple data container attached to spawned obstacles so their controllers can read JSON-provided parameters
/// in both tile units and world units.
/// </summary>
public class ObstacleConfigData : MonoBehaviour
{
    [Header("Movement - Tiles")]
    [Tooltip("Speed expressed in tiles per second (as provided by JSON)")]
    public float speedTilesPerSec = 0f;
    [Tooltip("Horizontal travel expressed in tiles (as provided by JSON)")]
    public int horizontalTiles = 0;
    [Tooltip("Vertical travel expressed in tiles (as provided by JSON)")]
    public int verticalTiles = 0;

    [Header("Movement - World Units")]
    [Tooltip("Speed converted to world units per second")]
    public float speedUnitsPerSec = 0f;
    [Tooltip("Horizontal travel converted to world units")]
    public float horizontalUnits = 0f;
    [Tooltip("Vertical travel converted to world units")]
    public float verticalUnits = 0f;

    [Header("Path/Pattern")]
    public string starterCorner = "bottom-left";
    public bool clockwise = true;
}
