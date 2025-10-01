using UnityEngine;

public class CameraController : MonoBehaviour
{
    private enum CameraMode { FollowPlayer, ManualControl }
    private CameraMode currentMode;

    [Header("Configuração de Seguir")]
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    [Header("Configuração de Controle Manual")]
    [Tooltip("Velocidade com que a câmera se move no modo manual.")]
    public float manualMoveSpeed = 10f;

    [Header("Limites da Fase")]
    public float minX;
    private float maxX;

    void Start()
    {

        currentMode = CameraMode.FollowPlayer;

        // Buscando os limites do LevelManager
        if (LevelManager.Instance != null)
        {
            maxX = LevelManager.Instance.levelEndX;
        }
        else
        {
            Debug.LogError("LevelManager não encontrado na cena!");
        }
    }

    void LateUpdate()
    {
        if (currentMode == CameraMode.FollowPlayer)
        {
            FollowPlayer();
        }
        else if (currentMode == CameraMode.ManualControl)
        {
            HandleManualControl();
        }
    }

    /// <summary>
    /// Lógica para a câmera seguir o jogador.
    /// </summary>
    private void FollowPlayer()
    {
        if (target != null)
        {
            Vector3 desiredPosition = new Vector3(target.position.x + offset.x, transform.position.y, transform.position.z);

            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }

    /// <summary>
    /// Lógica para o jogador controlar a câmera manualmente.
    /// </summary>
    private void HandleManualControl()
    {
        float horizontalInput = Input.GetAxis("Horizontal");

        Vector3 movement = new Vector3(horizontalInput * manualMoveSpeed * Time.deltaTime, 0, 0);

        Vector3 newPosition = transform.position + movement;
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);

        transform.position = newPosition;
    }

    /// <summary>
    /// Método público para ser chamado de fora (pelo PlayerController) para trocar o modo.
    /// </summary>
    public void EnableManualControl()
    {
        currentMode = CameraMode.ManualControl;
        //        target = null;
    }
}