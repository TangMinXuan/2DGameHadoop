using DG.Tweening;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HadoopCore.Scripts.UI {
    
    /// <summary>
    /// Transition style options
    /// </summary>
    public enum TransitionStyle {
        CircleMask,
        FullscreenFade
    }
    
    public class TransitionUI : MonoBehaviour {
        public static TransitionUI Instance { get; private set; }
        
        [Header("General Settings")]
        [SerializeField] private TransitionStyle defaultStyle = TransitionStyle.FullscreenFade;
        
        [Header("Circle Mask Settings")]
        [SerializeField] private float defaultSoftness = 0.02f;
        
        [Header("Fullscreen Fade Settings")]
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private float fadeOpenDuration = 0.5f;
        [SerializeField] private float fadeCloseDuration = 0.5f;

        private CanvasGroup _canvasGroup;
        private Material _runtimeMat;
        private Sequence _seq;
        private Image blockerImage;
        
        // Circle mask shader properties
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
            
            // 注意「??=」这种写法, 属于C sharp语法糖, 在Unity中可能有假"null"问题
            // 例如: GameObject obj; obj如果指向一个被Destroy的对象, 那么「??=」返回不为null, 而传统if (xxx != null)返回的是null
            // 通常,「??=」这种写法, 在Awake, Start中安全, 但在Update等函数中可能会有问题
            _canvasGroup ??= GetComponent<CanvasGroup>();
            blockerImage ??= transform.Find("Blocker")?.GetComponent<Image>();

            // Make sure the Image uses a unique material instance (for circle mask)
            if (blockerImage != null) {
                _runtimeMat = new Material(blockerImage.material);
                blockerImage.material = _runtimeMat;
            }

            // Ensure initial state is hidden
            UIUtil.SetUIVisible(_canvasGroup, false);
            SetSoftness(defaultSoftness);
            
            // Initialize fade overlay state
            if (fadeOverlay != null) {
                // Start with fade overlay invisible
                var c = fadeOverlay.color;
                c.a = 0f;
                fadeOverlay.color = c;
                fadeOverlay.gameObject.SetActive(false);
            }
        }

        private void OnDestroy() {

            if (Instance == this)
                Instance = null;

            _seq?.Kill();

            if (_runtimeMat != null)
                Destroy(_runtimeMat);
        }

        // ---------- Public API (unchanged signatures) ----------
        
        /// <summary>
        /// Generate a transition using the default style
        /// </summary>
        public Sequence GenerateTransition(GameObject targetObj, bool isOpen) {
            return isOpen ? Open(targetObj) : Close(targetObj);
        }
        
        /// <summary>
        /// Play open transition (reveal the scene) using the default style
        /// </summary>
        public Sequence Open(GameObject targetObj) {
            return defaultStyle switch {
                TransitionStyle.CircleMask => OpenCircleMask(targetObj),
                TransitionStyle.FullscreenFade => OpenFade(),
                _ => OpenCircleMask(targetObj)
            };
        }
        
        /// <summary>
        /// Play close transition (hide the scene) using the default style
        /// </summary>
        public Sequence Close(GameObject targetObj) {
            return defaultStyle switch {
                TransitionStyle.CircleMask => CloseCircleMask(targetObj),
                TransitionStyle.FullscreenFade => CloseFade(),
                _ => CloseCircleMask(targetObj)
            };
        }

        // ---------- Circle Mask Implementation (original logic) ----------
        
        private Sequence OpenCircleMask(GameObject targetObj) {
            _seq?.Kill();
            
            // Ensure circle mask is visible and fade overlay is hidden
            if (fadeOverlay != null) fadeOverlay.gameObject.SetActive(false);
            if (blockerImage != null) blockerImage.gameObject.SetActive(true);
            
            float radiusMax = 0f;
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .AppendCallback(() => {
                    Vector2 uv = Prepare(targetObj);
                    radiusMax = ComputeRadiusMax(uv);
                    SetRadius(-1);
                })
                .AppendInterval(0.5f)
                .AppendCallback(() => {
                    DOTween.To(() => -1f, r => SetRadius(r), radiusMax, 2f)
                        .SetUpdate(true)
                        .SetEase(Ease.Linear)
                        .OnComplete(() => UIUtil.SetUIVisible(_canvasGroup, false));
                });
            return _seq;
        }
        
        private Sequence CloseCircleMask(GameObject targetObj) {
            _seq?.Kill();
            
            // Ensure circle mask is visible and fade overlay is hidden
            if (fadeOverlay != null) fadeOverlay.gameObject.SetActive(false);
            if (blockerImage != null) blockerImage.gameObject.SetActive(true);
            
            float radiusMax = 0f;
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .AppendCallback(() => {
                    Vector2 uv = Prepare(targetObj);
                    radiusMax = ComputeRadiusMax(uv);
                    SetRadius(radiusMax);
                })
                .AppendInterval(0.5f)
                .AppendCallback(() => {
                    DOTween.To(() => radiusMax, r => SetRadius(r), -1f, 1f)
                        .SetUpdate(true)
                        .SetEase(Ease.OutQuart)
                        .OnComplete(() => UIUtil.SetUIVisible(_canvasGroup, false));
                });
            return _seq;
        }

        // ---------- Fullscreen Fade Implementation ----------
        
        private Sequence OpenFade() {
            _seq?.Kill();
            
            if (fadeOverlay == null) {
                Debug.LogWarning("TransitionUI: FadeOverlay is not assigned. Cannot play fade transition.");
                return DOTween.Sequence();
            }
            
            // Ensure fade overlay is visible and circle mask is hidden
            if (blockerImage != null) blockerImage.gameObject.SetActive(false);
            fadeOverlay.gameObject.SetActive(true);
            
            // Make canvas group visible to show fade overlay
            UIUtil.SetUIVisible(_canvasGroup, true);
            
            // Set initial state: fully opaque (black screen)
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
            
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .AppendInterval(0.1f) // Small delay for consistency
                .Append(DOTween.ToAlpha(() => fadeOverlay.color, x => fadeOverlay.color = x, 0f, fadeOpenDuration)
                    .SetEase(Ease.OutSine))
                .OnComplete(() => {
                    UIUtil.SetUIVisible(_canvasGroup, false);
                    fadeOverlay.gameObject.SetActive(false);
                });
            
            return _seq;
        }
        
        private Sequence CloseFade() {
            _seq?.Kill();
            
            if (fadeOverlay == null) {
                Debug.LogWarning("TransitionUI: FadeOverlay is not assigned. Cannot play fade transition.");
                return DOTween.Sequence();
            }
            
            // Ensure fade overlay is visible and circle mask is hidden
            if (blockerImage != null) blockerImage.gameObject.SetActive(false);
            fadeOverlay.gameObject.SetActive(true);
            
            // Make canvas group visible and interactive (blocks input during transition)
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            
            // Set initial state: fully transparent
            var c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
            
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .AppendInterval(0.1f) // Small delay for consistency
                .Append(DOTween.ToAlpha(() => fadeOverlay.color, x => fadeOverlay.color = x, 1f, fadeCloseDuration)
                    .SetEase(Ease.OutSine))
                .OnComplete(() => {
                    UIUtil.SetUIVisible(_canvasGroup, false);
                    fadeOverlay.gameObject.SetActive(false);
                });
            // Note: We do NOT hide CanvasGroup after close - scene will transition
            
            return _seq;
        }

        // ---------- Circle Mask Helpers ----------

        private Vector2 Prepare(GameObject targetObj) {
            if (_runtimeMat == null || blockerImage == null) return Vector2.one * 0.5f;
            
            // 计算 UV
            Vector2 uv;
            RectTransform rectTransform = targetObj.GetComponent<RectTransform>();
            if (rectTransform != null) {
                uv = RectToUV(rectTransform);
            } else {
                uv = WorldToUV(targetObj.transform.position);
            }
            
            UIUtil.SetUIVisible(_canvasGroup, true);
            _runtimeMat.SetVector(CenterId, uv);
            return uv;
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
    }
}
