using UnityEngine;
using System;

public class SignalColorManager : MonoBehaviour
{
    // ─── PlayerPrefs Keys — índices de preset ───────────────────────────────
    private const string KEY_J_IDX = "SignalJ_Index";
    private const string KEY_K_IDX = "SignalK_Index";
    private const string KEY_CLK_IDX = "SignalCLK_Index";
    private const string KEY_PRESET_IDX = "SignalPreset_Index";
    private const string KEY_CLEAR_IDX = "SignalClear_Index";
    private const string KEY_FEEDBACK_SUC_IDX = "SignalFeedbackSuccess_Index";
    private const string KEY_FEEDBACK_FAIL_IDX = "SignalFeedbackFailure_Index";

    // ─── PlayerPrefs Keys — cores customizadas (RGB 0-1) ────────────────────
    private const string KEY_J_R = "SignalJ_R"; private const string KEY_J_G = "SignalJ_G"; private const string KEY_J_B = "SignalJ_B";
    private const string KEY_K_R = "SignalK_R"; private const string KEY_K_G = "SignalK_G"; private const string KEY_K_B = "SignalK_B";
    private const string KEY_CLK_R = "SignalCLK_R"; private const string KEY_CLK_G = "SignalCLK_G"; private const string KEY_CLK_B = "SignalCLK_B";

    private const string KEY_PRESET_R = "SignalPreset_R"; private const string KEY_PRESET_G = "SignalPreset_G"; private const string KEY_PRESET_B = "SignalPreset_B";
    private const string KEY_CLEAR_R = "SignalClear_R"; private const string KEY_CLEAR_G = "SignalClear_G"; private const string KEY_CLEAR_B = "SignalClear_B";
    private const string KEY_FSUC_R = "SignalFSuc_R"; private const string KEY_FSUC_G = "SignalFSuc_G"; private const string KEY_FSUC_B = "SignalFSuc_B";
    private const string KEY_FFAIL_R = "SignalFFail_R"; private const string KEY_FFAIL_G = "SignalFFail_G"; private const string KEY_FFAIL_B = "SignalFFail_B";

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
        // Padrão
        new[] { 0, 0, 3, 3, 6, 4, 7 },

        // Daltonismo
        new[] { 1, 9, 2, 3, 6, 7, 5 },

        // Alto Contraste
        new[] { 1, 5, 3, 2, 6, 4, 7 },
    };
    public static readonly string[] PaletteNames = { "Padrão", "Daltonismo", "Alto Contraste" };

    // ─── Índices padrão ──────────────────────────────────────────────────────
    private const int DEFAULT_J_IDX = 0;
    private const int DEFAULT_K_IDX = 0;
    private const int DEFAULT_CLK_IDX = 6;
    private const int DEFAULT_PRESET_IDX = 0;
    private const int DEFAULT_CLEAR_IDX = 0;
    private const int DEFAULT_FEEDBACK_SUC_IDX = 4; // Verde
    private const int DEFAULT_FEEDBACK_FAIL_IDX = 7; // Vermelho

    // ─── Evento ──────────────────────────────────────────────────────────────
    public static event Action OnColorsChanged;

    public static SignalColorManager Instance { get; private set; }

    // ─── Propriedades públicas ───────────────────────────────────────────────
    public Color ColorJ { get; private set; }
    public Color ColorK { get; private set; }
    public Color ColorCLK { get; private set; }
    public Color ColorPreset { get; private set; }
    public Color ColorClear { get; private set; }
    public Color ColorFeedbackSuccess { get; private set; }
    public Color ColorFeedbackFailure { get; private set; }

    // CUSTOM_INDEX (-1) quando a cor atual é customizada
    public int IndexJ { get; private set; }
    public int IndexK { get; private set; }
    public int IndexCLK { get; private set; }
    public int IndexPreset { get; private set; }
    public int IndexClear { get; private set; }
    public int IndexFeedbackSuccess { get; private set; }
    public int IndexFeedbackFailure { get; private set; }

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

    /// <summary>
    /// Aplica uma paleta de acessibilidade nos sinais J, K e CLK.
    /// Preset, Clear e Feedback não são afetados pelas paletas.
    /// </summary>
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

    #region Restaurar Padrão

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

    #region Internos

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

    #region Salvar / Carregar

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

    private void NotifyAndSave()
    {
        PlayerPrefs.Save();
        OnColorsChanged?.Invoke();
    }

    /// <summary>
    /// Compara os índices atuais com as paletas registradas e retorna o índice da paleta ativa.
    /// Se as cores não baterem com nenhuma paleta oficial, retorna -1 (Personalizado).
    /// </summary>
    public int GetCurrentPaletteIndex()
    {
        for (int i = 0; i < Palettes.Length; i++)
        {
            int[] palette = Palettes[i];

            // Checagem seguindo a ordem exata de atribuição do ApplyPalette
            if (palette[0] == IndexJ &&
                palette[1] == IndexK &&
                palette[2] == IndexPreset &&
                palette[3] == IndexClear &&
                palette[4] == IndexCLK &&
                palette[5] == IndexFeedbackSuccess &&
                palette[6] == IndexFeedbackFailure)
            {
                return i; // Encontrou a paleta correspondente
            }
        }

        return CUSTOM_INDEX; // Retorna -1 se for uma combinação personalizada
    }

    #endregion
}