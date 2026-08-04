using UnityEngine;
using System;
using UnityEngine.Localization.Settings;

/// <summary>
/// Manages signal color configurations (J, K, CLK, Preset, Clear, Feedback Success, Feedback Failure) across preset palettes and custom RGB values.
/// Persists choices using <see cref="PlayerPrefs"/> and notifies listeners via <see cref="OnColorsChanged"/>.
/// Interacts with <see cref="LocalizationSettings"/> for palette localization.
/// </summary>
public class SignalColorManager : MonoBehaviour
{
    #region PlayerPrefs Keys

    /// <summary>PlayerPrefs key for J signal preset index.</summary>
    private const string KEY_J_IDX = "SignalJ_Index";

    /// <summary>PlayerPrefs key for K signal preset index.</summary>
    private const string KEY_K_IDX = "SignalK_Index";

    /// <summary>PlayerPrefs key for CLK signal preset index.</summary>
    private const string KEY_CLK_IDX = "SignalCLK_Index";

    /// <summary>PlayerPrefs key for Preset signal preset index.</summary>
    private const string KEY_PRESET_IDX = "SignalPreset_Index";

    /// <summary>PlayerPrefs key for Clear signal preset index.</summary>
    private const string KEY_CLEAR_IDX = "SignalClear_Index";

    /// <summary>PlayerPrefs key for Feedback Success signal preset index.</summary>
    private const string KEY_FEEDBACK_SUC_IDX = "SignalFeedbackSuccess_Index";

    /// <summary>PlayerPrefs key for Feedback Failure signal preset index.</summary>
    private const string KEY_FEEDBACK_FAIL_IDX = "SignalFeedbackFailure_Index";

    /// <summary>PlayerPrefs key for J signal Red RGB channel.</summary>
    private const string KEY_J_R = "SignalJ_R";
    /// <summary>PlayerPrefs key for J signal Green RGB channel.</summary>
    private const string KEY_J_G = "SignalJ_G";
    /// <summary>PlayerPrefs key for J signal Blue RGB channel.</summary>
    private const string KEY_J_B = "SignalJ_B";

    /// <summary>PlayerPrefs key for K signal Red RGB channel.</summary>
    private const string KEY_K_R = "SignalK_R";
    /// <summary>PlayerPrefs key for K signal Green RGB channel.</summary>
    private const string KEY_K_G = "SignalK_G";
    /// <summary>PlayerPrefs key for K signal Blue RGB channel.</summary>
    private const string KEY_K_B = "SignalK_B";

    /// <summary>PlayerPrefs key for CLK signal Red RGB channel.</summary>
    private const string KEY_CLK_R = "SignalCLK_R";
    /// <summary>PlayerPrefs key for CLK signal Green RGB channel.</summary>
    private const string KEY_CLK_G = "SignalCLK_G";
    /// <summary>PlayerPrefs key for CLK signal Blue RGB channel.</summary>
    private const string KEY_CLK_B = "SignalCLK_B";

    /// <summary>PlayerPrefs key for Preset signal Red RGB channel.</summary>
    private const string KEY_PRESET_R = "SignalPreset_R";
    /// <summary>PlayerPrefs key for Preset signal Green RGB channel.</summary>
    private const string KEY_PRESET_G = "SignalPreset_G";
    /// <summary>PlayerPrefs key for Preset signal Blue RGB channel.</summary>
    private const string KEY_PRESET_B = "SignalPreset_B";

    /// <summary>PlayerPrefs key for Clear signal Red RGB channel.</summary>
    private const string KEY_CLEAR_R = "SignalClear_R";
    /// <summary>PlayerPrefs key for Clear signal Green RGB channel.</summary>
    private const string KEY_CLEAR_G = "SignalClear_G";
    /// <summary>PlayerPrefs key for Clear signal Blue RGB channel.</summary>
    private const string KEY_CLEAR_B = "SignalClear_B";

