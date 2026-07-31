using UnityEngine.UIElements;

/// <summary>
/// Contrato que cada aba de configuração deve implementar.
/// O ConfigManager delega toda a lógica específica para estas implementações.
/// </summary>
public interface ISettingsTab
{
    /// <summary>
    /// Faz cache dos elementos da UI e inicializa o estado.
    /// </summary>
    void Init(VisualElement root);

    /// <summary>
    /// Registra os callbacks de interação.
    /// </summary>
    void RegisterCallbacks();

    /// <summary>
    /// Remove callbacks registrados.
    /// </summary>
    void UnregisterCallbacks();

    /// <summary>
    /// Chamado quando o idioma ativo muda.
    /// Implementações podem ignorar se não possuem textos localizados.
    /// </summary>
    void OnLocaleChanged();
}
