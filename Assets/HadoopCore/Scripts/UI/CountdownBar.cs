using System;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls a countdown progress bar with configurable tick marks.
/// 
/// RESOLUTION-INDEPENDENCE EXPLANATION:
/// Tick marks use normalized anchors (0..1) instead of pixel offsets.
/// When anchorMin.x == anchorMax.x == normalizedValue, the tick is positioned
/// at that percentage of the parent's width, regardless of actual pixel size.
/// This ensures proper alignment across different resolutions and Canvas Scaler settings.
/// </summary>
public class CountdownBar : MonoBehaviour {
    #region Inspector Fields

    [Header("UI References")]
    [Tooltip("The gradient fill Image (must be Type=Filled, Horizontal, Origin=Left)")]
    [SerializeField]
    private Image gradientFillImage;
    
    [SerializeField]
    private Sprite scaleImage;
    
    [SerializeField]
    private Sprite unscaleImage;

    [Tooltip("Tick mark RectTransforms in order (Scale_1, Scale_2, Scale_3)")] [SerializeField]
    private RectTransform[] tickRects;

    [Header("Countdown Settings")] [Tooltip("Total countdown duration in seconds")] [SerializeField]
    private float totalSeconds = 60f;

    [Tooltip("Threshold values in seconds (remaining time positions for tick marks)")] [SerializeField]
    public float[] thresholds = { 20f, 40f, 50f };

    [Tooltip("UI refresh interval in seconds (visual update cadence)")] [Range(0.1f, 1f)] [SerializeField]
    private float uiStepSeconds = 0.5f;

    #endregion

    #region Events

    /// <summary>
    /// Invoked when the countdown reaches zero.
    /// </summary>
    public event Action OnCountdownFinished;

    #endregion

    #region Private Fields

    private float _remainingSeconds;
    private float _timeSinceLastUIUpdate;
    private bool _isCountingDown;
    private bool _isInitialized;
    private int _nextThresholdToLog = -1; // Index of the next threshold to log (start from highest)

    #endregion

    #region Properties

    /// <summary>
    /// Current remaining seconds in the countdown.
    /// </summary>
    public float RemainingSeconds => _remainingSeconds;

    /// <summary>
    /// Whether the countdown is currently active.
    /// </summary>
    public bool IsCountingDown => _isCountingDown;

    /// <summary>
    /// Current fill amount (0..1).
    /// </summary>
    public float FillAmount => gradientFillImage != null ? gradientFillImage.fillAmount : 0f;


    #endregion

    #region Unity Lifecycle

    private void Awake() {
        ValidateReferences();
        
        if (totalSeconds <= 0f) {
            Debug.LogWarning($"[CountdownBarController] totalSeconds is <= 0. Countdown disabled.", this);
            SetFillAmount(0f);
            _isInitialized = false;
            return;
        }

        _isInitialized = true;
        ResetCountdown();

        // Position tick marks using normalized anchors
        LayoutTicks();

        // Initialize fill to full
        SetFillAmount(1f);
        
        LevelEventCenter.OnGamePaused += OnGamePaused;
        LevelEventCenter.OnGameResumed += OnGameResumed;
        LevelEventCenter.OnGameOver += OnGameOver;
        LevelEventCenter.OnGameSuccess += OnGameSuccess;
    }
    
    private void Start() {
        StartCountdown();
    }
    
    private void OnEnable() {
        ResetCountdown();
        StartCountdown();
    }

    private void Update() {
        if (!_isCountingDown || !_isInitialized)
            return;

        // Time.deltaTime:
        //  受时间缩放影响 -> 玩家进入Pause需要暂停计时
        //  值等于 Time.timeScale × 实际帧间隔时间
        //  当游戏暂停时(Time.timeScale = 0),返回 0
        //  适用于游戏逻辑,如角色移动、游戏计时器
        //
        // Time.unscaledDeltaTime:
        //  不受时间缩放影响
        //  值等于实际帧间隔时间
        //  即使游戏暂停(Time.timeScale = 0),仍正常计时
        //  适用于UI 动画、暂停菜单、调试工具
        float deltaTime = Time.deltaTime;

        // Accumulate time for countdown (precise tracking)
        _remainingSeconds -= deltaTime;
        _timeSinceLastUIUpdate += deltaTime;

        // Check if countdown finished
        if (_remainingSeconds <= 0f) {
            _remainingSeconds = 0f;
            _isCountingDown = false;
            UpdateUI();
            OnCountdownFinished?.Invoke();
            return;
        }

        // Update UI at fixed cadence to avoid unnecessary updates every frame
        if (_timeSinceLastUIUpdate >= uiStepSeconds) {
            _timeSinceLastUIUpdate = 0f;
            UpdateUI();
        }
    }

