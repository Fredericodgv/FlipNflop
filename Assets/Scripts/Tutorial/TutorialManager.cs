using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private TutorialCatalog catalogo;

    private ITutorialStrategy _estrategiaAtual;
    private readonly HashSet<string> _concluidos = new(); // no futuro, populado pela API de save

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        _estrategiaAtual?.Atualizar();
    }

    /// Ponto de entrada usado pelo TutorialTrigger.
    public void ExecutarTutorial(TutorialData dados)
    {
        if (dados == null) return;
        if (_concluidos.Contains(dados.id)) return; // não repete se já visto
        if (_estrategiaAtual != null) return;        // evita sobrepor tutoriais

        Time.timeScale = 0f;
        _estrategiaAtual = dados.tipo == TipoTutorial.Interativo
            ? new TutorialInterativo()
            : new TutorialObservacional();

        _estrategiaAtual.Iniciar(dados, () => ConcluirTutorial(dados));
    }

    /// Sobrecarga que já pensa no futuro fluxo via JSON: resolve o id pelo catálogo.
    public void ExecutarTutorialPorId(string id)
    {
        ExecutarTutorial(catalogo.ObterPorId(id));
    }

    private void ConcluirTutorial(TutorialData dados)
    {
        _estrategiaAtual.Finalizar();
        _estrategiaAtual = null;
        _concluidos.Add(dados.id);
        Time.timeScale = 1f;
        // TODO: notificar API de save que dados.id foi concluído
    }
}
