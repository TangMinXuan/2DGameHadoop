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
        [JsonProperty("curLayer")] public int CurLayer { get; set; } = 1;
        [JsonProperty("settings")] public Dictionary<string, JToken> Settings { get; set; } = new();
        [JsonProperty("levels")] public Dictionary<string, LevelProgress> LevelDic { get; set; } = new();
        [JsonProperty("extra")] public Dictionary<string, JToken> Extra { get; set; } = new();
        
        public static string GetSaveFilePath(string fileName = DefaultFileName) {
            // Application.persistentDataPath（持久化目录）跨 iOS/Android/PC 通用
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        /// <summary>
        /// 读取存档；不存在则创建默认档并写入磁盘；坏档则备份后重建。
        /// </summary>
        public static GameSaveData LoadOrCreate(Func<GameSaveData> createDefault = null, string fileName = DefaultFileName) {
            createDefault ??= GameManager.exposeCreateDefaultSave;
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

            try {
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
            } catch (Exception e) {
                Debug.LogError(e);
                throw new IOException($"Save failed.", e); // 包装异常，保留原始堆栈
            }
        }

        private static void BackupCorruptedSave(string archive) {
            try {
                var backupArchive = archive + ".corrupted_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Copy(archive, backupArchive, overwrite: true);
            } catch (Exception e) {
                Debug.LogWarning($"Backup corrupted save failed. path={archive}\n{e}");
            }
        }
    }

    [Serializable]
    public sealed class LevelProgress {
        
        [JsonProperty("levelId")] public int LevelId { get; set; }
        
        [JsonProperty("unlocked")] public bool Unlocked { get; set; } = false;
        
        [JsonProperty("requiredStars")] public int RequiredStars { get; set; } = 999;
        
        [JsonProperty("isPass")] public bool IsPass { get; set; } = false;

        [JsonProperty("bestStars")] public int BestStars { get; set; } = 0;

        [JsonProperty("bestTime")] public float BestTime { get; set; } = 0;
        
        public LevelProgress WithLevelId(int levelId) {
            LevelId = levelId;
            return this;
        }
        
        public LevelProgress WithUnlocked(bool unlocked) {
            Unlocked = unlocked;
            return this;
        }

        public LevelProgress WithRequiredStarts(int requiredStarts) {
            RequiredStars = requiredStarts;
            return this;
        }
        
        public LevelProgress WithIsPass(bool isPass) {
            IsPass = isPass;
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