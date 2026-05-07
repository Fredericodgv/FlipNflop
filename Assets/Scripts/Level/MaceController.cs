using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Moves the GameObject along a rectangular perimeter.
/// The rectangle is defined by <see cref="horizontalDistance"/> and <see cref="verticalDistance"/>.
/// The object starts at one corner (<see cref="startCorner"/>) and follows the perimeter in the
/// selected turning direction.
/// </summary>
public class MaceController : MonoBehaviour
{
    public enum Direction { Up, Right, Down, Left }
    public enum Corner { BottomLeft, BottomRight, TopRight, TopLeft }
    public enum TurningDirection { Clockwise, CounterClockwise }

    [Header("Rectangular Movement")]
    [Tooltip("Movement speed (world units per second).")]
    public float speed = 2.0f;

    [Tooltip("Total horizontal distance of the perimeter (world units). May be zero.")]
    public float horizontalDistance = 5.0f;

    [Tooltip("Total vertical distance of the perimeter (world units). May be zero.")]
    public float verticalDistance = 3.0f;

    [Tooltip("Corner where the object starts. The object's transform should be placed at that corner.")]
    public Corner startCorner = Corner.BottomLeft;

    [Tooltip("Direction of traversal around the rectangle.")]
    public TurningDirection turning = TurningDirection.Clockwise;

    [Tooltip("Tolerance used to detect arrival at a corner (in world units).")]
    public float cornerEpsilon = 0.001f;

    private Vector3 startPos;
    private Direction currentDirection;
    private float minX, maxX, minY, maxY;
    private Direction[] dirCycle;
    private int dirIndex;

    void Start()
    {
        // Initialize state from current transform position. If ApplyObstacleData was called
        // before Start, startPos will already have been set accordingly.
        startPos = transform.position;

        Direction startDir = GetStartDirection(startCorner, turning);
        dirCycle = BuildCycle(startDir, turning);
        dirIndex = 0;
        currentDirection = dirCycle[dirIndex];

        float dx = Mathf.Max(0f, horizontalDistance);
        float dy = Mathf.Max(0f, verticalDistance);

        // Compute bottom-left from the chosen corner and dimensions
        Vector3 bl = CornerToBottomLeft(startPos, dx, dy, startCorner);
        minX = bl.x; minY = bl.y; maxX = bl.x + dx; maxY = bl.y + dy;

        // Ensure transform is placed exactly at the configured start corner
        transform.position = startPos;
    }

    void Update()
    {
        // Nothing to do if there is no path or speed is zero
        if ((horizontalDistance <= 0f && verticalDistance <= 0f) || speed <= 0f)
            return;

        Vector3 target = GetCurrentTarget();
        Vector3 pos = transform.position;
        float step = speed * Time.deltaTime;

        // Move along the active axis toward the current target corner
        switch (currentDirection)
        {
            case Direction.Up:
            case Direction.Down:
                pos.y = Mathf.MoveTowards(pos.y, target.y, step);
                break;
            case Direction.Right:
            case Direction.Left:
                pos.x = Mathf.MoveTowards(pos.x, target.x, step);
                break;
        }

        transform.position = pos;

        // Handle corner transitions; allow up to 4 immediate transitions to cover degenerate edges
        int safety = 0;
        while (safety++ < 4)
        {
            target = GetCurrentTarget();
            if (IsAtTarget(transform.position, target) || EdgeLengthFor(currentDirection) <= 0f)
            {
                AdvanceDirection();
                continue;
            }
            break;
        }
    }

    /// <summary>
    /// Configure the obstacle using values computed by the loader.
    /// startTileX/Y are in tile units; this method converts them to world position using the provided tilemap.
    /// This method is intended to be called immediately after Instantiate so the controller is ready when Start runs.
    /// </summary>
    public void ApplyObstacleData(int startTileX, int startTileY, float speedUnits, float horizUnits, float vertUnits, string starterCornerStr, bool clockwiseFlag, int globalStartX, int floorYRow, bool yRelativeToFloor, Tilemap floorTilemap)
    {
        speed = speedUnits;
        horizontalDistance = horizUnits;
        verticalDistance = vertUnits;

        startCorner = ParseCorner(starterCornerStr, startCorner);
        turning = clockwiseFlag ? TurningDirection.Clockwise : TurningDirection.CounterClockwise;

        int cellX = globalStartX + startTileX;
        int cellY = yRelativeToFloor ? (floorYRow + startTileY) : startTileY;
        var cell = new Vector3Int(cellX, cellY, 0);
        Vector3 worldPos = (floorTilemap != null) ? floorTilemap.GetCellCenterWorld(cell) : new Vector3(cellX, cellY, 0f);

        transform.position = worldPos;
        startPos = worldPos;
    }

    private Corner ParseCorner(string value, Corner fallback)
    {
        if (string.IsNullOrEmpty(value)) return fallback;
        string v = value.Trim().ToLowerInvariant();
        switch (v)
        {
            case "bottom-left":
            case "bottom_left":
            case "bl": return Corner.BottomLeft;
            case "bottom-right":
            case "bottom_right":
            case "br": return Corner.BottomRight;
            case "top-right":
            case "top_right":
            case "tr": return Corner.TopRight;
            case "top-left":
            case "top_left":
            case "tl": return Corner.TopLeft;
            default: return fallback;
        }
    }

    private Vector3 GetCurrentTarget()
    {
        switch (currentDirection)
        {
            case Direction.Up:
                return new Vector3(transform.position.x, maxY, transform.position.z);
            case Direction.Right:
                return new Vector3(maxX, transform.position.y, transform.position.z);
            case Direction.Down:
                return new Vector3(transform.position.x, minY, transform.position.z);
            case Direction.Left:
                return new Vector3(minX, transform.position.y, transform.position.z);
            default:
                return transform.position;
        }
    }

