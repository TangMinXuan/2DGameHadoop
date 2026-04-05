using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HadoopCore.Scripts.Shared {
    public static class DisplaySettingTool {
        
        // 16:9 whitelist for PC settings UI.
        public static readonly Vector2Int[] PcResolutionWhitelist = {
            new Vector2Int(1280, 720),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160)
        };
        
        public static List<Vector2Int> GetAvailableResolutionOptions() {
            var options = new List<Vector2Int>();

            int maxWidth = Screen.currentResolution.width;
            int maxHeight = Screen.currentResolution.height;
            if (maxWidth <= 0 || maxHeight <= 0) {
                maxWidth = Screen.width;
                maxHeight = Screen.height;
            }

            for (int i = 0; i < PcResolutionWhitelist.Length; i++) {
                Vector2Int candidate = PcResolutionWhitelist[i];
                if (candidate.x <= maxWidth && candidate.y <= maxHeight) {
                    options.Add(candidate);
                }
            }

            if (options.Count == 0) {
                options.Add(new Vector2Int(maxWidth, maxHeight));
            }

            return options;
        }

        public static void ApplyDisplayMode(FullScreenMode displayMode, Vector2Int resolution) {
            if (displayMode == FullScreenMode.FullScreenWindow) {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            } else if (displayMode == FullScreenMode.Windowed) {
                Screen.SetResolution(resolution.x, resolution.y, FullScreenMode.Windowed);
            }
        }

        public static void ApplyFixedFrameSyncPolicy() {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }
    }
}