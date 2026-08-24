using UnityEngine;

public enum TipoTutorial { Interativo, Observacional }

[CreateAssetMenu(fileName = "NovoTutorial", menuName = "Tutoriais/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    [Header("Identificação")]
    public string id;                    // usado como chave no JSON e na API de save
    public string titulo;

    [Header("Comportamento")]
    public TipoTutorial tipo;

    [Header("Conteúdo")]
    [TextArea] public string texto;
    public Sprite icone;

    [Header("Interativo (se aplicável)")]
    public KeyCode teclaAlvo;

    [Header("Observacional (se aplicável)")]
    public float duracaoMinima = 2f;
    public bool dismissComQualquerTecla = true;
}
