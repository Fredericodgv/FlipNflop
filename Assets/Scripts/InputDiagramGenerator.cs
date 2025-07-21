using UnityEngine;
using UnityEngine.Tilemaps;

public class InputDiagramGenerator : MonoBehaviour
{
    [Header("Referências dos Tilemaps")]
    [SerializeField] private Tilemap j_InputTilemap;
    [SerializeField] private Tilemap k_InputTilemap;

    // --- MUDANÇA AQUI ---
    [Header("Arquivo de Configuração do Nível")]
    [Tooltip("Arraste aqui o arquivo .json da fase atual.")]
    [SerializeField] private TextAsset levelFile; // Agora temos apenas um arquivo

    [Header("Referências dos Tiles")]
    [SerializeField] private TileBase tile_0_Normal;
    [SerializeField] private TileBase tile_1_Rising;
    [SerializeField] private TileBase tile_2_Falling;
    [SerializeField] private TileBase tile_3_High;

    [Header("Configuração de Posição")]
    [SerializeField] private int startX = -5;
    [SerializeField] private int j_YRow = 8;
    [SerializeField] private int k_YRow = 5;

    void Awake()
    {
        if (!ValidateReferences()) return;

        j_InputTilemap.ClearAllTiles();
        k_InputTilemap.ClearAllTiles();

        // --- MUDANÇA AQUI ---
        // Lê e decodifica o arquivo JSON
        LevelData levelData = JsonUtility.FromJson<LevelData>(levelFile.text);

        // Converte as strings do JSON em arrays de booleanos
        bool[] j_signal = ParseInputString(levelData.j_signal);
        bool[] k_signal = ParseInputString(levelData.k_signal);

        // Gera os diagramas
        GenerateDiagram(j_InputTilemap, j_signal, j_YRow);
        GenerateDiagram(k_InputTilemap, k_signal, k_YRow);
    }

    // O resto do script (GenerateDiagram, ParseInputString, ValidateReferences)
    // pode continuar exatamente o mesmo da versão anterior.

    private void GenerateDiagram(Tilemap targetMap, bool[] signal, int yRow)
    {
        if (signal == null) return;

        for (int i = 0; i < signal.Length; i++)
        {
            TileBase tileToPlace = null;
            bool currentState = signal[i];

            if (currentState)
            {
                tileToPlace = tile_3_High;
            }
            else
            {
                bool isAfterHigh = (i > 0 && signal[i - 1]);
                bool isBeforeHigh = (i < signal.Length - 1 && signal[i + 1]);

                if (isBeforeHigh) { tileToPlace = tile_1_Rising; }
                else if (isAfterHigh) { tileToPlace = tile_2_Falling; }
                else { tileToPlace = tile_0_Normal; }
            }
            
            Vector3Int cellPosition = new Vector3Int(startX + i, yRow, 0);
            targetMap.SetTile(cellPosition, tileToPlace);
        }
    }

    private bool[] ParseInputString(string data)
    {
        if (string.IsNullOrEmpty(data)) return null;

        var signalList = new System.Collections.Generic.List<bool>();
        foreach (char c in data)
        {
            if (c == '1') { signalList.Add(true); }
            else if (c == '0') { signalList.Add(false); }
        }
        return signalList.ToArray();
    }

    private bool ValidateReferences()
    {
        if(j_InputTilemap == null || k_InputTilemap == null || levelFile == null || // Alterado aqui
           tile_0_Normal == null || tile_1_Rising == null || tile_2_Falling == null || tile_3_High == null)
        {
            Debug.LogError("ERRO: Uma ou mais referências não foram definidas no Inspector do InputDiagramGenerator!");
            return false;
        }
        return true;
    }
}