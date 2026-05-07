using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns all hint-related input actions and delegates to the appropriate hint components.
/// Remove hint input handling from PlayerController and assign it here instead.
///
/// INTEGRATION:
///   1. Add this component to the HintManager GameObject in the scene.
///   2. Assign clockLineHint and operationHint references in the Inspector.
///   3. Assign the two input action references from your Input Action Asset.
///   4. Remove toggleHintsAction and hintController references from PlayerController.
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

    // -------------------------------------------------------------------------
    // Input registration
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Handlers
    // -------------------------------------------------------------------------

    private void OnToggleClockLines(InputAction.CallbackContext ctx)
    {
        if (clockLineHint != null)
            clockLineHint.ToggleHintMode();
    }

    private void OnShowOperationHint(InputAction.CallbackContext ctx)
    {
        if (operationHint != null)
            operationHint.ShowHint();
    }
}