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

            maxX = (LevelManager.Instance.levelEndX - halfScreenWidth) + endPadding;
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

    private void HandleManualControl()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        Vector3 movement = new Vector3(horizontalInput * manualMoveSpeed * Time.deltaTime, 0, 0);
        Vector3 newPosition = transform.position + movement;
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        transform.position = newPosition;
    }

    public void EnableManualControl()
    {
        currentMode = CameraMode.ManualControl;
    }

    // Ativa o controle manual e ajusta o limite direito da câmera para um X específico (posição de morte do jogador, por exemplo).
    public void EnableManualControlWithRightLimit(float rightLimitWorldX)
    {
        // Mantém o centro da câmera exatamente onde já estava (no momento da morte)
        // e usa essa posição atual como limite direito de deslocamento.
        float desiredCenter = transform.position.x;
        maxX = Mathf.Max(minX, desiredCenter);
        currentMode = CameraMode.ManualControl;

        // Garante que a posição atual respeita os novos limites imediatamente
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        transform.position = pos;
    }
}