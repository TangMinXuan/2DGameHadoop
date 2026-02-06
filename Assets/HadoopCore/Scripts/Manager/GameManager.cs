using System;
using DG.Tweening;
using HadoopCore.Scripts.UI;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HadoopCore.Scripts.Manager {
    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        private GameObject player;

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

        private void Start() {
            // TODO for test
            // DOVirtual.DelayedCall(15f, () => {
            //     LevelEventCenter.TriggerGameSuccess();
            // }).SetUpdate(true);
        }

        // ===== Public API =====
        /// <summary>
        /// 加载指定名称的场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void LoadScene(string sceneName) {
            LoadingPageManager.LoadSceneWithLoading(sceneName);
        }
        
        public void ReloadCurrentSceneSynchronously() {
            string currentSceneName = GetCurrentSceneName();
            loadSceneSynchronously(currentSceneName);
        }

        public void loadSceneSynchronously(string sceneName) {
            SceneManager.LoadScene(sceneName);
        }

        public GameSaveData GetSaveData() {
            return GameSaveData.LoadOrCreate();
        }
        
        public string GetCurrentSceneName() {
            return SceneManager.GetActiveScene().name;
        }
        
        public string GetNextLevelName() {
            string currentSceneName = GetCurrentSceneName();
            int currentLevelNumber = int.Parse(currentSceneName.Split('_')[1]);
            return "Level_" + (currentLevelNumber + 1);
        }

        public void CalculateHorizontalSlidePositions(RectTransform panel,
            out Vector2 offscreenLeft,
            out Vector2 center,
            out Vector2 offscreenRight,
            float extraMargin = 50f // 可选：多推出去一点，防止边缘露出
        ) {
            // 确保 Canvas 已经完成布局
            Canvas.ForceUpdateCanvases();

            // 1. 中心位置
            center = Vector2.zero;

            // 2. 屏幕宽度（UI 像素坐标）
            float screenWidth = Screen.width;

            // 3. 面板宽度（已经是当前分辨率下的真实像素）
            float panelWidth = panel.rect.width;

            // 4. pivot 修正
            float pivotX = panel.pivot.x;

            // pivot = 0   → 左对齐
            // pivot = 0.5 → 居中
            // pivot = 1   → 右对齐

            // 5. 计算三个位置
            offscreenLeft = new Vector2(
                -screenWidth * 0.5f - panelWidth * (1f - pivotX) - extraMargin,
                center.y
            );

            offscreenRight = new Vector2(
                screenWidth * 0.5f + panelWidth * pivotX + extraMargin,
                center.y
            );
        }

        public Sequence GenerateTransition(bool isOpen) {
            return TransitionUI.Instance.GenerateTransition(null, isOpen);
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
            GenerateTransition(true); // 触发开屏效果
        }

        private void RefreshSceneReferences() {
            // Rebind scene objects after scene load (old references become destroyed)
            // 只有当 物体(这里是GameManager) 依赖了场景中的对象时(例如这里的player)，才需要刷新引用, 将指针指向新加载场景中的对象
            // 反例: LoadingPageManager, 它依赖的东西都挂在它子节点上,
            // 而子节点会跟着父节点 DontDestroyOnLoad , 因此它不需要刷新依赖
            MySugarUtil.AutoFindObjects(this, gameObject);
        }
    }
}