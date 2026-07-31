using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gera o gabarito (lista de quinas corretas) a partir dos eventos de saída do flip-flop.
///
/// ENTRADA:
///   - lowY / highY           : alturas mundo para nível lógico 0 e 1
///   - LevelJsonLoader        : encontrado na cena, fornece os eventos via
///                              ComputeOutputEventsFromParsedSignals()
///   - LevelManager.phaseEndX : limite direito da fase
///
/// SAÍDA:
///   - List<Vector3> CorrectCorners : lista de pontos (quinas) que definem o caminho correto,
///                                    em ordem de X crescente, sem duplicatas
/// </summary>
public class GabaritoGenerator
{
    public readonly float LowY;
    public readonly float HighY;

    /// <summary>
    /// Lista de quinas geradas pelo último <see cref="Generate"/> bem-sucedido.
    /// Nunca é null após a construção — pode estar vazia se o loader não for encontrado.
    /// </summary>
    public List<Vector3> CorrectCorners { get; private set; } = new();

    public GabaritoGenerator(float lowY, float highY)
    {
        LowY = lowY;
        HighY = highY;
    }

    /// <summary>
    /// Busca o <see cref="LevelJsonLoader"/> na cena e gera as quinas do gabarito.
    /// Deve ser chamado uma única vez no Awake do PathVerifier.
    /// </summary>
    public void Generate()
    {
        var loader = Object.FindAnyObjectByType<LevelJsonLoader>();
        if (loader == null)
        {
            Debug.LogError("[GabaritoGenerator] LevelJsonLoader não encontrado na cena.");
            CorrectCorners = new List<Vector3>();
            return;
        }

        var events = loader.ComputeOutputEventsFromParsedSignals();
        BuildFromEvents(events, initialX: 0f, initialState: false);
    }

    /// <summary>
    /// Reconstrói as quinas a partir de uma lista de eventos de sinal já calculada.
    /// Pode ser chamado externamente para testes ou pré-visualização no Editor.
    ///
    /// ENTRADA:
    ///   events        — lista de (x, value) representando cada transição de saída do flip-flop
    ///   initialX      — X de início do caminho (normalmente 0)
    ///   initialState  — estado lógico inicial da saída (false = LOW, true = HIGH)
    ///
    /// SAÍDA (efeito colateral):
    ///   CorrectCorners é sobrescrito com a nova lista de quinas
    /// </summary>
    public void BuildFromEvents(
        List<PathVerifier.SignalEvent> events,
        float initialX = 0f,
        bool initialState = false)
    {
        CorrectCorners = new List<Vector3>();
        bool qState = initialState;
        float startY = qState ? HighY : LowY;

        CorrectCorners.Add(new Vector3(initialX, startY, 0f));

        if (events == null || events.Count == 0)
        {
            float phaseEnd = LevelManager.Instance != null ? LevelManager.Instance.phaseEndX : 0f;
            CorrectCorners.Add(new Vector3(phaseEnd, startY, 0f));
            return;
        }

        events.Sort((a, b) => a.x.CompareTo(b.x));

        foreach (var ev in events)
        {
            float previousY = qState ? HighY : LowY;
            CorrectCorners.Add(new Vector3(ev.x, previousY, 0f));

            if (ev.value != qState)
            {
                float currentY = ev.value ? HighY : LowY;
                CorrectCorners.Add(new Vector3(ev.x, currentY, 0f));
                qState = ev.value;
            }
        }

        float endX = LevelManager.Instance != null
            ? LevelManager.Instance.phaseEndX
            : events[events.Count - 1].x;
        CorrectCorners.Add(new Vector3(endX, qState ? HighY : LowY, 0f));

        RemoveDuplicatesAndCollinear();
    }

    // ── Internos ───────────────────────────────────────────────────────────────

    private void RemoveDuplicatesAndCollinear()
    {
        for (int i = CorrectCorners.Count - 1; i > 0; i--)
        {
            if (Vector3.Distance(CorrectCorners[i], CorrectCorners[i - 1]) < 0.01f)
            {
                CorrectCorners.RemoveAt(i);
                continue;
            }

            if (i == 0 || i >= CorrectCorners.Count - 1) continue;

            if (CorrectCorners[i - 1].y == CorrectCorners[i].y &&
                CorrectCorners[i + 1].y == CorrectCorners[i].y)
            {
                CorrectCorners.RemoveAt(i);
            }
        }
    }
}
