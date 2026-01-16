using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HadoopCore.Scripts.Manager {
    [Serializable]
    public sealed class GameSaveData {
        
        [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; } = 1;
        [JsonProperty("version")] public string Version { get; set; } = "0.0.0";
        [JsonProperty("settings")] public Dictionary<string, JToken> Settings { get; set; } = new();
        [JsonProperty("levels")] public Dictionary<string, LevelProgress> Levels { get; set; } = new();
        [JsonProperty("extra")] public Dictionary<string, JToken> Extra { get; set; } = new();

        // -----------------------
        // 便捷方法：Settings
        // -----------------------
        public void SetSetting<T>(string key, T value) {
            Settings[key] = value == null ? JValue.CreateNull() : JToken.FromObject(value);
        }

        public T GetSetting<T>(string key, T defaultValue = default) {
            if (!Settings.TryGetValue(key, out var token) || token == null || token.Type == JTokenType.Null)
                return defaultValue;

            try {
                return token.ToObject<T>();
            }
            catch {
                return defaultValue;
            }
        }

        // -----------------------
        // 便捷方法：Extra
        // -----------------------
        public void SetExtra<T>(string key, T value) {
            Extra[key] = value == null ? JValue.CreateNull() : JToken.FromObject(value);
        }

        public T GetExtra<T>(string key, T defaultValue = default) {
            if (!Extra.TryGetValue(key, out var token) || token == null || token.Type == JTokenType.Null)
                return defaultValue;

            try {
                return token.ToObject<T>();
            }
            catch {
                return defaultValue;
            }
        }
    }

    [Serializable]
    public sealed class LevelProgress {
        [JsonProperty("unlocked")] public bool Unlocked { get; set; } = false;

        [JsonProperty("stars")] public int Stars { get; set; } = 0;

        [JsonProperty("bestTimeMs")] public long BestTimeMs { get; set; } = -1; // -1 = 未记录

        [JsonProperty("bestScore")] public int BestScore { get; set; } = 0;
    }
}