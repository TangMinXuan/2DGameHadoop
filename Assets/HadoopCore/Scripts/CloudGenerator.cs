using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns cloud prefabs at random intervals and positions.
/// Uses a coroutine-based spawn loop that starts on enable and stops on disable.
/// </summary>
public class CloudGenerator : MonoBehaviour
{
    [Header("Prefab & Sprites")]
    [SerializeField] private GameObject cloudPrefab;
    [SerializeField] private Sprite[] cloudSprites;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnInterval = 5f;

    [Header("Spawn Position")]
    [SerializeField] private float spawnX = -10f;
    [SerializeField] private float yMin = -2f;
    [SerializeField] private float yMax = 4f;

    [Header("Cloud Speed")]
    [SerializeField] private float speedMin = 1f;
    [SerializeField] private float speedMax = 3f;

    [Header("Despawn Settings")]
    [SerializeField] private float despawnMargin = 2f;

    [Header("Camera (optional - falls back to Camera.main)")]
    [SerializeField] private Camera targetCamera;

    private Coroutine _spawnCoroutine;

    private void OnEnable()
    {
        _spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnCloud();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnCloud()
    {
        // Validate cloudSprites array
        if (cloudSprites == null || cloudSprites.Length < 6)
        {
            Debug.LogError("[CloudGenerator] cloudSprites array is null or has fewer than 6 elements. Skipping spawn.");
            return;
        }

        // Validate cloudPrefab
        if (cloudPrefab == null)
        {
            Debug.LogError("[CloudGenerator] cloudPrefab is not assigned. Skipping spawn.");
            return;
        }

        // Resolve camera
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogError("[CloudGenerator] No camera available (targetCamera is null and Camera.main is null). Skipping spawn.");
            return;
        }

        // Randomize parameters
        float randomY = Random.Range(yMin, yMax);
        float randomSpeed = Random.Range(speedMin, speedMax);
        int spriteIndex = Random.Range(0, 6); // 0..5 inclusive

        Vector3 spawnPosition = new Vector3(spawnX, randomY, 0f);

        // Instantiate -> SetActive(false) -> configure -> SetActive(true)
        // This avoids OnEnable race condition
        GameObject cloudInstance = Instantiate(cloudPrefab, spawnPosition, Quaternion.identity);
        cloudInstance.SetActive(false);

        // Set sprite
        SpriteRenderer sr = cloudInstance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = cloudSprites[spriteIndex];
        }
        else
        {
            Debug.LogWarning("[CloudGenerator] Cloud prefab has no SpriteRenderer component.");
        }

        // Initialize CloudMover
        CloudMover mover = cloudInstance.GetComponent<CloudMover>();
        if (mover != null)
        {
            mover.Init(cam, randomSpeed, despawnMargin);
        }
        else
        {
            Debug.LogWarning("[CloudGenerator] Cloud prefab has no CloudMover component.");
        }

        // Activate after configuration
        cloudInstance.SetActive(true);
    }
}
