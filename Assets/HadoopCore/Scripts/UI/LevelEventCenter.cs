using System;

namespace HadoopCore.Scripts.UI
{
    public static class LevelEventCenter
    {
        // 声明事件 (Action可以理解为 方法指针)
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action OnGameOver;
        public static event Action OnGameSuccess;
        public static event Action OnGameRestart;
        public static event Action OnPlayerDied;
        
        // 发布事件
        public static void TriggerGamePaused() => OnGamePaused?.Invoke();
        public static void TriggerGameResumed() => OnGameResumed?.Invoke();
        public static void TriggerGameOver() => OnGameOver?.Invoke();
        public static void TriggerGameSuccess() => OnGameSuccess?.Invoke();
        public static void TriggerGameRestart() => OnGameRestart?.Invoke();
        public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();
    }
}