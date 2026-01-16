using HadoopCore.Scripts.Manager;
using TMPro;
using UnityEngine;

namespace HadoopCore.Scripts.UI {
    public class InGameUI : MonoBehaviour {
        [Header("Config")]
        [SerializeField] private int startSeconds = 60;

        [Header("Refs")]
        [SerializeField] private TMP_Text countdownTMP;

        private float _remainingSeconds;
        private bool _isPaused;
        private bool _isStopped;

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
            countdownTMP.text = secondsInt.ToString();
        }

        private void ResetSecondsToStart() {
            _remainingSeconds = Mathf.Max(0, startSeconds);
            _isStopped = _remainingSeconds <= 0f;
            _isPaused = false;
            UpdateText();
        }

        // ===== Public API =====

        public int GetRemainingSeconds() {
            return Mathf.CeilToInt(_remainingSeconds);
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
