using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Estrutura que define uma ação configurável no menu de rebind.
/// </summary>
[System.Serializable]
public struct ActionRebindSetup
{
    [Tooltip("Nome exibido na interface.")]
    public string labelText;

    public InputActionReference inputAction;

    [Tooltip("Índice do binding dentro da action.")]
    public int bindingIndex;
}

/// <summary>
/// Gerencia a interface de remapeamento de teclas.
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

    private InputActionRebindingExtensions.RebindingOperation currentRebindOperation;

    private Button restoreDefaultsButton;

    /// <summary>
    /// Inicializa referências do componente.
    /// </summary>
    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    /// <summary>
    /// Inicializa a interface e carrega bindings salvos.
    /// </summary>
    private void OnEnable()
    {
        if (uiDocument == null)
            return;

        VisualElement root = uiDocument.rootVisualElement;

        controlsScrollView = root.Q<ScrollView>(scrollViewName);
        restoreDefaultsButton = root.Q<Button>(restoreButtonName);

        RegisterCallbacks();

        LoadBindings();
        DrawUI();
    }

    /// <summary>
    /// Remove callbacks e encerra operações pendentes.
    /// </summary>
    private void OnDisable()
    {
        UnregisterCallbacks();

        if (currentRebindOperation != null)
        {
            currentRebindOperation.Dispose();
            currentRebindOperation = null;
        }
    }

    /// <summary>
    /// Registra callbacks da interface.
    /// </summary>
    private void RegisterCallbacks()
    {
        if (restoreDefaultsButton != null)
            restoreDefaultsButton.clicked += RestoreDefaults;
    }

    /// <summary>
    /// Remove callbacks registrados.
    /// </summary>
    private void UnregisterCallbacks()
    {
        if (restoreDefaultsButton != null)
            restoreDefaultsButton.clicked -= RestoreDefaults;
    }

    /// <summary>
    /// Gera dinamicamente a lista de controles configuráveis.
    /// </summary>
    private void DrawUI()
    {
        if (controlsScrollView == null || actionsToRebind == null)
            return;

        controlsScrollView.Clear();

        foreach (ActionRebindSetup setup in actionsToRebind)
        {
            if (setup.inputAction == null || setup.inputAction.action == null)
                continue;

            VisualElement row = CreateRow(setup);

            controlsScrollView.Add(row);
        }
    }

    /// <summary>
    /// Cria uma linha da interface de rebind.
    /// </summary>
    private VisualElement CreateRow(ActionRebindSetup setup)
    {
        VisualElement row = new VisualElement();

        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.Center;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 15;
        row.style.width = Length.Percent(100);

        Label label = new Label(setup.labelText);

        label.style.color = Color.white;
        label.style.fontSize = 20;
        label.style.width = 300;
        label.style.unityTextAlign = TextAnchor.MiddleLeft;

        Button button = new Button();

        button.AddToClassList("menu-button");
        button.style.width = 200;
        button.style.height = 50;

        UpdateButtonText(button, setup);

        button.clicked += () => StartRebind(setup, button);

        row.Add(label);
        row.Add(button);

        return row;
    }

    /// <summary>
    /// Inicia o processo de remapeamento de tecla.
    /// </summary>
    private void StartRebind(ActionRebindSetup setup, Button button)
    {
        if (currentRebindOperation != null)
            return;

        InputAction action = setup.inputAction.action;

        if (action == null)
            return;

        action.Disable();

        button.text = "[ Pressione ]";
        button.Blur();

        currentRebindOperation = action
            .PerformInteractiveRebinding(setup.bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => OnRebindComplete(operation, setup))
            .OnCancel(operation => OnRebindCanceled(operation, setup, button))
            .Start();
    }

    /// <summary>
    /// Finaliza um rebind concluído com sucesso.
    /// </summary>
    private void OnRebindComplete(
        InputActionRebindingExtensions.RebindingOperation operation,
        ActionRebindSetup setup)
    {
        operation.Dispose();
        currentRebindOperation = null;

        InputAction action = setup.inputAction.action;

        if (action == null)
            return;

        string newPath = action.bindings[setup.bindingIndex].effectivePath;

        ResolveConflicts(setup, newPath);

        action.Enable();

        SaveBindings();

        DrawUI();
    }

    /// <summary>
    /// Resolve conflitos entre bindings iguais.
    /// </summary>
    private void ResolveConflicts(ActionRebindSetup changedSetup, string newPath)
    {
        foreach (ActionRebindSetup otherSetup in actionsToRebind)
        {
            InputAction otherAction = otherSetup.inputAction.action;

            if (otherAction == null)
                continue;

            bool isSameBinding =
                otherAction == changedSetup.inputAction.action &&
                otherSetup.bindingIndex == changedSetup.bindingIndex;

            if (isSameBinding)
                continue;

            if (otherSetup.bindingIndex >= otherAction.bindings.Count)
                continue;

            InputBinding binding = otherAction.bindings[otherSetup.bindingIndex];

            string currentPath = binding.hasOverrides
                ? binding.overridePath
                : binding.path;

            if (currentPath == newPath)
                otherAction.ApplyBindingOverride(otherSetup.bindingIndex, string.Empty);
        }
    }

    /// <summary>
    /// Cancela um rebind em andamento.
    /// </summary>
    private void OnRebindCanceled(
        InputActionRebindingExtensions.RebindingOperation operation,
        ActionRebindSetup setup,
        Button button)
    {
        operation.Dispose();
        currentRebindOperation = null;

        InputAction action = setup.inputAction.action;

        if (action != null)
            action.Enable();

        UpdateButtonText(button, setup);
    }

    /// <summary>
    /// Atualiza o texto exibido em um botão de binding.
    /// </summary>
    private void UpdateButtonText(Button button, ActionRebindSetup setup)
    {
        InputAction action = setup.inputAction.action;

        if (action == null)
            return;

        if (setup.bindingIndex >= action.bindings.Count)
            return;

        InputBinding binding = action.bindings[setup.bindingIndex];

        string path = binding.hasOverrides
            ? binding.overridePath
            : binding.path;

        button.text = string.IsNullOrEmpty(path)
            ? "---"
            : InputControlPath.ToHumanReadableString(
                path,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    /// <summary>
    /// Salva os bindings customizados.
    /// </summary>
    private void SaveBindings()
    {
        if (inputAsset == null)
            return;

        string json = inputAsset.SaveBindingOverridesAsJson();

        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Carrega bindings salvos.
    /// </summary>
    private void LoadBindings()
    {
        if (inputAsset == null)
            return;

        string json = PlayerPrefs.GetString(SaveKey, string.Empty);

        if (!string.IsNullOrEmpty(json))
            inputAsset.LoadBindingOverridesFromJson(json);
    }

    /// <summary>
    /// Restaura os bindings padrão.
    /// </summary>
    private void RestoreDefaults()
    {
        if (currentRebindOperation != null)
            currentRebindOperation.Cancel();

        if (inputAsset != null)
        {
            inputAsset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        DrawUI();
    }
}