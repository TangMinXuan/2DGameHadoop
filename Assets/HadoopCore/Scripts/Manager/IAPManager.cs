using System;
using System.Collections;
using HadoopCore.Scripts.Shared;
using HadoopCore.Scripts.UI;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.SceneManagement;

namespace HadoopCore.Scripts.Manager {

    public class IAPManager : MonoBehaviour, IDetailedStoreListener {
        // ────────────────────── Singleton ──────────────────────
        public static IAPManager Instance { get; private set; }

        // ────────────────────── ★ 平台开关: 统一从 BuildConfig.Instance.CurrentTarget 读取 ──────────────────────
        private PlatformTarget CurrentBuildTarget => BuildEnvConfig.Instance != null
            ? BuildEnvConfig.Instance.CurrentTarget
            : PlatformTarget.IOS;
        // ────────────────────── Product IDs ──────────────────────
        public static class ProductIds {
            public const string RemoveAds = "ads_removed";
        }

        // ────────────────────── Persistence Keys ──────────────────────
        private static class PrefKeys {
            public const string AdsRemoved = "ads_removed";
        }

        // ────────────────────── Log Prefix ──────────────────────
        private const string Tag = "[IAP]";

        // ────────────────────── Events ──────────────────────
        /// <summary>IAP 系统初始化成功.</summary>
        public event Action OnInitializedSuccessfully;

        /// <summary>某个产品购买成功, 参数为 productId.</summary>
        public event Action<string> OnPurchaseSucceeded;


        /// <summary>去广告权限变化, true = 已拥有. 购买/恢复成功时触发.</summary>
        public event Action<bool> OnRemoveAdsEntitlementChanged;


        // ────────────────────── State ──────────────────────
        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;
        private bool _initializeCalled;
        [SerializeField] private float retryDelay = 5f;
        private ToastUI _toastUI;

        private void RefreshToastUI() {
            var go = GameObject.Find("ToastPanel");
            if (go != null)
                _toastUI = go.GetComponent<ToastUI>();
        }

        // ────────────────────── Public Properties ──────────────────────

        /// <summary>IAP 系统是否已成功初始化.</summary>
        public bool IsInitialized => _storeController != null && _extensionProvider != null;

        /// <summary>
        /// 用户是否已拥有去广告权限.<br/>
        /// Fake Store 模式: 以 PlayerPrefs 为准.<br/>
        /// 真实商店模式: 以 product.hasReceipt 为主, PlayerPrefs 为 fallback.
        /// </summary>
        public bool IsRemoveAdsOwned {
            get {
                if (IsInitialized) {
                    var product = _storeController.products.WithID(ProductIds.RemoveAds);
                    if (product != null && product.hasReceipt) return true;
                }
                return PlayerPrefs.GetInt(PrefKeys.AdsRemoved, 0) == 1;
            }
        }

        // ────────────────────── Lifecycle ──────────────────────

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshToastUI();
        }

        private async void Start() {
            await UnityServices.InitializeAsync();
            Initialize();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            _toastUI = null; // 清除旧引用，强制重新查找
            RefreshToastUI();
        }

