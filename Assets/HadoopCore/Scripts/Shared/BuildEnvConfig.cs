using UnityEngine;

namespace HadoopCore.Scripts.Shared {

    // ══════════════════════════════════════════════════════════════════════════
    // ★ 全局构建环境开关  (Inspector 中切换)
    //   开发阶段 → PlatformTarget.Editor
    //   打包前   → PlatformTarget.IOS
    // ══════════════════════════════════════════════════════════════════════════
    public enum PlatformTarget { Editor, IOS, Android, PC}

    /// <summary>
    /// 全局构建配置 – Singleton / DontDestroyOnLoad.<br/>
    /// Ad、IAP 等模块统一读取 <see cref="BuildEnvConfig.CurrentTarget"/> 来判断当前环境.
    /// </summary>
    public class BuildEnvConfig : MonoBehaviour {

        public static BuildEnvConfig Instance { get; private set; }

        [SerializeField] private PlatformTarget currentTarget = PlatformTarget.Editor;

        /// <summary>当前构建目标平台.</summary>
        public PlatformTarget CurrentTarget => currentTarget;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}

