using UnityEditor;
using UnityEngine;

namespace HadoopCore.Editor {
    public class MetaBallSpawner : EditorWindow {

        private enum BallType { WaterBall, FireBall }

        private GameObject _waterBallPrefab;
        private GameObject _fireBallPrefab;
        private BallType _ballType = BallType.WaterBall;
        private Vector2 _regionBottomLeft = Vector2.zero;
        private Vector2 _regionSize = new Vector2(10f, 10f);
        private float _ballRadius = 1f;
        private float _spacing;

        private const string PrefKeyBallType      = "MetaBallSpawner_BallType";
        private const string PrefKeyBottomLeftX   = "MetaBallSpawner_BottomLeftX";
        private const string PrefKeyBottomLeftY   = "MetaBallSpawner_BottomLeftY";
        private const string PrefKeySizeX         = "MetaBallSpawner_SizeX";
        private const string PrefKeySizeY         = "MetaBallSpawner_SizeY";
        private const string PrefKeyRadius        = "MetaBallSpawner_Radius";
        private const string PrefKeySpacing       = "MetaBallSpawner_Spacing";
        private const string PrefKeyWaterPrefab   = "MetaBallSpawner_WaterPrefab";
        private const string PrefKeyFirePrefab    = "MetaBallSpawner_FirePrefab";

        [MenuItem("Tools/MetaBall Spawner")]
        public static void ShowWindow() => GetWindow<MetaBallSpawner>("MetaBall Spawner");

        private void OnEnable() {
            SceneView.duringSceneGui += OnSceneGUI;
            LoadPrefs();
        }

        private void OnDisable() {
            SceneView.duringSceneGui -= OnSceneGUI;
            SavePrefs();
        }

        private void LoadPrefs() {
            _ballType        = (BallType)EditorPrefs.GetInt(PrefKeyBallType, 0);
            _regionBottomLeft = new Vector2(
                EditorPrefs.GetFloat(PrefKeyBottomLeftX, 0f),
                EditorPrefs.GetFloat(PrefKeyBottomLeftY, 0f));
            _regionSize      = new Vector2(
                EditorPrefs.GetFloat(PrefKeySizeX, 10f),
                EditorPrefs.GetFloat(PrefKeySizeY, 10f));
            _ballRadius      = EditorPrefs.GetFloat(PrefKeyRadius, 1f);
            _spacing         = EditorPrefs.GetFloat(PrefKeySpacing, 0f);

            string waterPath = EditorPrefs.GetString(PrefKeyWaterPrefab, "");
            string firePath  = EditorPrefs.GetString(PrefKeyFirePrefab, "");
            if (!string.IsNullOrEmpty(waterPath)) _waterBallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(waterPath);
            if (!string.IsNullOrEmpty(firePath))  _fireBallPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(firePath);
        }

        private void SavePrefs() {
            EditorPrefs.SetInt(PrefKeyBallType, (int)_ballType);
            EditorPrefs.SetFloat(PrefKeyBottomLeftX, _regionBottomLeft.x);
            EditorPrefs.SetFloat(PrefKeyBottomLeftY, _regionBottomLeft.y);
            EditorPrefs.SetFloat(PrefKeySizeX, _regionSize.x);
            EditorPrefs.SetFloat(PrefKeySizeY, _regionSize.y);
            EditorPrefs.SetFloat(PrefKeyRadius, _ballRadius);
            EditorPrefs.SetFloat(PrefKeySpacing, _spacing);
            EditorPrefs.SetString(PrefKeyWaterPrefab, _waterBallPrefab != null ? AssetDatabase.GetAssetPath(_waterBallPrefab) : "");
            EditorPrefs.SetString(PrefKeyFirePrefab,  _fireBallPrefab  != null ? AssetDatabase.GetAssetPath(_fireBallPrefab)  : "");
        }

        private void OnGUI() {
            GUILayout.Label("MetaBall 批量生成工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Prefab 配置", EditorStyles.boldLabel);
            _waterBallPrefab = (GameObject)EditorGUILayout.ObjectField("Water Ball Prefab", _waterBallPrefab, typeof(GameObject), false);
            _fireBallPrefab  = (GameObject)EditorGUILayout.ObjectField("Fire Ball Prefab",  _fireBallPrefab,  typeof(GameObject), false);

            EditorGUILayout.Space();
            GUILayout.Label("生成配置", EditorStyles.boldLabel);
            _ballType         = (BallType)EditorGUILayout.EnumPopup("Ball Type", _ballType);
            _regionBottomLeft = EditorGUILayout.Vector2Field("Region Bottom-Left", _regionBottomLeft);
            _regionSize       = EditorGUILayout.Vector2Field("Region Size (W x H)", _regionSize);
            _ballRadius       = EditorGUILayout.FloatField("Ball Radius", _ballRadius);
            _spacing          = EditorGUILayout.FloatField("Spacing", _spacing);

            if (EditorGUI.EndChangeCheck()) {
                // 只刷新 SceneView 预览，不写入 EditorPrefs
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("生成", GUILayout.Height(30))) {
                SavePrefs(); // 生成前保存一次，兼顾崩溃场景
                SpawnBalls();
            }
        }

