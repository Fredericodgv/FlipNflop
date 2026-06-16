using UnityEngine;

public static class LevelSequenceManager
{

    public static readonly string[] Levels = new string[]
    {
        "first_level", "second_level", "third_level", "fourth_level", "fifth_level",
        "sixth_level", "seventh_level", "eighth_level", "ninth_level"
    };

    public static int CurrentLevelIndex { get; set; } = 0;

    public static bool HasNextLevel()
    {
        return CurrentLevelIndex + 1 < Levels.Length;
    }
}