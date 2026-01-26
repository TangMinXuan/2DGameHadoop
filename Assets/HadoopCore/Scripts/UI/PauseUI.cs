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
        private Vector2 _screenLeft, _screenCenter, _screenRight;
        
        private void Awake() {
            MySugarUtil.AutoFindObjects(this, gameObject);
            
            _canvasGroup = GetComponent<CanvasGroup>();

            // 使用 MySugarUtil 查找 Menu 对象和三个按钮
            menuRefs.obj = MySugarUtil.TryToFindObject(gameObject, "Menu");
            menuRefs.resumeBtn = MySugarUtil.TryToFindObject(menuRefs.obj, "ResumeBtn");
            menuRefs.retryBtn = MySugarUtil.TryToFindObject(menuRefs.obj, "RetryBtn");
            menuRefs.exitBtn = MySugarUtil.TryToFindObject(menuRefs.obj, "ExitBtn");

            // 查找 DOTweenAnimation 组件
            DOTweenAnimation[] anims = menuRefs.obj.GetComponents<DOTweenAnimation>();
            menuRefs.menuAnimIn = Array.Find(anims, a => a.id == "Menu_In");
            menuRefs.menuAnimOut = Array.Find(anims, a => a.id == "Menu_Out");

            GameManager.Instance.CalculateHorizontalSlidePositions(menuRefs.obj.GetComponent<RectTransform>(), 
                out _screenLeft, out _screenCenter, out _screenRight);
            menuRefs.obj.GetComponent<RectTransform>().anchoredPosition = _screenLeft;
            
            if (menuRefs.menuAnimIn != null) {
                menuRefs.menuAnimIn.autoKill = false;
                menuRefs.menuAnimIn.CreateTween(true, false);
                menuRefs.menuAnimIn.endValueV2 = _screenCenter;
                menuRefs.menuAnimIn.tween?.OnStart(() => { menuRefs.obj.GetComponent<RectTransform>().anchoredPosition = _screenLeft; });
            }
            
            if (menuRefs.menuAnimOut != null) {
                menuRefs.menuAnimOut.autoKill = false;
                menuRefs.menuAnimOut.CreateTween(true, false);
                menuRefs.menuAnimIn.endValueV2 = _screenRight;
                menuRefs.menuAnimOut.tween?.OnStart(() => { menuRefs.obj.GetComponent<RectTransform>().anchoredPosition = _screenCenter; });
                menuRefs.menuAnimOut.tween?.OnComplete(() => { UIUtil.SetUIVisible(_canvasGroup, false); });
            }

            LevelEventCenter.OnGamePaused += OnGamePaused;
            LevelEventCenter.OnGameResumed += OnGameResumed;
            UIUtil.SetUIVisible(_canvasGroup, false);
        }

        public void OnResumeBtnClick() {
            LevelEventCenter.TriggerGameResumed();
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