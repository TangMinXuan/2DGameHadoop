namespace HadoopCore.Scripts.Shared {
    public static class AdConfig {
        public static string AppKey => GetAppKey();
        public static string RewardedVideoAdUnitId => GetRewardedVideoAdUnitId();

        static string GetAppKey() {
            #if UNITY_ANDROID
            return "85460dcd";
            #elif UNITY_IPHONE
            return "25822bb2d";
            #else
            return "unexpected_platform";
            #endif
        }

        static string GetBannerAdUnitId() {
            #if UNITY_ANDROID
            return "thnfvcsog13bhn08";
            #elif UNITY_IPHONE
            return "iep3rxsyp9na3rw8";
            #else
            return "unexpected_platform";
            #endif
        }

        static string GetInterstitialAdUnitId() {
            #if UNITY_ANDROID
            return "aeyqi3vqlv6o8sh9";
            #elif UNITY_IPHONE
            return "wmgt0712uuux8ju4";
            #else
            return "unexpected_platform";
            #endif
        }

        static string GetRewardedVideoAdUnitId() {
            #if UNITY_ANDROID
            return "76yy3nay3ceui2a3";
            #elif UNITY_IPHONE
            return "0u2libav3st2pqup";
            #else
            return "unexpected_platform";
            #endif
        }
    }
}