        private void SpawnBalls() {
            GameObject prefab = _ballType == BallType.WaterBall ? _waterBallPrefab : _fireBallPrefab;
            if (prefab == null) {
                EditorUtility.DisplayDialog("Error", $"{_ballType} Prefab 未配置!", "OK");
                return;
            }

            string parentName = _ballType == BallType.WaterBall ? "WaterBallCollection" : "FireBallCollection";

            GameObject existingParent = GameObject.Find(parentName);
            if (existingParent != null) {
                bool overwrite = EditorUtility.DisplayDialog("覆盖确认",
                    $"场景中已存在 {parentName}，是否删除后重新生成？", "是", "取消");
                if (!overwrite) return;
                Undo.DestroyObjectImmediate(existingParent);
            }

            GameObject parent = new GameObject(parentName);
            Undo.RegisterCreatedObjectUndo(parent, $"Create {parentName}");

            float step = _ballRadius * 2f + _spacing;
            float startX = _regionBottomLeft.x + _ballRadius;
            float startY = _regionBottomLeft.y + _ballRadius;
            float endX   = _regionBottomLeft.x + _regionSize.x - _ballRadius;
            float endY   = _regionBottomLeft.y + _regionSize.y - _ballRadius;

            int count = 0;
            for (float y = startY; y <= endY + 0.001f; y += step) {
                for (float x = startX; x <= endX + 0.001f; x += step) {
                    GameObject ball = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    ball.transform.position = new Vector3(x, y, 0f);
                    ball.transform.SetParent(parent.transform);
                    Undo.RegisterCreatedObjectUndo(ball, "Spawn MetaBall");
                    count++;
                }
            }

            Debug.Log($"[MetaBallSpawner] 已生成 {count} 个 {_ballType}，父节点: {parentName}");
        }

        private void OnSceneGUI(SceneView sceneView) {
            float step   = _ballRadius * 2f + _spacing;
            // 防止 step <= 0 导致死循环
            if (step <= 0.001f) return;

            float startX = _regionBottomLeft.x + _ballRadius;
            float startY = _regionBottomLeft.y + _ballRadius;
            float endX   = _regionBottomLeft.x + _regionSize.x - _ballRadius;
            float endY   = _regionBottomLeft.y + _regionSize.y - _ballRadius;

            // 矩形区域轮廓
            Vector3 bl = new Vector3(_regionBottomLeft.x, _regionBottomLeft.y, 0f);
            Vector3 tl = new Vector3(_regionBottomLeft.x, _regionBottomLeft.y + _regionSize.y, 0f);
            Vector3 tr = new Vector3(_regionBottomLeft.x + _regionSize.x, _regionBottomLeft.y + _regionSize.y, 0f);
            Vector3 br = new Vector3(_regionBottomLeft.x + _regionSize.x, _regionBottomLeft.y, 0f);

            Handles.DrawSolidRectangleWithOutline(
                new[] { bl, tl, tr, br },
                new Color(0f, 1f, 1f, 0.05f),
                Color.cyan
            );

            // 预估球的数量，超过阈值则不绘制圆圈
            int countX = Mathf.FloorToInt((_regionSize.x - _ballRadius * 2f) / step) + 1;
            int countY = Mathf.FloorToInt((_regionSize.y - _ballRadius * 2f) / step) + 1;
            int estimatedCount = Mathf.Max(0, countX) * Mathf.Max(0, countY);

            const int MaxPreviewCount = 200;
            if (estimatedCount > MaxPreviewCount) {
                Handles.color = Color.yellow;
                Handles.Label(new Vector3(_regionBottomLeft.x, _regionBottomLeft.y + _regionSize.y + 0.5f, 0f),
                    $"预览已隐藏（共 {estimatedCount} 个球，超过 {MaxPreviewCount} 上限）");
                return;
            }

            // 每个球的预览圆圈
            Handles.color = new Color(1f, 1f, 0f, 0.4f);
            for (float y = startY; y <= endY + 0.001f; y += step) {
                for (float x = startX; x <= endX + 0.001f; x += step) {
                    Handles.DrawWireDisc(new Vector3(x, y, 0f), Vector3.forward, _ballRadius);
                }
            }
        }

    }
}
