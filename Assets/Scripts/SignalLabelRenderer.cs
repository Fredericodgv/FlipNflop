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

    [Header("Position Settings")]
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

        // Position labels: J always at 1.35 * Screen.height, Clock always at 0.45 * Screen.height
        bool hasAsync = (levelJsonLoader.ParsedPresetSignal != null || levelJsonLoader.ParsedClearSignal != null);
        float jY = Screen.height * 1.325f - Screen.height / 2f;
        float clockY = Screen.height * 0.45f - Screen.height / 2f;

        float kY, presetY = 0, clearY = 0;

        if (hasAsync)
        {
            // K, Preset, Clear evenly spaced between J and Clock
            float totalSpace = jY - clockY;
            float step = totalSpace / 4f; // 4 intervals for K, Preset, Clear, Clock
            kY = jY - step;
            presetY = jY - 2 * step;
            clearY = jY - 3 * step;
        }
        else
        {
            // K exactly between J and Clock
            kY = (jY + clockY) / 2f;
        }

        // Always create J, K, Clock labels
        jLabel = CreateLabel("J_Label", jLabelSprite, jY, parent);
        kLabel = CreateLabel("K_Label", kLabelSprite, kY, parent);
        clockLabel = CreateLabel("Clock_Label", clockLabelSprite, clockY, parent);

        // Conditionally create Preset and Clear labels
        if (levelJsonLoader.ParsedPresetSignal != null)
        {
            presetLabel = CreateLabel("Preset_Label", presetLabelSprite, presetY, parent);
        }

        if (levelJsonLoader.ParsedClearSignal != null)
        {
            clearLabel = CreateLabel("Clear_Label", clearLabelSprite, clearY, parent);
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
    private GameObject CreateLabel(string name, Sprite sprite, float yPos, Transform parent)
    {
        if (sprite == null)
        {
            Debug.LogWarning($"SignalLabelRenderer: Sprite for {name} is not assigned. Skipping.");
            return null;
        }

        GameObject labelObj = new GameObject(name);
        labelObj.transform.SetParent(parent);

        // Set RectTransform for UI positioning
        RectTransform rt = labelObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(sprite.rect.width * labelScale, sprite.rect.height * labelScale);
        // Anchor to left-center
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(0, yPos);

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
        textRt.anchorMin = new Vector2(0, 0.5f);
        textRt.anchorMax = new Vector2(0, 0.5f);
        textRt.pivot = new Vector2(0, 0.5f);
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
