using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    //1 - Camera segue o player

    //offset da camera em relacao ao player
    private Vector3 offset = new Vector3(0f, 0f, -10f);
    //tempo para a camera se mover
    private float smoothTime = 0.25f;
    //velocidade da camera
    private Vector3 velocity = Vector3.zero;

    //2 - Valores mínimos e máximos de x,y,z para a camera
    public Vector3 minValue, maxValue;

    //Alvo da camera
    [SerializeField] private Transform alvo;

    void Update()
    {
        Camera();
    }

    /*
    se for usar FixedUpdate, por n motivos, mudar o Interpolate do Rigidbody2D para Interpolate
    */

    void Camera()
    {
        //Definir valores mínimos e máximos de x,y,z para a camera

        if(alvo != null)
        {
            Vector3 posicaoAlvo = alvo.position + offset;
            //Verificar se posicaoAlvo está dentro dos limites
            //Limitar para os valores mínimos e máximos
            Vector3 posicaoLimite = new Vector3(
            Mathf.Clamp(posicaoAlvo.x, minValue.x, maxValue.x), //permite apenas que o valor de x fique entre o min e o max
            Mathf.Clamp(posicaoAlvo.y, minValue.y, maxValue.y), //permite apenas que o valor de y fique entre o min e o max
            Mathf.Clamp(posicaoAlvo.z, minValue.z, maxValue.z)  //permite apenas que o valor de z fique entre o min e o max
            );
            transform.position = Vector3.SmoothDamp(transform.position, posicaoLimite, ref velocity, smoothTime);
        }

    }

}
