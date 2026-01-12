using DG.Tweening;
using System;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace HadoopCore.Scripts.UI {
    public class TransitionUI : MonoBehaviour {
        [SerializeField] private GameObject levelManager;
        [SerializeField] private Image blockerImage;
        [SerializeField] private float defaultSoftness = 0.02f;

        private CanvasGroup _canvasGroup;
        private Material _runtimeMat;
        private Tween _radiusTween;
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");

        private void Awake() {
            MySugarUtil.AutoFindObjects(this, gameObject);
            
            _canvasGroup = GetComponent<CanvasGroup>();
            if (blockerImage == null) {
                blockerImage = GetComponentInChildren<Image>(true);
            }

            // Make sure the Image uses a unique material instance
            if (blockerImage != null) {
                _runtimeMat = new Material(blockerImage.material);
                blockerImage.material = _runtimeMat;
            }

            // Ensure initial state is hidden
            SetOverlayVisible(false);
            SetSoftness(defaultSoftness);
        }

        private void Start() {
            // 游戏从主角视角开始
            OpenFromWorld(levelManager.GetComponent<LevelManager>().GetPlayerTransform().position, Camera.main, 1f);
        }

        private void OnDestroy() {
            _radiusTween?.Kill();
        }

        // ---------- Public API ----------

        public void CloseFromWorld(Vector3 worldPos, Camera worldCamera, float duration, Action onComplete = null) {
            var uv = WorldToUV(worldPos, worldCamera);
            CloseFromUV(uv, duration, onComplete);
        }

        public void OpenFromWorld(Vector3 worldPos, Camera worldCamera, float duration) {
            var uv = WorldToUV(worldPos, worldCamera);
            OpenFromUV(uv, duration);
        }

        public void CloseFromRect(RectTransform rect, Camera uiCamera, float duration, Action onComplete = null) {
            var uv = RectToUV(rect, uiCamera);
            CloseFromUV(uv, duration, onComplete);
        }

        public void OpenFromRect(RectTransform rect, Camera uiCamera, float duration) {
            var uv = RectToUV(rect, uiCamera);
            OpenFromUV(uv, duration);
        }

        public void CloseFromUV(Vector2 centerUV, float duration, Action onComplete = null) {
            Prepare(centerUV);
            float radiusMax = ComputeRadiusMax(centerUV);
            SetRadius(radiusMax);
            _radiusTween?.Kill();
            _radiusTween = DOTween.To(() => radiusMax, r => SetRadius(r), -1f, duration)
                .SetEase(Ease.OutQuart)
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void OpenFromUV(Vector2 centerUV, float duration) {
            Prepare(centerUV);
            float radiusMax = ComputeRadiusMax(centerUV);
            SetRadius(-1f);
            _radiusTween?.Kill();
            _radiusTween = DOTween.To(() => 0f, r => SetRadius(r), radiusMax, duration)
                .SetUpdate(true)
                .SetEase(Ease.Linear)
                .OnComplete(() => SetOverlayVisible(false));
        }

        // ---------- Helpers ----------

        private void Prepare(Vector2 centerUV) {
            if (_runtimeMat == null || blockerImage == null) return;
            SetOverlayVisible(true);
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

        private void SetOverlayVisible(bool visible) {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = visible;
            _canvasGroup.interactable = visible;
        }

        private static Vector2 WorldToUV(Vector3 worldPos, Camera cam) {
            if (cam == null) cam = Camera.main;
            var screen = cam.WorldToScreenPoint(worldPos);
            return new Vector2(screen.x / Screen.width, screen.y / Screen.height);
        }

        private static Vector2 RectToUV(RectTransform rect, Camera uiCamera) {
            var screen = RectTransformUtility.WorldToScreenPoint(uiCamera, rect.position);
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