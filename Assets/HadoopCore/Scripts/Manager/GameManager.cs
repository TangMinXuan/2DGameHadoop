using System;
using System.Threading.Tasks;
using DG.Tweening;
using HadoopCore.Scripts.SceneController;
using HadoopCore.Scripts.UI;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HadoopCore.Scripts.Manager {
    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        private static int levelCnt = 35;

        private GameObject player;
        private bool _isPaused;
        private float _cachedScale = 1f; // 缓存时间缩放值. 缺少这个变量会导致下落的物体停止在半空
        private InputAction _esc;
        private string _previousSceneName = "GameStartPage";
        private GameSaveData _gameSaveData;


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

            // 不再使用 PlayerInput 组件, 改为直接创建 InputAction
            // 原因: 多个 PlayerInput 会通过 InputUser 独占设备配对,
            // 导致 Player 的 PlayerInput 拿不到键盘/鼠标, OnScreenButton 也失效
            _esc = new InputAction("Esc", InputActionType.Button, "<Keyboard>/escape");
            _esc.Enable();
            
            _esc.performed += EscBtnListener;

            LevelEventCenter.OnGamePaused += Pause;
            LevelEventCenter.OnGameResumed += Resume;
            LevelEventCenter.OnGameOver += GameOver;
            LevelEventCenter.OnLevelFinishedSignReset += LevelFinishedSignReset;
        }

        private void Start() {
        }

        // ===== Public API =====
        public void LoadScene(string sceneName) {
            _previousSceneName = GetCurrentSceneName();
            LevelEventCenter.TriggerLevelFinishedSignReset();
            LoadingPageManager.LoadSceneWithLoading(sceneName);
        }
        
        public void ReloadCurrentSceneSynchronously() {
            string currentSceneName = GetCurrentSceneName();
            _previousSceneName = currentSceneName;
            loadSceneSynchronously(currentSceneName);
        }

        public void loadSceneSynchronously(string sceneName, LoadSceneMode mode = LoadSceneMode.Single) {
            _previousSceneName = GetCurrentSceneName();
            LevelEventCenter.TriggerLevelFinishedSignReset();
            SceneManager.LoadScene(sceneName, mode);
        }
        
        public void loadPreviousScene() {
            loadSceneSynchronously(_previousSceneName);
        }

        #region 存档相关
        public GameSaveData GetSaveData() {
            if (_gameSaveData == null) {
                _gameSaveData = GameSaveData.LoadOrCreate(CreateDefaultSave);
            }
            return _gameSaveData;
        }

        public void SaveGameDataAsync(GameSaveData gameSaveData) {
            if (gameSaveData == null) {
                return;
            }
            _gameSaveData = gameSaveData; // 同步更新内存引用
            GameSaveData.Save(_gameSaveData);
            Debug.Log("[GameManager] SaveGameDataAsync: save completed.");
        }
        
        /// <summary>
        /// 计算解锁某一层所需的最低星数。
        /// 公式：t*50%*1 + t*40%*2 + t*10%*3，其中 t = 上一层最大关卡 id，结果向上取整。
        /// 第1层（levels 1-5）无需星星，返回 0。
        /// </summary>
        private static int CalcRequiredStarsForLayer(int layer) {
            if (layer <= 1) return 0;
            // 上一层最大关卡 id = (layer - 1) * 5
            int t = (layer - 1) * 5;
            float required = t * 0.5f * 1 + t * 0.4f * 2 + t * 0.1f * 3;
            return Mathf.CeilToInt(required);
        }
        
        private static GameSaveData CreateDefaultSave() {
            var data = new GameSaveData {
                SchemaVersion = 1,
                Version = Application.version,
                CurLayer = 1
            };

            // 默认 settings
            data.Settings.Add("musicVolume", 0.4f);
            data.Settings.Add("sfxVolume", 0.9f);
            data.Settings.Add("language", "en");

            // 初始化50个关卡，每5关为一层，共10层
            // 第1层（Level_1~5）直接解锁，其余层初始锁定
            // 解锁下一层所需星数：t*50%*1 + t*40%*2 + t*10%*3，t = 上一层最大关卡 id
            for (int i = 1; i <= levelCnt; i++) {
                string levelName = $"Level_{i}";
                int layer = Mathf.CeilToInt(i / 5f); // 第几层（1-based）
                bool unlocked = layer == 1;           // 只有第1层默认解锁
                int requiredStars = CalcRequiredStarsForLayer(layer);

                data.LevelDic[levelName] = new LevelProgress {
                    LevelId = i,
                    Unlocked = unlocked,
                    BestStars = 0,
                    BestTime = 0,
                    RequiredStars = requiredStars
                };
            }
            return data;
        }

        public static GameSaveData exposeCreateDefaultSave() {
            return CreateDefaultSave();
        }

        #endregion
        
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
            float extraMargin = 100f // 可选：多推出去一点，防止边缘露出
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
            LevelEventCenter.OnLevelFinishedSignReset -= LevelFinishedSignReset;
            
            // 空值检查：防止在Awake中检测到重复实例后直接return，导致_esc未初始化
            if (_esc != null) {
                _esc.performed -= EscBtnListener;
                _esc.Disable();
                _esc.Dispose();
                _esc = null;
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
        }

        private void Resume() {
            _isPaused = false;
            Time.timeScale = _cachedScale;
        }

        private void GameOver() {
            Debug.Log("Game Over");
            // Pause();
        }

        private void LevelFinishedSignReset() {
        }

        private void GameSuccess() {
            Debug.Log("Game Success");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            RefreshSceneReferences();
            LevelEventCenter.TriggerLevelFinishedSignReset();
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