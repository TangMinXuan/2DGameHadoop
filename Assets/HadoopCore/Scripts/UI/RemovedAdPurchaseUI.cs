using System.Text;
using DG.Tweening;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Button = UnityEngine.UI.Button;

namespace HadoopCore.Scripts.UI {
    public class RemovedAdPurchaseUI : MonoBehaviour {
        
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button purchaseBtn;
        [SerializeField] private Button closeBtn;
        [SerializeField] private Button restorePurchaseBtn;
        [SerializeField] private TMP_Text removeAdPriceText;

        private TMP_Text _closeBtnText;
        private string _closeBtnOriginalText;
        private Sequence _countdownSeq;
        private readonly StringBuilder _sb = new StringBuilder();
        private CanvasGroup _menuCanvasGroup;
        private Sequence _seq;
        
        private void Start() {
            _menuCanvasGroup = menuPanel.GetComponent<CanvasGroup>();
            UIUtil.SetUIVisible(_menuCanvasGroup, false);
            purchaseBtn.onClick.AddListener(OnPurchaseBtnClicked);
            closeBtn.onClick.AddListener(OnCloseBtnClicked);
            restorePurchaseBtn.onClick.AddListener(OnRestorePurchaseBtnClicked);

            _closeBtnText = closeBtn.GetComponentInChildren<TMP_Text>();
            if (_closeBtnText != null) {
                _closeBtnOriginalText = _closeBtnText.text;
            }
            
            removeAdPriceText.SetText(IAPManager.Instance.GetLocalizedPrice(IAPManager.ProductIds.RemoveAds));
        }

        public void PopupPanel(UnityAction onPurchaseBtnClicked = null, UnityAction onCloseBtnClicked = null, int closeBtnDelay = 0) {
            removeAdPriceText.SetText(IAPManager.Instance.GetLocalizedPrice(IAPManager.ProductIds.RemoveAds));
            UIUtil.SetUIVisible(_menuCanvasGroup, true);
            menuPanel.transform.DOScale(1f, 0.3f)
                .SetUpdate(true)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
            
            if (onPurchaseBtnClicked != null) {
                purchaseBtn.onClick.AddListener(onPurchaseBtnClicked);
                purchaseBtn.onClick.AddListener(() => AudioManager.Instance.PlayBtnSfx());
            }
            if (onCloseBtnClicked != null) {
                closeBtn.onClick.AddListener(onCloseBtnClicked);
                closeBtn.onClick.AddListener(() => AudioManager.Instance.PlayBtnSfx());
            }
            if (closeBtnDelay > 0) {
                closeBtn.interactable = false;
                _countdownSeq = DOTween.Sequence();
                for (int i = closeBtnDelay; i >= 1; i--) {
                    int remaining = i; // 闭包捕获
                    _countdownSeq.AppendCallback(() => {
                        _sb.Clear();
                        _sb.Append(remaining);
                        _sb.Append("s  ");
                        _sb.Append(_closeBtnOriginalText);
                        _closeBtnText?.SetText(_sb);
                    });
                    _countdownSeq.AppendInterval(1f);
                }
                _countdownSeq.AppendCallback(() => {
                    _closeBtnText?.SetText(_closeBtnOriginalText);
                    closeBtn.interactable = true;
                });
            }
        }

        private void OnPurchaseBtnClicked() {
            _seq = DOTween.Sequence()
                .SetId("PurchaseBtnTween")
                .Append(purchaseBtn.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(purchaseBtn.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => {
                    menuPanel.transform.DOScale(0.1f, 0.3f)
                        .SetUpdate(true)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() => {
                            IAPManager.Instance.BuyRemoveAds();
                            UIUtil.SetUIVisible(_menuCanvasGroup, false);
                        } )
                        .SetLink(gameObject);
                })
                .SetLink(gameObject);
        }

        private void OnCloseBtnClicked() {
            _seq = DOTween.Sequence()
                .SetId("closeBtnTween")
                .Append(closeBtn.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(closeBtn.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => {
                    menuPanel.transform.DOScale(0.1f, 0.3f)
                        .SetUpdate(true)
                        .SetEase(Ease.OutBack)
                        .OnComplete( () => UIUtil.SetUIVisible(_menuCanvasGroup, false) )
                        .SetLink(gameObject);
                })
                .SetLink(gameObject);
        }
        
        private void OnRestorePurchaseBtnClicked() {
            _seq = DOTween.Sequence()
                .SetId("RestorePurchase")
                .Append(restorePurchaseBtn.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(restorePurchaseBtn.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => {
                    menuPanel.transform.DOScale(0.1f, 0.3f)
                        .SetUpdate(true)
                        .SetEase(Ease.OutBack)
                        .OnComplete( () => {
                            UIUtil.SetUIVisible(_menuCanvasGroup, false);
                            IAPManager.Instance.RestorePurchases();
                        })
                        .SetLink(gameObject);
                })
                .SetLink(gameObject);
        }
        
        

        private void OnDestroy() {
            _countdownSeq?.Kill();
            _seq?.Kill();
        }
    }
}