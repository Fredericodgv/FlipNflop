using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dynamically places UI HUD labels at exact screen positions matching tilemap signal lines (J, K, Preset, Clear, Clock).
/// Invoked by <see cref="LevelJsonLoader"/> during level rendering.
/// </summary>
public class SignalLabelRenderer : MonoBehaviour
{
    #region Serialized Fields

    [Header("Label Sprites")]
    [SerializeField] private Sprite jLabelSprite;
    [SerializeField] private Sprite kLabelSprite;
    [SerializeField] private Sprite presetLabelSprite;
    [SerializeField] private Sprite clearLabelSprite;
    [SerializeField] private Sprite clockLabelSprite;

    [Header("Position Settings")]
    [Tooltip("X offset from left edge of the screen")]
    [SerializeField] private float xOffset = 10f;

    [Tooltip("Width and height of labels in pixels")]
    [SerializeField] private float labelSizePixels = 50f;

    [Tooltip("Scale multiplier for label sprites")]
    [SerializeField] private float labelScale = 1f;

    [Tooltip("Update positions in real-time (useful if camera moves vertically)")]
    [SerializeField] private bool updateInRealTime = false;

    [Header("References")]
    [SerializeField] private LevelJsonLoader levelJsonLoader;
    [SerializeField] private UnityEngine.Tilemaps.Tilemap inputTilemap;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas canvas;

    [Header("Parent Transform (Optional)")]
    [SerializeField] private Transform labelsParent;

    #endregion

    #region Private State

    private GameObject jLabel;
    private GameObject kLabel;
    private GameObject presetLabel;
    private GameObject clearLabel;
    private GameObject clockLabel;

    /// <summary>
    /// Cached Y row coordinates and active state configurations for real-time positioning updates.
    /// </summary>
    private int curJY, curKY, curPresetY, curClearY, curClockY;
    private bool curHasPreset, curHasClear;
    private bool curAsyncActiveHigh, curClockActiveHigh;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Caches main camera reference if unassigned.
    /// </summary>
    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    /// <summary>
    /// Re-renders label positions in real-time if <see cref="updateInRealTime"/> is enabled.
    /// </summary>
    private void Update()
    {
        if (updateInRealTime && jLabel != null)
        {
            GenerateLabels(curJY, curKY, curPresetY, curClearY, curClockY, curHasPreset, curHasClear, curAsyncActiveHigh, curClockActiveHigh);
        }
    }

    /// <summary>
    /// Cleans up instantiated label GameObjects on destroy.
    /// </summary>
    private void OnDestroy() => ClearExistingLabels();

    #endregion

    #region Public API

    /// <summary>
    /// Generates and positions all signal labels based on tilemap Y row coordinates.
    /// Invoked by <see cref="LevelJsonLoader.RenderLevel"/>.
    /// </summary>
    /// <param name="jY">Y tile row index for J signal.</param>
    /// <param name="kY">Y tile row index for K signal.</param>
    /// <param name="presetY">Y tile row index for Preset signal.</param>
    /// <param name="clearY">Y tile row index for Clear signal.</param>
    /// <param name="clockY">Y tile row index for Clock signal.</param>
    /// <param name="hasPreset">Whether Preset signal line exists in current level.</param>
    /// <param name="hasClear">Whether Clear signal line exists in current level.</param>
    /// <param name="isAsyncActiveHigh">True if asynchronous signals are active high; false if active low (overlined).</param>
    /// <param name="isClockActiveHigh">True if clock active edge is rising; false if falling (overlined).</param>
    public void GenerateLabels(int jY, int kY, int presetY, int clearY, int clockY, bool hasPreset, bool hasClear,
                               bool isAsyncActiveHigh = true, bool isClockActiveHigh = true)
    {
        curJY = jY; curKY = kY; curPresetY = presetY;
        curClearY = clearY; curClockY = clockY;
        curHasPreset = hasPreset; curHasClear = hasClear;

        curAsyncActiveHigh = isAsyncActiveHigh;
        curClockActiveHigh = isClockActiveHigh;

        ClearExistingLabels();

        if (mainCamera == null || inputTilemap == null || canvas == null)
        {
            Debug.LogWarning("SignalLabelRenderer: Camera, Tilemap, or Canvas reference missing!");
            return;
        }

        Transform parent = (labelsParent != null) ? labelsParent : canvas.transform;

        jLabel = CreateLabel("J_Label", jLabelSprite, jY, xOffset, parent, true);
        kLabel = CreateLabel("K_Label", kLabelSprite, kY, xOffset, parent, true);

        clockLabel = CreateLabel("Clock_Label", clockLabelSprite, clockY, xOffset, parent, isClockActiveHigh);

        if (hasPreset) presetLabel = CreateLabel("Preset_Label", presetLabelSprite, presetY, xOffset, parent, isAsyncActiveHigh);
        if (hasClear) clearLabel = CreateLabel("Clear_Label", clearLabelSprite, clearY, xOffset, parent, isAsyncActiveHigh);
    }

