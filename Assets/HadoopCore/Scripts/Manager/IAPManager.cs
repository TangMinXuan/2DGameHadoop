// ============================================================================
// IAPManager.cs  —  Single entry point for all Unity IAP logic.
//
// Platform : iOS only (v1, Google Play not in scope)
//
// ── 平台分支说明 ────────────────────────────────────────────────────────────
//   • UNITY_IOS 已定义 (真机/上架)  → 连接 App Store, 支持 RestorePurchases.
//   • UNITY_IOS 未定义 (Editor/其他) → 自动使用 Fake Store 弹窗, 无需任何额外
//     Scripting Define Symbol, receipt 不可靠, 权限以 PlayerPrefs 为准.
//
// ── 使用示例 (UI / 业务代码) ────────────────────────────────────────────────
//   IAPManager.Instance.OnInitializedSuccessfully    += () => RefreshPriceUI();
//   IAPManager.Instance.OnPurchaseSucceeded          += pid => Debug.Log($"Purchased {pid}");
//   IAPManager.Instance.OnRemoveAdsEntitlementChanged += owned => adBanner.SetActive(!owned);
//
//   // 初始化 (通常在启动场景 Awake/Start 中调用一次)
//   IAPManager.Instance.Initialize();
//
//   // 购买去广告 (绑定 Button.onClick)
//   buyBtn.onClick.AddListener(() => IAPManager.Instance.BuyRemoveAds());
//
//   // 查询是否已去广告
//   if (IAPManager.Instance.IsRemoveAdsOwned) { /* 隐藏广告 */ }
// ============================================================================

