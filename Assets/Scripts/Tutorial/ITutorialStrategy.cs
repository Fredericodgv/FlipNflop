public interface ITutorialStrategy
{
    void Iniciar(TutorialData dados, System.Action aoConcluir);
    void Atualizar();
    void Finalizar();
}
