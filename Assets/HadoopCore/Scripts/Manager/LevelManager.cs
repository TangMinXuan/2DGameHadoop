using UnityEngine;
using UnityEngine.InputSystem;

namespace HadoopCore.Scripts.Manager {
    public class LevelManager : MonoBehaviour {
        [SerializeField] private GameObject player;

        private bool _isPaused;
        private float _cachedScale = 1f; // 缓存时间缩放值. 缺少这个变量会导致下落的物体停止在半空
        private PlayerInput _playerInput;
        private InputAction _esc;


        void Awake() {
            _playerInput = GetComponent<PlayerInput>();
            _esc = _playerInput.actions["Esc"];
            _esc.performed += EscBtnListener;

            LevelEventCenter.OnGamePaused += Pause;
            LevelEventCenter.OnGameResumed += Resume;
            LevelEventCenter.OnGameOver += GameOver;
            LevelEventCenter.OnGameRestart += GameRestart;
        }

        void OnDestroy() {
            LevelEventCenter.OnGamePaused -= Pause;
            LevelEventCenter.OnGameResumed -= Resume;
            LevelEventCenter.OnGameOver -= GameOver;
            LevelEventCenter.OnGameRestart -= GameRestart;
            _esc.performed -= EscBtnListener;
        }
        
        private void EscBtnListener(InputAction.CallbackContext ctx) {
            _isPaused = !_isPaused;
            if (_isPaused) {
                LevelEventCenter.TriggerGamePaused();
            } else {
                LevelEventCenter.TriggerGameResumed();
            }
        }

        public Transform GetPlayerTransform() {
            return player.transform;
        }

        private void Pause() {
            _isPaused = true;

            _cachedScale = Time.timeScale;
            Time.timeScale = 0f; // 全局停表

            AudioListener.pause = true; // 全局音频暂停
            Cursor.visible = true; // 桌面端可见鼠标
            Cursor.lockState = CursorLockMode.None;
        }

        private void Resume() {
            _isPaused = false;
            Time.timeScale = _cachedScale;
            AudioListener.pause = false;
            // 如果你的游戏需要锁鼠标：
            // Cursor.visible = false;
            // Cursor.lockState = CursorLockMode.Locked;
            // EventBus.Raise(GameResumed);
        }

        private void GameOver() {
            Debug.Log("Game Over");
            // Pause();
        }

        private void GameRestart() {
            Debug.Log("Game Restarted");
        }

        private void GameSuccess() {
            Debug.Log("Game Success");
        }
    }
}