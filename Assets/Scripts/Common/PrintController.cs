using System;
using UnityEngine;

/// <summary>
/// Utility controller for capturing in-game screenshots when pressing the designated hotkey.
/// Interacts with Unity's <see cref="ScreenCapture"/> API.
/// </summary>
public class PrintController : MonoBehaviour
{
    #region Unity Lifecycle

    /// <summary>
    /// Checks for screenshot hotkey input once per frame and saves a PNG image.
    /// Interacts with Unity's <see cref="Input"/> and <see cref="ScreenCapture"/> APIs.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ScreenCapture.CaptureScreenshot("print-" + DateTime.Now.Ticks + ".png", 2);
        }
    }

    #endregion
}
