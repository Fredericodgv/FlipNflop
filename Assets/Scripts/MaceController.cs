using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaceController : MonoBehaviour
{
    public float speed = 2.0f;
    private float startPosY;

    public float maxDistance = 5.0f;
    // Start is called before the first frame update
    void Start()
    {
        startPosY = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {     
        if (transform.position.y - startPosY > maxDistance || transform.position.y < startPosY)
        {
            speed = -speed;
        }
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }
}