    #endregion

    #region Initialization

    private void ValidateReferences() {
        if (gradientFillImage == null) {
            Debug.LogError($"[CountdownBarController] gradientFillImage is not assigned on {gameObject.name}!", this);
        }
        else if (gradientFillImage.type != Image.Type.Filled) {
            Debug.LogWarning(
                $"[CountdownBarController] gradientFillImage should be Image.Type.Filled for proper fill behavior.",
                this);
        }

        if (tickRects == null || tickRects.Length == 0) {
            Debug.LogWarning(
                $"[CountdownBarController] tickRects array is empty on {gameObject.name}. No tick marks will be positioned.",
                this);
        }
        else {
            for (int i = 0; i < tickRects.Length; i++) {
                if (tickRects[i] == null) {
                    Debug.LogWarning($"[CountdownBarController] tickRects[{i}] is null on {gameObject.name}.", this);
                }
            }
        }
    }

    #endregion

    #region Tick Layout

    /// <summary>
    /// Positions tick marks using normalized anchors for resolution independence.
    /// 
    /// WHY NORMALIZED ANCHORS:
    /// Setting anchorMin.x = anchorMax.x = (threshold / totalSeconds) positions
    /// the tick at that percentage of the parent width. This is resolution-independent
    /// because anchors work in normalized space (0..1), not pixels.
    /// The tick will always be at the correct relative position regardless of:
    /// - Screen resolution
    /// - Canvas Scaler reference resolution
    /// - Parent RectTransform size changes
    /// </summary>
    private void LayoutTicks() {
        if (tickRects == null || thresholds == null)
            return;

        if (totalSeconds <= 0f) {
            Debug.LogWarning("[CountdownBarController] Cannot layout ticks: totalSeconds <= 0");
            return;
        }

        // Use minimum length to handle mismatched arrays gracefully
        int count = Mathf.Min(tickRects.Length, thresholds.Length);

        if (count < tickRects.Length) {
            Debug.LogWarning(
                $"[CountdownBarController] thresholds array ({thresholds.Length}) is shorter than tickRects array ({tickRects.Length}). Only first {count} ticks will be positioned.");
        }

        for (int i = 0; i < count; i++) {
            if (tickRects[i] == null)
                continue;

            // Clamp threshold to valid range
            float threshold = Mathf.Clamp(thresholds[i], 0f, totalSeconds);

            // Calculate normalized position (0..1)
            // Since fillAmount represents remaining time, and fill goes from right to left as time decreases,
            // the tick at threshold T should be at position T/totalSeconds
            float normalizedX = threshold / totalSeconds;

            // Set anchors to the normalized position
            // This makes the tick position resolution-independent
            Vector2 anchorMin = tickRects[i].anchorMin;
            Vector2 anchorMax = tickRects[i].anchorMax;

            anchorMin.x = normalizedX;
            anchorMax.x = normalizedX;

            tickRects[i].anchorMin = anchorMin;
            tickRects[i].anchorMax = anchorMax;

            // Reset X offset to 0 so position is purely anchor-based
            // Preserve Y position (anchoredPosition.y) to maintain vertical layout
            Vector2 anchoredPos = tickRects[i].anchoredPosition;
            anchoredPos.x = 0f;
            tickRects[i].anchoredPosition = anchoredPos;
        }
    }

    #endregion

    #region UI Update

    /// <summary>
    /// Updates the fill amount on the gradient image.
    /// </summary>
    private void SetFillAmount(float amount) {
        if (gradientFillImage != null) {
            gradientFillImage.fillAmount = Mathf.Clamp01(amount);
        }
    }

