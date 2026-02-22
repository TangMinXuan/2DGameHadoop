using System;

namespace HadoopCore.Scripts.Manager {
    public static class LevelEventCenter {
        private static bool _isLevelFinished = false;
        
        // 声明事件 (Action可以理解为 方法指针)
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action OnGameOver;
        public static event Action OnGameSuccess;
        public static event Action OnLevelFinishedSignReset;
        public static event Action OnPlayerDied;

        // 发布事件
        public static void TriggerGamePaused() => OnGamePaused?.Invoke();
        public static void TriggerGameResumed() => OnGameResumed?.Invoke();
        public static void TriggerGameOver() {
            if (_isLevelFinished) return;
            _isLevelFinished = true;
            OnGameOver?.Invoke();
        }

        public static void TriggerGameSuccess() {
            if (_isLevelFinished) return;
            _isLevelFinished = true;
            OnGameSuccess?.Invoke();
        }

        public static void TriggerLevelFinishedSignReset() {
            _isLevelFinished = false; // 重置状态
            OnLevelFinishedSignReset?.Invoke();
        }
        public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();
    }
}