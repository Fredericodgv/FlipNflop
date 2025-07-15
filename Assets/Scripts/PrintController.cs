using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PrintController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ScreenCapture.CaptureScreenshot("print-" + DateTime.Now.Ticks + ".png", 2);
        }
    }
}
