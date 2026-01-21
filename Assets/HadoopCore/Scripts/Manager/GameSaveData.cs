using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HadoopCore.Scripts.Manager {
    [Serializable]
    public sealed class GameSaveData {
        
        private const string DefaultFileName = "save.json";

        private static readonly JsonSerializerSettings JsonSettings = new() {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None, // 关键：不要开 TypeNameHandling（安全风险 + 容易出奇怪类型）
        };
        
        [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; } = 1;
        [JsonProperty("version")] public string Version { get; set; } = "0.0.0";
        [JsonProperty("totalStarts")] public int TotalStarts { get; set; } = 0;
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

        // -----------------------
        // 便捷方法：TotalStars & Save
        // -----------------------
        /// <summary>
        /// 计算并更新总星数，如果比当前值大则更新.
        /// </summary>
        /// <returns>是否更新了TotalStarts</returns>
        public void UpdateTotalStars() {
            int newTotal = 0;
            foreach (var level in Levels.Values) {
                if (level != null) {
                    newTotal += level.BestStars;
                }
            }
            
            if (newTotal > TotalStarts) {
                TotalStarts = newTotal;
                SaveToFile();
            }
        }

        /// <summary>
        /// 将当前存档数据保存到磁盘
        /// </summary>
        public void SaveToFile(string fileName = DefaultFileName) {
            Save(this, fileName);
        }

        // -----------------------
        // 静态方法：文件操作
        // -----------------------
        
        public static string GetSaveFilePath(string fileName = DefaultFileName) {
            // Application.persistentDataPath（持久化目录）跨 iOS/Android/PC 通用
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        /// <summary>
        /// 读取存档；不存在则创建默认档并写入磁盘；坏档则备份后重建。
        /// </summary>
        public static GameSaveData LoadOrCreate(string fileName = DefaultFileName, Func<GameSaveData> createDefault = null) {
            createDefault ??= CreateDefaultSave;

            var archive = GetSaveFilePath(fileName);

            if (!File.Exists(archive)) {
                var fresh = createDefault();
                Save(fresh, fileName);
                return fresh;
            }

            try {
                var json = File.ReadAllText(archive);
                var data = JsonConvert.DeserializeObject<GameSaveData>(json, JsonSettings);

                if (data == null)
                    throw new Exception("Deserialized save data is null.");

                // 可选：在这里做 schemaVersion 迁移
                // data = MigrateIfNeeded(data);

                return data;
            } catch (Exception e) {
                Debug.LogWarning($"Save load failed, will backup and recreate. path={archive}\n{e}");

                BackupCorruptedSave(archive);

                var fresh = createDefault();
                Save(fresh, fileName);
                return fresh;
            }
        }

        /// <summary>
        /// 保存存档：采用 tmp 原子写入，降低写一半崩溃导致坏档的概率。
        /// </summary>
        public static void Save(GameSaveData data, string fileName = DefaultFileName) {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var path = GetSaveFilePath(fileName);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(data, JsonSettings);

            var tmpPath = path + ".tmp";
            File.WriteAllText(tmpPath, json);

            // 覆盖写：先删旧档再 move（兼容性更好）
            if (File.Exists(path))
                File.Delete(path);

            File.Move(tmpPath, path); // 改名
        }

        private static void BackupCorruptedSave(string archive) {
            try {
                var backupArchive = archive + ".corrupted_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Copy(archive, backupArchive, overwrite: true);
            } catch (Exception e) {
                Debug.LogWarning($"Backup corrupted save failed. path={archive}\n{e}");
            }
        }

        private static GameSaveData CreateDefaultSave() {
            var data = new GameSaveData {
                SchemaVersion = 1,
                Version = Application.version
            };

            // 默认 settings
            data.SetSetting("musicVolume", 0.8f);
            data.SetSetting("sfxVolume", 0.8f);
            data.SetSetting("language", "en");

            // 初始化20个关卡
            for (int i = 1; i <= 20; i++) {
                string levelName = $"Level_{i}";
                int requiredStars;
                bool unlocked;

                // 前5关解锁，RequiredStars = 0
                if (i <= 5) {
                    unlocked = true;
                    requiredStars = 0;
                }
                // 第6-10关，RequiredStars = 10
                else if (i <= 10) {
                    unlocked = false;
                    requiredStars = 10;
                }
                // 第11-15关，RequiredStars = 20
                else if (i <= 15) {
                    unlocked = false;
                    requiredStars = 20;
                }
                // 第16-20关，RequiredStars = 30
                else {
                    unlocked = false;
                    requiredStars = 30;
                }

                data.Levels[levelName] = new LevelProgress {
                    Unlocked = unlocked,
                    BestStars = 0,
                    BestTime = 0,
                    RequiredStars = requiredStars
                };
            }

            return data;
        }
    }

    [Serializable]
    public sealed class LevelProgress {
        [JsonProperty("unlocked")] public bool Unlocked { get; set; } = false;
        
        [JsonProperty("requiredStars")] public int RequiredStars { get; set; } = 999;

        [JsonProperty("bestStars")] public int BestStars { get; set; } = 0;

        [JsonProperty("bestTime")] public float BestTime { get; set; } = 0;
        
        public LevelProgress WithUnlocked(bool unlocked) {
            Unlocked = unlocked;
            return this;
        }

        public LevelProgress WithRequiredStarts(int requiredStarts) {
            RequiredStars = requiredStarts;
            return this;
        }
        
        public LevelProgress WithBestStars(int bestStars) {
            BestStars = bestStars;
            return this;
        }

        public LevelProgress WithBestTime(float bestTime) {
            BestTime = bestTime;
            return this;
        }
    }
}