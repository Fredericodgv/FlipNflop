using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Define uma ação configurável no menu de rebind.
/// </summary>
[System.Serializable]
public struct ActionRebindSetup
{
    [Tooltip("Chave de localização para o nome exibido na interface.")]
    public LocalizedString labelText;

    public InputActionReference inputAction;

    [Tooltip("Índice do binding dentro da action.")]
    public int bindingIndex;
}

/// <summary>
/// Gerencia a interface de remapeamento de teclas com suporte a localização.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class RebindManager : MonoBehaviour
{
    private const string SaveKey = "ControlBindingsSaved";

    [Header("Configuração das Teclas")]
    [SerializeField] private InputActionAsset inputAsset;
    [SerializeField] private ActionRebindSetup[] actionsToRebind;

    [Header("Elementos UI")]
    [SerializeField] private string scrollViewName = "ControlsScrollView";
    [SerializeField] private string restoreButtonName = "BtnRestoreControls";

    private UIDocument uiDocument;
    private ScrollView controlsScrollView;
    private Button restoreDefaultsButton;

    private InputActionRebindingExtensions.RebindingOperation currentRebindOperation;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument == null) return;

        VisualElement root = uiDocument.rootVisualElement;

        controlsScrollView = root.Q<ScrollView>(scrollViewName);
        restoreDefaultsButton = root.Q<Button>(restoreButtonName);

        RegisterCallbacks();
        LoadBindings();

        // Aguarda localização estar pronta antes de gerar a UI
        LocalizationSettings.InitializationOperation.Completed += _ => DrawUI();

        // Se já estiver pronta, desenha imediatamente
        if (LocalizationSettings.InitializationOperation.IsDone)
            DrawUI();

        // Redesenha quando o idioma mudar
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        UnregisterCallbacks();

        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        currentRebindOperation?.Dispose();
        currentRebindOperation = null;
    }

    // -------------------------------------------------------------------------
    // Callbacks
    // -------------------------------------------------------------------------

    private void RegisterCallbacks()
    {
        if (restoreDefaultsButton != null)
            restoreDefaultsButton.clicked += RestoreDefaults;
    }

    private void UnregisterCallbacks()
    {
        if (restoreDefaultsButton != null)
            restoreDefaultsButton.clicked -= RestoreDefaults;
    }

    private void OnLocaleChanged(Locale _) => DrawUI();

    // -------------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gera dinamicamente a lista de controles configuráveis.
    /// </summary>
    private void DrawUI()
    {
        if (controlsScrollView == null || actionsToRebind == null) return;

        controlsScrollView.Clear();

        foreach (ActionRebindSetup setup in actionsToRebind)
        {
            if (setup.inputAction == null || setup.inputAction.action == null) continue;

            controlsScrollView.Add(CreateRow(setup));
        }
    }

    /// <summary>
    /// Cria uma linha da interface de rebind com label localizado.
    /// </summary>
    private VisualElement CreateRow(ActionRebindSetup setup)
    {
        VisualElement row = new();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.Center;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 15;
        row.style.width = Length.Percent(100);

        // Label localizado
        Label label = new();
        label.style.color = Color.white;
        label.style.fontSize = 20;
        label.style.width = 300;
        label.style.unityTextAlign = TextAnchor.MiddleLeft;

        // Carrega o texto localizado de forma assíncrona e atualiza o label
        var loadOp = setup.labelText.GetLocalizedStringAsync();
        loadOp.Completed += op => label.text = op.Result;

        // Fallback imediato enquanto carrega
        if (loadOp.IsDone)
            label.text = loadOp.Result;

        // Botão de binding
        Button button = new();
        button.AddToClassList("menu-button");
        button.style.width = 200;
        button.style.height = 50;

        UpdateButtonText(button, setup);

        button.clicked += () => StartRebind(setup, button);

        row.Add(label);
        row.Add(button);

        return row;
    }

    // -------------------------------------------------------------------------
    // Rebind logic (inalterada)
    // -------------------------------------------------------------------------

    private void StartRebind(ActionRebindSetup setup, Button button)
    {
        if (currentRebindOperation != null) return;

        InputAction action = setup.inputAction.action;
        if (action == null) return;

        action.Disable();

        button.text = "[ Pressione ]";
        button.Blur();

        currentRebindOperation = action
            .PerformInteractiveRebinding(setup.bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op => OnRebindComplete(op, setup))
            .OnCancel(op => OnRebindCanceled(op, setup, button))
            .Start();
    }

    private void OnRebindComplete(
        InputActionRebindingExtensions.RebindingOperation operation,
        ActionRebindSetup setup)
    {
        operation.Dispose();
        currentRebindOperation = null;

        InputAction action = setup.inputAction.action;
        if (action == null) return;

        string newPath = action.bindings[setup.bindingIndex].effectivePath;
        ResolveConflicts(setup, newPath);

        action.Enable();
        SaveBindings();
        DrawUI();
    }

    private void ResolveConflicts(ActionRebindSetup changedSetup, string newPath)
    {
        foreach (ActionRebindSetup otherSetup in actionsToRebind)
        {
            InputAction otherAction = otherSetup.inputAction.action;
            if (otherAction == null) continue;

            bool isSameBinding =
                otherAction == changedSetup.inputAction.action &&
                otherSetup.bindingIndex == changedSetup.bindingIndex;

            if (isSameBinding) continue;
            if (otherSetup.bindingIndex >= otherAction.bindings.Count) continue;

            InputBinding binding = otherAction.bindings[otherSetup.bindingIndex];
            string currentPath = binding.hasOverrides ? binding.overridePath : binding.path;

            if (currentPath == newPath)
                otherAction.ApplyBindingOverride(otherSetup.bindingIndex, string.Empty);
        }
    }

    private void OnRebindCanceled(
        InputActionRebindingExtensions.RebindingOperation operation,
        ActionRebindSetup setup,
        Button button)
    {
        operation.Dispose();
        currentRebindOperation = null;

        setup.inputAction.action?.Enable();
        UpdateButtonText(button, setup);
    }

    private void UpdateButtonText(Button button, ActionRebindSetup setup)
    {
        InputAction action = setup.inputAction.action;
        if (action == null) return;
        if (setup.bindingIndex >= action.bindings.Count) return;

        InputBinding binding = action.bindings[setup.bindingIndex];
        string path = binding.hasOverrides ? binding.overridePath : binding.path;

        button.text = string.IsNullOrEmpty(path)
            ? "---"
            : InputControlPath.ToHumanReadableString(
                path,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    // -------------------------------------------------------------------------
    // Save / Load
    // -------------------------------------------------------------------------

    private void SaveBindings()
    {
        if (inputAsset == null) return;
        PlayerPrefs.SetString(SaveKey, inputAsset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }

    private void LoadBindings()
    {
        if (inputAsset == null) return;
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
            inputAsset.LoadBindingOverridesFromJson(json);
    }

    private void RestoreDefaults()
    {
        currentRebindOperation?.Cancel();

        if (inputAsset != null)
        {
            inputAsset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        DrawUI();
    }
}
