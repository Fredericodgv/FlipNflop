using UnityEngine;
using System;
using UnityEngine.Localization.Settings;

/// <summary>
/// Proxy/bridge for signal color settings, delegating data storage to <see cref="GameSettings"/>.
/// Maintains backward-compatible public API (properties, methods, events) so that all existing consumers
/// (<see cref="LevelJsonLoader"/>, <see cref="PathVerifier"/>, <see cref="ColorSettingsTab"/>) continue working unchanged.
/// Persists as a singleton MonoBehaviour via <see cref="Object.DontDestroyOnLoad(Object)"/>.
/// Interacts with <see cref="GameSettings"/> for data, <see cref="LocalizationSettings"/> for palette localization.
/// </summary>
public class SignalColorManager : MonoBehaviour
{
    #region Events & Singleton

    /// <summary>
    /// Event invoked whenever any signal color configuration is changed.
    /// Forwards from <see cref="GameSettings.OnColorsChanged"/>.
    /// </summary>
    public static event Action OnColorsChanged
    {
        add => GameSettings.OnColorsChanged += value;
        remove => GameSettings.OnColorsChanged -= value;
    }

    /// <summary>
    /// Singleton instance for global access to <see cref="SignalColorManager"/>.
    /// </summary>
    public static SignalColorManager Instance { get; private set; }

    #endregion

    #region Public Constants & Static Data (delegated to GameSettings)

    /// <summary>
    /// Special index (-1) used to indicate that a signal is using a custom RGB color.
    /// </summary>
    public const int CUSTOM_INDEX = GameSettings.CUSTOM_INDEX;

    /// <summary>
    /// Array of predefined preset colors available for signal swatch selection.
    /// Delegates to <see cref="GameSettings.PresetColors"/>.
    /// </summary>
    public static Color[] PresetColors => GameSettings.PresetColors;

    /// <summary>
    /// Array of accessibility color palettes.
    /// Delegates to <see cref="GameSettings.Palettes"/>.
    /// </summary>
    public static int[][] Palettes => GameSettings.Palettes;

    #endregion

    #region Public Properties (delegated to GameSettings)

    /// <summary>Current calculated <see cref="Color"/> for J signal.</summary>
    public Color ColorJ => GameSettings.Instance.ColorJ;

    /// <summary>Current calculated <see cref="Color"/> for K signal.</summary>
    public Color ColorK => GameSettings.Instance.ColorK;

    /// <summary>Current calculated <see cref="Color"/> for CLK signal.</summary>
    public Color ColorCLK => GameSettings.Instance.ColorCLK;

    /// <summary>Current calculated <see cref="Color"/> for Preset signal.</summary>
    public Color ColorPreset => GameSettings.Instance.ColorPreset;

    /// <summary>Current calculated <see cref="Color"/> for Clear signal.</summary>
    public Color ColorClear => GameSettings.Instance.ColorClear;

    /// <summary>Current calculated <see cref="Color"/> for Feedback Success visual element.</summary>
    public Color ColorFeedbackSuccess => GameSettings.Instance.ColorFeedbackSuccess;

    /// <summary>Current calculated <see cref="Color"/> for Feedback Failure visual element.</summary>
    public Color ColorFeedbackFailure => GameSettings.Instance.ColorFeedbackFailure;

    /// <summary>Preset index for J signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexJ => GameSettings.Instance.IndexJ;

    /// <summary>Preset index for K signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexK => GameSettings.Instance.IndexK;

    /// <summary>Preset index for CLK signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexCLK => GameSettings.Instance.IndexCLK;

    /// <summary>Preset index for Preset signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexPreset => GameSettings.Instance.IndexPreset;

    /// <summary>Preset index for Clear signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexClear => GameSettings.Instance.IndexClear;

    /// <summary>Preset index for Feedback Success signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexFeedbackSuccess => GameSettings.Instance.IndexFeedbackSuccess;

    /// <summary>Preset index for Feedback Failure signal, or <see cref="CUSTOM_INDEX"/> (-1).</summary>
    public int IndexFeedbackFailure => GameSettings.Instance.IndexFeedbackFailure;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Configures singleton instance, ensures <see cref="GameSettings"/> is initialized,
    /// and marks this object persistent across scene loads via <see cref="Object.DontDestroyOnLoad(Object)"/>.
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

            GameSettings.Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Localization

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

    #region Public API (delegated to GameSettings)

    /// <summary>
    /// Applies a preset color palette by index across all signals.
    /// Delegates to <see cref="GameSettings.ApplyPalette(int)"/>.
    /// </summary>
    /// <param name="paletteIndex">Index of palette to apply from <see cref="Palettes"/>.</param>
    public void ApplyPalette(int paletteIndex)
    {
        GameSettings.Instance.ApplyPalette(paletteIndex);
    }

    /// <summary>
    /// Sets a signal color by preset index and saves without firing notifications.
    /// Delegates to <see cref="GameSettings.SetColorByIndex(string, int)"/>.
    /// </summary>
    /// <param name="signal">Signal name string.</param>
    /// <param name="colorIndex">Index from <see cref="PresetColors"/>.</param>
    public void SetColorByIndex(string signal, int colorIndex)
    {
        GameSettings.Instance.SetColorByIndex(signal, colorIndex);
    }

    /// <summary>
    /// Sets a signal color by preset index, saves, and triggers color change notification.
    /// Delegates to <see cref="GameSettings.SetAndNotify(string, int)"/>.
    /// </summary>
    /// <param name="signal">Signal name string.</param>
    /// <param name="colorIndex">Index from <see cref="PresetColors"/>.</param>
    public void SetAndNotify(string signal, int colorIndex)
    {
        GameSettings.Instance.SetAndNotify(signal, colorIndex);
    }

    /// <summary>
    /// Sets a custom RGB color for a signal, updates index to <see cref="CUSTOM_INDEX"/>, and triggers notification.
    /// Delegates to <see cref="GameSettings.SetCustomColor(string, Color)"/>.
    /// </summary>
    /// <param name="signal">Signal name string.</param>
    /// <param name="color">Custom RGBA <see cref="Color"/> value.</param>
    public void SetCustomColor(string signal, Color color)
    {
        GameSettings.Instance.SetCustomColor(signal, color);
    }

    /// <summary>
    /// Restores all signal colors to default preset indices.
    /// Delegates to <see cref="GameSettings.RestoreDefaultColors"/>.
    /// </summary>
    public void RestoreDefaultColors()
    {
        GameSettings.Instance.RestoreDefaultColors();
    }

    /// <summary>
    /// Evaluates current active signal indices to determine matching preset palette index.
    /// Delegates to <see cref="GameSettings.GetCurrentPaletteIndex"/>.
    /// </summary>
    /// <returns>Palette index (0, 1, 2) or <see cref="CUSTOM_INDEX"/> (-1).</returns>
    public int GetCurrentPaletteIndex()
    {
        return GameSettings.Instance.GetCurrentPaletteIndex();
    }

    #endregion
}