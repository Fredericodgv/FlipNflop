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
        if (Levels == null || Levels.Length == 0)
            return false;

        return CurrentLevelIndex + 1 < Levels.Length;
    }
}