using UnityEngine;
using TMPro;

/// <summary>
/// Owns the in-game timer and computes the final score.
///
/// Formula:
///   score = max(0, round(accuracyRatio * maxScore) - floor(elapsedTime))
///
/// Where accuracyRatio = correctSegments / totalSegments (provided by PathVerifier).
/// Each elapsed second deducts 1 point from the accuracy-based maximum.
/// maxScore is configurable in the Inspector (default: 1000).
///
/// INTEGRATION:
///   1. Add this component to a GameObject in each level scene.
///   2. Assign timerText and scoreText in the Inspector.
///   3. In PathVerifier.DrawFeedbackLines(), call:
///        ScoreController.Instance?.ReportResult(correctSegments, totalSegments);
/// </summary>
public class ScoreController : MonoBehaviour
{
    public static ScoreController Instance { get; private set; }

    [Header("In-Game UI")]
    [Tooltip("TMP label showing the running timer during gameplay.")]
    public TextMeshProUGUI timerText;

    [Tooltip("TMP label showing the final score after the level ends.")]
    public TextMeshProUGUI scoreText;

    [Header("Score Settings")]
    [Tooltip("Maximum possible score (achieved with 100% accuracy in 0 seconds).")]
    [SerializeField] private int maxScore = 1000;

    // -------------------------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------------------------

    private float elapsedTime;
    private bool isRunning;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        StartTimer();
    }

    private void Update()
    {
        if (!isRunning) return;
        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }

    // -------------------------------------------------------------------------
    // Timer
    // -------------------------------------------------------------------------

    public void StartTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
    }

    public void StopTimer() => isRunning = false;
    public float GetElapsedTime() => elapsedTime;

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        int m = Mathf.FloorToInt(elapsedTime / 60f);
        int s = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = $"{m:00}:{s:00}";
    }

    // -------------------------------------------------------------------------
    // Score — called by PathVerifier
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stops the timer, computes and displays the final score.
    /// </summary>
    /// <param name="correctSegments">Number of path segments the player drew correctly.</param>
    /// <param name="gabaritoTotal">Total number of segments in the reference path.</param>
    public void ReportResult(int correctSegments, int gabaritoTotal)
    {
        StopTimer();

        int score = ComputeScore(correctSegments, gabaritoTotal, elapsedTime);

        Debug.Log($"[ScoreController] correct:{correctSegments}/{gabaritoTotal} | time:{elapsedTime:F1}s | score:{score}");

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            string resultado = correctSegments >= gabaritoTotal ? "Diagrama Correto!" : "Diagrama Incorreto";
            scoreText.text = $"{resultado}\nPontuação: {score}";
        }
    }

    // -------------------------------------------------------------------------
    // Formula
    // -------------------------------------------------------------------------

    /// <summary>
    /// score = max(0, round(accuracyRatio * maxScore) - floor(elapsedSeconds))
    /// </summary>
    private int ComputeScore(int correct, int total, float seconds)
    {
        if (total <= 0) return 0;
        float accuracy = (float)correct / total;
        int accuracyPart = Mathf.RoundToInt(accuracy * maxScore);
        int timePenalty = Mathf.FloorToInt(seconds);
        return Mathf.Max(0, accuracyPart - timePenalty);
    }
}