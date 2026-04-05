using System.Collections.Generic;
using DG.Tweening;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Shared;
using HadoopCore.Scripts.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HadoopCore.Scripts.SceneController {
    public class StartMenuController : MonoBehaviour {
        [Header("Button References")] 
        [SerializeField] private Button btnStartGame;
        [SerializeField] private Button btnSettings;
        [SerializeField] private Button btnAbout;
        [SerializeField] private Button shoppingCartBtn;
        [SerializeField] private Button adRemovedOwnedIcon;
        [SerializeField] private RemovedAdPurchaseUI removedAdPurchaseUI;
        [SerializeField] private ToastUI toastUI;

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
            
            if (shoppingCartBtn != null) {
                shoppingCartBtn.onClick.AddListener(OnCartBtnClicked);
            }            
            
            if (adRemovedOwnedIcon != null) {
                adRemovedOwnedIcon.onClick.AddListener(OnAdRemovedOwnedIconClicked);
            }
        }

        private void Start() {
            // IAP / Ad 相关UI
            if (IAPManager.Instance.enableIapInitialization && AdManager.Instance.enableAdInitialization) {
                ShoppingCartUI();
            }
        }

        private void ShoppingCartUI() {
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
        }
    }
}