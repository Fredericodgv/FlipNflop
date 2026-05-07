using UnityEngine;
using TMPro;

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
    private float elapsedTime;
    private bool isRunning;


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

    /// <summary>
    /// Calculates de final score
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