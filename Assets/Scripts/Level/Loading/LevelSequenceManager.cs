using UnityEngine;

/// <summary>
/// Static manager handling sequential level loading progression.
/// Used by level completion flow and navigation UI components to track level indices and check for remaining levels.
/// </summary>
public static class LevelSequenceManager
{
    #region Fields & Properties

    /// <summary>
    /// Ordered sequence list of level asset names to load.
    /// </summary>
    public static readonly string[] Levels = new string[]
    {
        "first_level", "second_level", "third_level", "fourth_level", "fifth_level",
        "sixth_level", "seventh_level", "eighth_level", "ninth_level"
    };

    /// <summary>
    /// Gets or sets the zero-based index of the currently active level.
    /// </summary>
    public static int CurrentLevelIndex { get; set; } = 0;

    #endregion

    #region Public API

    /// <summary>
    /// Checks whether there is another level following <see cref="CurrentLevelIndex"/> in <see cref="Levels"/>.
    /// </summary>
    /// <returns>True if a next level exists; otherwise, false.</returns>
    public static bool HasNextLevel()
    {
        if (Levels == null || Levels.Length == 0)
            return false;

        return CurrentLevelIndex + 1 < Levels.Length;
    }

    #endregion
}