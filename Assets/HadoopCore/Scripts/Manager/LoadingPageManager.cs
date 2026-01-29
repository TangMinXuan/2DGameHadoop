using UnityEngine;
using DG.Tweening;
using HadoopCore.Scripts.Utils;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace HadoopCore.Scripts.Manager {
    /// <summary>
    /// Put this on a GameObject in LoadingScene.
    /// Responsibilities:
    /// 1) Fade in loading UI (optional)
    /// 2) Load target scene via LoadSceneAsync
    /// 3) Smooth progress UI and enforce min show time
    /// 4) Fade out (optional) then activate scene
    /// 
    /// Uses DOTween for all animations and timing.
    /// </summary>
    public class LoadingPageManager : MonoBehaviour {
        private static string _pendingTargetScene = "LevelSelectMenu";

        public static void LoadSceneWithLoading(string targetSceneName, string loadingSceneName = "LoadingPage") {
            _pendingTargetScene = targetSceneName;
            SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Single);
        }

        // ---------------------------
        // Inspector: UI references
        // ---------------------------

        [Header("UI References (optional)")] [SerializeField]
        private CanvasGroup loadingUIRootCanvasGroup; // for fade in/out

        [SerializeField] private Slider progressSlider; // set value 0..1
        [SerializeField] private Image progressFill; // set fillAmount 0..1 (if not using Slider)
        [SerializeField] private TMP_Text progressText; // e.g. "57%"
        [SerializeField] private TMP_Text tipText; // optional tips text

        [Header("Tips (optional)")] [SerializeField]
        private string[] tips;

        [SerializeField] private bool randomTip = true;

        // ---------------------------
        // Inspector: behavior tuning
        // ---------------------------

        [Header("Timing")]
        [Tooltip("Ensure loading page shows at least this many seconds (real time, unscaled).")]
        [SerializeField]
        private float minShowSeconds = 1f;

        [Header("Progress Smoothing")]
        [Tooltip("Duration for progress bar animation (seconds).")]
        [SerializeField]
        private float progressTweenDuration = 0.3f;

        // ---------------------------
        // Runtime state
        // ---------------------------

        private float _displayProgress; // the value shown to UI
        private AsyncOperation _asyncOp;
        private Tween _progressTween;
        private float _startRealtime;

        private void Awake() {
            // Setup UI initial state
            UIUtil.SetUIVisible(loadingUIRootCanvasGroup, false);

            SetProgressUI(0f);

            // Tips
            if (tipText != null && tips != null && tips.Length > 0) {
                tipText.text = randomTip ? tips[Random.Range(0, tips.Length)] : tips[0];
            }
        }

        private void Start() {
            StartLoading();
        }

        private void StartLoading() {
            _startRealtime = Time.realtimeSinceStartup;
            
            UIUtil.SetUIVisible(loadingUIRootCanvasGroup, true);

            // Start async load
            _asyncOp = SceneManager.LoadSceneAsync(_pendingTargetScene, LoadSceneMode.Single);
            if (_asyncOp == null) {
                Debug.LogError($"[LoadingPageManager] LoadSceneAsync returned null for scene: {_pendingTargetScene}");
                return;
            }
            _asyncOp.allowSceneActivation = false;

            DOTween.To(
                () => _displayProgress,
                x => {
                    _displayProgress = x;
                    SetProgressUI(x);
                },
                1f, // target value (will be overridden by OnUpdate)
                minShowSeconds
            )
            .SetUpdate(true) // Use unscaled time
            .SetEase(Ease.Linear)
            .OnUpdate(OnProgressUpdate)
            .OnComplete(OnLoadingComplete);
        }

        private void OnProgressUpdate() {
            // Calculate target progress based on AsyncOperation
            float targetProgress;
            if (_asyncOp.progress < 0.9f) {
                // Scene still loading, map 0-0.9 to 0-0.9 of our display
                targetProgress = Mathf.Clamp01(_asyncOp.progress / 0.9f) * 0.9f;
            } else {
                // Scene loaded (waiting for activation), smoothly go to 1.0
                float elapsed = Time.realtimeSinceStartup - _startRealtime;
                float remainingTime = Mathf.Max(0, minShowSeconds - elapsed);
                // Lerp from 0.9 to 1.0 over the remaining time
                float t = remainingTime > 0 ? 1f - (remainingTime / minShowSeconds) : 1f;
                targetProgress = Mathf.Lerp(0.9f, 1f, t);
            }

            // Smoothly animate to target
            _progressTween?.Kill();
            _progressTween = DOTween.To(
                () => _displayProgress,
                x => {
                    _displayProgress = x;
                    SetProgressUI(x);
                },
                targetProgress,
                progressTweenDuration
            ).SetUpdate(true).SetEase(Ease.OutQuad);
        }

        private void OnLoadingComplete() {
            // Kill any pending progress tween
            _progressTween?.Kill();
            
            // Ensure progress is at 100%
            DOTween.To(
                () => _displayProgress,
                x => {
                    _displayProgress = x;
                    SetProgressUI(x);
                },
                1f,
                0.2f
            )
            .SetUpdate(true)
            .SetEase(Ease.OutQuad)
            .OnComplete(ActivateScene);
        }

        private void ActivateScene() {
            UIUtil.SetUIVisible(loadingUIRootCanvasGroup, false);
            
            // Activate scene
            if (_asyncOp != null) {
                _asyncOp.allowSceneActivation = true;
            }
        }

        private void SetProgressUI(float v01) {
            v01 = Mathf.Clamp01(v01);

            if (progressSlider != null)
                progressSlider.value = v01;

            if (progressFill != null)
                progressFill.fillAmount = v01;

            if (progressText != null)
                progressText.text = Mathf.RoundToInt(v01 * 100f) + "%";
        }

        private void OnDestroy() {
            // Clean up tweens
            _progressTween?.Kill();
            DOTween.Kill(this);
        }
    }
}