using System;
using Unity.Services.Core;
using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace HadoopCore.Scripts.Manager {
    /// <summary>
    /// Production-ready IAP manager – Singleton / DontDestroyOnLoad.
    /// Implements <see cref="IDetailedStoreListener"/> for full purchase lifecycle handling.
    /// </summary>
    public class IAPManager : MonoBehaviour, IDetailedStoreListener {
        // ────────────────────── Singleton ──────────────────────
        public static IAPManager Instance { get; private set; }

        // ────────────────────── Product IDs ──────────────────────
        public static class ProductIds {
            public const string RemoveAds = "remove_ads";
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
        }

        private async void Start() {
            // TODO 临时测试, 删除持久化的购买凭证
            PlayerPrefs.DeleteKey(PrefKeys.AdsRemoved);
            PlayerPrefs.Save();

            
            await UnityServices.InitializeAsync(); // 必须先初始化 Unity Services 才能使用 IAP
            Initialize();
            #if UNITY_IOS
            Debug.Log($"{Tag} Platform: iOS – connecting to App Store.");
            #else
            Debug.Log($"{Tag} Platform: non-iOS – Fake Store mode active.");
            #endif
        }

        private void OnDestroy() {
            if (Instance != this) return;
            Instance = null;
        }

        // ────────────────────── Public API ──────────────────────

        /// <summary>
        /// 初始化 Unity IAP. 建议在启动场景中调用一次.
        /// 重复调用会被忽略并 log 提示.
        /// </summary>
        private void Initialize() {
            if (IsInitialized || _initializeCalled) { /* ... */ return; }
            _initializeCalled = true;

            var module = StandardPurchasingModule.Instance();

            #if !UNITY_IOS
            // 非 iOS 平台 (Editor / 其他) 自动启用 Fake Store
            module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;
            module.useFakeStoreAlways = true;
            Debug.Log($"{Tag} [FakeStore] Non-iOS platform detected – Fake Store enabled.");
            #endif

            var builder = ConfigurationBuilder.Instance(module);
            builder.AddProduct(ProductIds.RemoveAds, ProductType.NonConsumable);
            UnityPurchasing.Initialize(this, builder);
        }

        /// <summary>
        /// 发起购买去广告. 若已拥有则不会重复购买.
        /// </summary>
        public void BuyRemoveAds() {
            if (IsRemoveAdsOwned) {
                Debug.Log($"{Tag} Remove-Ads already owned. Purchase skipped.");
                OnPurchaseSucceeded?.Invoke(ProductIds.RemoveAds);
                return;
            }

            if (!IsInitialized) {
                Debug.LogWarning($"{Tag} IAP not initialized yet.");
                return;
            }

            Debug.Log($"{Tag} Initiating purchase: {ProductIds.RemoveAds}");
            _storeController.InitiatePurchase(ProductIds.RemoveAds);
        }


        /// <summary>
        /// 恢复购买 (仅 iOS 真实商店). Fake Store 模式下仅打印日志.
        /// </summary>
        public void RestorePurchases() {
            #if UNITY_IOS
            if (!IsInitialized) {
                Debug.LogWarning($"{Tag} Cannot restore – IAP not initialized.");
                return;
            }

            Debug.Log($"{Tag} Restoring purchases on iOS …");
            var apple = _extensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions((success, error) => {
                if (success) {
                    Debug.Log($"{Tag} Restore completed successfully.");
                    // 恢复的每笔交易会再次触发 ProcessPurchase
                }
                else {
                    Debug.LogWarning($"{Tag} Restore failed: {error}");
                }
            });
            #else
            // Fake Store 模式: 权限由 PlayerPrefs 持久化, 无需走商店恢复流程.
            Debug.Log($"{Tag} [FakeStore] RestorePurchases skipped – checking local PlayerPrefs.");
            if (PlayerPrefs.GetInt(PrefKeys.AdsRemoved, 0) == 1)
                OnRemoveAdsEntitlementChanged?.Invoke(true);
            #endif
        }

        /// <summary>
        /// 获取产品的本地化价格字符串 (例如 "¥6.00"), 用于 UI 展示.<br/>
        /// Fake Store 模式下返回兜底价格 "$1.99".<br/>
        /// 返回 null 表示产品不存在或 IAP 未初始化.
        /// </summary>
        public string GetLocalizedPrice(string productId) {
            #if UNITY_IOS
            if (!IsInitialized) return null;
            var product = _storeController.products.WithID(productId);
            return product?.metadata?.localizedPriceString;
            #else
            return "$1.99";
            #endif
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
            _initializeCalled = false; // 允许重试
            Debug.LogError($"{Tag} Initialization failed: {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message) {
            _initializeCalled = false;
            Debug.LogError($"{Tag} Initialization failed: {error} – {message}");
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
                Debug.LogError($"{Tag} Purchase failed: {productId}, reason={failureDescription.reason}, msg={failureDescription.message}");
        }

        /// <summary>购买失败回调 (IStoreListener 基础接口, 不会被调用因为实现了 IDetailedStoreListener).</summary>
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
            PlayerPrefs.SetInt(PrefKeys.AdsRemoved, 1);
            PlayerPrefs.Save();
            Debug.Log($"{Tag} Entitlement granted: Remove-Ads.");
            OnRemoveAdsEntitlementChanged?.Invoke(true);
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

        [ContextMenu("Debug/Simulate RestorePurchases")]
        private void DebugSimulateRestore() {
            RestorePurchases();
        }
        #endif
    }
}

// ============================================================================
// ── 最小使用示例 (例如挂到按钮所在 Canvas 上的脚本) ──────────────────────────
// ============================================================================
//
// using UnityEngine;
// using UnityEngine.UI;
// using HadoopCore.Scripts.Manager;
//
// public class IAPUIExample : MonoBehaviour {
//     [SerializeField] private Button buyRemoveAdsBtn;
//     [SerializeField] private Button restoreBtn;
//     [SerializeField] private GameObject adBanner;
//     [SerializeField] private Text priceLabel;
//
//     void Start() {
//         var iap = IAPManager.Instance;
//
//         // 1) 订阅事件
//         iap.OnInitializedSuccessfully += () => {
//             Debug.Log("IAP Ready!");
//             string price = iap.GetLocalizedPrice("remove_ads");
//             if (price != null) priceLabel.text = price;
//         };
//
//         iap.OnPurchaseSucceeded += pid => Debug.Log($"Purchased: {pid}");
//
//         iap.OnRemoveAdsEntitlementChanged += owned => {
//             adBanner.SetActive(!owned);
//             buyRemoveAdsBtn.gameObject.SetActive(!owned);
//         };
//
//         // 2) 初始化 (仅需调用一次, 内部防重入)
//         iap.Initialize();
//
//         // 3) 按钮绑定
//         buyRemoveAdsBtn.onClick.AddListener(() => iap.BuyRemoveAds());
//         restoreBtn.onClick.AddListener(() => iap.RestorePurchases());
//
//         // 4) 立即刷新 UI (应对已购买的情况)
//         adBanner.SetActive(!iap.IsRemoveAdsOwned);
//         buyRemoveAdsBtn.gameObject.SetActive(!iap.IsRemoveAdsOwned);
//     }
// }