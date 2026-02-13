using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Manages dynamic and static clouds from scene collections.
/// No Instantiate/Destroy - cloud counts controlled by child objects.
/// </summary>
public class CloudGenerator : MonoBehaviour {
    [Header("Scene Collections")]
    [SerializeField] private Transform dynamicCloudsCollection;
    [SerializeField] private Transform staticCloudsCollection;

    [Header("Camera (optional - falls back to Camera.main)")]
    [SerializeField] private Camera targetCamera;

    [Header("Cloud Sprites")]
    [SerializeField] private Sprite[] cloudSprites;

    [Header("=== DYNAMIC CLOUDS ===")]
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int spawnBatchCount = 1;
    [SerializeField] private float spawnX = -10f;
    [SerializeField] private Vector2 dynamicYRange = new Vector2(-2f, 4f);
    [SerializeField] private Vector2 dynamicSpeedRange = new Vector2(1f, 3f);
    [SerializeField] private float despawnMargin = 2f;

    [Header("=== STATIC CLOUDS ===")]
    [SerializeField] private int staticMinCount = 2;
    [SerializeField] private int staticMaxCount = 5;
    [SerializeField] private float staticXMin = -6f;
    [SerializeField] private float staticXMax = 6f;
    [SerializeField] private float staticYMin = 2f;
    [SerializeField] private float staticYMax = 4f;
    [SerializeField] private Vector2 staticAlphaRange = new Vector2(0.6f, 1f);
    [SerializeField] private float staticFadeInDuration = 1.5f;
    [SerializeField] private float staticVisibleDuration = 3f;
    [SerializeField] private float staticFadeOutDuration = 1.5f;
    [SerializeField] private float staticCycleInterval = 2f;

    // Cached references (no LINQ)
    private List<CloudMover> _dynamicClouds = new List<CloudMover>();
    private List<SpriteRenderer> _staticClouds = new List<SpriteRenderer>();
    private int _runningDynamicCount;
    private Camera _resolvedCamera;
    private Coroutine _dynamicSpawnCoroutine;
    private Sequence _staticMasterSequence;
    private bool _staticLoopRunning;

    // Temp list for wave selection (avoids allocation each wave)
    private List<int> _tempIndices = new List<int>();

    private void OnEnable() {
        // Resolve camera
        _resolvedCamera = targetCamera != null ? targetCamera : Camera.main;
        if (_resolvedCamera == null)
        {
            Debug.LogError("[CloudGenerator] No camera available. Disabling.");
            return;
        }

        // Cache dynamic clouds
        CacheDynamicClouds();

        // Cache static clouds
        CacheStaticClouds();

        // Start dynamic spawn coroutine
        if (_dynamicClouds.Count > 0)
        {
            _dynamicSpawnCoroutine = StartCoroutine(DynamicSpawnLoop());
        }

        // Start static wave loop
        if (_staticClouds.Count > 0)
        {
            StartStaticLoop();
        }
    }

    private void OnDisable()
    {
        // Stop dynamic coroutine
        if (_dynamicSpawnCoroutine != null)
        {
            StopCoroutine(_dynamicSpawnCoroutine);
            _dynamicSpawnCoroutine = null;
        }

        // Stop all dynamic cloud loops
        for (int i = 0; i < _dynamicClouds.Count; i++)
        {
            if (_dynamicClouds[i] != null)
            {
                _dynamicClouds[i].StopLoop();
            }
        }
        _runningDynamicCount = 0;

        // Kill static master sequence
        _staticLoopRunning = false;
        if (_staticMasterSequence != null && _staticMasterSequence.IsActive())
        {
            _staticMasterSequence.Kill();
            _staticMasterSequence = null;
        }

        // Kill any individual tweens on static clouds and hide them
        for (int i = 0; i < _staticClouds.Count; i++)
        {
            if (_staticClouds[i] != null)
            {
                DOTween.Kill(_staticClouds[i]);
                Color c = _staticClouds[i].color;
                c.a = 0f;
                _staticClouds[i].color = c;
            }
        }
    }

    #region Caching

    private void CacheDynamicClouds() {
        _dynamicClouds.Clear();
        _runningDynamicCount = 0;

        if (dynamicCloudsCollection == null)
        {
            Debug.LogError("[CloudGenerator] dynamicCloudsCollection is not assigned.");
            return;
        }

        int childCount = dynamicCloudsCollection.childCount;
        for (int i = 0; i < childCount; i++) {
            Transform child = dynamicCloudsCollection.GetChild(i);
            CloudMover mover = child.GetComponent<CloudMover>();
            if (mover != null) {
                _dynamicClouds.Add(mover);
                // Ensure cloud starts hidden/inactive position
                child.gameObject.SetActive(true);
            }
        }
    }

