using UnityEngine;
using TMPro;

/// <summary>
/// Manages level timer tracking, score calculation based on accuracy and elapsed time, and UI display updates.
/// Interacts with <see cref="TextMeshProUGUI"/> components for UI output and <see cref="ResultScreenController"/> to set end-of-level results.
/// </summary>
public class ScoreController : MonoBehaviour
{
    #region Singleton & Fields

    /// <summary>
    /// Singleton instance reference for global access.
    /// </summary>
    public static ScoreController Instance { get; private set; }

    [Header("In-Game UI")]
    [Tooltip("TMP label showing the running timer during gameplay.")]
    public TextMeshProUGUI timerText;

    [Tooltip("TMP label showing the final score after the level ends.")]
    public TextMeshProUGUI scoreText;

    [Header("Score Settings")]
    [Tooltip("Maximum possible score (achieved with 100% accuracy in 0 seconds).")]
    [SerializeField] private int maxScore = 1000;

    private float elapsedTime;
    private bool isRunning;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes singleton instance.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Hides score text display if present and starts the timer.
    /// </summary>
    private void Start()
    {
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        StartTimer();
    }

    /// <summary>
    /// Advances timer each frame while active and updates timer UI.
    /// </summary>
    private void Update()
    {
        if (!isRunning) return;
        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }

    #endregion

    #region Timer Control

    /// <summary>
    /// Resets elapsed time to zero and activates the timer.
    /// </summary>
    public void StartTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
    }

    /// <summary>
    /// Pauses timer progression.
    /// </summary>
    public void StopTimer() => isRunning = false;

    /// <summary>
    /// Gets the current elapsed level time in seconds.
    /// </summary>
    /// <returns>Elapsed time in seconds.</returns>
    public float GetElapsedTime() => elapsedTime;

    #endregion

    #region Score Evaluation & Reporting

    /// <summary>
    /// Stops the timer, computes the final score based on segment accuracy and time, and reports results to <see cref="ResultScreenController"/>.
    /// </summary>
    /// <param name="correctSegments">Number of path segments the player drew correctly.</param>
    /// <param name="gabaritoTotal">Total number of segments in the reference path.</param>
    /// <param name="success">Whether the overall level attempt was successful.</param>
    public void ReportResult(int correctSegments, int gabaritoTotal, bool success)
    {
        StopTimer();

        int score = ComputeScore(correctSegments, gabaritoTotal, elapsedTime);
        int misses = gabaritoTotal - correctSegments;
        if (misses < 0) misses = 0;

        int m = Mathf.FloorToInt(elapsedTime / 60f);
        int s = Mathf.FloorToInt(elapsedTime % 60f);
        string timeString = $"{m:00}:{s:00}";

        Debug.Log($"[ScoreController] correct:{correctSegments}/{gabaritoTotal} | time:{timeString} | score:{score}");

        ResultScreenController.Instance.SetResultData(
            score.ToString(),
            timeString
        );
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Updates the TMP text component with formatted MM:SS elapsed time.
    /// Interacts with <see cref="TextMeshProUGUI"/>.
    /// </summary>
    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        int m = Mathf.FloorToInt(elapsedTime / 60f);
        int s = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = $"{m:00}:{s:00}";
    }

    /// <summary>
    /// Calculates the final score value based on accuracy ratio minus elapsed seconds penalty.
    /// score = max(0, round(accuracyRatio * maxScore) - floor(elapsedSeconds))
    /// </summary>
    /// <param name="correct">Number of correct segments.</param>
    /// <param name="total">Total segments.</param>
    /// <param name="seconds">Total elapsed time in seconds.</param>
    /// <returns>Final calculated score integer.</returns>
    private int ComputeScore(int correct, int total, float seconds)
    {
        if (total <= 0) return 0;
        float accuracy = (float)correct / total;
        int accuracyPart = Mathf.RoundToInt(accuracy * maxScore);
        int timePenalty = Mathf.FloorToInt(seconds);
        return Mathf.Max(0, accuracyPart - timePenalty);
    }

    #endregion
}