        private void OnDestroy() {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        // ────────────────────── Public API ──────────────────────

        /// <summary>
        /// 初始化 Unity IAP. 建议在启动场景中调用一次.
        /// 重复调用会被忽略并 log 提示.
        /// </summary>
        private void Initialize() {
            if (IsInitialized || _initializeCalled) { return; }
            _initializeCalled = true;

            var module = StandardPurchasingModule.Instance();

            if (CurrentBuildTarget == PlatformTarget.IOS) {
                // ── 真机 iOS，连接 App Store ──────────────────────────────
                Debug.Log($"{Tag} [iOS] Connecting to App Store.");
            } else if (CurrentBuildTarget == PlatformTarget.Editor) {
                // ── Fake Store 模式 (Editor 开发阶段) ────────────────────
                // Default: 全自动成功，不弹任何窗口
                // StandardUser: 购买时弹窗，初始化自动成功
                // DeveloperUser: 初始化+购买都弹窗
                module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;
                module.useFakeStoreAlways = true;
                Debug.Log($"{Tag} [FakeStore] Fake Store enabled.");
            } else {
                // ── 不支持的平台，拒绝初始化 ─────────────────────────────
                Debug.LogError($"{Tag} Unsupported platform – IAP initialization aborted.");
                _initializeCalled = false;
                return;
            }

            var builder = ConfigurationBuilder.Instance(module);
            builder.AddProduct(ProductIds.RemoveAds, ProductType.NonConsumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public void BuyRemoveAds() {
            if (IsRemoveAdsOwned) {
                Debug.Log($"{Tag} Remove Ads is already owned. Purchase skipped.");
                _toastUI?.ShowToastMsg("Remove Ads is already owned");
                OnPurchaseSucceeded?.Invoke(ProductIds.RemoveAds);
                return;
            } 

            if (!IsInitialized) {
                Debug.LogWarning($"{Tag} IAP not initialized yet.");
                _toastUI?.ShowToastMsg("App Store is temporarily unavailable, please try again later", 2);
                return;
            }
            
            var product = _storeController.products.WithID(ProductIds.RemoveAds);
            if (product == null || !product.availableToPurchase) {
                Debug.LogWarning($"{Tag} Product not available: {ProductIds.RemoveAds}. " +
                                 "Check App Store Connect configuration.");
                _toastUI?.ShowToastMsg($"Product not available: {ProductIds.RemoveAds}. please try again later", 2);
                return;
            }

            Debug.Log($"{Tag} Initiating purchase: {ProductIds.RemoveAds}");
            _storeController.InitiatePurchase(ProductIds.RemoveAds);
        }
        
        public void RestorePurchases() {
            if (CurrentBuildTarget == PlatformTarget.IOS) {
                if (!IsInitialized) {
                    Debug.LogWarning($"{Tag} Cannot restore – IAP not initialized.");
                    return;
                }
                Debug.Log($"{Tag} Restoring purchases on iOS …");
                _toastUI?.ShowToastMsg("Restoring purchases on iOS …");
                var apple = _extensionProvider.GetExtension<IAppleExtensions>();
                apple.RestoreTransactions((success, error) => {
                    if (success) {
                        Debug.Log($"{Tag} Restore completed successfully.");
                        _toastUI?.ShowToastMsg("Product: Removed Ad restore successfully");
                    } else {
                        Debug.LogWarning($"{Tag} Restore failed: {error}");
                        _toastUI?.ShowToastMsg("Product: Removed Ad restore failed");
                    }
                });
            }
            else if (CurrentBuildTarget == PlatformTarget.Editor) {
                // Fake Store 模式: 权限由 PlayerPrefs 持久化, 无需走商店恢复流程.
                Debug.Log($"{Tag} [FakeStore] RestorePurchases skipped – checking local PlayerPrefs.");
                if (PlayerPrefs.GetInt(PrefKeys.AdsRemoved, 0) == 1)
                    OnRemoveAdsEntitlementChanged?.Invoke(true);
            }
            else {
                Debug.LogWarning($"{Tag} RestorePurchases not supported on this platform.");
            }
        }

        /// <summary>
        /// 获取产品的本地化价格字符串 (例如 "¥6.00"), 用于 UI 展示.<br/>
        /// Fake Store 模式下返回兜底价格 "$1.99".<br/>
        /// 返回 null 表示产品不存在或 IAP 未初始化.
        /// </summary>
        public string GetLocalizedPrice(string productId) {
            if (CurrentBuildTarget == PlatformTarget.IOS) {
                if (!IsInitialized) {
                    return null;
                }
                var product = _storeController.products.WithID(productId);
                return product?.metadata?.localizedPriceString;
            }
            if (CurrentBuildTarget == PlatformTarget.Editor)
                return "$1.99";
            return null;
        }

        // ────────────────────── IStoreListener Implementation ──────────────────────

        /// <summary>Unity IAP 初始化成功回调.</summary>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions) {
            Debug.Log($"{Tag} Initialization succeeded.");
            _storeController = controller;
            _extensionProvider = extensions;

            // 检查是否已有去广告 receipt (真实商店重装/恢复场景)
            var product = controller.products.WithID(ProductIds.RemoveAds);
            if (product != null && product.hasReceipt) {
                Debug.Log($"{Tag} Existing receipt found for Remove-Ads. Granting entitlement.");
                GrantRemoveAdsEntitlement();
            }


            OnInitializedSuccessfully?.Invoke();
        }

        public void OnInitializeFailed(InitializationFailureReason error) {
            _initializeCalled = false;
            Debug.LogError($"{Tag} Initialization failed: {error}. Retrying in {retryDelay}s…");
            StartCoroutine(RetryInitializeAfterDelay());
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message) {
            _initializeCalled = false;
            Debug.LogError($"{Tag} Initialization failed: {error} – {message}. Retrying in {retryDelay}s…");
            StartCoroutine(RetryInitializeAfterDelay());
        }

        /// <summary>
        /// 购买流程完成时回调. 返回 Complete 表示确认收货, Pending 表示延迟.
        /// </summary>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) {
            string productId = args.purchasedProduct.definition.id;
            Debug.Log($"{Tag} ProcessPurchase: {productId}");

            if (string.Equals(productId, ProductIds.RemoveAds, StringComparison.Ordinal)) {
                GrantRemoveAdsEntitlement();
                OnPurchaseSucceeded?.Invoke(productId);
                Debug.Log($"{Tag} Remove-Ads purchase processed successfully.");
            } else {
                Debug.LogWarning($"{Tag} Unrecognized product: {productId}");
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription) {
            string productId = product.definition.id;
            if (failureDescription.reason == PurchaseFailureReason.UserCancelled)
                Debug.Log($"{Tag} Purchase cancelled by user: {productId}");
            else
                Debug.Log($"{Tag} Purchase failed: {productId}, reason={failureDescription.reason}, msg={failureDescription.message}");
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) {
            string productId = product.definition.id;
            if (failureReason == PurchaseFailureReason.UserCancelled)
                Debug.Log($"{Tag} Purchase cancelled by user: {productId}");
            else
                Debug.LogError($"{Tag} Purchase failed: {productId}, reason={failureReason}");
        }

        // ────────────────────── Internal Helpers ──────────────────────

        /// <summary>
        /// 授予去广告权限: 写入 PlayerPrefs + 触发事件.
        /// 幂等操作, 多次调用安全.
        /// </summary>
        private void GrantRemoveAdsEntitlement() {
            if (PlayerPrefs.GetInt(PrefKeys.AdsRemoved, 0) == 0) {
                _toastUI?.ShowToastMsg("Product: Removed Ad purchased successfully"); // 仅第一次购买时显示成功消息
            }
            PlayerPrefs.SetInt(PrefKeys.AdsRemoved, 1);
            PlayerPrefs.Save();
            Debug.Log($"{Tag} Entitlement granted: Remove-Ads.");
            OnRemoveAdsEntitlementChanged?.Invoke(true);
        }

        private IEnumerator RetryInitializeAfterDelay() {
            yield return new WaitForSeconds(retryDelay);
            Initialize();
        }

        #if UNITY_EDITOR
        [ContextMenu("Debug/Reset Remove-Ads Entitlement")]
        private void DebugResetEntitlement() {
            PlayerPrefs.DeleteKey(PrefKeys.AdsRemoved);
            PlayerPrefs.Save();
            Debug.Log($"{Tag} [Debug] Remove-Ads entitlement reset.");
            OnRemoveAdsEntitlementChanged?.Invoke(false);
        }

        [ContextMenu("Debug/Simulate Own Remove-Ads")]
        private void DebugSimulateOwnRemoveAds() {
            GrantRemoveAdsEntitlement();
            Debug.Log($"{Tag} [Debug] Remove-Ads simulated as owned.");
        }
        #endif
    }
}