using System;
using Cinemachine;
using DG.Tweening;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HadoopCore.Scripts.UI {
    public class PassUI : MonoBehaviour {
        [Header("Scene Refs")]
        [SerializeField] private GameObject cameraRig;
        [SerializeField] private GameObject transitionUI;
        [SerializeField] private GameObject nextLevelBtn;
        [SerializeField] private GameObject exitBtn;

        [Header("Content Refs")]
        [SerializeField] private GameObject inGameUI;
        [SerializeField] private GameObject timeTitle;
        [SerializeField] private GameObject timeValue;
        [SerializeField] private GameObject bestTimeTitle;
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
        }

        private void Start() {
            // TODO 测试用
            DOVirtual.DelayedCall(40f, () => {
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
            
            RectTransform[] starsToAnimate = new RectTransform[_start];
            if (_start >= 1 && start_1 != null) starsToAnimate[0] = start_1.GetComponent<RectTransform>();
            if (_start >= 2 && start_2 != null) starsToAnimate[1] = start_2.GetComponent<RectTransform>();
            if (_start >= 3 && start_3 != null) starsToAnimate[2] = start_3.GetComponent<RectTransform>();
            
            _seq.Insert(3f, TweenStarsEnter(starsToAnimate));
        }

        private void RefreshContent() {
            int remainingSeconds = _inGameUI != null ? _inGameUI.GetRemainingSeconds() : 0;
            var saveData = LevelManager.Instance.GetSaveData();
            if (saveData == null) {
                SetTMP(timeValue, remainingSeconds.ToString());
                SetTMP(bestTimeValue, remainingSeconds.ToString());
                _start = CalculateStars(remainingSeconds);
                ApplyStars(_start);
            }
            saveData.Levels.TryGetValue(LevelManager.Instance.GetCurrentSceneName(), out var levelProgress);
            levelProgress ??= new LevelProgress().WithUnlocked(true);
            
            // 1) TimeRemain
            SetTMP(timeValue, remainingSeconds.ToString());

            // 2) BestTimeRemain
            int bestTimeToShow = Mathf.Max(levelProgress.BestTime, remainingSeconds);
            SetTMP(bestTimeValue, bestTimeToShow.ToString());

            // 3) Stars
            _start = CalculateStars(remainingSeconds);
            ApplyStars(_start);
            
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

        private static int CalculateStars(int remainingSeconds) {
            if (remainingSeconds >= 55) return 3;
            if (remainingSeconds >= 50) return 2;
            if (remainingSeconds >= 30) return 1;
            return 0;
        }

        private void ApplyStars(int stars) {
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

        private static void SetTMP(GameObject go, string text) {
            if (go == null) return;
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp == null) return;
            tmp.text = text;
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
        
        private Sequence TweenStarsEnter(RectTransform[] stars) {
            const float dur = 0.45f;
            const float stagger = 0.12f;

            var root = DOTween.Sequence()
                .SetUpdate(true);

            for (int i = 0; i < stars.Length; i++) {
                RectTransform rt = stars[i];
                rt.localScale = Vector3.zero;
                float startTime = i * stagger;
                root.Insert(startTime, rt.DOScale(Vector3.one, dur).SetEase(Ease.OutBack));
            }

            return root;
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