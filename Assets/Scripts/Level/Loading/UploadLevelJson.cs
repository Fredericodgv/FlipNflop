/// <summary>
/// Static class storing raw content of an uploaded custom level JSON file.
/// Allows cross-scene data transfer without requiring scene parameter passing or persistent storage.
/// Read and consumed by <see cref="LevelJsonLoader"/> during level initialization.
/// </summary>
public static class UploadedLevelJson
{
    #region Public API

    /// <summary>
    /// Gets or sets the raw JSON string content of the uploaded custom level.
    /// </summary>
    public static string Content { get; set; }

    #endregion
}