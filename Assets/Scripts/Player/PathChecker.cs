using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resultado imutável produzido pelo <see cref="PathChecker.Evaluate"/>.
/// </summary>
public readonly struct PathCheckResult
{
    /// <summary>true quando todas as quinas do gabarito foram atingidas.</summary>
    public readonly bool IsCorrect;

    /// <summary>Número de segmentos do gabarito cobertos pelo caminho do jogador.</summary>
    public readonly int CoveredSegments;

    /// <summary>Total de segmentos do gabarito (= correctCorners.Count - 1).</summary>
    public readonly int GabaritoTotal;

    /// <summary>Quinas do gabarito que o jogador não atingiu.</summary>
    public readonly IReadOnlyList<Vector3> MissedCorners;

    /// <summary>Resultado booleano por quina: true = acertou, false = errou.</summary>
    public readonly IReadOnlyList<bool> CornerChecks;

    public PathCheckResult(
        bool isCorrect,
        int coveredSegments,
        int gabaritoTotal,
        List<Vector3> missedCorners,
        List<bool> cornerChecks)
    {
        IsCorrect = isCorrect;
        CoveredSegments = coveredSegments;
        GabaritoTotal = gabaritoTotal;
        MissedCorners = missedCorners.AsReadOnly();
        CornerChecks = cornerChecks.AsReadOnly();
    }
}

/// <summary>
/// Avalia se o caminho do jogador corresponde ao gabarito.
///
/// ENTRADA:
///   - List<Vector3> correctCorners : gabarito gerado pelo <see cref="GabaritoGenerator"/>
///   - List<Vector3> playerPath     : pontos gravados pelo SignalPath do jogador
///   - float cornerTolerance        : raio máximo (unidades mundo) para considerar uma quina atingida
///
/// SAÍDA:
///   - <see cref="PathCheckResult"/> : struct com todos os dados de resultado
///
/// Esta classe não possui estado mutável: cada chamada a <see cref="Evaluate"/> é independente.
/// </summary>
public class PathChecker
{
    private readonly float cornerTolerance;
    private readonly bool enableDebugLogs;

    public PathChecker(float cornerTolerance, bool enableDebugLogs = false)
    {
        this.cornerTolerance = cornerTolerance;
        this.enableDebugLogs = enableDebugLogs;
    }

    /// <summary>
    /// Avalia o caminho do jogador contra o gabarito.
    ///
    /// ENTRADA:
    ///   correctCorners — quinas corretas geradas pelo GabaritoGenerator
    ///   playerPath     — pontos do trajeto desenhado pelo jogador
    ///
    /// SAÍDA:
    ///   PathCheckResult com IsCorrect, CoveredSegments, GabaritoTotal, MissedCorners e CornerChecks
    /// </summary>
    public PathCheckResult Evaluate(List<Vector3> correctCorners, List<Vector3> playerPath)
    {
        var missedCorners = new List<Vector3>();
        var cornerChecks = new List<bool>();

        if (enableDebugLogs)
        {
            Debug.Log("<color=cyan>[PathChecker] Iniciando verificação do caminho</color>");
            Debug.Log($"  Pontos do jogador: {playerPath.Count}");
            Debug.Log($"  Quinas do gabarito: {correctCorners.Count}");
            Debug.Log($"  Tolerância: {cornerTolerance}");
        }

        int cornerIndex = 0;
        foreach (Vector3 correctCorner in correctCorners)
        {
            bool hit = EvaluateCornerHit(correctCorner, playerPath, out float minDist, out Vector3 closest);
            cornerChecks.Add(hit);

            if (!hit)
            {
                missedCorners.Add(correctCorner);
                if (enableDebugLogs)
                    Debug.LogWarning($"  <color=red>✗ Quina #{cornerIndex} PERDIDA:</color> Pos={correctCorner} | Distância mínima={minDist:F2} | Ponto mais próximo={closest}");
            }
            else if (enableDebugLogs)
            {
                Debug.Log($"  <color=green>✓ Quina #{cornerIndex} OK:</color> Pos={correctCorner} | Distância={minDist:F2}");
            }

            cornerIndex++;
        }

        bool isCorrect = !cornerChecks.Contains(false);
        int gabaritoTotal = correctCorners.Count - 1;
        int coveredSegments = CountCoveredSegments(correctCorners, playerPath);

        if (enableDebugLogs)
        {
            int correct = cornerChecks.FindAll(x => x).Count;
            Debug.Log($"<color=yellow>[PathChecker] Resultado: {correct}/{cornerChecks.Count} quinas atingidas</color>");
        }

        return new PathCheckResult(isCorrect, coveredSegments, gabaritoTotal, missedCorners, cornerChecks);
    }

    // ── Internos ───────────────────────────────────────────────────────────────

    /// <summary>
    /// ENTRADA: uma quina do gabarito + o caminho completo do jogador
    /// SAÍDA:   wasHit (bool), minDistance (float), closestPoint (Vector3)
    /// </summary>
    private bool EvaluateCornerHit(
        Vector3 corner,
        List<Vector3> playerPath,
        out float minDistance,
        out Vector3 closestPoint)
    {
        minDistance = float.MaxValue;
        closestPoint = Vector3.zero;
        bool wasHit = false;

        for (int i = 0; i < playerPath.Count - 1; i++)
        {
            Vector3 closest = FindClosestPointOnSegment(corner, playerPath[i], playerPath[i + 1]);
            float distance = Vector3.Distance(closest, corner);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = closest;
            }

            if (distance <= cornerTolerance)
            {
                wasHit = true;
                break;
            }
        }

        return wasHit;
    }

    /// <summary>
    /// ENTRADA: gabarito + caminho do jogador
    /// SAÍDA:   número de segmentos do gabarito cujo ponto médio está a ≤ cornerTolerance do caminho do jogador
    /// </summary>
    private int CountCoveredSegments(List<Vector3> correctCorners, List<Vector3> playerPath)
    {
        int covered = 0;
        for (int i = 0; i < correctCorners.Count - 1; i++)
        {
            Vector3 midpoint = (correctCorners[i] + correctCorners[i + 1]) / 2f;
            Vector3 closest = FindClosestPointOnFullPath(midpoint, playerPath);
            if (Vector3.Distance(midpoint, closest) <= cornerTolerance)
                covered++;
        }
        return covered;
    }

    private Vector3 FindClosestPointOnFullPath(Vector3 target, List<Vector3> path)
    {
        if (path.Count == 0) return Vector3.zero;
        if (path.Count == 1) return path[0];

        Vector3 best = path[0];
        float bestSqr = (best - target).sqrMagnitude;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 candidate = FindClosestPointOnSegment(target, path[i], path[i + 1]);
            float sqr = (candidate - target).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = candidate; }
        }
        return best;
    }

    private static Vector3 FindClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 dir = b - a;
        float lenSqr = dir.sqrMagnitude;
        if (lenSqr < 0.0001f) return a;
        float t = Mathf.Clamp01(Vector3.Dot(point - a, dir) / lenSqr);
        return a + dir * t;
    }
}
