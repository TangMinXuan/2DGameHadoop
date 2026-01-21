using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;


namespace HadoopCore.Scripts.Manager {
public static class SaveSystem
{
    private const string DefaultFileName = "save.json";

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None, // 关键：不要开 TypeNameHandling（安全风险 + 容易出奇怪类型）
    };

    public static string GetSaveFile(string fileName = DefaultFileName)
    {
        // Application.persistentDataPath（持久化目录）跨 iOS/Android/PC 通用
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    /// <summary>
    /// 读取存档；不存在则创建默认档并写入磁盘；坏档则备份后重建。
    /// </summary>
    public static GameSaveData LoadOrCreate(string fileName = DefaultFileName, Func<GameSaveData> createDefault = null) {
        createDefault ??= CreateDefaultSave;

        var archive = GetSaveFile(fileName);

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

        var path = GetSaveFile(fileName);
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

    private static void BackupCorruptedSave(string archive)
    {
        try
        {
            var backupArchive = archive + ".corrupted_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.Copy(archive, backupArchive, overwrite: true);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Backup corrupted save failed. path={archive}\n{e}");
        }
    }

    private static GameSaveData CreateDefaultSave()
    {
        var data = new GameSaveData
        {
            SchemaVersion = 1,
            Version = Application.version
        };

        // 默认 settings
        data.SetSetting("musicVolume", 0.8f);
        data.SetSetting("sfxVolume", 0.8f);
        data.SetSetting("language", "en");

        // 初始化20个关卡
        for (int i = 1; i <= 20; i++)
        {
            string levelName = $"Level_{i}";
            int requiredStars;
            bool unlocked;

            // 前5关解锁，RequiredStars = 0
            if (i <= 5)
            {
                unlocked = false;
                requiredStars = 0;
            }
            // 第6-10关，RequiredStars = 10
            else if (i <= 10)
            {
                unlocked = false;
                requiredStars = 10;
            }
            // 第11-15关，RequiredStars = 20
            else if (i <= 15)
            {
                unlocked = false;
                requiredStars = 20;
            }
            // 第16-20关，RequiredStars = 30
            else
            {
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
}