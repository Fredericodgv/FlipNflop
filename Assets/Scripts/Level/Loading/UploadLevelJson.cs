/// <summary>
/// Static class to hold the content of an uploaded level JSON. This allows the content to be accessed across different scenes without needing to pass it through scene management or use a more complex data persistence solution.
/// </summary>

public static class UploadedLevelJson
{
    public static string Content { get; set; }
}