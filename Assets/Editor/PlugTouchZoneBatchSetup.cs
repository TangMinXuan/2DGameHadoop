using System.IO;
using System.Linq;
using HadoopCore.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HadoopCore.Editor {
    public static class PlugTouchZoneBatchSetup {
        [MenuItem("Tools/Plug/Batch Setup Touch Zones In Folder...")]
        public static void BatchSetupInFolder() {
            // 弹出文件夹选择面板（相对 Assets）
            string folder = EditorUtility.OpenFolderPanel(
                "选择包含 Scene 的文件夹",
                Application.dataPath,
                ""
            );

            if (string.IsNullOrEmpty(folder)) return;

            // 转换为相对路径（Assets/...）
            if (!folder.StartsWith(Application.dataPath)) {
                Debug.LogError("[PlugBatch] 请选择 Assets 目录下的文件夹！");
                return;
            }
            string relativeFolderPath = "Assets" + folder.Substring(Application.dataPath.Length);

            // 搜索该文件夹（含子目录）下所有 .unity 文件
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { relativeFolderPath });
            if (guids.Length == 0) {
                Debug.LogWarning($"[PlugBatch] 在 {relativeFolderPath} 下未找到任何 Scene 文件。");
                return;
            }

            // 保存当前已打开的场景，避免丢失
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            int totalPlugs = 0;
            int totalScenes = 0;

            foreach (string guid in guids) {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"[PlugBatch] 正在处理: {scenePath}");

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                // 找到场景中所有 Plug（含未激活的）
                var plugs = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Plug>(includeInactive: true))
                    .ToList();

                if (plugs.Count == 0) {
                    Debug.Log($"[PlugBatch]   └─ 未找到 Plug，跳过。");
                    continue;
                }

                foreach (var plug in plugs) {
                    Undo.RecordObject(plug.transform, "Batch Setup Touch Zones");
                    plug.SetupTouchZones();
                    EditorUtility.SetDirty(plug);
                }

                EditorSceneManager.SaveScene(scene);
                totalPlugs += plugs.Count;
                totalScenes++;

                Debug.Log($"[PlugBatch]   └─ 处理了 {plugs.Count} 个 Plug，已保存。");
            }

            Debug.Log($"[PlugBatch] ✅ 完成！共处理 {totalScenes} 个场景，{totalPlugs} 个 Plug。");
            EditorUtility.DisplayDialog(
                "批量处理完成",
                $"共处理 {totalScenes} 个场景，{totalPlugs} 个 Plug。",
                "OK"
            );
        }
    }
}