    /// <summary>PlayerPrefs key for Feedback Success Red RGB channel.</summary>
    private const string KEY_FSUC_R = "SignalFSuc_R";
    /// <summary>PlayerPrefs key for Feedback Success Green RGB channel.</summary>
    private const string KEY_FSUC_G = "SignalFSuc_G";
    /// <summary>PlayerPrefs key for Feedback Success Blue RGB channel.</summary>
    private const string KEY_FSUC_B = "SignalFSuc_B";

    /// <summary>PlayerPrefs key for Feedback Failure Red RGB channel.</summary>
    private const string KEY_FFAIL_R = "SignalFFail_R";
    /// <summary>PlayerPrefs key for Feedback Failure Green RGB channel.</summary>
    private const string KEY_FFAIL_G = "SignalFFail_G";
    /// <summary>PlayerPrefs key for Feedback Failure Blue RGB channel.</summary>
    private const string KEY_FFAIL_B = "SignalFFail_B";

    /// <summary>
    /// Special index (-1) used to indicate that a signal is using a custom RGB color.
    /// </summary>
    public const int CUSTOM_INDEX = -1;

    #endregion

    #region Preset Colors & Palettes

    /// <summary>
    /// Array of predefined preset colors available for signal swatch selection.
    /// Index order: 0 White, 1 Yellow, 2 Cyan, 3 Orange, 4 Green, 5 Magenta, 6 Blue, 7 Red, 8 Pink, 9 Purple.
    /// </summary>
    public static readonly Color[] PresetColors = new Color[]
    {
        Color.white,                            // 0 White
        new Color(1.00f, 0.93f, 0.00f),         // 1 Yellow
        new Color(0.00f, 1.00f, 1.00f),         // 2 Cyan
        new Color(1.00f, 0.55f, 0.00f),         // 3 Orange
        new Color(0.27f, 1.00f, 0.27f),         // 4 Green
        new Color(1.00f, 0.13f, 1.00f),         // 5 Magenta
        new Color(0.11f, 0.56f, 1.00f),         // 6 Blue
        new Color(1.00f, 0.20f, 0.20f),         // 7 Red
        new Color(1.00f, 0.53f, 0.80f),         // 8 Pink
        new Color(0.73f, 0.27f, 1.00f),         // 9 Purple
    };

    /// <summary>
    /// Array of accessibility color palettes mapping preset color indices for each signal:
    /// [0] Default, [1] Colorblindness, [2] High Contrast.
    /// </summary>
    public static readonly int[][] Palettes = new int[][]
    {
        new[] { 0, 0, 3, 3, 6, 4, 7 }, // Default
        new[] { 1, 9, 2, 3, 6, 7, 5 }, // Colorblind
        new[] { 1, 5, 3, 2, 6, 4, 7 }, // High Contrast
    };

    #endregion

    #region Default Indexes

    /// <summary>Default preset color index for J signal.</summary>
    private const int DEFAULT_J_IDX = 0;

    /// <summary>Default preset color index for K signal.</summary>
    private const int DEFAULT_K_IDX = 0;

    /// <summary>Default preset color index for CLK signal.</summary>
    private const int DEFAULT_CLK_IDX = 6;

    /// <summary>Default preset color index for Preset signal.</summary>
    private const int DEFAULT_PRESET_IDX = 0;

    /// <summary>Default preset color index for Clear signal.</summary>
    private const int DEFAULT_CLEAR_IDX = 0;

    /// <summary>Default preset color index for Feedback Success signal (Green).</summary>
    private const int DEFAULT_FEEDBACK_SUC_IDX = 4;

    /// <summary>Default preset color index for Feedback Failure signal (Red).</summary>
    private const int DEFAULT_FEEDBACK_FAIL_IDX = 7;

    #endregion

    #region Events & Singleton

    /// <summary>
    /// Event invoked whenever any signal color configuration is changed.
    /// </summary>
    public static event Action OnColorsChanged;

    /// <summary>
    /// Singleton instance for global access to <see cref="SignalColorManager"/>.
    /// </summary>
    public static SignalColorManager Instance { get; private set; }

    #endregion

