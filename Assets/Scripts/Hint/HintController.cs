using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns all hint-related input actions and delegates commands to <see cref="ClockLineHint"/> and <see cref="OperationHint"/> components.
/// Listens to input events configured via Unity Input System <see cref="InputActionReference"/>.
/// </summary>
public class HintController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Hint Components")]
    [Tooltip("Reference to the ClockLineHint component for toggling grid line visual modes.")]
    [SerializeField] private ClockLineHint clockLineHint;

    [Tooltip("Reference to the OperationHint component for displaying flip-flop operation labels.")]
    [SerializeField] private OperationHint operationHint;

    [Header("Input Actions")]
    [Tooltip("Action reference that toggles clock line hints.")]
    [SerializeField] private InputActionReference toggleClockLinesAction;

    [Tooltip("Action reference that triggers the operation hint labels.")]
    [SerializeField] private InputActionReference showOperationHintAction;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Subscribes to input action events on enable.
    /// Interacts with <see cref="InputActionReference"/> event callbacks.
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
    /// Unsubscribes from input action events on disable to prevent memory leaks.
    /// Interacts with <see cref="InputActionReference"/> event callbacks.
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

    #endregion

    #region Event Handlers

    /// <summary>
    /// Event handler that toggles the clock line hint mode on the associated <see cref="ClockLineHint"/> component.
    /// </summary>
    /// <param name="ctx">Input action callback context.</param>
    private void OnToggleClockLines(InputAction.CallbackContext ctx)
    {
        if (clockLineHint != null)
            clockLineHint.ToggleHintMode();
    }

    /// <summary>
    /// Event handler that triggers the operation hint on the associated <see cref="OperationHint"/> component.
    /// </summary>
    /// <param name="ctx">Input action callback context.</param>
    private void OnShowOperationHint(InputAction.CallbackContext ctx)
    {
        if (operationHint != null)
            operationHint.ShowHint();
    }

    #endregion
}