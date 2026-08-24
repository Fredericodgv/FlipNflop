using UnityEngine;

public class TutorialObservacional : ITutorialStrategy
{
    private TutorialData _dados;
    private System.Action _aoConcluir;
    private float _tempoInicio;
    private bool _ativo;

    public void Iniciar(TutorialData dados, System.Action aoConcluir)
    {
        _dados = dados;
        _aoConcluir = aoConcluir;
        _tempoInicio = Time.unscaledTime; // unscaled: continua contando mesmo com timeScale = 0
        _ativo = true;
        // TutorialUI.Instance.Mostrar(dados);
    }

    public void Atualizar()
    {
        if (!_ativo) return;

        bool passouTempoMinimo = Time.unscaledTime - _tempoInicio >= _dados.duracaoMinima;
        bool pediuDismiss = _dados.dismissComQualquerTecla && Input.anyKeyDown;

        if (passouTempoMinimo && pediuDismiss)
        {
            _ativo = false;
            _aoConcluir?.Invoke();
        }
    }

    public void Finalizar()
    {
        // TutorialUI.Instance.Esconder();
    }
}
