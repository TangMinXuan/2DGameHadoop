using HadoopCore.Scripts.Manager;
using TMPro;
using UnityEngine;

namespace HadoopCore.Scripts.UI {
    public class InGameUI : MonoBehaviour {
        // 预先缓存 0-60 的字符串，完全避免 GC 分配
        private static readonly string[] CachedSecondsStrings = new string[61];
        
        static InGameUI() {
            for (int i = 0; i <= 60; i++) {
                CachedSecondsStrings[i] = i.ToString();
            }
        }
        
        [Header("Config")]
        [SerializeField] private float startSeconds = 60f;

        [Header("Refs")]
        [SerializeField] private TMP_Text countdownTMP;

        private float _remainingSeconds;
        private bool _isPaused;
        private bool _isStopped;
        private int _lastDisplayedSeconds = -1; // 缓存上一次显示的秒数，避免重复更新

        private void Awake() {
            if (countdownTMP == null) {
                countdownTMP = GetComponentInChildren<TMP_Text>(true);
            }

            LevelEventCenter.OnGamePaused += OnGamePaused;
            LevelEventCenter.OnGameResumed += OnGameResumed;
            LevelEventCenter.OnGameOver += OnGameOver;
            LevelEventCenter.OnGameSuccess += OnGameSuccess;

            ResetSecondsToStart();
        }

        private void OnEnable() {
            // 每次进入 / 重新启用当前 Scene 时重置
            ResetSecondsToStart();
        }

        private void Update() {
            if (_isPaused || _isStopped) {
                return;
            }

            if (_remainingSeconds <= 0f) {
                _remainingSeconds = 0f;
                _isStopped = true;
                UpdateText();
                return;
            }

            _remainingSeconds -= Time.deltaTime;
            if (_remainingSeconds <= 0f) {
                _remainingSeconds = 0f;
                _isStopped = true;
            }

            UpdateText();
        }

        private void UpdateText() {
            if (countdownTMP == null) {
                return;
            }

            int secondsInt = Mathf.CeilToInt(_remainingSeconds);
            
            // 只在秒数实际变化时才更新文本，避免频繁的字符串分配和 TMP 重新排版
            if (secondsInt != _lastDisplayedSeconds) {
                _lastDisplayedSeconds = secondsInt;
                
                // 使用预先缓存的字符串，完全避免 GC 分配
                if (secondsInt >= 0 && secondsInt < CachedSecondsStrings.Length) {
                    countdownTMP.text = CachedSecondsStrings[secondsInt];
                } else {
                    // 防御性代码：超出范围时降级为 ToString()
                    countdownTMP.text = secondsInt.ToString();
                }
            }
        }

        private void ResetSecondsToStart() {
            _remainingSeconds = Mathf.Max(0, startSeconds);
            _isStopped = _remainingSeconds <= 0f;
            _isPaused = false;
            UpdateText();
        }

        // ===== Public API =====

        public float GetRemainingSeconds() {
            return _remainingSeconds;
        }

        public void ResetSecondsTo60() {
            startSeconds = 60;
            ResetSecondsToStart();
        }

        // ===== Event handlers =====

        private void OnGamePaused() {
            _isPaused = true;
        }

        private void OnGameResumed() {
            if (_isStopped) {
                return;
            }

            _isPaused = false;
        }

        private void OnGameOver() {
            _isPaused = true;
        }

        private void OnGameSuccess() {
            _isPaused = true;
        }

        private void OnDestroy() {
            LevelEventCenter.OnGamePaused -= OnGamePaused;
            LevelEventCenter.OnGameResumed -= OnGameResumed;
            LevelEventCenter.OnGameOver -= OnGameOver;
            LevelEventCenter.OnGameSuccess -= OnGameSuccess;
        }
    }
}