    #region Public Properties

    /// <summary>Current calculated <see cref="Color"/> for J signal.</summary>
    public Color ColorJ { get; private set; }

    /// <summary>Current calculated <see cref="Color"/> for K signal.</summary>
    public Color ColorK { get; private set; }

    /// <summary>Current calculated <see cref="Color"/> for CLK signal.</summary>
    public Color ColorCLK { get; private set; }

    /// <summary>Current calculated <see cref="Color"/> for Preset signal.</summary>
    public Color ColorPreset { get; private set; }

    /// <summary>Current calculated <see cref="Color"/> for Clear signal.</summary>
    public Color ColorClear { get; private set; }

    /// <summary>Current calculated <see cref="Color"/> for Feedback Success visual element.</summary>
    public Color ColorFeedbackSuccess { get; private set; }

    /// <summary>Current calculated <see cref="Color"/> for Feedback Failure visual element.</summary>
    public Color ColorFeedbackFailure { get; private set; }

    /// <summary>Preset index for J signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexJ { get; private set; }

    /// <summary>Preset index for K signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexK { get; private set; }

    /// <summary>Preset index for CLK signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexCLK { get; private set; }

    /// <summary>Preset index for Preset signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexPreset { get; private set; }

    /// <summary>Preset index for Clear signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexClear { get; private set; }

    /// <summary>Preset index for Feedback Success signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexFeedbackSuccess { get; private set; }

    /// <summary>Preset index for Feedback Failure signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexFeedbackFailure { get; private set; }

    #endregion

    #region Unity Lifecycle & Initialization

    /// <summary>
    /// Configures singleton instance, marks object persistent across scene loads via <see cref="Object.DontDestroyOnLoad(Object)"/>,
    /// and loads saved colors from <see cref="PlayerPrefs"/>.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
            LoadColors();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Retrieves localized palette name string from Unity Localization table ("ConfigMenu").
    /// Interacts with <see cref="LocalizationSettings.StringDatabase"/>.
    /// </summary>
    /// <param name="index">Palette index (0: Default, 1: Colorblind, 2: High Contrast, else Custom).</param>
    /// <returns>Localized palette string.</returns>
    public static string GetLocalizedPaletteName(int index)
    {
        string tableName = "ConfigMenu";

        string key = index switch
        {
            0 => "palette_default",
            1 => "palette_colorblind",
            2 => "palette_highcontrast",
            _ => "palette_custom"
        };

        return LocalizationSettings.StringDatabase.GetLocalizedString(tableName, key);
    }

    #endregion

    #region Palette Management

    /// <summary>
    /// Applies a preset color palette by index across all signals and saves choices to <see cref="PlayerPrefs"/>.
    /// Invokes <see cref="OnColorsChanged"/>.
    /// </summary>
    /// <param name="paletteIndex">Index of palette to apply from <see cref="Palettes"/>.</param>
    public void ApplyPalette(int paletteIndex)
    {
        if (paletteIndex < 0 || paletteIndex >= Palettes.Length)
            return;

        int[] palette = Palettes[paletteIndex];

        SetColorByIndex("J", palette[0]);
        SetColorByIndex("K", palette[1]);
        SetColorByIndex("Preset", palette[2]);
        SetColorByIndex("Clear", palette[3]);
        SetColorByIndex("CLK", palette[4]);
        SetColorByIndex("FeedbackSuccess", palette[5]);
        SetColorByIndex("FeedbackFailure", palette[6]);

        NotifyAndSave();
    }

    #endregion

    #region Preset & Custom Color Modifiers

