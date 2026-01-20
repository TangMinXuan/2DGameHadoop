using Cinemachine;
using DG.Tweening;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HadoopCore.Scripts.UI {
    public class PassUI : MonoBehaviour {
        [Header("Scene Refs")]
        [SerializeField] private GameObject cameraRig;
        [SerializeField] private GameObject transitionUI;
        [SerializeField] private GameObject nextLevelBtn;
        [SerializeField] private GameObject exitBtn;

        [Header("Content Refs")] 
        [SerializeField] private GameObject menu;
        [SerializeField] private GameObject inGameUI;
        [SerializeField] private GameObject timeValue;
        [SerializeField] private GameObject bestTimeValue;
        [SerializeField] private GameObject start_1;
        [SerializeField] private GameObject start_2;
        [SerializeField] private GameObject start_3;

        [Header("Star Sprites")]
        [SerializeField] private Sprite start;

        private InGameUI _inGameUI;
        private CinemachineVirtualCamera _vCamGameplay;
        private CanvasGroup _canvasGroup;
        private Sequence _seq;
        private float _initialOrthographicSize = 8f;
        private DOTweenAnimation MenuDOTweenAnimation;
        private int _start = 0;
        private float _timeVal = 0f;
        private float _bestTimeVal = 0f;
        private Vector2 _screenLeft, _screenCenter, _screenRight;

        private void Awake() {
            MySugarUtil.AutoFindObjects(this, gameObject);

            _canvasGroup = GetComponent<CanvasGroup>();
            _vCamGameplay = cameraRig.GetComponentInChildren<CinemachineVirtualCamera>();
            _initialOrthographicSize = _vCamGameplay.m_Lens.OrthographicSize;
            MenuDOTweenAnimation = MySugarUtil.TryToFindComponent(gameObject, "Menu", MenuDOTweenAnimation);

            if (inGameUI != null) {
                _inGameUI = inGameUI.GetComponent<InGameUI>();
            }

            LevelEventCenter.OnGameSuccess += GameSuccess;
            UIUtil.SetUIVisible(_canvasGroup, false);
            
            LevelManager.Instance.CalculateHorizontalSlidePositions(menu.GetComponent<RectTransform>(), 
                out _screenLeft, out _screenCenter, out _screenRight);
            menu.GetComponent<RectTransform>().anchoredPosition = _screenLeft;
        }

        private void Start() {
            // TODO 测试用
            DOVirtual.DelayedCall(10f, () => {
                LevelEventCenter.TriggerGameSuccess();
            }).SetUpdate(true);
        }

        public void OnNextLevelBtnClick() {
            Tween transitionTween = transitionUI.GetComponent<TransitionUI>()
                .CloseFromRect(nextLevelBtn.GetComponent<RectTransform>(), Camera.current, 1f);
            transitionTween.OnComplete(() => LevelManager.Instance.JumpToNextLevel());
        }

        public void OnExitBtnClick() {
            transitionUI.GetComponent<TransitionUI>()
                .CloseFromRect(exitBtn.GetComponent<RectTransform>(), Camera.current, 1f)
                .OnComplete(() => LevelManager.Instance.LoadScene("LevelSelectMenu"));
        }

        private void GameSuccess() {
            RefreshContent();

            UIUtil.SetUIVisible(_canvasGroup, true);
            
            _seq = DOTween.Sequence()
                .SetId("PassPresentation")
                .SetUpdate(true);
            _seq.Join(TweenZoomIn(5f));
            _seq.Insert(2f, MenuDOTweenAnimation.GetTweens()[0]);
            _seq.Append(TweenNumCountUpFloat(timeValue.GetComponent<TMP_Text>(), _timeVal));
            _seq.Append(TweenNumCountUpFloat(bestTimeValue.GetComponent<TMP_Text>(), _bestTimeVal));
            _seq.AppendInterval(0.5f);
            _seq.Append(TweenStarsEnter()); 
        }

        private void RefreshContent() {
            float remainingSeconds = _inGameUI != null ? _inGameUI.GetRemainingSeconds() : 0;
            var saveData = LevelManager.Instance.GetSaveData();
            if (saveData == null) {
                _timeVal = remainingSeconds;
                _bestTimeVal = remainingSeconds;
                _start = CalculateStars(remainingSeconds);
                ApplyStarsImg(_start);
            }
            saveData.Levels.TryGetValue(LevelManager.Instance.GetCurrentSceneName(), out var levelProgress);
            levelProgress ??= new LevelProgress().WithUnlocked(true);
            
            // 1) TimeRemain
            _timeVal = remainingSeconds;

            // 2) BestTimeRemain
            _bestTimeVal = Mathf.Max(levelProgress.BestTime, remainingSeconds);

            // 3) Stars
            _start = CalculateStars(remainingSeconds);
            ApplyStarsImg(_start);
            
            // 4) Update Save Data
            bool isUpdated = false;
            if (remainingSeconds > levelProgress.BestTime) {
                levelProgress.BestTime = remainingSeconds;
                isUpdated = true;
            }
            if (_start > levelProgress.BestStars) {
                levelProgress.BestStars = _start;
                isUpdated = true;
            }
            if (isUpdated) {
                saveData.Levels[LevelManager.Instance.GetCurrentSceneName()] = levelProgress;
                saveData.UpdateTotalStars();
            }
        }

        private static int CalculateStars(float remainingSeconds) {
            if (remainingSeconds >= 55) return 3;
            if (remainingSeconds >= 50) return 2;
            if (remainingSeconds >= 30) return 1;
            return 0;
        }

        private void ApplyStarsImg(int stars) {
            if (start == null) return;
            
            if (stars >= 1 && start_1 != null) {
                var img1 = start_1.GetComponent<Image>();
                if (img1 != null) img1.sprite = start;
            }
            
            if (stars >= 2 && start_2 != null) {
                var img2 = start_2.GetComponent<Image>();
                if (img2 != null) img2.sprite = start;
            }
            
            if (stars >= 3 && start_3 != null) {
                var img3 = start_3.GetComponent<Image>();
                if (img3 != null) img3.sprite = start;
            }
        }

        private Sequence TweenZoomIn(float orthographicSize) {
            Vector2 playerPos = LevelManager.Instance.GetPlayerTransform().position;
            return DOTween.Sequence()
                .SetUpdate(true)
                .Join(_vCamGameplay.transform.DOMove(new Vector3(playerPos.x, playerPos.y + 2, _vCamGameplay.transform.position.z), 
                        1f)
                    .SetEase(Ease.OutBack))
                .Join(DOTween.To(() => _vCamGameplay.m_Lens.OrthographicSize, 
                    x => _vCamGameplay.m_Lens.OrthographicSize = x, 
                    orthographicSize, 
                    1f)
                    .SetEase(Ease.OutBack));
        }
        
        private Sequence TweenStarsEnter() {
            RectTransform[] starsToAnimate = new RectTransform[_start];
            if (_start >= 1 && start_1 != null) starsToAnimate[0] = start_1.GetComponent<RectTransform>();
            if (_start >= 2 && start_2 != null) starsToAnimate[1] = start_2.GetComponent<RectTransform>();
            if (_start >= 3 && start_3 != null) starsToAnimate[2] = start_3.GetComponent<RectTransform>();
            const float dur = 0.45f;
            const float stagger = 0.12f;
            var root = DOTween.Sequence()
                .SetUpdate(true);

            for (int i = 0; i < starsToAnimate.Length; i++) {
                RectTransform rt = starsToAnimate[i];
                rt.localScale = Vector3.zero;
                float startTime = i * stagger;
                root.Insert(startTime, rt.DOScale(Vector3.one, dur).SetEase(Ease.OutBack));
            }

            return root;
        }
        
        Tween TweenNumCountUpFloat(TMP_Text tmp, float target, 
            float duration = 1f, int decimals = 1, bool useUnscaledTime = true) {
            // 预先准备 format，避免每帧拼字符串
            string fmt = decimals > 0 ? "{0:" + decimals + "}" : "{0:0}";
            
            // 保护：target <= 0 直接落到目标
            if (target <= 0f) {
                tmp.SetText(fmt, target);
                return DOTween.Sequence(); // 空 tween 占位
            }

            float v = 0f;
            
            // 决定显示粒度：1, 0.1, 0.01 ...
            float step = Mathf.Pow(10f, -decimals);

            // 用于减少重复写 UI
            float lastShown = float.NaN;
            
            tmp.SetText(fmt, 0f);

            return DOTween.To(
                    () => v,
                    x => {
                        // 1) 永不超过目标（避免任何“提前到下一格”）
                        v = Mathf.Min(x, target);

                        // 2) 量化：向下取整到步进格子（无 Round，不会提前跳）
                        float shown = Mathf.Floor(v / step) * step;

                        // 3) 只在显示值变化时才刷新 TMP
                        if (!float.IsNaN(lastShown) && Mathf.Abs(shown - lastShown) < (step * 0.5f))
                            return;

                        lastShown = shown;
                        tmp.SetText(fmt, shown);
                    },
                    target,
                    duration
                )
                .SetEase(Ease.InCubic)
                .SetUpdate(useUnscaledTime)
                .OnComplete(() => {
                    // 4) 最终强制精确显示 target
                    tmp.SetText(fmt, target);
                });
        }
        

        private void OnDisable() {
            if (_seq != null && _seq.IsActive()) {
                _seq.Kill();
                _seq = null;
            }

            UIUtil.SetUIVisible(_canvasGroup, false);
        }

        private void OnDestroy() {
            if (_vCamGameplay != null) {
                _vCamGameplay.m_Lens.OrthographicSize = _initialOrthographicSize;
            }

            UIUtil.SetUIVisible(_canvasGroup, false);
            LevelEventCenter.OnGameSuccess -= GameSuccess;
        }
    }
}