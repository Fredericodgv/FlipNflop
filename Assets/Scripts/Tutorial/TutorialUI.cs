using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TutorialUI : MonoBehaviour
{
    public static TutorialUI Instance { get; private set; }

    private VisualElement _container;
    private Label _texto;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var root = GetComponent<UIDocument>().rootVisualElement;
        _container = root.Q<VisualElement>("TutorialContainer");
        _texto = root.Q<Label>("TutorialText");
    }

    public void Mostrar(TutorialData dados)
    {
        if (_container == null || dados == null) return;

        _texto.text = MontarTexto(dados);
        _container.style.display = DisplayStyle.Flex;
    }

    public void Esconder()
    {
        if (_container == null) return;
        _container.style.display = DisplayStyle.None;
    }

    private string MontarTexto(TutorialData dados)
    {
        if (string.IsNullOrEmpty(dados.texto)) return "";

        if (dados.tipo == TipoTutorial.Interativo)
        {
            return dados.texto.Replace("{TECLA}", NomeAmigavel(dados.teclaAlvo));
        }

        return dados.texto;
    }

    private string NomeAmigavel(KeyCode tecla)
    {
        switch (tecla)
        {
            case KeyCode.Space: return "ESPAÇO";
            case KeyCode.LeftShift:
            case KeyCode.RightShift: return "SHIFT";
            case KeyCode.LeftControl:
            case KeyCode.RightControl: return "CTRL";
            case KeyCode.UpArrow: return "SETA PRA CIMA";
            case KeyCode.DownArrow: return "SETA PRA BAIXO";
            case KeyCode.LeftArrow: return "SETA PRA ESQUERDA";
            case KeyCode.RightArrow: return "SETA PRA DIREITA";
            default: return tecla.ToString().ToUpper();
        }
    }
}
