using UnityEngine;

public class TutorialInterativo : ITutorialStrategy
{
    private TutorialData _dados;
    private System.Action _aoConcluir;
    private bool _ativo;

    public void Iniciar(TutorialData dados, System.Action aoConcluir)
    {
        _dados = dados;
        _aoConcluir = aoConcluir;
        _ativo = true;
        // TutorialUI.Instance.Mostrar(dados);
    }

    public void Atualizar()
    {
        if (!_ativo) return;

        if (Input.GetKeyDown(_dados.teclaAlvo))
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
