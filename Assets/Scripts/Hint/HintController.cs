using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns all hint-related input actions and delegates to the appropriate hint components.
/// Remove hint input handling from PlayerController and assign it here instead.
/// </summary>
public class HintController : MonoBehaviour
{
    [Header("Hint Components")]
    [SerializeField] private ClockLineHint clockLineHint;
    [SerializeField] private OperationHint operationHint;

    [Header("Input Actions")]
    [Tooltip("Action that toggles clock line hints (previously in PlayerController).")]
    [SerializeField] private InputActionReference toggleClockLinesAction;

    [Tooltip("Action that triggers the operation hint (J/K/Op labels).")]
    [SerializeField] private InputActionReference showOperationHintAction;

    /// <summary>
    /// Subscribes to input actions on enable and unsubscribes on disable.
    /// </summary>
    private void OnEnable()
    {
        if (toggleClockLinesAction != null)
        {
            toggleClockLinesAction.action.performed += OnToggleClockLines;
            toggleClockLinesAction.action.Enable();
        }

        if (showOperationHintAction != null)
        {
            showOperationHintAction.action.performed += OnShowOperationHint;
            showOperationHintAction.action.Enable();
        }
    }

    /// <summary>
    /// Unsubscribes from input actions to prevent memory leaks and unintended behavior when the object is disabled.
    /// </summary>
    private void OnDisable()
    {
        if (toggleClockLinesAction != null)
        {
            toggleClockLinesAction.action.performed -= OnToggleClockLines;
            toggleClockLinesAction.action.Disable();
        }

        if (showOperationHintAction != null)
        {
            showOperationHintAction.action.performed -= OnShowOperationHint;
            showOperationHintAction.action.Disable();
        }
    }

    /// <summary>
    /// Toggles the clock line hint mode
    /// </summary>
    private void OnToggleClockLines(InputAction.CallbackContext ctx)
    {
        if (clockLineHint != null)
            clockLineHint.ToggleHintMode();
    }

    /// <summary>
    /// Shows the operation hint (J/K/Op labels) 
    /// </summary>
    private void OnShowOperationHint(InputAction.CallbackContext ctx)
    {
        if (operationHint != null)
            operationHint.ShowHint();
    }
}