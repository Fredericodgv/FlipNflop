using UnityEngine.UIElements;

/// <summary>
/// Defines the contract that each settings tab module implementation must fulfill.
/// Interacts with <see cref="ConfigManager"/>, which delegates tab initialization, event handling, and localization updates to implementations of this interface.
/// </summary>
public interface ISettingsTab
{
    /// <summary>
    /// Caches UI Toolkit elements from the root hierarchy and initializes internal state.
    /// Interacts with <see cref="VisualElement"/>.
    /// </summary>
    /// <param name="root">The root <see cref="VisualElement"/> container of the options menu.</param>
    void Init(VisualElement root);

    /// <summary>
    /// Registers event callbacks for user interaction controls.
    /// </summary>
    void RegisterCallbacks();

    /// <summary>
    /// Unregisters event callbacks to prevent memory leaks and dangling references.
    /// </summary>
    void UnregisterCallbacks();

    /// <summary>
    /// Called when the active localization locale changes.
    /// Implementations update localized text labels or UI options as needed.
    /// Interacts with Unity Localization system.
    /// </summary>
    void OnLocaleChanged();
}
