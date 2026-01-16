using Cinemachine;
using DG.Tweening;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using UnityEngine;


namespace HadoopCore.Scripts.UI {
    public class PassUI : MonoBehaviour {
        [SerializeField] private GameObject cameraRig;
        [SerializeField] private GameObject transitionUI;
        [SerializeField] private GameObject nextLevelBtn;
        [SerializeField] private GameObject exitBtn;
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

            LevelEventCenter.OnGameSuccess += GameSuccess;
            UIUtil.SetUIVisible(_canvasGroup, false);
        }

        public void OnNextLevelBtnClick() {
            transitionUI.GetComponent<TransitionUI>()
                .CloseFromRect(nextLevelBtn.GetComponent<RectTransform>(), Camera.current, 1f);
            LevelManager.Instance.JumpToNextLevel();
        }

        public void OnExitBtnClick() {
            transitionUI.GetComponent<TransitionUI>()
                .CloseFromRect(exitBtn.GetComponent<RectTransform>(), Camera.current, 1f);
        }

        private void GameSuccess() {
            UIUtil.SetUIVisible(_canvasGroup, true);
            _seq = DOTween.Sequence()
                .SetId("PassPresentation")
                .SetUpdate(true);
            _seq.Join(TweenZoomIn(5f));
            _seq.InsertCallback(2f, () => { MenuDOTweenAnimation.DOPlay(); });
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