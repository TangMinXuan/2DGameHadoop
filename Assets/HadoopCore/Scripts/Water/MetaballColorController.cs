using UnityEngine;

namespace HadoopCore.Scripts.Water {
    /// <summary>
    /// 运行时动态控制 Metaball 材质的颜色（水球/火球等）。
    /// 挂到任意常驻 GameObject 上，通过 Inspector 或代码修改颜色即可实时生效。
    /// 
    /// 使用方式:
    ///   - 将此脚本挂到 WaterCanvas 或任意管理对象上
    ///   - 将 MAT_Metaball 和 MAT_Metaball_FireBall 拖入 entries 数组
    ///   - 运行时修改 entries[i].fillColor / strokeColor 即可动态变色
    ///   - 也可通过代码调用 SetFillColor / SetStrokeColor
    /// </summary>
    public class MetaballColorController : MonoBehaviour {
        [System.Serializable]
        public class MetaballEntry {
            public string label = "Water";
            public Material metaballMaterial;
            public Color fillColor = new Color(0.45f, 0.68f, 0.93f, 1f);
            public Color strokeColor = new Color(0.45f, 0.68f, 0.94f, 1f);
        }

        [Header("Metaball 材质配置")] public MetaballEntry[] entries;

        // shader property IDs (cached for performance)
        private static readonly int ColorProp = Shader.PropertyToID("_Color");
        private static readonly int StrokeColorProp = Shader.PropertyToID("_StrokeColor");

        private void Start() {
            ApplyAllColors();
        }

        private void OnValidate() {
            // 编辑器下修改 Inspector 时也能实时预览
            ApplyAllColors();
        }

        /// <summary>
        /// 将所有 entry 的颜色同步到材质上
        /// </summary>
        public void ApplyAllColors() {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++) {
                ApplyColor(i);
            }
        }

        /// <summary>
        /// 应用指定 entry 的颜色到材质
        /// </summary>
        public void ApplyColor(int index) {
            if (entries == null || index < 0 || index >= entries.Length) return;
            var entry = entries[index];
            if (entry.metaballMaterial == null) return;

            entry.metaballMaterial.SetColor(ColorProp, entry.fillColor);
            entry.metaballMaterial.SetColor(StrokeColorProp, entry.strokeColor);
        }

        /// <summary>
        /// 运行时通过代码设置填充色
        /// </summary>
        public void SetFillColor(int index, Color color) {
            if (entries == null || index < 0 || index >= entries.Length) return;
            entries[index].fillColor = color;
            ApplyColor(index);
        }

        /// <summary>
        /// 运行时通过代码设置描边色
        /// </summary>
        public void SetStrokeColor(int index, Color color) {
            if (entries == null || index < 0 || index >= entries.Length) return;
            entries[index].strokeColor = color;
            ApplyColor(index);
        }

        /// <summary>
        /// 通过 label 查找并设置填充色
        /// </summary>
        public void SetFillColorByLabel(string label, Color color) {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++) {
                if (entries[i].label == label) {
                    SetFillColor(i, color);
                    return;
                }
            }
        }

        /// <summary>
        /// 通过 label 查找并设置描边色
        /// </summary>
        public void SetStrokeColorByLabel(string label, Color color) {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++) {
                if (entries[i].label == label) {
                    SetStrokeColor(i, color);
                    return;
                }
            }
        }
    }
}