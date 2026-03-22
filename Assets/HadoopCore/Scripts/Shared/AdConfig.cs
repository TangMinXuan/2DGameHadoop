namespace HadoopCore.Scripts.Shared {
    public static class AdConfig {
        public static string AppKey => GetAppKey();
        public static string BannerAdUnitId => GetBannerAdUnitId();
        public static string InterstitialAdUnitId => GetInterstitialAdUnitId();
        public static string RewardedVideoAdUnitId => GetRewardedVideoAdUnitId();

        static string GetAppKey() => "25822bb2d";
        static string GetBannerAdUnitId() => "7vbyqim0qvdjbmiv";
        static string GetInterstitialAdUnitId() => "1pulhv1qmatprpe5";
        static string GetRewardedVideoAdUnitId() => "0u2libav3st2pqup";
    }
}