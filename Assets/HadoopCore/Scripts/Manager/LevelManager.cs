using HadoopCore.Scripts.UI;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HadoopCore.Scripts.Manager {
    public class LevelManager : MonoBehaviour {
        public static LevelManager Instance { get; private set; }

        private GameObject player;
        private GameObject transitionUI;

        private bool _isPaused;
        private float _cachedScale = 1f; // 缓存时间缩放值. 缺少这个变量会导致下落的物体停止在半空
        private PlayerInput _playerInput;
        private InputAction _esc;


        void Awake() {
            // Persistent manager: prevent duplicates across scene loads
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RefreshSceneReferences();
            SceneManager.sceneLoaded += OnSceneLoaded;

            _playerInput = GetComponent<PlayerInput>();
            _esc = _playerInput.actions["Esc"];
            _esc.performed += EscBtnListener;

            LevelEventCenter.OnGamePaused += Pause;
            LevelEventCenter.OnGameResumed += Resume;
            LevelEventCenter.OnGameOver += GameOver;
            LevelEventCenter.OnGameRestart += GameRestart;
        }
        
        // ===== Public API =====
        public void JumpToNextLevel() {
            string currentSceneName = GetCurrentSceneName();
            int currentLevelNumber = int.Parse(currentSceneName.Split('_')[1]);
            string nextSceneName = "Level_" + (currentLevelNumber + 1);
            LoadScene(nextSceneName);
        }
        
        /// <summary>
        /// 加载指定名称的场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void LoadScene(string sceneName) {
            SceneManager.LoadScene(sceneName);
        }

        public GameSaveData GetSaveData() {
            return SaveSystem.LoadOrCreate();
        }
        
        /// <summary>
        /// 获取当前场景的名字
        /// </summary>
        /// <returns>当前活动场景的名称</returns>
        public string GetCurrentSceneName() {
            return SceneManager.GetActiveScene().name;
        }

        
        void OnDestroy() {
            // 只有当这是真正的单例实例时，才需要清理事件订阅
            // 如果是重复实例，在Awake中就return了，事件根本没订阅
            if (Instance != this) {
                return;
            }

            LevelEventCenter.OnGamePaused -= Pause;
            LevelEventCenter.OnGameResumed -= Resume;
            LevelEventCenter.OnGameOver -= GameOver;
            LevelEventCenter.OnGameRestart -= GameRestart;
            
            // 空值检查：防止在Awake中检测到重复实例后直接return，导致_esc未初始化
            if (_esc != null) {
                _esc.performed -= EscBtnListener;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;

            Instance = null;
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
            // Unity "fake null" handling: destroyed objects compare == null
            if (player == null) {
                RefreshSceneReferences();
            }

            return player != null ? player.transform : null;
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            RefreshSceneReferences();
        }

        private void RefreshSceneReferences() {
            // Rebind scene objects after scene load (old references become destroyed)
            MySugarUtil.AutoFindObjects(this, gameObject);

            // 触发开屏效果
            PlayOpeningTransition();
        }

        /// <summary>
        /// 播放场景开场的过渡效果
        /// </summary>
        private void PlayOpeningTransition() {
            if (transitionUI == null || player == null) {
                return;
            }

            var transitionUIComponent = transitionUI.GetComponent<TransitionUI>();
            if (transitionUIComponent == null) {
                Debug.LogWarning("[LevelManager] TransitionUI component not found on TransitionUI GameObject.");
                return;
            }

            var playerTf = player.transform;
            if (playerTf != null && Camera.main != null) {
                transitionUIComponent.OpenFromWorld(playerTf.position, Camera.main, 1f);
            }
        }
    }
}