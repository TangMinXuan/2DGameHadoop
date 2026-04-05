using System;
using UnityEngine;

namespace HadoopCore.Scripts.Manager {
    // Temporary fake implementation used when LevelPlay SDK is unavailable.
    public static class LevelPlay {
        public static event Action<LevelPlayConfiguration> OnInitSuccess;
        public static event Action<LevelPlayInitError> OnInitFailed;
        public static event Action<LevelPlayImpressionData> OnImpressionDataReady;

        public static void ValidateIntegration() {
            Debug.Log("[FakeLevelPlay] ValidateIntegration called.");
        }

        public static void SetMetaData(string key, string value) {
            Debug.Log($"[FakeLevelPlay] SetMetaData called. {key}={value}");
        }

        public static void Init(string appKey) {
            Debug.Log($"[FakeLevelPlay] Init called. appKey={appKey}");
            OnInitSuccess?.Invoke(new LevelPlayConfiguration());
            OnImpressionDataReady?.Invoke(new LevelPlayImpressionData());
        }

        public static void LaunchTestSuite() {
            Debug.Log("[FakeLevelPlay] LaunchTestSuite called.");
        }

        public static void EmitInitFailed(string message = "Fake init failed") {
            Debug.Log($"[FakeLevelPlay] EmitInitFailed called. message={message}");
            OnInitFailed?.Invoke(new LevelPlayInitError(message));
        }
    }

    public sealed class LevelPlayRewardedAd {
        private readonly string _adUnitId;

        public event Action<LevelPlayAdInfo> OnAdLoaded;
        public event Action<LevelPlayAdError> OnAdLoadFailed;
        public event Action<LevelPlayAdInfo> OnAdDisplayed;
        public event Action<LevelPlayAdInfo, LevelPlayReward> OnAdRewarded;
        public event Action<LevelPlayAdInfo> OnAdClosed;
        public event Action<LevelPlayAdInfo> OnAdInfoChanged;

        public LevelPlayRewardedAd(string adUnitId) {
            _adUnitId = adUnitId;
            Debug.Log($"[FakeLevelPlay][Rewarded] Created. adUnitId={_adUnitId}");
        }

        public bool IsAdReady() {
            Debug.Log($"[FakeLevelPlay][Rewarded] IsAdReady called. adUnitId={_adUnitId}, ready=true");
            return true;
        }

        public void LoadAd() {
            Debug.Log($"[FakeLevelPlay][Rewarded] LoadAd called. adUnitId={_adUnitId}");
            var adInfo = new LevelPlayAdInfo(_adUnitId);
            OnAdLoaded?.Invoke(adInfo);
            OnAdInfoChanged?.Invoke(adInfo);
        }

        public void ShowAd() {
            Debug.Log($"[FakeLevelPlay][Rewarded] ShowAd called. adUnitId={_adUnitId}");
            var adInfo = new LevelPlayAdInfo(_adUnitId);
            OnAdDisplayed?.Invoke(adInfo);
            OnAdRewarded?.Invoke(adInfo, new LevelPlayReward("FakeReward", 1));
            OnAdClosed?.Invoke(adInfo);
        }

        public void DestroyAd() {
            Debug.Log($"[FakeLevelPlay][Rewarded] DestroyAd called. adUnitId={_adUnitId}");
        }
    }

    public sealed class LevelPlayInterstitialAd {
        private readonly string _adUnitId;

        public event Action<LevelPlayAdInfo> OnAdLoaded;
        public event Action<LevelPlayAdError> OnAdLoadFailed;
        public event Action<LevelPlayAdInfo> OnAdDisplayed;
        public event Action<LevelPlayAdInfo> OnAdClosed;

        public LevelPlayInterstitialAd(string adUnitId) {
            _adUnitId = adUnitId;
            Debug.Log($"[FakeLevelPlay][Interstitial] Created. adUnitId={_adUnitId}");
        }

        public bool IsAdReady() {
            Debug.Log($"[FakeLevelPlay][Interstitial] IsAdReady called. adUnitId={_adUnitId}, ready=true");
            return true;
        }

        public void LoadAd() {
            Debug.Log($"[FakeLevelPlay][Interstitial] LoadAd called. adUnitId={_adUnitId}");
            OnAdLoaded?.Invoke(new LevelPlayAdInfo(_adUnitId));
        }

        public void ShowAd() {
            Debug.Log($"[FakeLevelPlay][Interstitial] ShowAd called. adUnitId={_adUnitId}");
            var adInfo = new LevelPlayAdInfo(_adUnitId);
            OnAdDisplayed?.Invoke(adInfo);
            OnAdClosed?.Invoke(adInfo);
        }

        public void DestroyAd() {
            Debug.Log($"[FakeLevelPlay][Interstitial] DestroyAd called. adUnitId={_adUnitId}");
        }
    }

    public sealed class LevelPlayBannerAd {
        private readonly string _adUnitId;
        private readonly LevelPlayAdSize _adSize;
        private readonly LevelPlayBannerPosition _position;

        public event Action<LevelPlayAdInfo> OnAdLoaded;
        public event Action<LevelPlayAdError> OnAdLoadFailed;

        public LevelPlayBannerAd(string adUnitId, LevelPlayAdSize adSize, LevelPlayBannerPosition position) {
            _adUnitId = adUnitId;
            _adSize = adSize;
            _position = position;
            Debug.Log($"[FakeLevelPlay][Banner] Created. adUnitId={_adUnitId}, size={_adSize}, position={_position}");
        }

        public void LoadAd() {
            Debug.Log($"[FakeLevelPlay][Banner] LoadAd called. adUnitId={_adUnitId}");
            OnAdLoaded?.Invoke(new LevelPlayAdInfo(_adUnitId));
        }

        public void HideAd() {
            Debug.Log($"[FakeLevelPlay][Banner] HideAd called. adUnitId={_adUnitId}");
        }

        public void DestroyAd() {
            Debug.Log($"[FakeLevelPlay][Banner] DestroyAd called. adUnitId={_adUnitId}");
        }
    }

    public enum LevelPlayAdSize {
        BANNER
    }

    public enum LevelPlayBannerPosition {
        BottomCenter
    }

    public sealed class LevelPlayConfiguration {
    }

    public sealed class LevelPlayInitError {
        public string Message { get; }

        public LevelPlayInitError(string message = "Fake init error") {
            Message = message;
        }

        public override string ToString() {
            return Message;
        }
    }

    public sealed class LevelPlayAdInfo {
        public string AdUnitId { get; }

        public LevelPlayAdInfo(string adUnitId = "fake_ad_unit") {
            AdUnitId = adUnitId;
        }

        public override string ToString() {
            return $"AdUnitId={AdUnitId}";
        }
    }

    public sealed class LevelPlayAdError {
        public string Message { get; }

        public LevelPlayAdError(string message = "Fake ad error") {
            Message = message;
        }

        public override string ToString() {
            return Message;
        }
    }

    public sealed class LevelPlayReward {
        public string Name { get; }
        public int Amount { get; }

        public LevelPlayReward(string name = "FakeReward", int amount = 1) {
            Name = name;
            Amount = amount;
        }
    }

    public sealed class LevelPlayImpressionData {
        public string AllData => "Fake impression data";

        public override string ToString() {
            return AllData;
        }
    }
}

