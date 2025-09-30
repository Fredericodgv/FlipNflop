using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform target; 
    public float smoothSpeed = 0.125f; 
    public Vector3 offset; 

    [Header("Camera X Limits")]
    public float minX = 6.9f;

    void LateUpdate()
    {
        // Verifica se o alvo (jogador) foi definido
        if (target != null)
        {
            //A câmera seguirá o jogador no eixo X, 
            Vector3 desiredPosition = new Vector3(target.position.x + offset.x, transform.position.y, transform.position.z);
            
            // Aplica os limites de X
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, LevelManager.Instance.levelEndX);

            // Suaviza o movimento da câmera
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            
            transform.position = smoothedPosition;
        }
    }
}