    private float EdgeLengthFor(Direction dir)
    {
        return (dir == Direction.Left || dir == Direction.Right) ? Mathf.Max(0f, horizontalDistance)
                                                                  : Mathf.Max(0f, verticalDistance);
    }

    private bool IsAtTarget(Vector3 pos, Vector3 target)
    {
        if (currentDirection == Direction.Up || currentDirection == Direction.Down)
            return Mathf.Abs(pos.y - target.y) <= cornerEpsilon;
        else
            return Mathf.Abs(pos.x - target.x) <= cornerEpsilon;
    }

    private void AdvanceDirection()
    {
        if (dirCycle == null || dirCycle.Length == 0)
        {
            Direction startDir = GetStartDirection(startCorner, turning);
            dirCycle = BuildCycle(startDir, turning);
        }
        dirIndex = (dirIndex + 1) % dirCycle.Length;
        currentDirection = dirCycle[dirIndex];
    }

    private Direction[] BuildCycle(Direction startDir, TurningDirection sense)
    {
        Direction[] baseCW = new[] { Direction.Up, Direction.Right, Direction.Down, Direction.Left };
        Direction[] baseCCW = new[] { Direction.Up, Direction.Left, Direction.Down, Direction.Right };
        var baseSeq = (sense == TurningDirection.Clockwise) ? baseCW : baseCCW;

        int idx = 0;
        for (int i = 0; i < baseSeq.Length; i++) { if (baseSeq[i] == startDir) { idx = i; break; } }
        Direction[] result = new Direction[4];
        for (int i = 0; i < 4; i++) { result[i] = baseSeq[(idx + i) % 4]; }
        return result;
    }

    private Direction GetStartDirection(Corner corner, TurningDirection sense)
    {
        if (sense == TurningDirection.Clockwise)
        {
            switch (corner)
            {
                case Corner.BottomLeft: return Direction.Up;
                case Corner.TopLeft: return Direction.Right;
                case Corner.TopRight: return Direction.Down;
                case Corner.BottomRight: return Direction.Left;
            }
        }
        else // CounterClockwise
        {
            switch (corner)
            {
                case Corner.BottomLeft: return Direction.Right;
                case Corner.BottomRight: return Direction.Up;
                case Corner.TopRight: return Direction.Left;
                case Corner.TopLeft: return Direction.Down;
            }
        }
        return Direction.Up;
    }

    private Vector3 CornerToBottomLeft(Vector3 cornerPos, float dx, float dy, Corner corner)
    {
        switch (corner)
        {
            case Corner.BottomLeft: return cornerPos;
            case Corner.BottomRight: return new Vector3(cornerPos.x - dx, cornerPos.y, cornerPos.z);
            case Corner.TopRight: return new Vector3(cornerPos.x - dx, cornerPos.y - dy, cornerPos.z);
            case Corner.TopLeft: return new Vector3(cornerPos.x, cornerPos.y - dy, cornerPos.z);
            default: return cornerPos;
        }
    }

    #region Gizmos
    [Header("Gizmos")]
    public bool drawPathGizmos = true;
    public Color gizmoColor = Color.yellow;
    public float gizmoCornerRadius = 0.07f;

    private void OnDrawGizmosSelected()
    {
        if (!drawPathGizmos) return;

        Vector3 editorStart = Application.isPlaying ? startPos : transform.position;
        float dx = Mathf.Max(0f, horizontalDistance);
        float dy = Mathf.Max(0f, verticalDistance);
        Vector3 bl = CornerToBottomLeft(editorStart, dx, dy, startCorner);

        Gizmos.color = gizmoColor;

        if (dx <= 0f && dy <= 0f)
        {
            Gizmos.DrawSphere(editorStart, gizmoCornerRadius);
            return;
        }

        Vector3 tl = new Vector3(bl.x, bl.y + dy, bl.z);
        Vector3 tr = new Vector3(bl.x + dx, bl.y + dy, bl.z);
        Vector3 br = new Vector3(bl.x + dx, bl.y, bl.z);

        if (dx > 0f && dy > 0f)
        {
            Gizmos.DrawLine(bl, tl);
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);

            Gizmos.DrawSphere(bl, gizmoCornerRadius);
            Gizmos.DrawSphere(tl, gizmoCornerRadius);
            Gizmos.DrawSphere(tr, gizmoCornerRadius);
            Gizmos.DrawSphere(br, gizmoCornerRadius);
        }
        else if (dx == 0f && dy > 0f)
        {
            Vector3 bottom = bl;
            Vector3 top = tl;
            Gizmos.DrawLine(bottom, top);
            Gizmos.DrawSphere(bottom, gizmoCornerRadius);
            Gizmos.DrawSphere(top, gizmoCornerRadius);
        }
        else if (dx > 0f && dy == 0f)
        {
            Vector3 left = tl;
            Vector3 right = tr;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawSphere(left, gizmoCornerRadius);
            Gizmos.DrawSphere(right, gizmoCornerRadius);
        }

        if (Application.isPlaying)
        {
            Vector3 dir = Vector3.zero;
            switch (currentDirection)
            {
                case Direction.Up: dir = Vector3.up; break;
                case Direction.Right: dir = Vector3.right; break;
                case Direction.Down: dir = Vector3.down; break;
                case Direction.Left: dir = Vector3.left; break;
            }
            if (dir != Vector3.zero)
            {
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.6f);
                Gizmos.DrawRay(transform.position, dir * 0.5f);
            }
        }
    }
    #endregion
}