    #endregion

    #region Label Creation & Helpers

    /// <summary>
    /// Converts a tilemap Y row index into Canvas UI coordinates and creates a UI label object with optional overline.
    /// </summary>
    private GameObject CreateLabel(string name, Sprite sprite, int tileY, float xPixels, Transform parent, bool isActiveHigh)
    {
        if (sprite == null) return null;

        GameObject labelObj = new GameObject(name);
        labelObj.transform.SetParent(parent, false);

        RectTransform rt = labelObj.AddComponent<RectTransform>();
        float scaledSize = labelSizePixels * labelScale;
        rt.sizeDelta = new Vector2(scaledSize, scaledSize);

        Vector3 worldPos = inputTilemap.CellToWorld(new Vector3Int(0, tileY, 0));
        worldPos.y += inputTilemap.cellSize.y / 2f;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out Vector2 canvasLocalPoint);

        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(xPixels, canvasLocalPoint.y);

        Image img = labelObj.AddComponent<Image>();
        img.sprite = sprite;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(labelObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = GetSigla(name);
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.sizeDelta = rt.sizeDelta;
        textRt.anchorMin = new Vector2(0, 1);
        textRt.anchorMax = new Vector2(0, 1);
        textRt.pivot = new Vector2(0, 1);
        textRt.anchoredPosition = Vector2.zero;

        if (!isActiveHigh)
        {
            GameObject overlineObj = new GameObject("Overline");
            overlineObj.transform.SetParent(textObj.transform, false);
            Image lineImg = overlineObj.AddComponent<Image>();
            lineImg.color = Color.white;

            RectTransform lineRt = overlineObj.GetComponent<RectTransform>();

            lineRt.anchorMin = new Vector2(0.2f, 0.5f);
            lineRt.anchorMax = new Vector2(0.8f, 0.5f);
            lineRt.pivot = new Vector2(0.5f, 0f);

            lineRt.sizeDelta = new Vector2(0, 2);

            float yOffset = (tmp.fontSize / 2f) + 2f;
            lineRt.anchoredPosition = new Vector2(0, yOffset);
        }

        return labelObj;
    }

    /// <summary>
    /// Returns abbreviated acronym string label for a signal key name.
    /// </summary>
    private string GetSigla(string name)
    {
        if (name.Contains("J")) return "J";
        if (name.Contains("K")) return "K";
        if (name.Contains("Preset")) return "PRE";
        if (name.Contains("Clear")) return "CLR";
        if (name.Contains("Clock")) return "CLK";
        return "";
    }

    /// <summary>
    /// Destroys all currently instantiated label GameObjects.
    /// </summary>
    private void ClearExistingLabels()
    {
        if (jLabel != null) Destroy(jLabel);
        if (kLabel != null) Destroy(kLabel);
        if (presetLabel != null) Destroy(presetLabel);
        if (clearLabel != null) Destroy(clearLabel);
        if (clockLabel != null) Destroy(clockLabel);
    }

    #endregion
}