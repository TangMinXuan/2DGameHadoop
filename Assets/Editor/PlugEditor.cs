using HadoopCore.Scripts;
using UnityEditor;
using UnityEngine;

namespace Editor {
    [CustomEditor(typeof(Plug))]
    public class PlugEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Touch Zone Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("▶  Setup Touch Zones", GUILayout.Height(30))) {
                var plug = (Plug)target;
                Undo.RecordObjects(
                    new Object[] {
                        plug.transform,
                        plug
                    },
                    "Setup Touch Zones"
                );

                // 同时记录子对象，以支持 Undo
                foreach (Transform child in plug.transform) {
                    Undo.RecordObject(child, "Setup Touch Zones");
                    var col = child.GetComponent<BoxCollider2D>();
                    if (col != null) Undo.RecordObject(col, "Setup Touch Zones");
                }

                plug.SetupTouchZones();

                // 标记 prefab 已修改
                EditorUtility.SetDirty(plug);
                PrefabUtility.RecordPrefabInstancePropertyModifications(plug);
            }
        }
    }
}

