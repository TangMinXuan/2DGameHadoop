using System;
using System.Collections;
using HadoopCore.Scripts.Shared;
using UnityEngine;

namespace HadoopCore.Scripts.Manager {
    public class AdManager : MonoBehaviour {
        public static AdManager Instance { get; private set; }
        
        public bool IsInitialized { get; private set; }
        
        [SerializeField] public bool enableAdInitialization; // 总开关: 关闭时不进入平台判断, 不初始化广告
        
        [SerializeField] private float RetryDelay = 5f;
        
        
        // ────────────────────── ★ 平台开关: 统一从 BuildConfig.Instance.CurrentTarget 读取 ──────────────────────
        private PlatformTarget CurrentBuildTarget => BuildEnvConfig.Instance.CurrentTarget;
        private bool IsAdPlatformSupported => CurrentBuildTarget == PlatformTarget.IOS ||
                                              CurrentBuildTarget == PlatformTarget.Editor;

        // ────────────────────── Rewarded ──────────────────────
        private LevelPlayRewardedAd rewardedVideoAd;
        private bool _isLoading = false;
        private bool _hasRewardTicket = false;
        private Action _onAdClosed = null;

        // ────────────────────── Interstitial ──────────────────────
        private LevelPlayInterstitialAd interstitialAd;
        private bool _isInterstitialLoading = false;
        private Action _onInterstitialClosed = null;

        // ────────────────────── Banner ──────────────────────
        private LevelPlayBannerAd bannerAd;
        private bool _bannerVisible = false;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() {
            if (!enableAdInitialization) {
                Debug.Log("[AdManager] Ad initialization is disabled by switch.");
                return;
            }

            if (!IsAdPlatformSupported) {
                Debug.LogWarning($"[AdManager] Ad initialization blocked on platform: {CurrentBuildTarget}");
                return;
            }

            if (CurrentBuildTarget == PlatformTarget.Editor) {
                LevelPlay.ValidateIntegration();
                LevelPlay.SetMetaData("is_test_suite", "enable");
            }
            LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
            LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
            LevelPlay.Init(AdConfig.AppKey);
        }
        
        public bool ShowRewardedVideoAd(Action onClosed) {
            if (onClosed == null) return false;
            if (IsInitialized && rewardedVideoAd != null && rewardedVideoAd.IsAdReady()) {
                _onAdClosed = onClosed;
                rewardedVideoAd.ShowAd();
                return true;
            }
            return false;
        }

        public bool ConsumeRewardTicket() {
            if (!_hasRewardTicket) {
                return false;
            }
            _hasRewardTicket = false;
            return true;
        }
        
        private void LoadRewardedVideoAd() {
            if (!IsInitialized || rewardedVideoAd == null) {
                return;
            }
            if (_isLoading) {
                return; // 防止重复加载
            }
            _isLoading = true;
            rewardedVideoAd.LoadAd();
        }

        void SdkInitializationCompletedEvent(LevelPlayConfiguration config) {
            LevelPlay.OnImpressionDataReady += ImpressionDataReadyEvent;

            // Rewarded
            rewardedVideoAd = new LevelPlayRewardedAd(AdConfig.RewardedVideoAdUnitId);
            rewardedVideoAd.OnAdLoaded += RewardedVideoOnLoadedEvent;
            rewardedVideoAd.OnAdLoadFailed += RewardedVideoOnAdLoadFailedEvent;
            rewardedVideoAd.OnAdDisplayed += RewardedVideoOnAdDisplayedEvent;
            rewardedVideoAd.OnAdRewarded += RewardedVideoOnAdRewardedEvent;
            rewardedVideoAd.OnAdClosed += RewardedVideoOnAdClosedEvent;
            rewardedVideoAd.OnAdInfoChanged += RewardedVideoOnAdInfoChangedEvent;

            // Interstitial
            interstitialAd = new LevelPlayInterstitialAd(AdConfig.InterstitialAdUnitId);
            interstitialAd.OnAdLoaded += InterstitialOnLoadedEvent;
            interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
            interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
            interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;

            // Banner
            bannerAd = new LevelPlayBannerAd(AdConfig.BannerAdUnitId, LevelPlayAdSize.BANNER, LevelPlayBannerPosition.BottomCenter);
            bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
            bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;

            IsInitialized = true;
            
            if (CurrentBuildTarget == PlatformTarget.Editor) {
                Debug.Log("[AdManager] 当前为 Editor 模式，已启用 LevelPlay 测试套件");
                LevelPlay.LaunchTestSuite();
            }

            // 进入游戏立即预加载
            // LoadRewardedVideoAd();
            LoadInterstitialAd();
        }

