using System;
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
        [SerializeField] private GameObject menu;
        private CanvasGroup _canvasGroup;
        private Sequence _seq;
        
        private void Awake() {
            _canvasGroup = GetComponent<CanvasGroup>();

            LevelEventCenter.OnGamePaused += OnGamePaused;
            LevelEventCenter.OnGameResumed += OnGameResumed;
            UIUtil.SetUIVisible(_canvasGroup, false);
        }

        public void OnResumeBtnClick() {
            LevelEventCenter.TriggerGameResumed();
        }
        
        public void OnRetryBtnClick() {
            transitionUI.GetComponent<TransitionUI>()
                .CloseFromRect(retryBtn.GetComponent<RectTransform>(), Camera.main, 1f);
            
        }
        
        public void OnExitBtnClick() {
            transitionUI.GetComponent<TransitionUI>()
                .CloseFromRect(retryBtn.GetComponent<RectTransform>(), Camera.main, 1f);
            
        }

        // TODO: 注册事件中, 不加[SerializeField]会导致引用被清空
        private void OnGamePaused() {
            UIUtil.SetUIVisible(_canvasGroup, true);
            var anims = menu.GetComponents<DOTweenAnimation>();
            DOTweenAnimation menuAnimIn = Array.Find(anims, a => a.id == "Menu_In");
            DOTweenAnimation menuAnimOut = Array.Find(anims, a => a.id == "Menu_Out");
            menuAnimOut.DOKill(complete: true);      
            menuAnimIn.DORestart();    
        }

        private void OnGameResumed() {
            var anims = menu.GetComponents<DOTweenAnimation>();
            DOTweenAnimation menuAnimIn = Array.Find(anims, a => a.id == "Menu_In");
            DOTweenAnimation menuAnimOut = Array.Find(anims, a => a.id == "Menu_Out");
            menuAnimIn.DOKill(complete: true);
            menuAnimOut.DORestart();
            menuAnimOut.tween?.OnComplete(() => {
                UIUtil.SetUIVisible(_canvasGroup, false);
            });
        }

        private void OnDestroy() {
            LevelEventCenter.OnGamePaused -= OnGamePaused;
            LevelEventCenter.OnGameResumed -= OnGameResumed;
        }
    }
}