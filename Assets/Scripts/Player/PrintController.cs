using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PrintController : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ScreenCapture.CaptureScreenshot("print-" + DateTime.Now.Ticks + ".png", 2);
        }
    }
}
