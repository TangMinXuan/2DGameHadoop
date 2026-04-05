using System;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Shared;
using UnityEngine;

namespace HadoopCore.Scripts.SceneController {
    public class BootstrapController : MonoBehaviour {
        private void Start() {
            // 1. 帧率设置：尝试跑满 120Hz，普通设备会自动降级到 60Hz
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            
            // 2. 沉浸式设置：防止误触 Home 条 (仅 iOS 有效)
            #if UNITY_IOS
            UnityEngine.iOS.Device.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All;
            #endif
            
            #if UNITY_ANDROID
            // 获取当前屏幕分辨率
            Resolution currentRes = Screen.currentResolution;
            // 强制重新设置分辨率，并请求 120Hz 刷新率
            Screen.SetResolution(currentRes.width, currentRes.height, true, 120);
            #endif
        
            // 3. 永不息屏 (可选，防止玩家思考时手机自动黑屏)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            
            if (BuildEnvConfig.Instance.CurrentTarget == PlatformTarget.PC) {
                GameSaveData saveData = GameManager.Instance.GetSaveData();
                // 同步当前分辨率 与 存档内的分辨率一致
                FullScreenMode targetMode = (FullScreenMode)System.Enum.Parse(typeof(FullScreenMode), saveData.Settings["displayMode"].ToString());
                if (targetMode == FullScreenMode.Windowed) {
                    string[] res = saveData.Settings["resolution"].ToString().Split('x');
                    Vector2Int targetRes = new Vector2Int(int.Parse(res[0]), int.Parse(res[1]));
                    DisplaySettingTool.ApplyDisplayMode(targetMode, targetRes);
                } else {
                    DisplaySettingTool.ApplyDisplayMode(targetMode, Vector2Int.zero);
                }
            }

            DisplaySettingTool.ApplyFixedFrameSyncPolicy();

            jumpToMainMenu();
        }

        private void jumpToMainMenu() {
            GameManager.Instance.loadSceneSynchronously("GameStartPage");
        }
    }
}