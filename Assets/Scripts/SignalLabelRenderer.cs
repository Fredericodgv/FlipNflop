using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Positions HUD label images at the exact Y positions of signal lines (J, K, Preset, Clear, Clock).
/// Coordinates with LevelJsonLoader to determine dynamic Y positions based on async signal presence.
/// </summary>
public class SignalLabelRenderer : MonoBehaviour
{
    [Header("Label Sprites")]
    [Tooltip("Sprite for J signal label")]
    [SerializeField] private Sprite jLabelSprite;
    [Tooltip("Sprite for K signal label")]
    [SerializeField] private Sprite kLabelSprite;
    [Tooltip("Sprite for Preset signal label (optional, shown only if preset exists)")]
    [SerializeField] private Sprite presetLabelSprite;
    [Tooltip("Sprite for Clear signal label (optional, shown only if clear exists)")]
    [SerializeField] private Sprite clearLabelSprite;
    [Tooltip("Sprite for Clock signal label")]
    [SerializeField] private Sprite clockLabelSprite;

    [Header("Position Settings")]
    [Tooltip("X offset from camera position (negative = left of camera center)")]
    [SerializeField] private float labelXOffset = -8f;
    [Tooltip("Z position for labels (typically 0 or slight offset for sorting)")]
    [SerializeField] private float labelZ = 0f;
    [Tooltip("Scale multiplier for label sprites")]
    [SerializeField] private float labelScale = 1f;

    [Header("References")]
    [Tooltip("Reference to LevelJsonLoader to check for async signal presence")]
    [SerializeField] private LevelJsonLoader levelJsonLoader;
    [Tooltip("Reference to input tilemap to convert tilemap Y coords to world coords")]
    [SerializeField] private UnityEngine.Tilemaps.Tilemap inputTilemap;
    [Tooltip("Reference to main camera (if null, will use Camera.main)")]
    [SerializeField] private Camera mainCamera;

    [Header("Parent Transform (Optional)")]
    [Tooltip("Parent transform for spawned labels. If null, labels are parented to this object.")]
    [SerializeField] private Transform labelsParent;

    private GameObject jLabel;
    private GameObject kLabel;
    private GameObject presetLabel;
    private GameObject clearLabel;
    private GameObject clockLabel;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        // Update label X positions to follow camera
        UpdateLabelXPositions();
    }

    /// <summary>
    /// Updates the X position of all labels to follow the camera.
    /// </summary>
    private void UpdateLabelXPositions()
    {
        if (mainCamera == null) return;

        float cameraX = mainCamera.transform.position.x;
        float targetX = cameraX + labelXOffset;

        if (jLabel != null)
        {
            Vector3 pos = jLabel.transform.position;
            pos.x = targetX;
            jLabel.transform.position = pos;
        }

        if (kLabel != null)
        {
            Vector3 pos = kLabel.transform.position;
            pos.x = targetX;
            kLabel.transform.position = pos;
        }

        if (presetLabel != null)
        {
            Vector3 pos = presetLabel.transform.position;
            pos.x = targetX;
            presetLabel.transform.position = pos;
        }

        if (clearLabel != null)
        {
            Vector3 pos = clearLabel.transform.position;
            pos.x = targetX;
            clearLabel.transform.position = pos;
        }

        if (clockLabel != null)
        {
            Vector3 pos = clockLabel.transform.position;
            pos.x = targetX;
            clockLabel.transform.position = pos;
        }
    }

    /// <summary>
    /// Generates and positions all signal labels after LevelJsonLoader has parsed signals.
    /// Call this from LevelJsonLoader.Awake() after signals are parsed.
    /// </summary>
    public void GenerateLabels()
    {
        ClearExistingLabels();

        if (levelJsonLoader == null)
        {
            Debug.LogWarning("SignalLabelRenderer: LevelJsonLoader reference is missing. Cannot determine signal positions.");
            return;
        }

        if (inputTilemap == null)
        {
            Debug.LogWarning("SignalLabelRenderer: Input Tilemap reference is missing. Cannot convert tilemap coords to world coords.");
            return;
        }

        // Determine Y positions using same logic as LevelJsonLoader
        bool hasAsync = (levelJsonLoader.ParsedPresetSignal != null || levelJsonLoader.ParsedClearSignal != null);
        int j_Y = 12;
        int k_Y = hasAsync ? 10 : 8;
        int preset_Y = 8;
        int clear_Y = 6;
        int clock_Y = 4;

        Transform parent = labelsParent != null ? labelsParent : transform;

        // Always create J, K, Clock labels
        jLabel = CreateLabel("J_Label", jLabelSprite, j_Y, parent);
        kLabel = CreateLabel("K_Label", kLabelSprite, k_Y, parent);
        clockLabel = CreateLabel("Clock_Label", clockLabelSprite, clock_Y, parent);

        // Conditionally create Preset and Clear labels
        if (levelJsonLoader.ParsedPresetSignal != null)
        {
            presetLabel = CreateLabel("Preset_Label", presetLabelSprite, preset_Y, parent);
        }

        if (levelJsonLoader.ParsedClearSignal != null)
        {
            clearLabel = CreateLabel("Clear_Label", clearLabelSprite, clear_Y, parent);
        }
    }

    /// <summary>
    /// Creates a single label GameObject with SpriteRenderer at the specified Y position.
    /// Converts tilemap Y coordinate to world position.
    /// </summary>
    private GameObject CreateLabel(string name, Sprite sprite, int tilemapY, Transform parent)
    {
        if (sprite == null)
        {
            Debug.LogWarning($"SignalLabelRenderer: Sprite for {name} is not assigned. Skipping.");
            return null;
        }

        GameObject labelObj = new GameObject(name);
        labelObj.transform.SetParent(parent);

        // Convert tilemap Y to world Y (using tilemap cell to world conversion)
        Vector3 worldPos = inputTilemap.CellToWorld(new Vector3Int(0, tilemapY, 0));
        // Add half cell size to center the label vertically in the tile
        worldPos.y += inputTilemap.cellSize.y / 2f;

        // X position will be updated in LateUpdate to follow camera
        if (mainCamera != null)
        {
            worldPos.x = mainCamera.transform.position.x + labelXOffset;
        }
        else
        {
            worldPos.x = labelXOffset; // Fallback if camera not set yet
        }

        worldPos.z = labelZ;

        labelObj.transform.position = worldPos;
        labelObj.transform.localScale = Vector3.one * labelScale;

        SpriteRenderer sr = labelObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10; // Ensure labels render on top

        return labelObj;
    }

    /// <summary>
    /// Destroys all existing label GameObjects.
    /// </summary>
    private void ClearExistingLabels()
    {
        if (jLabel != null) Destroy(jLabel);
        if (kLabel != null) Destroy(kLabel);
        if (presetLabel != null) Destroy(presetLabel);
        if (clearLabel != null) Destroy(clearLabel);
        if (clockLabel != null) Destroy(clockLabel);

        jLabel = null;
        kLabel = null;
        presetLabel = null;
        clearLabel = null;
        clockLabel = null;
    }

    /// <summary>
    /// Public method to update label positions if signals change dynamically.
    /// </summary>
    public void UpdateLabelPositions()
    {
        GenerateLabels();
    }

    private void OnDestroy()
    {
        ClearExistingLabels();
    }
}
