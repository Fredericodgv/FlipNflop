using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Utility controller for capturing in-game screenshots when pressing the designated hotkey.
/// Interacts with Unity's <see cref="ScreenCapture"/> API and <see cref="Keyboard"/> API.
/// </summary>
public class PrintController : MonoBehaviour
{
    #region Unity Lifecycle

    /// <summary>
    /// Checks for screenshot hotkey input once per frame and saves a PNG image.
    /// Interacts with Unity's <see cref="Keyboard"/> and <see cref="ScreenCapture"/> APIs.
    /// </summary>
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            ScreenCapture.CaptureScreenshot("print-" + DateTime.Now.Ticks + ".png", 2);
        }
    }

    #endregion
}
