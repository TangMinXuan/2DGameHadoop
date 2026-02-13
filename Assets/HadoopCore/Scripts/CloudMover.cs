using UnityEngine;
using DG.Tweening;

/// <summary>
/// Handles dynamic cloud movement using DOTween.
/// No Instantiate/Destroy - cloud loops forever once started.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CloudMover : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private Tween _moveTween;
    private bool _isRunning;

    // Cached parameters for looping
    private Camera _camera;
    private float _spawnX;
    private Vector2 _yRange;
    private Vector2 _speedRange;
    private Sprite[] _sprites;
    private float _despawnMargin;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Debug.LogError($"[CloudMover] SpriteRenderer missing on {gameObject.name}");
        }
    }

    /// <summary>
    /// Start the dynamic movement loop. Cloud will loop forever until stopped.
    /// </summary>
    /// <param name="cam">Camera for bounds calculation.</param>
    /// <param name="spawnX">X position where cloud spawns (off-screen left).</param>
    /// <param name="yRange">Y range (min, max) for random spawn position.</param>
    /// <param name="speedRange">Speed range (min, max) for random speed.</param>
    /// <param name="sprites">Array of cloud sprites to randomly pick from.</param>
    /// <param name="despawnMargin">Distance past camera right edge before looping.</param>
    public void StartDynamicLoop(Camera cam, float spawnX, Vector2 yRange, Vector2 speedRange, Sprite[] sprites, float despawnMargin)
    {
        if (cam == null)
        {
            Debug.LogError($"[CloudMover] Camera is null on {gameObject.name}. Cannot start.");
            return;
        }

        if (_spriteRenderer == null)
        {
            Debug.LogError($"[CloudMover] SpriteRenderer is null on {gameObject.name}. Cannot start.");
            return;
        }

        // Cache parameters
        _camera = cam;
        _spawnX = spawnX;
        _yRange = yRange;
        _speedRange = speedRange;
        _sprites = sprites;
        _despawnMargin = despawnMargin;
        _isRunning = true;

        // Start first loop
        StartNextLoop();
    }

    /// <summary>
    /// Stop the dynamic loop. Cloud will stop at current position.
    /// </summary>
    public void StopLoop()
    {
        _isRunning = false;
        KillTween();
    }

    /// <summary>
    /// Check if this cloud is currently running its loop.
    /// </summary>
    public bool IsRunning => _isRunning;

    private void StartNextLoop()
    {
        if (!_isRunning) return;
        if (_camera == null || _spriteRenderer == null) return;

        // Kill previous tween to prevent accumulation
        KillTween();

        // Randomize sprite
        if (_sprites != null && _sprites.Length > 0)
        {
            int spriteIndex = Random.Range(0, _sprites.Length);
            _spriteRenderer.sprite = _sprites[spriteIndex];
        }

        // Randomize Y position
        float randomY = Random.Range(_yRange.x, _yRange.y);
        transform.position = new Vector3(_spawnX, randomY, transform.position.z);

        // Randomize speed
        float speed = Random.Range(_speedRange.x, _speedRange.y);
        if (speed <= 0f)
        {
            Debug.LogWarning($"[CloudMover] Speed <= 0 on {gameObject.name}. Clamping to 0.1.");
            speed = 0.1f;
        }

        // Ensure sprite is fully visible
        Color c = _spriteRenderer.color;
        c.a = 1f;
        _spriteRenderer.color = c;

        // Calculate right camera bound for 2D orthographic
        float zDistance = Mathf.Abs(_camera.transform.position.z - transform.position.z);
        Vector3 viewportRight = _camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, zDistance));
        float cameraRight = viewportRight.x;

        float targetX = cameraRight + _despawnMargin;
        float currentX = transform.position.x;
        float distance = targetX - currentX;

        if (distance <= 0f)
        {
            // Already past target, restart immediately
            StartNextLoop();
            return;
        }

        float duration = distance / speed;

        // Start DOTween movement
        _moveTween = transform.DOMoveX(targetX, duration)
            .SetEase(Ease.Linear)
            .OnComplete(StartNextLoop);
    }

    private void KillTween()
    {
        if (_moveTween != null && _moveTween.IsActive())
        {
            _moveTween.Kill();
            _moveTween = null;
        }
    }

    private void OnDisable()
    {
        _isRunning = false;
        KillTween();
    }

    private void OnDestroy()
    {
        _isRunning = false;
        KillTween();
    }
}
