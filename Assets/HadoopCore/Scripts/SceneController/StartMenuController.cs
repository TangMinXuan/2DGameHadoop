using System;
using DG.Tweening;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HadoopCore.Scripts.SceneController {
    public class StartMenuController : MonoBehaviour {
        [Header("Button References")] [SerializeField]
        private Button btnStartGame;

        [SerializeField] private Button btnSettings;
        [SerializeField] private Button btnAbout;
        [SerializeField] private Button logoBtn;
        [SerializeField] private Button shoppingCartBtn;
        [SerializeField] private Button adRemovedOwnedIcon;
        [SerializeField] private RemovedAdPurchaseUI removedAdPurchaseUI;
        [SerializeField] private ToastUI toastUI;

        private Sequence _seq;

        // Easter egg: 连续点击 logoBtn 5次触发
        private int _logoClickCount = 0;
        private float _lastLogoClickTime = 0f;
        private const int EggClickThreshold = 5;
        private const float EggTimeWindow = 1.5f;

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
            
            if (logoBtn != null) {
                logoBtn.onClick.AddListener(OnLogoClicked);
            }
            
            if (shoppingCartBtn != null) {
                shoppingCartBtn.onClick.AddListener(OnCartBtnClicked);
            }            
            
            if (adRemovedOwnedIcon != null) {
                adRemovedOwnedIcon.onClick.AddListener(OnAdRemovedOwnedIconClicked);
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
            
            // 4. 如果IAP初始化成功, 根据玩家是否已购买去显示对应UI
            if (IAPManager.Instance.IsRemoveAdsOwned) {
                shoppingCartBtn.gameObject.SetActive(false);
                adRemovedOwnedIcon.gameObject.SetActive(true);
            } else {
                shoppingCartBtn.gameObject.SetActive(true);
                adRemovedOwnedIcon.gameObject.SetActive(false);
            }
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

        private void OnLogoClicked() {
            float now = Time.unscaledTime;
            if (now - _lastLogoClickTime > EggTimeWindow) {
                _logoClickCount = 1;
            } else {
                _logoClickCount++;
            }
            _lastLogoClickTime = now;

            // 每次点击给轻微弹跳反馈，让玩家感知"正在积攒"
            logoBtn.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 5, 0.5f).SetLink(gameObject);

            if (_logoClickCount >= EggClickThreshold) {
                _logoClickCount = 0;
                TriggerEasterEgg();
            }
        }

        private void TriggerEasterEgg() {
            Debug.Log("[EasterEgg] 彩蛋触发！连续点击Logo 5次，你发现了隐藏彩蛋！");
        }

        private void OnAdRemovedOwnedIconClicked() {
            toastUI.ShowToastMsg("Remove Ads is already owned");
        }
        
        private void OnCartBtnClicked() {
            AudioManager.Instance.PlayBtnSfx();
            _seq = DOTween.Sequence()
                .SetId("CartBtnTween")
                .Append(shoppingCartBtn.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(shoppingCartBtn.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => removedAdPurchaseUI.PopupPanel(closeBtnDelay:0))
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

            if (logoBtn != null) {
                logoBtn.onClick.RemoveListener(OnLogoClicked);
            }
        }
    }
}