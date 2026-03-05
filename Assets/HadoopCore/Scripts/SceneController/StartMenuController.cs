using System;
using DG.Tweening;
using HadoopCore.Scripts.Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HadoopCore.Scripts.SceneController {
    public class StartMenuController : MonoBehaviour {
        [Header("Button References")] [SerializeField]
        private Button btnStartGame;

        [SerializeField] private Button btnSettings;
        [SerializeField] private Button btnAbout;

        private Sequence _seq;

        private void Awake() {
            if (btnStartGame != null) {
                btnStartGame.onClick.AddListener(OnStartGameClicked);
            }

            if (btnSettings != null) {
                btnSettings.onClick.AddListener(OnSettingsClicked);
            }

            if (btnAbout != null) {
                btnAbout.onClick.AddListener(OnAboutClicked);
            }
        }

        private void Start() {
            // 1. 帧率设置：尝试跑满 120Hz，普通设备会自动降级到 60Hz
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            
            // 2. 沉浸式设置：防止误触 Home 条 (仅 iOS 有效)
            #if UNITY_IOS
            UnityEngine.iOS.Device.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All;
            #endif
        
            // 3. 永不息屏 (可选，防止玩家思考时手机自动黑屏)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void OnStartGameClicked() {
            AudioManager.Instance.PlayBtnSfx();
            _seq = DOTween.Sequence()
                .SetId("StartGameBtnTween")
                .Append(btnStartGame.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(btnStartGame.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => GameManager.Instance.loadSceneSynchronously("LevelSelectMenu"))
                .SetLink(gameObject);
        }

        private void OnSettingsClicked() {
            AudioManager.Instance.PlayBtnSfx();
            _seq = DOTween.Sequence()
                .SetId("SettingsBtnTween")
                .Append(btnSettings.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(btnSettings.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => GameManager.Instance.loadSceneSynchronously("SettingsMenu", LoadSceneMode.Additive))
                .SetLink(gameObject);
        }

        private void OnAboutClicked() {
            AudioManager.Instance.PlayBtnSfx();
            _seq = DOTween.Sequence()
                .SetId("AboutBtnTween")
                .Append(btnAbout.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(btnAbout.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => GameManager.Instance.loadSceneSynchronously("AboutPage"))
                .SetLink(gameObject);
        }

        private void OnDestroy() {
            _seq?.Kill();
            _seq = null;

            // Clean up listeners
            if (btnStartGame != null) {
                btnStartGame.onClick.RemoveListener(OnStartGameClicked);
            }

            if (btnSettings != null) {
                btnSettings.onClick.RemoveListener(OnSettingsClicked);
            }

            if (btnAbout != null) {
                btnAbout.onClick.RemoveListener(OnAboutClicked);
            }
        }
    }
}