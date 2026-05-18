using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
public class BinaryBoolArrayConverter : JsonConverter<bool[]>
{
    public override bool[] ReadJson(JsonReader reader, Type objectType, bool[] existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        string data = (string)reader.Value;
        if (string.IsNullOrWhiteSpace(data)) return null;

        data = data.Replace(" ", ""); // Remove os espaços ("1011 00" vira "101100")
        bool[] result = new bool[data.Length];
        for (int i = 0; i < data.Length; i++) result[i] = data[i] == '1';
        return result;
    }
    public override void WriteJson(JsonWriter writer, bool[] value, JsonSerializer serializer) => throw new NotImplementedException();
}

public class UnityColorConverter : JsonConverter<Color>
{
    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        string hex = (string)reader.Value;
        if (string.IsNullOrWhiteSpace(hex)) return Color.white;

        if (!hex.StartsWith("#")) hex = "#" + hex;
        if (ColorUtility.TryParseHtmlString(hex, out Color color)) return color;
        return Color.white;
    }
    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer) => throw new NotImplementedException();
}

public class AsyncActiveConverter : JsonConverter<int>
{
    public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            string val = ((string)reader.Value).ToLower().Trim();
            return (val == "high") ? 1 : 0;
        }
        if (reader.TokenType == JsonToken.Integer) return Convert.ToInt32(reader.Value);
        return 1;
    }
    public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer) => throw new NotImplementedException();
}

// ---------------------------------------------------------
// CLASSE DE DADOS (DTO)
// ---------------------------------------------------------

[Serializable]
public class LevelData
{
    [JsonProperty("levelName")]
    public string LevelName { get; set; }

    [JsonProperty("clockCycles")]
    public int ClockCycles { get; set; }

    [JsonProperty("clockCicles")] // Fallback para JSONs antigos com erro de digitação
    private int ClockCiclesLegacy { set => ClockCycles = ClockCycles > 0 ? ClockCycles : value; }

    [JsonProperty("asyncActive")]
    [JsonConverter(typeof(AsyncActiveConverter))]
    public int AsyncActive { get; set; } = 1;

    [JsonProperty("activeClockEdge")]
    public string ActiveClockEdge { get; set; } = "falling";

    [JsonProperty("jSignal")]
    [JsonConverter(typeof(BinaryBoolArrayConverter))]
    public bool[] JSignal { get; set; }

    [JsonProperty("kSignal")]
    [JsonConverter(typeof(BinaryBoolArrayConverter))]
    public bool[] KSignal { get; set; }

    [JsonProperty("presetSignal")]
    [JsonConverter(typeof(BinaryBoolArrayConverter))]
    public bool[] PresetSignal { get; set; }

    [JsonProperty("clearSignal")]
    [JsonConverter(typeof(BinaryBoolArrayConverter))]
    public bool[] ClearSignal { get; set; }

    [JsonProperty("jSignalColor")]
    [JsonConverter(typeof(UnityColorConverter))]
    public Color JSignalColor { get; set; } = Color.white;

    [JsonProperty("kSignalColor")]
    [JsonConverter(typeof(UnityColorConverter))]
    public Color KSignalColor { get; set; } = Color.white;

    [JsonProperty("presetSignalColor")]
    [JsonConverter(typeof(UnityColorConverter))]
    public Color PresetSignalColor { get; set; } = Color.white;

    [JsonProperty("clearSignalColor")]
    [JsonConverter(typeof(UnityColorConverter))]
    public Color ClearSignalColor { get; set; } = Color.white;

    [JsonProperty("clockSignalColor")]
    [JsonConverter(typeof(UnityColorConverter))]
    public Color ClockSignalColor { get; set; } = Color.white;

    [JsonProperty("floor")]
    [JsonConverter(typeof(BinaryBoolArrayConverter))]
    public bool[] Floor { get; set; }

    [JsonProperty("ceiling")]
    [JsonConverter(typeof(BinaryBoolArrayConverter))]
    public bool[] Ceiling { get; set; }

    [JsonProperty("obstacles")]
    public List<ObstacleSpawner.ObstacleData> Obstacles { get; set; }
}