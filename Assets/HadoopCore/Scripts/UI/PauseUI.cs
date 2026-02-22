using System;
using DG.Tweening;
using HadoopCore.Scripts.Annotation;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HadoopCore.Scripts.UI {
    [Serializable]
    internal class MenuRefs {
        public GameObject obj;
        [NonSerialized, DontNeedAutoFind] public Button resumeBtn;
        [NonSerialized, DontNeedAutoFind] public Button settingsBtn;
        [NonSerialized, DontNeedAutoFind] public Button retryBtn;
        [NonSerialized, DontNeedAutoFind] public Button exitBtn;
    }

    public class PauseUI : MonoBehaviour {
        
        [SerializeField] private MenuRefs menuRefs;
        private CanvasGroup _canvasGroup;
        private Sequence _seq;
        
        private void Awake() {
            MySugarUtil.AutoFindObjects(this, gameObject);
            
            _canvasGroup = GetComponent<CanvasGroup>();

            // 使用 MySugarUtil 查找 Menu 对象和按钮
            menuRefs.obj = MySugarUtil.TryToFindObject(gameObject, "Menu");
            menuRefs.resumeBtn = MySugarUtil.TryToFindObject(menuRefs.obj, "ResumeBtn").GetComponent<Button>();
            menuRefs.settingsBtn = MySugarUtil.TryToFindObject(menuRefs.obj, "SettingsBtn").GetComponent<Button>();
            menuRefs.retryBtn = MySugarUtil.TryToFindObject(menuRefs.obj, "RetryBtn").GetComponent<Button>();
            menuRefs.exitBtn = MySugarUtil.TryToFindObject(menuRefs.obj, "ExitBtn").GetComponent<Button>();
            menuRefs.resumeBtn.onClick.AddListener(OnResumeBtnClick);
            menuRefs.settingsBtn.onClick.AddListener(OnSettingsBtnClick);
            menuRefs.retryBtn.onClick.AddListener(OnRetryBtnClick);
            menuRefs.exitBtn.onClick.AddListener(OnExitBtnClick);

            LevelEventCenter.OnGamePaused += OnGamePaused;
            LevelEventCenter.OnGameResumed += OnGameResumed;
            UIUtil.SetUIVisible(_canvasGroup, false);
        }

        private void OnResumeBtnClick() {
            _seq?.Kill();
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .Append(menuRefs.resumeBtn.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(menuRefs.resumeBtn.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => LevelEventCenter.TriggerGameResumed())
                .SetLink(gameObject);
        }
        
        private void OnSettingsBtnClick() {
            _seq?.Kill();
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .Append(menuRefs.settingsBtn.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(menuRefs.settingsBtn.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => GameManager.Instance.loadSceneSynchronously("SettingsMenu", LoadSceneMode.Additive))
                .SetLink(gameObject);
        }
        
        private void OnRetryBtnClick() {
            _seq?.Kill();
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .Append(menuRefs.retryBtn.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(menuRefs.retryBtn.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => GameManager.Instance.ReloadCurrentSceneSynchronously())
                .SetLink(gameObject);
        }
        
        private void OnExitBtnClick() {
            _seq?.Kill();
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .Append(menuRefs.exitBtn.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(menuRefs.exitBtn.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => GameManager.Instance.loadSceneSynchronously("GameStartPage"));
        }

        private void OnGamePaused() {
            menuRefs.obj.transform.localScale = Vector3.zero;
            UIUtil.SetUIVisible(_canvasGroup, true);
            menuRefs.obj.transform.DOScale(1f, 0.3f)
                .SetUpdate(true)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
        }

        private void OnGameResumed() {
            menuRefs.obj.transform.DOScale(0.1f, 0.3f)
                .SetUpdate(true)
                .SetEase(Ease.OutBack)
                .OnComplete( () => UIUtil.SetUIVisible(_canvasGroup, false) )
                .SetLink(gameObject);
        }

        private void OnDestroy() {
            _seq?.Kill();
            _seq = null;

            menuRefs.resumeBtn.onClick.RemoveListener(OnResumeBtnClick);
            menuRefs.settingsBtn.onClick.RemoveListener(OnSettingsBtnClick);
            menuRefs.retryBtn.onClick.RemoveListener(OnRetryBtnClick);
            menuRefs.exitBtn.onClick.RemoveListener(OnExitBtnClick);
            LevelEventCenter.OnGamePaused -= OnGamePaused;
            LevelEventCenter.OnGameResumed -= OnGameResumed;
        }
    }
}