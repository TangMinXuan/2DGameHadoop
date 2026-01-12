using System;
using DG.Tweening;
using HadoopCore.Scripts.Annotation;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using UnityEngine;

namespace HadoopCore.Scripts.UI {
    [Serializable]
    internal class MenuRefs {
        public GameObject obj;
        [NonSerialized] public GameObject resumeBtn;
        [NonSerialized] public GameObject retryBtn;
        [NonSerialized] public GameObject exitBtn;
        [NonSerialized, DontNeedAutoFind] public DOTweenAnimation menuAnimIn;
        [NonSerialized, DontNeedAutoFind] public DOTweenAnimation menuAnimOut;
    }

    public class PauseUI : MonoBehaviour {
        
        [SerializeField] private GameObject transitionUI;
        [SerializeField] private MenuRefs menuRefs;
        private CanvasGroup _canvasGroup;
        
        private void Awake() {
            MySugarUtil.AutoFindObjects(this, gameObject);
            
            _canvasGroup = GetComponent<CanvasGroup>();

            // 使用 MySugarUtil 查找 Menu 对象和三个按钮
            menuRefs.obj = MySugarUtil.TryToFindObject(gameObject, "Menu", menuRefs.obj);
            menuRefs.resumeBtn = MySugarUtil.TryToFindObject(menuRefs.obj, "ResumeBtn", menuRefs.resumeBtn);
            menuRefs.retryBtn = MySugarUtil.TryToFindObject(menuRefs.obj, "RetryBtn", menuRefs.retryBtn);
            menuRefs.exitBtn = MySugarUtil.TryToFindObject(menuRefs.obj, "ExitBtn", menuRefs.exitBtn);

            // 查找 DOTweenAnimation 组件
            var anims = menuRefs.obj.GetComponents<DOTweenAnimation>();
            menuRefs.menuAnimIn = Array.Find(anims, a => a.id == "Menu_In");
            menuRefs.menuAnimOut = Array.Find(anims, a => a.id == "Menu_Out");

            if (menuRefs.menuAnimIn != null) {
                menuRefs.menuAnimIn.autoKill = false;
                menuRefs.menuAnimIn.CreateTween(true, false);
                menuRefs.menuAnimIn.tween?.OnStart(() => { menuRefs.obj.transform.localPosition = Vector3.zero; });
            }
            
            if (menuRefs.menuAnimOut != null) {
                menuRefs.menuAnimOut.autoKill = false;
                menuRefs.menuAnimOut.CreateTween(true, false);
                menuRefs.menuAnimOut.tween?.OnStart(() => { menuRefs.obj.transform.localPosition = Vector3.zero; });
                menuRefs.menuAnimOut.tween?.OnComplete(() => { UIUtil.SetUIVisible(_canvasGroup, false); });
            }

            LevelEventCenter.OnGamePaused += OnGamePaused;
            LevelEventCenter.OnGameResumed += OnGameResumed;
            UIUtil.SetUIVisible(_canvasGroup, false);
        }

        public void OnResumeBtnClick() {
            LevelEventCenter.TriggerGameResumed();
        }
        
        public void OnRetryBtnClick() {
            transitionUI.GetComponent<TransitionUI>()
                .CloseFromRect(menuRefs.retryBtn.GetComponent<RectTransform>(), Camera.main, 1f);
            
        }
        
        public void OnExitBtnClick() {
            transitionUI.GetComponent<TransitionUI>()
                .CloseFromRect(menuRefs.exitBtn.GetComponent<RectTransform>(), Camera.main, 1f);
            
        }

        private void OnGamePaused() {
            UIUtil.SetUIVisible(_canvasGroup, true);

            if (menuRefs.menuAnimIn != null) {
                menuRefs.menuAnimIn.DORestartById("Menu_In");
            }
        }

        private void OnGameResumed() {
            if (menuRefs.menuAnimOut != null) {
                menuRefs.menuAnimOut.DORestartById("Menu_Out");
            }
        }

        private void OnDestroy() {
            LevelEventCenter.OnGamePaused -= OnGamePaused;
            LevelEventCenter.OnGameResumed -= OnGameResumed;
        }
    }
}