    /// <summary>
    /// Updates all UI elements (fill and text).
    /// </summary>
    private void UpdateUI() {
        // Calculate fill amount based on remaining time
        float fillAmount = totalSeconds > 0f ? Mathf.Clamp01(_remainingSeconds / totalSeconds) : 0f;
        SetFillAmount(fillAmount);
        
        // Log when fillAmount crosses threshold boundaries (only once per threshold)
        if (thresholds != null && totalSeconds > 0f && _nextThresholdToLog >= 0) {
            while (_nextThresholdToLog >= 0) {
                float normalizedThreshold = thresholds[_nextThresholdToLog] / totalSeconds;
                if (fillAmount < normalizedThreshold) {
                    tickRects[_nextThresholdToLog].GetComponent<Image>().sprite = unscaleImage;
                    _nextThresholdToLog--;
                }
                else {
                    break;
                }
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Starts or restarts the countdown from totalSeconds.
    /// </summary>
    public void StartCountdown() {
        if (totalSeconds <= 0f) {
            Debug.LogWarning("[CountdownBarController] Cannot start countdown: totalSeconds <= 0");
            return;
        }

        _remainingSeconds = totalSeconds;
        _timeSinceLastUIUpdate = 0f;
        _isCountingDown = true;
        _isInitialized = true;
        _nextThresholdToLog = thresholds != null ? thresholds.Length - 1 : -1;


        // Immediately update UI to show full bar
        UpdateUI();
    }

    /// <summary>
    /// Stops the countdown at current position.
    /// </summary>
    public void StopCountdown() {
        _isCountingDown = false;
    }

    /// <summary>
    /// Resumes the countdown from current position.
    /// </summary>
    public void ResumeCountdown() {
        if (_remainingSeconds > 0f && totalSeconds > 0f) {
            _isCountingDown = true;
            _timeSinceLastUIUpdate = 0f;
        }
    }
    
    /// <summary>
    /// Resets the countdown to full without starting it.
    /// </summary>
    public void ResetCountdown() {
        _remainingSeconds = totalSeconds;
        _timeSinceLastUIUpdate = 0f;
        _isCountingDown = false;
        _nextThresholdToLog = thresholds != null ? thresholds.Length - 1 : -1;
        
        UpdateUI();
    }

    public float GetRemainingSeconds() {
        return _remainingSeconds;
    }
    
    /// <summary>
    /// Sets a new total duration and re-layouts tick marks.
    /// Does not restart the countdown automatically.
    /// </summary>
    /// <param name="newTotal">New total seconds for the countdown.</param>
    public void SetTotalSeconds(float newTotal) {
        if (newTotal <= 0f) {
            Debug.LogWarning("[CountdownBarController] SetTotalSeconds: value must be > 0");
            totalSeconds = 0f;
            SetFillAmount(0f);
            _isInitialized = false;
            return;
        }

        totalSeconds = newTotal;
        _isInitialized = true;

        // Re-layout ticks for new total
        LayoutTicks();
    }

    /// <summary>
    /// Sets new threshold values and re-layouts tick marks.
    /// </summary>
    /// <param name="newThresholds">New threshold values in seconds.</param>
    public void SetThresholds(float[] newThresholds) {
        if (newThresholds == null) {
            Debug.LogWarning("[CountdownBarController] SetThresholds: array cannot be null");
            return;
        }

        thresholds = newThresholds;
        _nextThresholdToLog = thresholds.Length - 1;

        // Re-layout ticks with new thresholds
        LayoutTicks();
    }

    /// <summary>
    /// Sets the remaining time directly (useful for loading saved state).
    /// </summary>
    /// <param name="seconds">Remaining seconds to set.</param>
    public void SetRemainingSeconds(float seconds) {
        _remainingSeconds = Mathf.Clamp(seconds, 0f, totalSeconds);
        UpdateUI();
    }

    #endregion
    
    // ===== Event handlers =====

    private void OnGamePaused() {
        StopCountdown();
    }

    private void OnGameResumed() {
        ResumeCountdown();
    }

    private void OnGameOver() {
        StopCountdown();
    }

    private void OnGameSuccess() {
        StopCountdown();
    }

    private void OnDestroy() {
        LevelEventCenter.OnGamePaused -= OnGamePaused;
        LevelEventCenter.OnGameResumed -= OnGameResumed;
        LevelEventCenter.OnGameOver -= OnGameOver;
        LevelEventCenter.OnGameSuccess -= OnGameSuccess;
    }

    #region Editor Helpers

#if UNITY_EDITOR
    /// <summary>
    /// Called in editor when values change. Re-layouts ticks for preview.
    /// </summary>
    private void OnValidate() {
        // Delay to next frame to avoid issues during serialization
        UnityEditor.EditorApplication.delayCall += () => {
            if (this == null) return;

            if (Application.isPlaying) return;

            // Re-layout ticks when thresholds or totalSeconds change in inspector
            if (tickRects != null && tickRects.Length > 0 && totalSeconds > 0f) {
                LayoutTicks();
            }
        };
    }
#endif

    #endregion
}

