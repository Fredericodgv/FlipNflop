using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Moves background texture UV coordinates continuously to create a scrolling parallax effect.
/// </summary>
public class BackgroundMove : MonoBehaviour
{
    #region Serialized Fields

    /// <summary>
    /// MeshRenderer component reference holding the background material.
    /// </summary>
    [FormerlySerializedAs("mr")]
    public MeshRenderer meshRenderer;

    /// <summary>
    /// Scrolling speed for background texture offset along the X axis.
    /// </summary>
    public float speed;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Updates texture main offset each frame based on speed and delta time.
    /// </summary>
    private void Update()
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.mainTextureOffset += new Vector2(speed * Time.deltaTime, 0);
        }
    }

    #endregion
}
