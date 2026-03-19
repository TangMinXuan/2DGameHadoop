using System;
using System.Collections;
using HadoopCore.Scripts.Shared;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace HadoopCore.Scripts.Manager {
    public class AdManager : MonoBehaviour {
        public static AdManager Instance { get; private set; }
        
        [SerializeField] private float RetryDelay = 5f;
        
        private LevelPlayRewardedAd rewardedVideoAd;
        private bool isAdsEnabled = false;
        private bool _isLoading = false;
        private bool _hasRewardTicket = false;
        private Action _onAdClosed = null;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() {
            LevelPlay.ValidateIntegration();
            LevelPlay.SetMetaData("is_test_suite", "enable"); 
            LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
            LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
            LevelPlay.Init(Shared.AdConfig.AppKey);
        }
        
        public bool ShowRewardedVideoAd(Action onClosed) {
            if (onClosed == null) return false;
            if (isAdsEnabled && rewardedVideoAd != null && rewardedVideoAd.IsAdReady()) {
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
            if (!isAdsEnabled || rewardedVideoAd == null) {
                return;
            }
            if (_isLoading) {
                return; // 防止重复加载
            }
            _isLoading = true;
            rewardedVideoAd.LoadAd();
        }

        void SdkInitializationCompletedEvent(LevelPlayConfiguration config) {
            // Register to ImpressionDataReadyEvent
            LevelPlay.OnImpressionDataReady += ImpressionDataReadyEvent;
            rewardedVideoAd = new LevelPlayRewardedAd(Shared.AdConfig.RewardedVideoAdUnitId);
            rewardedVideoAd.OnAdLoaded += RewardedVideoOnLoadedEvent;
            rewardedVideoAd.OnAdLoadFailed += RewardedVideoOnAdLoadFailedEvent;
            rewardedVideoAd.OnAdDisplayed += RewardedVideoOnAdDisplayedEvent;
            rewardedVideoAd.OnAdRewarded += RewardedVideoOnAdRewardedEvent;
            rewardedVideoAd.OnAdClosed += RewardedVideoOnAdClosedEvent;
            rewardedVideoAd.OnAdInfoChanged += RewardedVideoOnAdInfoChangedEvent;
            isAdsEnabled = true;
            
            LevelPlay.LaunchTestSuite();
            
            // 进入游戏立即预加载
            LoadRewardedVideoAd();
        }

        void SdkInitializationFailedEvent(LevelPlayInitError error) {
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
        }

        void RewardedVideoOnAdDisplayedFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) {
            Debug.LogError($"[RewardedAdManager] 广告展示失败: {error}");
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

        private void OnDisable() {
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
        }

        private void OnDestroy() {
            if (Instance != this) {
                return;
            }
        }
    }
}