    /// <summary>
    /// Sets a signal color by preset index and saves to <see cref="PlayerPrefs"/> without firing notifications.
    /// </summary>
    /// <param name="signal">Signal name string ("J", "K", "CLK", "Preset", "Clear", "FeedbackSuccess", "FeedbackFailure").</param>
    /// <param name="colorIndex">Index from <see cref="PresetColors"/>.</param>
    public void SetColorByIndex(string signal, int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= PresetColors.Length) return;
        ApplyToSignal(signal, colorIndex, PresetColors[colorIndex]);
        SaveIndex(signal, colorIndex);
    }

    /// <summary>
    /// Sets a signal color by preset index, saves to <see cref="PlayerPrefs"/>, and triggers <see cref="OnColorsChanged"/>.
    /// </summary>
    /// <param name="signal">Signal name string.</param>
    /// <param name="colorIndex">Index from <see cref="PresetColors"/>.</param>
    public void SetAndNotify(string signal, int colorIndex)
    {
        SetColorByIndex(signal, colorIndex);
        NotifyAndSave();
    }

    /// <summary>
    /// Sets a custom RGB color for a signal, updates index to <see cref="CUSTOM_INDEX"/>, saves to <see cref="PlayerPrefs"/>, and triggers <see cref="OnColorsChanged"/>.
    /// </summary>
    /// <param name="signal">Signal name string.</param>
    /// <param name="color">Custom RGBA <see cref="Color"/> value.</param>
    public void SetCustomColor(string signal, Color color)
    {
        ApplyToSignal(signal, CUSTOM_INDEX, color);
        SaveCustomColor(signal, color);
        SaveIndex(signal, CUSTOM_INDEX);
        NotifyAndSave();
    }

    /// <summary>
    /// Saves custom RGB channel values for a signal into <see cref="PlayerPrefs"/>.
    /// </summary>
    /// <param name="signal">Signal name string.</param>
    /// <param name="c">Target color.</param>
    private void SaveCustomColor(string signal, Color c)
    {
        switch (signal)
        {
            case "J":
                PlayerPrefs.SetFloat(KEY_J_R, c.r); PlayerPrefs.SetFloat(KEY_J_G, c.g); PlayerPrefs.SetFloat(KEY_J_B, c.b);
                break;
            case "K":
                PlayerPrefs.SetFloat(KEY_K_R, c.r); PlayerPrefs.SetFloat(KEY_K_G, c.g); PlayerPrefs.SetFloat(KEY_K_B, c.b);
                break;
            case "CLK":
                PlayerPrefs.SetFloat(KEY_CLK_R, c.r); PlayerPrefs.SetFloat(KEY_CLK_G, c.g); PlayerPrefs.SetFloat(KEY_CLK_B, c.b);
                break;
            case "Preset":
                PlayerPrefs.SetFloat(KEY_PRESET_R, c.r); PlayerPrefs.SetFloat(KEY_PRESET_G, c.g); PlayerPrefs.SetFloat(KEY_PRESET_B, c.b);
                break;
            case "Clear":
                PlayerPrefs.SetFloat(KEY_CLEAR_R, c.r); PlayerPrefs.SetFloat(KEY_CLEAR_G, c.g); PlayerPrefs.SetFloat(KEY_CLEAR_B, c.b);
                break;
            case "FeedbackSuccess":
                PlayerPrefs.SetFloat(KEY_FSUC_R, c.r); PlayerPrefs.SetFloat(KEY_FSUC_G, c.g); PlayerPrefs.SetFloat(KEY_FSUC_B, c.b);
                break;
            case "FeedbackFailure":
                PlayerPrefs.SetFloat(KEY_FFAIL_R, c.r); PlayerPrefs.SetFloat(KEY_FFAIL_G, c.g); PlayerPrefs.SetFloat(KEY_FFAIL_B, c.b);
                break;
        }
    }

    #endregion

    #region Restore Defaults

    /// <summary>
    /// Restores all signal colors to default preset indices, saves to <see cref="PlayerPrefs"/>, and triggers <see cref="OnColorsChanged"/>.
    /// </summary>
    public void RestoreDefaultColors()
    {
        SetColorByIndex("J", DEFAULT_J_IDX);
        SetColorByIndex("K", DEFAULT_K_IDX);
        SetColorByIndex("CLK", DEFAULT_CLK_IDX);
        SetColorByIndex("Preset", DEFAULT_PRESET_IDX);
        SetColorByIndex("Clear", DEFAULT_CLEAR_IDX);
        SetColorByIndex("FeedbackSuccess", DEFAULT_FEEDBACK_SUC_IDX);
        SetColorByIndex("FeedbackFailure", DEFAULT_FEEDBACK_FAIL_IDX);
        NotifyAndSave();
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Assigns index and color properties for a given signal type.
    /// </summary>
    /// <param name="signal">Signal key name.</param>
    /// <param name="index">Preset index or <see cref="CUSTOM_INDEX"/>.</param>
    /// <param name="color">Target color.</param>
    private void ApplyToSignal(string signal, int index, Color color)
    {
        switch (signal)
        {
            case "J": IndexJ = index; ColorJ = color; break;
            case "K": IndexK = index; ColorK = color; break;
            case "CLK": IndexCLK = index; ColorCLK = color; break;
            case "Preset": IndexPreset = index; ColorPreset = color; break;
            case "Clear": IndexClear = index; ColorClear = color; break;
            case "FeedbackSuccess": IndexFeedbackSuccess = index; ColorFeedbackSuccess = color; break;
            case "FeedbackFailure": IndexFeedbackFailure = index; ColorFeedbackFailure = color; break;
        }
    }

    /// <summary>
    /// Saves preset index value for a signal to <see cref="PlayerPrefs"/>.
    /// </summary>
    /// <param name="signal">Signal key name.</param>
    /// <param name="index">Preset index value.</param>
    private void SaveIndex(string signal, int index)
    {
        switch (signal)
        {
            case "J": PlayerPrefs.SetInt(KEY_J_IDX, index); break;
            case "K": PlayerPrefs.SetInt(KEY_K_IDX, index); break;
            case "CLK": PlayerPrefs.SetInt(KEY_CLK_IDX, index); break;
            case "Preset": PlayerPrefs.SetInt(KEY_PRESET_IDX, index); break;
            case "Clear": PlayerPrefs.SetInt(KEY_CLEAR_IDX, index); break;
            case "FeedbackSuccess": PlayerPrefs.SetInt(KEY_FEEDBACK_SUC_IDX, index); break;
            case "FeedbackFailure": PlayerPrefs.SetInt(KEY_FEEDBACK_FAIL_IDX, index); break;
        }
    }

    #endregion

    #region Save & Load Logic

    /// <summary>
    /// Loads stored signal preset indices and custom RGB color values from <see cref="PlayerPrefs"/>.
    /// </summary>
    private void LoadColors()
    {
        IndexJ = PlayerPrefs.GetInt(KEY_J_IDX, DEFAULT_J_IDX);
        IndexK = PlayerPrefs.GetInt(KEY_K_IDX, DEFAULT_K_IDX);
        IndexCLK = PlayerPrefs.GetInt(KEY_CLK_IDX, DEFAULT_CLK_IDX);
        IndexPreset = PlayerPrefs.GetInt(KEY_PRESET_IDX, DEFAULT_PRESET_IDX);
        IndexClear = PlayerPrefs.GetInt(KEY_CLEAR_IDX, DEFAULT_CLEAR_IDX);
        IndexFeedbackSuccess = PlayerPrefs.GetInt(KEY_FEEDBACK_SUC_IDX, DEFAULT_FEEDBACK_SUC_IDX);
        IndexFeedbackFailure = PlayerPrefs.GetInt(KEY_FEEDBACK_FAIL_IDX, DEFAULT_FEEDBACK_FAIL_IDX);

        ColorJ = LoadColorForSignal("J", IndexJ, DEFAULT_J_IDX);
        ColorK = LoadColorForSignal("K", IndexK, DEFAULT_K_IDX);
        ColorCLK = LoadColorForSignal("CLK", IndexCLK, DEFAULT_CLK_IDX);
        ColorPreset = LoadColorForSignal("Preset", IndexPreset, DEFAULT_PRESET_IDX);
        ColorClear = LoadColorForSignal("Clear", IndexClear, DEFAULT_CLEAR_IDX);
        ColorFeedbackSuccess = LoadColorForSignal("FeedbackSuccess", IndexFeedbackSuccess, DEFAULT_FEEDBACK_SUC_IDX);
        ColorFeedbackFailure = LoadColorForSignal("FeedbackFailure", IndexFeedbackFailure, DEFAULT_FEEDBACK_FAIL_IDX);
    }

    /// <summary>
    /// Loads specific color for a signal based on preset index or custom RGB values stored in <see cref="PlayerPrefs"/>.
    /// </summary>
    /// <param name="signal">Signal key name.</param>
    /// <param name="index">Stored index.</param>
    /// <param name="defaultIndex">Fallback default index.</param>
    /// <returns>Resolved color.</returns>
    private Color LoadColorForSignal(string signal, int index, int defaultIndex)
    {
        if (index == CUSTOM_INDEX)
        {
            switch (signal)
            {
                case "J": return new Color(PlayerPrefs.GetFloat(KEY_J_R, 1f), PlayerPrefs.GetFloat(KEY_J_G, 1f), PlayerPrefs.GetFloat(KEY_J_B, 1f));
                case "K": return new Color(PlayerPrefs.GetFloat(KEY_K_R, 1f), PlayerPrefs.GetFloat(KEY_K_G, 1f), PlayerPrefs.GetFloat(KEY_K_B, 1f));
                case "CLK": return new Color(PlayerPrefs.GetFloat(KEY_CLK_R, 1f), PlayerPrefs.GetFloat(KEY_CLK_G, 1f), PlayerPrefs.GetFloat(KEY_CLK_B, 1f));
                case "Preset": return new Color(PlayerPrefs.GetFloat(KEY_PRESET_R, 1f), PlayerPrefs.GetFloat(KEY_PRESET_G, 1f), PlayerPrefs.GetFloat(KEY_PRESET_B, 1f));
                case "Clear": return new Color(PlayerPrefs.GetFloat(KEY_CLEAR_R, 1f), PlayerPrefs.GetFloat(KEY_CLEAR_G, 1f), PlayerPrefs.GetFloat(KEY_CLEAR_B, 1f));
                case "FeedbackSuccess": return new Color(PlayerPrefs.GetFloat(KEY_FSUC_R, 1f), PlayerPrefs.GetFloat(KEY_FSUC_G, 1f), PlayerPrefs.GetFloat(KEY_FSUC_B, 1f));
                case "FeedbackFailure": return new Color(PlayerPrefs.GetFloat(KEY_FFAIL_R, 1f), PlayerPrefs.GetFloat(KEY_FFAIL_G, 1f), PlayerPrefs.GetFloat(KEY_FFAIL_B, 1f));
            }
        }

        int clamped = Mathf.Clamp(index, 0, PresetColors.Length - 1);
        return PresetColors[clamped];
    }

    /// <summary>
    /// Saves all changes to <see cref="PlayerPrefs"/> and invokes <see cref="OnColorsChanged"/>.
    /// </summary>
    private void NotifyAndSave()
    {
        PlayerPrefs.Save();
        OnColorsChanged?.Invoke();
    }

    /// <summary>
    /// Evaluates current active signal indices to determine matching preset palette index from <see cref="Palettes"/>, or returns <see cref="CUSTOM_INDEX"/>.
    /// </summary>
    /// <returns>Palette index (0, 1, 2) or <see cref="CUSTOM_INDEX"/> (-1).</returns>
    public int GetCurrentPaletteIndex()
    {
        for (int i = 0; i < Palettes.Length; i++)
        {
            int[] palette = Palettes[i];

            if (palette[0] == IndexJ &&
                palette[1] == IndexK &&
                palette[2] == IndexPreset &&
                palette[3] == IndexClear &&
                palette[4] == IndexCLK &&
                palette[5] == IndexFeedbackSuccess &&
                palette[6] == IndexFeedbackFailure)
            {
                return i;
            }
        }

        return CUSTOM_INDEX;
    }

    #endregion
}