using UnityEngine;

// Usando no backgorund e no ContrastOverlaySprite para que sigam a posição da câmera principal
public class CameraFollower : MonoBehaviour
{
    [Tooltip("Seguir movimento horizontal (X)")]
    [SerializeField] private bool followX = true;

    [Tooltip("Seguir movimento vertical (Y)")]
    [SerializeField] private bool followY = true;

    [Tooltip("Offset em relação à câmera (útil para ajuste fino de posição)")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    private Camera _cam;
    private Vector3 _initialPos;

    private void Awake()
    {
        _cam = Camera.main;
        _initialPos = transform.position;
    }

    private void LateUpdate()
    {
        if (_cam == null) return;

        Vector3 camPos = _cam.transform.position;
        Vector3 newPos = transform.position;

        if (followX) newPos.x = camPos.x + offset.x;
        if (followY) newPos.y = camPos.y + offset.y;

        // Mantém o Z original do objeto
        newPos.z = _initialPos.z;

        transform.position = newPos;
    }
}