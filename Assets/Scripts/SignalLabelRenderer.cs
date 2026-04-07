using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Position Settings (in pixels)")]
    [Tooltip("Y position for J label from top of screen")]
    [SerializeField] private float jLabelY = 100f;
    [Tooltip("Y position for K label from top of screen")]
    [SerializeField] private float kLabelY = 200f;
    [Tooltip("Y position for Preset label from top of screen")]
    [SerializeField] private float presetLabelY = 300f;
    [Tooltip("Y position for Clear label from top of screen")]
    [SerializeField] private float clearLabelY = 400f;
    [Tooltip("Y position for Clock label from top of screen")]
    [SerializeField] private float clockLabelY = 500f;
    [Tooltip("X offset from left edge")]
    [SerializeField] private float xOffset = 10f;
    [Tooltip("Width and height of labels in pixels (will scale with Canvas Scaler)")]
    [SerializeField] private float labelSizePixels = 50f;
    [Tooltip("Update positions in real-time (temporary for tweaking)")]
    [SerializeField] private bool updateInRealTime = false;
    [Tooltip("Scale multiplier for label sprites")]
    [SerializeField] private float labelScale = 1f;

    [Header("References")]
    [Tooltip("Reference to LevelJsonLoader to check for async signal presence")]
    [SerializeField] private LevelJsonLoader levelJsonLoader;
    [Tooltip("Reference to input tilemap to convert tilemap Y coords to world coords")]
    [SerializeField] private UnityEngine.Tilemaps.Tilemap inputTilemap;
    [Tooltip("Reference to main camera (if null, will use Camera.main)")]
    [SerializeField] private Camera mainCamera;
    [Tooltip("Reference to the Canvas where labels will be placed")]
    [SerializeField] private Canvas canvas;

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

    private void Update()
    {
        if (updateInRealTime)
        {
            UpdateLabelPositions();
        }
    }





    /// <summary>
    /// Generates and positions all signal labels after LevelJsonLoader has parsed signals.
    /// Call this from LevelJsonLoader.Awake() after signals are parsed.
    /// </summary>
    public void GenerateLabels()
    {
        ClearExistingLabels();

        if (levelJsonLoader == null || mainCamera == null || inputTilemap == null)
        {
            Debug.LogWarning("SignalLabelRenderer: LevelJsonLoader, MainCamera, or InputTilemap reference is missing.");
            return;
        }

        Transform parent = (labelsParent != null) ? labelsParent : (canvas != null ? canvas.transform : transform);

        // Position labels using fixed pixel values (will be scaled by Canvas Scaler)
        bool hasAsync = (levelJsonLoader.ParsedPresetSignal != null || levelJsonLoader.ParsedClearSignal != null);

        // Always create J, K, Clock labels
        jLabel = CreateLabel("J_Label", jLabelSprite, jLabelY, xOffset, parent);
        kLabel = CreateLabel("K_Label", kLabelSprite, kLabelY, xOffset, parent);
        clockLabel = CreateLabel("Clock_Label", clockLabelSprite, clockLabelY, xOffset, parent);

        // Conditionally create Preset and Clear labels
        if (levelJsonLoader.ParsedPresetSignal != null)
        {
            presetLabel = CreateLabel("Preset_Label", presetLabelSprite, presetLabelY, xOffset, parent);
        }

        if (levelJsonLoader.ParsedClearSignal != null)
        {
            clearLabel = CreateLabel("Clear_Label", clearLabelSprite, clearLabelY, xOffset, parent);
        }
    }

    /// <summary>
    /// Converts tilemap Y coordinate to world Y position.
    /// </summary>
    private float GetWorldY(int tilemapY)
    {
        Vector3 worldPos = inputTilemap.CellToWorld(new Vector3Int(0, tilemapY, 0));
        worldPos.y += inputTilemap.cellSize.y / 2f;
        return worldPos.y;
    }

    /// <summary>
    /// Calculates the scale factor based on current screen resolution.
    /// Reference resolution is 1080p (height=1080).
    /// </summary>
    private float GetResolutionScale()
    {
        return Screen.height / 1080f;
    }

    /// <summary>
    /// Converts world Y position to screen Y position for UI anchoring.
    /// </summary>
    private float CalculateScreenY(float worldY)
    {
        Vector3 screenPos = mainCamera.WorldToScreenPoint(new Vector3(0, worldY, 0));
        return screenPos.y - Screen.height / 2f;
    }

    /// <summary>
    /// Gets the sigla for the signal based on the label name.
    /// </summary>
    private string GetSigla(string name)
    {
        if (name.Contains("J")) return "J";
        if (name.Contains("K")) return "K";
        if (name.Contains("Preset")) return "P";
        if (name.Contains("Clear")) return "C";
        if (name.Contains("Clock")) return "CLK";
        return "";
    }
    private GameObject CreateLabel(string name, Sprite sprite, float yPixels, float xPixels, Transform parent)
    {
        if (sprite == null)
        {
            Debug.LogWarning($"SignalLabelRenderer: Sprite for {name} is not assigned. Skipping.");
            return null;
        }

        GameObject labelObj = new GameObject(name);
        labelObj.transform.SetParent(parent);

        // Set RectTransform for UI positioning anchored to top-left
        RectTransform rt = labelObj.AddComponent<RectTransform>();
        float scaledSize = labelSizePixels * labelScale * GetResolutionScale();
        rt.sizeDelta = new Vector2(scaledSize, scaledSize);
        // Anchor to top-left
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(xPixels, -yPixels); // Negative Y for down from top

        Image img = labelObj.AddComponent<Image>();
        img.sprite = sprite;

        // Add text label
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(labelObj.transform);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = GetSigla(name);
        tmp.fontSize = name.Contains("Clock") ? 18 : 24; // Smaller font for CLK
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.sizeDelta = rt.sizeDelta; // same size as image
        textRt.anchorMin = new Vector2(0, 1);
        textRt.anchorMax = new Vector2(0, 1);
        textRt.pivot = new Vector2(0, 1);
        textRt.anchoredPosition = new Vector2(0, 0); // centered on the image

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