        void SdkInitializationFailedEvent(LevelPlayInitError error) {
            IsInitialized = false;
            Debug.Log($"[LevelPlaySample] Received SdkInitializationFailedEvent with Error: {error}");
        }


        void RewardedVideoOnLoadedEvent(LevelPlayAdInfo adInfo) {
            _isLoading = false;
            Debug.Log("[RewardedAdManager] 广告加载成功，已就绪");
        }

        void RewardedVideoOnAdLoadFailedEvent(LevelPlayAdError error) {
            _isLoading = false;
            Debug.LogWarning($"[RewardedAdManager] 广告加载失败: {error}，{RetryDelay}秒后重试");
            StartCoroutine(RetryLoadAfterDelay());
        }

        void RewardedVideoOnAdDisplayedEvent(LevelPlayAdInfo adInfo) {
            AudioManager.Instance?.PauseBgm();
        }

        void RewardedVideoOnAdDisplayedFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) {
            Debug.LogError($"[RewardedAdManager] 广告展示失败: {error}");
            AudioManager.Instance?.ResumeBgm();
            // 展示失败也要重新加载
            LoadRewardedVideoAd();
        }

        void RewardedVideoOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward reward) {
            Debug.Log($"[RewardedAdManager] 发放奖励: {reward.Name} x{reward.Amount}");
            _hasRewardTicket = true;
        }

        void RewardedVideoOnAdClickedEvent(LevelPlayAdInfo adInfo) {
        }

        void RewardedVideoOnAdClosedEvent(LevelPlayAdInfo adInfo) {
            Debug.Log("[RewardedAdManager] 广告已关闭，立即预加载下一个");
            AudioManager.Instance?.ResumeBgm();
            LoadRewardedVideoAd();
            
            // 延迟一帧再判断，给 OnAdRewarded 留一点时间（极端情况下回调顺序颠倒）
            StartCoroutine(DelayedClosedCallback());
        }
        
        private IEnumerator DelayedClosedCallback() {
            yield return new WaitForSeconds(0.5f); // 等0.5秒
            var callback = _onAdClosed;
            _onAdClosed = null;
            callback?.Invoke();
        }

        void RewardedVideoOnAdInfoChangedEvent(LevelPlayAdInfo adInfo) {
        }

        void ImpressionDataReadyEvent(LevelPlayImpressionData impressionData) {
            // Debug.Log($"[LevelPlaySample] Received ImpressionDataReadyEvent ToString(): {impressionData}");
            // Debug.Log($"[LevelPlaySample] Received ImpressionDataReadyEvent allData: {impressionData.AllData}");
        }
        
        private IEnumerator RetryLoadAfterDelay() {
            yield return new WaitForSeconds(RetryDelay);
            LoadRewardedVideoAd();
        }

        // ════════════════════════════════════════════
        // Interstitial  公开 API
        // ════════════════════════════════════════════

        /// <summary>展示插屏广告，无广告可用时返回 false。onClosed 在广告关闭后回调。</summary>
        public bool ShowInterstitialAd(Action onClosed = null) {
            if (!IsInitialized || interstitialAd == null || !interstitialAd.IsAdReady()) return false;
            _onInterstitialClosed = onClosed;
            interstitialAd.ShowAd();
            return true;
        }

        private void LoadInterstitialAd() {
            if (!IsInitialized || interstitialAd == null) return;
            if (_isInterstitialLoading) return;
            _isInterstitialLoading = true;
            interstitialAd.LoadAd();
        }

        // ────── Interstitial 事件回调 ──────

        void InterstitialOnLoadedEvent(LevelPlayAdInfo adInfo) {
            _isInterstitialLoading = false;
            Debug.Log("[InterstitialAd] 加载成功，已就绪");
        }

        void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error) {
            _isInterstitialLoading = false;
            Debug.LogWarning($"[InterstitialAd] 加载失败: {error}，{RetryDelay}秒后重试");
            StartCoroutine(RetryInterstitialAfterDelay());
        }

        void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo) {
            AudioManager.Instance?.PauseBgm();
        }

        void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo) {
            Debug.Log("[InterstitialAd] 广告已关闭，立即预加载下一个");
            AudioManager.Instance?.ResumeBgm();
            LoadInterstitialAd();
            StartCoroutine(DelayedInterstitialClosedCallback());
        }

        private IEnumerator DelayedInterstitialClosedCallback() {
            yield return new WaitForSeconds(0.5f);
            var callback = _onInterstitialClosed;
            _onInterstitialClosed = null;
            callback?.Invoke();
        }

        private IEnumerator RetryInterstitialAfterDelay() {
            yield return new WaitForSeconds(RetryDelay);
            LoadInterstitialAd();
        }

        // ════════════════════════════════════════════
        // Banner  公开 API
        // ════════════════════════════════════════════
        public void ShowBanner() {
            if (!IsInitialized || bannerAd == null) return;
            if (IAPManager.Instance.IsRemoveAdsOwned) {
                Debug.Log("[BannerAd] 已购买去广告，跳过 Banner 展示");
                return;
            }
            _bannerVisible = true;
            bannerAd.LoadAd();
        }

        public void HideBanner() {
            if (bannerAd == null) return;
            _bannerVisible = false;
            bannerAd.HideAd();
        }

        // ────── Banner 事件回调 ──────

        void BannerOnAdLoadedEvent(LevelPlayAdInfo adInfo) {
            Debug.Log("[BannerAd] 加载成功，已展示");
        }

        void BannerOnAdLoadFailedEvent(LevelPlayAdError error) {
            Debug.LogWarning($"[BannerAd] 加载失败: {error}，{RetryDelay}秒后重试");
            StartCoroutine(RetryBannerAfterDelay());
        }

        private IEnumerator RetryBannerAfterDelay() {
            yield return new WaitForSeconds(RetryDelay);
            if (bannerAd != null && IsInitialized && _bannerVisible) {
                bannerAd.LoadAd();
            }
        }

        private void OnDisable() {
            IsInitialized = false;
            // Rewarded
            if (rewardedVideoAd != null) {
                rewardedVideoAd.OnAdLoaded -= RewardedVideoOnLoadedEvent;
                rewardedVideoAd.OnAdLoadFailed -= RewardedVideoOnAdLoadFailedEvent;
                rewardedVideoAd.OnAdDisplayed -= RewardedVideoOnAdDisplayedEvent;
                rewardedVideoAd.OnAdRewarded -= RewardedVideoOnAdRewardedEvent;
                rewardedVideoAd.OnAdClosed -= RewardedVideoOnAdClosedEvent;
                rewardedVideoAd.OnAdInfoChanged -= RewardedVideoOnAdInfoChangedEvent;
                rewardedVideoAd.DestroyAd();
                rewardedVideoAd = null;
            }

            // Interstitial
            if (interstitialAd != null) {
                interstitialAd.OnAdLoaded -= InterstitialOnLoadedEvent;
                interstitialAd.OnAdLoadFailed -= InterstitialOnAdLoadFailedEvent;
                interstitialAd.OnAdDisplayed -= InterstitialOnAdDisplayedEvent;
                interstitialAd.OnAdClosed -= InterstitialOnAdClosedEvent;
                interstitialAd.DestroyAd();
                interstitialAd = null;
            }

            // Banner
            if (bannerAd != null) {
                bannerAd.OnAdLoaded -= BannerOnAdLoadedEvent;
                bannerAd.OnAdLoadFailed -= BannerOnAdLoadFailedEvent;
                bannerAd.DestroyAd();
                bannerAd = null;
            }
        }

        private void OnDestroy() {
            if (Instance != this) return;
        }
    }
}