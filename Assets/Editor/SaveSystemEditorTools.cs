using UnityEditor;
using UnityEngine;
using HadoopCore.Scripts.Manager;

namespace HadoopCore.Editor {
    /// <summary>
    /// Unity Editor 工具：用于在编辑器模式下操作存档系统
    /// </summary>
    public static class SaveSystemEditorTools {
        
        [MenuItem("Tools/Save System/Create Default Save")]
        public static void CreateDefaultSaveFile() {
            GameSaveData saveData = GameSaveData.LoadOrCreate();
            Debug.Log($"[SaveSystemEditorTools] Default save file created/loaded at: {GameSaveData.GetSaveFilePath()}");
            Debug.Log($"[SaveSystemEditorTools] Total levels: {saveData.LevelDic.Count}");
            Debug.Log($"[SaveSystemEditorTools] Save data:\n{UnityEngine.JsonUtility.ToJson(saveData, true)}");
        }
        
        [MenuItem("Tools/Save System/Reset Save to Default")]
        public static void ResetSaveToDefault() {
            bool confirmed = EditorUtility.DisplayDialog(
                "Reset Save Data",
                "Are you sure you want to reset the save file to default?\nThis will delete all progress!",
                "Yes, Reset",
                "Cancel"
            );
            
            if (!confirmed) {
                Debug.Log("[SaveSystemEditorTools] Save reset cancelled.");
                return;
            }
            
            string savePath = GameSaveData.GetSaveFilePath();
            if (System.IO.File.Exists(savePath)) {
                System.IO.File.Delete(savePath);
                Debug.Log($"[SaveSystemEditorTools] Deleted existing save file: {savePath}");
            }
            
            GameSaveData newSave = GameSaveData.LoadOrCreate();
            Debug.Log($"[SaveSystemEditorTools] Created new default save file with {newSave.LevelDic.Count} levels.");
        }
        
        [MenuItem("Tools/Save System/Open Save File Location")]
        public static void OpenSaveFileLocation() {
            string savePath = GameSaveData.GetSaveFilePath();
            string directory = System.IO.Path.GetDirectoryName(savePath);
            
            if (!System.IO.Directory.Exists(directory)) {
                System.IO.Directory.CreateDirectory(directory);
                Debug.Log($"[SaveSystemEditorTools] Created directory: {directory}");
            }
            
            EditorUtility.RevealInFinder(savePath);
            Debug.Log($"[SaveSystemEditorTools] Opening save location: {directory}");
        }
        
        [MenuItem("Tools/Save System/Print Save Data")]
        public static void PrintSaveData() {
            string savePath = GameSaveData.GetSaveFilePath();
            
            if (!System.IO.File.Exists(savePath)) {
                Debug.LogWarning($"[SaveSystemEditorTools] Save file does not exist: {savePath}");
                return;
            }
            
            GameSaveData saveData = GameSaveData.LoadOrCreate();
            
            Debug.Log($"[SaveSystemEditorTools] ========== SAVE DATA ==========");
            Debug.Log($"Schema Version: {saveData.SchemaVersion}");
            Debug.Log($"Game Version: {saveData.Version}");
            Debug.Log($"Total Stars: {saveData.TotalStarts}");
            Debug.Log($"Total Levels: {saveData.LevelDic.Count}");
            Debug.Log($"\n--- Level Progress ---");
            
            foreach (var kvp in saveData.LevelDic) {
                var level = kvp.Value;
                Debug.Log($"{kvp.Key}: Unlocked={level.Unlocked}, Stars={level.BestStars}, Time={level.BestTime:F1}s, Required={level.RequiredStars}");
            }
            
            Debug.Log($"[SaveSystemEditorTools] ================================");
        }
        
        [MenuItem("Tools/Save System/Unlock All Levels (Cheat)")]
        public static void UnlockAllLevels() {
            GameSaveData saveData = GameSaveData.LoadOrCreate();
            
            int unlockedCount = 0;
            foreach (var kvp in saveData.LevelDic) {
                if (!kvp.Value.Unlocked) {
                    kvp.Value.Unlocked = true;
                    unlockedCount++;
                }
            }
            
            GameSaveData.Save(saveData);
            Debug.Log($"[SaveSystemEditorTools] Unlocked {unlockedCount} levels. All levels are now accessible!");
        }

        /// <summary>
        /// 为审核人员生成友好的存档：
        /// - 解锁所有关卡
        /// - levelId 之前的关卡：isPass=true, BestStars=3
        /// - levelId 及之后的关卡：已解锁，但 isPass=false（模拟正在打的状态）
        /// </summary>
        [MenuItem("Tools/Save System/Generate Friendly Data For Reviewer (Level 30)")]
        public static void GenerateFriendlyDataForReviewer() {
            GenerateFriendlyDataForReviewer(30);
        }

        public static void GenerateFriendlyDataForReviewer(int levelId = 30) {
            GameSaveData saveData = GameSaveData.LoadOrCreate();

            foreach (var kvp in saveData.LevelDic) {
                var level = kvp.Value;
                level.Unlocked = true;

                if (level.LevelId < levelId) {
                    // levelId 之前：全部通关，满星
                    level.IsPass = true;
                    level.BestStars = 3;
                } else {
                    // levelId 及之后：已解锁但未通关
                    level.IsPass = false;
                    level.BestStars = 0;
                }
            }

            GameSaveData.Save(saveData);
            Debug.Log($"[SaveSystemEditorTools] Reviewer data generated: " +
                      $"Levels 1~{levelId - 1} passed with 3 stars, " +
                      $"Level {levelId}~end unlocked but not passed.");
        }
        
    }
}

