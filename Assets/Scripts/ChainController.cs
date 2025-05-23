using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainController : MonoBehaviour
{
    public float speed = 2.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Rotate Around Head
        transform.RotateAround(transform.parent.position, Vector3.forward, 45 * Time.deltaTime * speed);
    }
}
