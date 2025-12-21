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
    [Tooltip("A posição X onde a câmera PARA no início da fase.")]
    public float minX;
    [Tooltip("Uma folga para a câmera ir um pouco além do final da tela. Use 0 para parar na borda.")]
    public float endPadding = 2f;
    private float maxX;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        currentMode = CameraMode.FollowPlayer;

        if (LevelManager.Instance != null)
        {
            float halfScreenWidth = cam.orthographicSize * cam.aspect;
            float levelRight = LevelManager.Instance.levelEndX;

            float computedMax = (levelRight - halfScreenWidth) + endPadding;
            maxX = Mathf.Max(minX, computedMax);
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
    /// Clamps the camera X position based on current limits.
    /// </summary>
    private float ClampCameraX(float x)
    {
        return Mathf.Clamp(x, minX, maxX);
    }

    private void FollowPlayer()
    {
        if (target != null)
        {
            Vector3 desiredPosition = new Vector3(target.position.x + offset.x, transform.position.y, transform.position.z);
            desiredPosition.x = ClampCameraX(desiredPosition.x);
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }

    private void HandleManualControl()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        Vector3 movement = new Vector3(horizontalInput * manualMoveSpeed * Time.deltaTime, 0, 0);
        Vector3 newPosition = transform.position + movement;
        newPosition.x = ClampCameraX(newPosition.x);
        transform.position = newPosition;
    }

    public void EnableManualControl()
    {
        currentMode = CameraMode.ManualControl;
    }

    /// <summary>
    /// Enables manual control and locks the right limit at the camera's current position.
    /// Used when the player dies to prevent camera from moving further right.
    /// </summary>
    public void EnableManualControlWithRightLimit(float rightLimitWorldX)
    {
        float currentCenterX = transform.position.x;
        maxX = Mathf.Max(minX, currentCenterX);

        currentMode = CameraMode.ManualControl;

        // Apply clamping immediately to respect new limits
        Vector3 pos = transform.position;
        pos.x = ClampCameraX(pos.x);
        transform.position = pos;
    }
}