    private void CacheStaticClouds()
    {
        _staticClouds.Clear();

        if (staticCloudsCollection == null)
        {
            Debug.LogError("[CloudGenerator] staticCloudsCollection is not assigned.");
            return;
        }

        int childCount = staticCloudsCollection.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = staticCloudsCollection.GetChild(i);
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                _staticClouds.Add(sr);
                // Start hidden
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
                child.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError($"[CloudGenerator] Static child '{child.name}' missing SpriteRenderer component.");
            }
        }

        // Pre-allocate temp indices list
        _tempIndices.Capacity = _staticClouds.Count;
    }

    #endregion

    #region Dynamic Clouds

    private IEnumerator DynamicSpawnLoop() {
        while (true) {
            SpawnDynamicBatch();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnDynamicBatch() {
        int totalDynamic = _dynamicClouds.Count;
        int availableSlots = totalDynamic - _runningDynamicCount;

        if (availableSlots <= 0) return;

        int toStart = spawnBatchCount;
        if (toStart > availableSlots) toStart = availableSlots;

        int started = 0;
        for (int i = 0; i < totalDynamic && started < toStart; i++) {
            CloudMover mover = _dynamicClouds[i];
            if (mover != null && !mover.IsRunning) {
                mover.StartDynamicLoop(
                    _resolvedCamera,
                    spawnX,
                    dynamicYRange,
                    dynamicSpeedRange,
                    cloudSprites,
                    despawnMargin
                );
                _runningDynamicCount++;
                started++;
            }
        }
    }

    #endregion

    #region Static Clouds

    private void StartStaticLoop()
    {
        if (cloudSprites == null || cloudSprites.Length == 0)
        {
            Debug.LogError("[CloudGenerator] cloudSprites array is null or empty. Static loop disabled.");
            return;
        }

        _staticLoopRunning = true;
        RunStaticWave();
    }

    private void RunStaticWave()
    {
        if (!_staticLoopRunning) return;

        int staticCount = _staticClouds.Count;
        if (staticCount == 0) return;

        // Clamp min/max to available
        int minCount = staticMinCount;
        int maxCount = staticMaxCount;
        if (minCount < 0) minCount = 0;
        if (maxCount > staticCount) maxCount = staticCount;
        if (minCount > maxCount) minCount = maxCount;

        // Determine wave count
        int waveCount = Random.Range(minCount, maxCount + 1);
        if (waveCount <= 0)
        {
            // No clouds to show, just wait and repeat
            ScheduleNextWave();
            return;
        }

        // Select k distinct random indices (Fisher-Yates partial shuffle, no LINQ)
        _tempIndices.Clear();
        for (int i = 0; i < staticCount; i++)
        {
            _tempIndices.Add(i);
        }

        // Shuffle first waveCount elements
        for (int i = 0; i < waveCount; i++)
        {
            int j = Random.Range(i, staticCount);
            int temp = _tempIndices[i];
            _tempIndices[i] = _tempIndices[j];
            _tempIndices[j] = temp;
        }

        // Configure selected clouds
        for (int i = 0; i < waveCount; i++)
        {
            int idx = _tempIndices[i];
            SpriteRenderer sr = _staticClouds[idx];

            // Randomize position
            float x = Random.Range(staticXMin, staticXMax);
            float y = Random.Range(staticYMin, staticYMax);
            sr.transform.position = new Vector3(x, y, sr.transform.position.z);

            // Randomize sprite
            if (cloudSprites.Length > 0)
            {
                int spriteIdx = Random.Range(0, cloudSprites.Length);
                sr.sprite = cloudSprites[spriteIdx];
            }

            // Start at alpha 0
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
        }

        // Ensure non-selected clouds are hidden
        for (int i = waveCount; i < staticCount; i++)
        {
            int idx = _tempIndices[i];
            SpriteRenderer sr = _staticClouds[idx];
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
        }

        // Build master sequence for synchronized animation
        if (_staticMasterSequence != null && _staticMasterSequence.IsActive())
        {
            _staticMasterSequence.Kill();
        }

        _staticMasterSequence = DOTween.Sequence();

        // Create individual fade sequences and join them
        for (int i = 0; i < waveCount; i++)
        {
            int idx = _tempIndices[i];
            SpriteRenderer sr = _staticClouds[idx];

            float targetAlpha = Random.Range(staticAlphaRange.x, staticAlphaRange.y);

            Sequence cloudSeq = DOTween.Sequence();
            cloudSeq.Append(sr.DOFade(targetAlpha, staticFadeInDuration));
            cloudSeq.AppendInterval(staticVisibleDuration);
            cloudSeq.Append(sr.DOFade(0f, staticFadeOutDuration));

            _staticMasterSequence.Join(cloudSeq);
        }

        // After wave completes, schedule next wave
        _staticMasterSequence.OnComplete(ScheduleNextWave);
    }

    private void ScheduleNextWave()
    {
        if (!_staticLoopRunning) return;

        // Wait for cycle interval then run next wave
        if (_staticMasterSequence != null && _staticMasterSequence.IsActive())
        {
            _staticMasterSequence.Kill();
        }

        _staticMasterSequence = DOTween.Sequence();
        _staticMasterSequence.AppendInterval(staticCycleInterval);
        _staticMasterSequence.OnComplete(RunStaticWave);
    }

    #endregion
}
