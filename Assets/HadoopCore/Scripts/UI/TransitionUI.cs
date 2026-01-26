using DG.Tweening;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HadoopCore.Scripts.UI {
    public class TransitionUI : MonoBehaviour {
        public static TransitionUI Instance { get; private set; }
        
        [SerializeField] private float defaultSoftness = 0.02f;

        private CanvasGroup _canvasGroup;
        private Material _runtimeMat;
        private Sequence _seq;
        private Image blockerImage;
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");

        /**
         * TODO 重构方案:
         * 1. 使用单例模式: 挂在Level Manager下面可以吗?
         * 2. 增加事件监听, 例如在场景切换时自动播放过渡动画
         * 3. 概念总结:
         *     1) Vector3 worldPos - 世界坐标, RectTransform rect - UI 局部坐标, Vector2(screen.x / Screen.width, ...) - 归一化 UV 坐标
         *     2) Camera.main 与 Camera.current
         *     3) UV坐标
         *     4) rect.position, rect.anchoredPosition, rect.localPosition 的区别
         * 4. 三个public方法, 分别接受:
         *     1) 世界物体: 如player(worldPos)
         *     2) UI物体: 如一个button(RectTransform)
         *     3) UV坐标: 适用于快速调试
         *     Open/Close 直接用参数来确定
         * 
         */
        
        
        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // TODO DontDestroyOnLoad only works for root GameObjects or components on root GameObjects.
            DontDestroyOnLoad(gameObject);
            RefreshSceneReferences();
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // 注意「??=」这种写法, 属于C sharp语法糖, 在Unity中可能有假“null”问题
            // 例如: GameObject obj; obj如果指向一个被Destroy的对象, 那么「??=」返回不为null, 而传统if (xxx != null)返回的是null
            // 通常,「??=」这种写法, 在Awake, Start中安全, 但在Update等函数中可能会有问题
            _canvasGroup ??= GetComponent<CanvasGroup>();
            blockerImage ??= GetComponentInChildren<Image>(true);

            // Make sure the Image uses a unique material instance
            if (blockerImage != null) {
                _runtimeMat = new Material(blockerImage.material);
                blockerImage.material = _runtimeMat;
            }

            // Ensure initial state is hidden
            UIUtil.SetUIVisible(_canvasGroup, false);
            SetSoftness(defaultSoftness);
        }


        private void OnDestroy() {
            _seq?.Kill();
        }

        // ---------- Public API ----------
        public Sequence GenerateTransition(GameObject targetObj, bool isOpen) {
            RectTransform rectTransform = targetObj.GetComponent<RectTransform>();
            if (rectTransform != null) {
                // 是 UI 对象
                Vector2 uv = RectToUV(rectTransform);
                return isOpen ? Open(uv): Close(uv);
            } else {
                // 是普通 3D 对象
                Vector2 uv = WorldToUV(targetObj.transform.position);
                return isOpen ? Open(uv): Close(uv);
            }
        }
        
        public Sequence Open(Vector2 centerUV) {
            Prepare(centerUV);
            float radiusMax = ComputeRadiusMax(centerUV);
            _seq?.Kill();
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .AppendCallback(() => SetRadius(-1))
                .AppendInterval(0.5f)
                .Append(DOTween.To(() => -1f, r => SetRadius(r), radiusMax, 2f)
                    .SetEase(Ease.Linear) // TODO: 我想测试一下先慢后快的效果
                    .OnComplete(() => UIUtil.SetUIVisible(_canvasGroup, false))
                );
            return _seq;
        }
        
        public Sequence Close(Vector2 centerUV) {
            Prepare(centerUV);
            float radiusMax = ComputeRadiusMax(centerUV);
            _seq?.Kill();
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .AppendCallback(() => SetRadius(radiusMax))
                .AppendInterval(0.5f)
                .Append(DOTween.To(() => radiusMax, r => SetRadius(r), -1f, 1f)
                    .SetEase(Ease.OutQuart)
                    .OnComplete(() => SetRadius(-1f))
                );
            return _seq;
        }

        // ---------- Helpers ----------

        private void Prepare(Vector2 centerUV) {
            if (_runtimeMat == null || blockerImage == null) return;
            UIUtil.SetUIVisible(_canvasGroup, true);
            _runtimeMat.SetVector(CenterId, centerUV);
        }

        private void SetRadius(float r) {
            if (_runtimeMat == null) return;
            _runtimeMat.SetFloat(RadiusId, r);
        }

        private void SetSoftness(float s) {
            if (_runtimeMat == null) return;
            _runtimeMat.SetFloat(SoftnessId, s);
        }

        private static Vector2 WorldToUV(Vector3 worldPos) {
            Vector2 screen = Camera.main.WorldToScreenPoint(worldPos);
            return new Vector2(screen.x / Screen.width, screen.y / Screen.height);
        }

        private static Vector2 RectToUV(RectTransform rect) {
            // rect.position返回的是rect的Pivot, 默认是(0.5, 0.5)Panel 中心
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            return new Vector2(screen.x / Screen.width, screen.y / Screen.height);
        }

        private static float ComputeRadiusMax(Vector2 centerUV) {
            float aspect = (float)Screen.width / Screen.height;
            Vector2[] corners = { new(0f, 0f), new(1f, 0f), new(0f, 1f), new(1f, 1f) };
            float max = 0f;
            foreach (var c in corners) {
                Vector2 d = c - centerUV;
                d.x *= aspect;
                float dist = d.magnitude;
                if (dist > max) max = dist;
            }

            return max;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            RefreshSceneReferences();
        }
        
        private void RefreshSceneReferences() {
            // Rebind scene objects after scene load (old references become destroyed)
            MySugarUtil.AutoFindObjects(this, gameObject);
        }
    }
}