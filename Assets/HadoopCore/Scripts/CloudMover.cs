using UnityEngine;
using DG.Tweening;

/// <summary>
/// Moves a cloud GameObject to the right using DOTween and destroys it when it exits the camera view.
/// Must be initialized via Init() before activation.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CloudMover : MonoBehaviour
{
    private Camera _camera;
    private float _speed;
    private float _despawnMargin;
    private bool _isInitialized;

    private Tween _moveTween;

    /// <summary>
    /// Initialize the cloud mover with required parameters.
    /// Must be called before the GameObject is activated.
    /// </summary>
    /// <param name="cam">The camera used to calculate the right boundary.</param>
    /// <param name="speed">Movement speed (units per second).</param>
    /// <param name="despawnMargin">Extra distance beyond camera right edge before destroying.</param>
    public void Init(Camera cam, float speed, float despawnMargin)
    {
        _camera = cam;
        _speed = speed;
        _despawnMargin = despawnMargin;
        _isInitialized = true;
    }

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            // Not yet initialized - this can happen if the prefab is placed in scene directly
            // without going through CloudGenerator. In that case, do nothing on enable.
            return;
        }

        StartMovement();
    }

    private void StartMovement()
    {
        if (_camera == null)
        {
            Debug.LogError("[CloudMover] Camera is null. Cannot start movement. Destroying.");
            Destroy(gameObject);
            return;
        }

        // Handle invalid speed
        if (_speed <= 0f)
        {
            Debug.LogWarning("[CloudMover] Speed is <= 0. Clamping to 0.1 to avoid infinite duration.");
            _speed = 0.1f;
        }

        // Calculate right camera bound for 2D orthographic
        // Using the cloud's z position relative to camera for proper 2D calculation
        float zDistance = Mathf.Abs(_camera.transform.position.z - transform.position.z);
        Vector3 viewportRight = _camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, zDistance));
        float cameraRight = viewportRight.x;

        // Target position: camera right edge + despawn margin
        float targetX = cameraRight + _despawnMargin;
        float currentX = transform.position.x;

        // Calculate duration based on distance and speed
        float distance = targetX - currentX;
        
        if (distance <= 0f)
        {
            // Already past target, destroy immediately
            Debug.LogWarning("[CloudMover] Cloud is already past target position. Destroying.");
            Destroy(gameObject);
            return;
        }

        float duration = distance / _speed;

        // Start DOTween movement
        _moveTween = transform.DOMoveX(targetX, duration)
            .SetEase(Ease.Linear)
            .OnComplete(OnMovementComplete);
    }

    private void OnMovementComplete()
    {
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        KillTween();
    }

    private void OnDestroy()
    {
        KillTween();
    }

    private void KillTween()
    {
        if (_moveTween != null && _moveTween.IsActive())
        {
            _moveTween.Kill();
            _moveTween = null;
        }
    }
}
