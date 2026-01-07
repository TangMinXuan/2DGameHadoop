using System;
using Cinemachine;
using DG.Tweening;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.UI;


namespace HadoopCore.Scripts.UI
{
    public class PassUI : MonoBehaviour
    {
        [SerializeField] private GameObject cameraRig;
        [SerializeField] private GameObject transitionUI;
        [SerializeField] private GameObject nextLevelBtn;
        [SerializeField] private GameObject exitBtn;
        private CinemachineVirtualCamera _vCamGameplay;
        private CanvasGroup _canvasGroup;
        private Sequence _seq;
        private float _initialOrthographicSize = 8f;
        private DOTweenAnimation MenuDOTweenAnimation;
        
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _vCamGameplay = cameraRig.GetComponentInChildren<CinemachineVirtualCamera>();
            _initialOrthographicSize = _vCamGameplay.m_Lens.OrthographicSize;
            MenuDOTweenAnimation = MySugarUtil.TryToFindComponent(gameObject, "Menu", MenuDOTweenAnimation);

            LevelEventCenter.OnGameSuccess += GameSuccess;
            UIUtil.SetUIVisible(_canvasGroup, false);
        }

        public void onNextLevelBtnClick()
        {
            transitionUI.GetComponent<TransitionUI>().CloseFromRect(nextLevelBtn.GetComponent<RectTransform>(), Camera.current, 1f);
        }
        
        public void onExitBtnClick()
        {
            transitionUI.GetComponent<TransitionUI>().CloseFromRect(exitBtn.GetComponent<RectTransform>(), Camera.current, 1f);
        }

        private void GameSuccess()
        {
            UIUtil.SetUIVisible(_canvasGroup, true);
            _seq = DOTween.Sequence()
                .SetId("PassPresentation")
                .SetUpdate(true);
            _seq.Join( TweenZoomIn(5f).SetEase(Ease.Linear) );
            _seq.InsertCallback(1f, () =>
            {
                MenuDOTweenAnimation.DOPlay();
            });
        }


        private Tween TweenZoomIn(float orthographicSize)
        {
            return DOTween.To(
                () => _vCamGameplay.m_Lens.OrthographicSize,
                x => _vCamGameplay.m_Lens.OrthographicSize = x,
                orthographicSize,
                0.35f
            );
        }

        private void OnDisable()
        {
            if (_seq != null && _seq.IsActive())
            {
                _seq.Kill();
                _seq = null;
            }
            UIUtil.SetUIVisible(_canvasGroup, false);
        }
        
        private void OnDestroy()
        {
            if (_vCamGameplay != null)
            {
                _vCamGameplay.m_Lens.OrthographicSize = _initialOrthographicSize;
            }
            UIUtil.SetUIVisible(_canvasGroup, false);
            LevelEventCenter.OnGameSuccess -= GameSuccess;
        }
    }
}