using DG.Tweening;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using UnityEngine;

namespace HadoopCore.Scripts.UI {
    public class PauseUI : MonoBehaviour {
        
        [SerializeField] private GameObject transitionUI;
        [SerializeField] private GameObject resumeBtn;
        [SerializeField] private GameObject retryBtn;
        [SerializeField] private GameObject exitBtn;
        private CanvasGroup _canvasGroup;
        private DOTweenAnimation MenuDOTweenAnimation;
        private Sequence _seq;
        
        private void Awake() {
            _canvasGroup = GetComponent<CanvasGroup>();
            MenuDOTweenAnimation = MySugarUtil.TryToFindComponent(gameObject, "Menu", MenuDOTweenAnimation);

            LevelEventCenter.OnGamePaused += OnGamePaused;
            LevelEventCenter.OnGameResumed += OnGameResumed;
            UIUtil.SetUIVisible(_canvasGroup, false);
        }

        public void OnResumeBtnClick() {
            transitionUI.GetComponent<TransitionUI>()
                .CloseFromRect(retryBtn.GetComponent<RectTransform>(), Camera.main, 1f);
            
        }
        
        public void OnRetryBtnClick() {
            transitionUI.GetComponent<TransitionUI>()
                .CloseFromRect(retryBtn.GetComponent<RectTransform>(), Camera.main, 1f);
            
        }
        
        public void OnExitBtnClick() {
            transitionUI.GetComponent<TransitionUI>()
                .CloseFromRect(retryBtn.GetComponent<RectTransform>(), Camera.main, 1f);
            
        }

        private void OnGamePaused() {
            UIUtil.SetUIVisible(_canvasGroup, true);
            MenuDOTweenAnimation.DOPlay();
        }

        private void OnGameResumed() {
            UIUtil.SetUIVisible(_canvasGroup, false);
        }
    }
}