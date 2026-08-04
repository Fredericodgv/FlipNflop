using UnityEngine;

/// <summary>
/// Keeps background and overlay sprites aligned with the position of the main camera.
/// Supports axis locks (X/Y) and custom offset adjustments.
/// </summary>
public class CameraFollower : MonoBehaviour
{
    #region Serialized Fields

    [Tooltip("Follow horizontal movement (X axis).")]
    [SerializeField] private bool followX = true;

    [Tooltip("Follow vertical movement (Y axis).")]
    [SerializeField] private bool followY = true;

    [Tooltip("Offset relative to camera position for fine positioning adjustments.")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    #endregion

    #region Private Fields

    private Camera _cam;
    private Vector3 _initialPos;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Caches camera reference and initial object position.
    /// </summary>
    private void Awake()
    {
        _cam = Camera.main;
        _initialPos = transform.position;
    }

    /// <summary>
    /// Updates object position to match camera position according to configured axes and offsets.
    /// </summary>
    private void LateUpdate()
    {
        if (_cam == null) return;

        Vector3 camPos = _cam.transform.position;
        Vector3 newPos = transform.position;

        if (followX) newPos.x = camPos.x + offset.x;
        if (followY) newPos.y = camPos.y + offset.y;

        newPos.z = _initialPos.z;

        transform.position = newPos;
    }

    #endregion
}