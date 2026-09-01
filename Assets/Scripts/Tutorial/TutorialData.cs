using UnityEngine;

public enum TipoTutorial { Interativo, Observacional }

[CreateAssetMenu(fileName = "NovoTutorial", menuName = "Tutoriais/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    [Header("Identificação")]
    public string id;
    public string titulo;

    [Header("Exibição")]
    [Tooltip("Interativo mostra a tecla-alvo como prompt visual; Observacional mostra só texto/ícone. " +
             "Não afeta mais a lógica de conclusão — ambos concluem ao sair da zona.")]
    public TipoTutorial tipo;

    [TextArea] public string texto;
    // public Sprite icone;

    [Tooltip("Usado apenas como prompt visual quando tipo = Interativo (ex: ícone da tecla Space).")]
    public KeyCode teclaAlvo;
}
