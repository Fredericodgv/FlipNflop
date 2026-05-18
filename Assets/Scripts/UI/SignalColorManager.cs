using UnityEngine;
using System;

public class SignalColorManager : MonoBehaviour
{
    // ─── PlayerPrefs Keys — índices de preset ───────────────────────────────
    private const string KEY_J_IDX   = "SignalJ_Index";
    private const string KEY_K_IDX   = "SignalK_Index";
    private const string KEY_CLK_IDX = "SignalCLK_Index";

    // ─── PlayerPrefs Keys — cores customizadas (RGB 0-1) ────────────────────
    private const string KEY_J_R   = "SignalJ_R";   private const string KEY_J_G   = "SignalJ_G";   private const string KEY_J_B   = "SignalJ_B";
    private const string KEY_K_R   = "SignalK_R";   private const string KEY_K_G   = "SignalK_G";   private const string KEY_K_B   = "SignalK_B";
    private const string KEY_CLK_R = "SignalCLK_R"; private const string KEY_CLK_G = "SignalCLK_G"; private const string KEY_CLK_B = "SignalCLK_B";

    // Índice especial que indica cor customizada
    public const int CUSTOM_INDEX = -1;

    // ─── Cores presetadas ────────────────────────────────────────────────────
    public static readonly Color[] PresetColors = new Color[]
    {
        Color.white,                            // 0 Branco
        new Color(1.00f, 0.93f, 0.00f),         // 1 Amarelo
        new Color(0.00f, 1.00f, 1.00f),         // 2 Ciano
        new Color(1.00f, 0.55f, 0.00f),         // 3 Laranja
        new Color(0.27f, 1.00f, 0.27f),         // 4 Verde
        new Color(1.00f, 0.13f, 1.00f),         // 5 Magenta
        new Color(0.11f, 0.56f, 1.00f),         // 6 Azul
        new Color(1.00f, 0.20f, 0.20f),         // 7 Vermelho
        new Color(1.00f, 0.53f, 0.80f),         // 8 Rosa
        new Color(0.73f, 0.27f, 1.00f),         // 9 Roxo
    };

    public static readonly string[] PresetColorNames = new string[]
    {
        "Branco", "Amarelo", "Ciano", "Laranja", "Verde",
        "Magenta", "Azul", "Vermelho", "Rosa", "Roxo"
    };

    // ─── Paletas de acessibilidade ───────────────────────────────────────────
    public static readonly int[][] Palettes = new int[][]
    {
        new[] { 0, 0, 0 },
        new[] { 6, 3, 0 },
        new[] { 1, 2, 5 },
    };
    public static readonly string[] PaletteNames = { "Padrão", "Daltonismo", "Alto Contraste" };

    private const int DEFAULT_J_IDX   = 0;
    private const int DEFAULT_K_IDX   = 0;
    private const int DEFAULT_CLK_IDX = 0;

    // ─── Evento ──────────────────────────────────────────────────────────────
    public static event Action OnColorsChanged;

    public static SignalColorManager Instance { get; private set; }

    public Color ColorJ   { get; private set; }
    public Color ColorK   { get; private set; }
    public Color ColorCLK { get; private set; }

    // CUSTOM_INDEX (-1) quando a cor atual é customizada
    public int IndexJ   { get; private set; }
    public int IndexK   { get; private set; }
    public int IndexCLK { get; private set; }

    #region Inicialização

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadColors();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Aplicar Paleta

    public void ApplyPalette(int paletteIndex)
    {
        if (paletteIndex < 0 || paletteIndex >= Palettes.Length) return;
        SetColorByIndex("J",   Palettes[paletteIndex][0]);
        SetColorByIndex("K",   Palettes[paletteIndex][1]);
        SetColorByIndex("CLK", Palettes[paletteIndex][2]);
        NotifyAndSave();
    }

    #endregion

    #region Definir Cor por Índice (preset)

    public void SetColorByIndex(string signal, int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= PresetColors.Length) return;
        ApplyToSignal(signal, colorIndex, PresetColors[colorIndex]);
        SaveIndex(signal, colorIndex);
    }

    public void SetAndNotify(string signal, int colorIndex)
    {
        SetColorByIndex(signal, colorIndex);
        NotifyAndSave();
    }

    #endregion

    #region Definir Cor Customizada (RGB livre)

    public void SetCustomColor(string signal, Color color)
    {
        ApplyToSignal(signal, CUSTOM_INDEX, color);
        SaveCustomColor(signal, color);
        SaveIndex(signal, CUSTOM_INDEX);
        NotifyAndSave();
    }

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
        }
    }

    #endregion

    #region Restaurar Padrão

    public void RestoreDefaultColors()
    {
        SetColorByIndex("J",   DEFAULT_J_IDX);
        SetColorByIndex("K",   DEFAULT_K_IDX);
        SetColorByIndex("CLK", DEFAULT_CLK_IDX);
        NotifyAndSave();
    }

    #endregion

    #region Internos

    private void ApplyToSignal(string signal, int index, Color color)
    {
        switch (signal)
        {
            case "J":   IndexJ   = index; ColorJ   = color; break;
            case "K":   IndexK   = index; ColorK   = color; break;
            case "CLK": IndexCLK = index; ColorCLK = color; break;
        }
    }

    private void SaveIndex(string signal, int index)
    {
        switch (signal)
        {
            case "J":   PlayerPrefs.SetInt(KEY_J_IDX,   index); break;
            case "K":   PlayerPrefs.SetInt(KEY_K_IDX,   index); break;
            case "CLK": PlayerPrefs.SetInt(KEY_CLK_IDX, index); break;
        }
    }

    #endregion

    #region Salvar / Carregar

    private void LoadColors()
    {
        IndexJ   = PlayerPrefs.GetInt(KEY_J_IDX,   DEFAULT_J_IDX);
        IndexK   = PlayerPrefs.GetInt(KEY_K_IDX,   DEFAULT_K_IDX);
        IndexCLK = PlayerPrefs.GetInt(KEY_CLK_IDX, DEFAULT_CLK_IDX);

        ColorJ   = LoadColorForSignal("J",   IndexJ,   DEFAULT_J_IDX);
        ColorK   = LoadColorForSignal("K",   IndexK,   DEFAULT_K_IDX);
        ColorCLK = LoadColorForSignal("CLK", IndexCLK, DEFAULT_CLK_IDX);
    }

    private Color LoadColorForSignal(string signal, int index, int defaultIndex)
    {
        if (index == CUSTOM_INDEX)
        {
            // Carrega RGB salvo da cor customizada
            switch (signal)
            {
                case "J":   return new Color(PlayerPrefs.GetFloat(KEY_J_R, 1f),   PlayerPrefs.GetFloat(KEY_J_G, 1f),   PlayerPrefs.GetFloat(KEY_J_B, 1f));
                case "K":   return new Color(PlayerPrefs.GetFloat(KEY_K_R, 1f),   PlayerPrefs.GetFloat(KEY_K_G, 1f),   PlayerPrefs.GetFloat(KEY_K_B, 1f));
                case "CLK": return new Color(PlayerPrefs.GetFloat(KEY_CLK_R, 1f), PlayerPrefs.GetFloat(KEY_CLK_G, 1f), PlayerPrefs.GetFloat(KEY_CLK_B, 1f));
            }
        }
        int clamped = Mathf.Clamp(index, 0, PresetColors.Length - 1);
        return PresetColors[clamped];
    }

    private void NotifyAndSave()
    {
        PlayerPrefs.Save();
        OnColorsChanged?.Invoke();
    }

    #endregion
}