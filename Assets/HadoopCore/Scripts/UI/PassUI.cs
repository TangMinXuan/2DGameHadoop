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
        [SerializeField] private Sprite unstart;

        private InGameUI _inGameUI;
        private CinemachineVirtualCamera _vCamGameplay;
        private CanvasGroup _canvasGroup;
        private Sequence _seq;
        private float _initialOrthographicSize = 8f;
        private DOTweenAnimation MenuDOTweenAnimation;

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
            _seq.InsertCallback(2f, () => { MenuDOTweenAnimation.DOPlay(); });
        }

        private void RefreshContent() {
            int remainingSeconds = _inGameUI != null ? _inGameUI.GetRemainingSeconds() : 0;

            // 1) TimeRemain
            SetTMP(timeValue, remainingSeconds.ToString());

            // 2) BestTimeRemain
            int bestTimeToShow = remainingSeconds;
            var saveData = LevelManager.Instance != null ? LevelManager.Instance.GetSaveData() : null;
            if (saveData != null) {
                string levelName = LevelManager.Instance.GetCurrentSceneName();
                if (saveData.Levels != null && saveData.Levels.TryGetValue(levelName, out var levelProgress) && levelProgress != null) {
                    int oldBestTime = levelProgress.BestTime;
                    bestTimeToShow = Mathf.Max(levelProgress.BestTime, remainingSeconds);
                    
                    if (remainingSeconds > oldBestTime) {
                        levelProgress.BestTime = remainingSeconds;
                        LevelManager.Instance.WriteSaveData(saveData);
                    }
                }
            }
            SetTMP(bestTimeValue, bestTimeToShow.ToString());

            // 3) Stars
            int stars = CalculateStarsByRemainingSeconds(remainingSeconds);
            ApplyStars(stars);
        }

        private static int CalculateStarsByRemainingSeconds(int remainingSeconds) {
            if (remainingSeconds >= 55) return 3;
            if (remainingSeconds >= 50) return 2;
            if (remainingSeconds >= 30) return 1;
            return 0;
        }

        private void ApplyStars(int stars) {
            ApplyStar(start_1, 1 <= stars);
            ApplyStar(start_2, 2 <= stars);
            ApplyStar(start_3, 3 <= stars);
        }

        private void ApplyStar(GameObject starObj, bool filled) {
            if (starObj == null) return;
            var img = starObj.GetComponent<Image>();
            if (img == null) return;

            // User will assign sprites later; keep current sprite if missing.
            if (start == null || unstart == null) return;

            img.sprite = filled ? start : unstart;
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