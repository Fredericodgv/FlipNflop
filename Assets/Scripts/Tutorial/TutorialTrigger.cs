using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [Tooltip("Hoje: arraste manualmente pra testar. No futuro: setado via código a partir do id do JSON.")]
    public TutorialData tutorial;

    [Tooltip("Se true, o trigger se destrói após disparar uma vez.")]
    public bool dispararUmaVez = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        TutorialManager.Instance.ExecutarTutorial(tutorial);

        if (dispararUmaVez) Destroy(gameObject);
    }

    /// Método auxiliar pensado pro LevelManager usar quando instanciar via JSON:
    /// cria o GameObject, adiciona o collider e configura tudo num só lugar.
    public static TutorialTrigger CriarNaPosicao(TutorialData dados, Vector3 posicaoMundo)
    {
        var obj = new GameObject($"TutorialTrigger_{dados.id}");
        obj.transform.position = posicaoMundo;

        var collider = obj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        var trigger = obj.AddComponent<TutorialTrigger>();
        trigger.tutorial = dados;

        return trigger;
    }
}
