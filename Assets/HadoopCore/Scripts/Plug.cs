using System;
using System.Collections;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace HadoopCore.Scripts {
    [Serializable]
    public class ShakeConfig {
        public enum ShakeAlgorithm {
            RandomNoise,
            DampedSine
        }

        [Header("Shake Target")] public Transform shakePivot;

        [Header("Shake Algorithm")] public ShakeAlgorithm algorithm = ShakeAlgorithm.RandomNoise;
        public bool useUnscaledTime = false;

        [Header("Timing")] [Min(0.01f)] public float duration = 0.16f;
        [Min(1f)] public float frequencyHz = 22f;

        [Header("Amplitude (World Units / Degrees)")]
        public Vector2 posAmplitudeRange = new Vector2(0.01f, 0.06f);

        public Vector2 rotAmplitudeRange = new Vector2(1.5f, 8f);

        [Header("Curves")] [Tooltip("强度曲线：输入 level01(0..1)，输出强度倍率(0..1)")]
        public AnimationCurve amplitudeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("包络曲线：输入 t01(0..1)，输出衰减(建议 1 -> 0)")]
        public AnimationCurve envelopeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        // 运行时状态（不序列化）
        [NonSerialized] public Vector3 baseLocalPos;
        [NonSerialized] public Quaternion baseLocalRot;
        [NonSerialized] public Coroutine shakeCoroutine;
    }

    public class Plug : MonoBehaviour, IPointerClickHandler {
        [SerializeField] private ShakeConfig shakeConfig;
        [SerializeField] private Sprite[] phaseSprites;
        [SerializeField] private GameObject plugBreakVFXPrefab;
        [SerializeField] private GameObject exclamationMarkVFX;
        [SerializeField] private GameObject hammerStrikeVFX;

        [Header("Touch Zones")]
        [SerializeField] private Transform shaftCollider;
        [SerializeField] private Transform ringCollider;
        [SerializeField] private Transform shaftTouchZone;
        [SerializeField] private Transform ringTouchZone;

        private SpriteRenderer _spriteRenderer;
        private Rigidbody2D _rb;
        private int _clickCount = 0;
        private int _phase = 0;
        private bool _isShaking = false;
        private Vector3 mouseClickPosition = Vector3.zero;
        private HammerController _hammerController;

        private void Awake() {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start() {
            LoadSpriteForPhase();

            // 尝试在场景中寻找已有的 HammerController
            _hammerController = FindObjectOfType<HammerController>();
            if (_hammerController == null && hammerStrikeVFX != null) {
                // 没找到则在同级节点下创建一个
                GameObject instance = Instantiate(hammerStrikeVFX, transform.parent);
                _hammerController = instance.GetComponent<HammerController>();
            }
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (_isShaking) {
                return;
            }

            _clickCount++;
            mouseClickPosition = MouseClickPositionUtil.get(eventData);
            ActivateOnceHammerStrikeVFX(mouseClickPosition);
            
            if (_clickCount > 4) {
                DestroyPlug();
                return;
            }

            if (_clickCount == 1) {
                _phase = 1;
            }
            else if (_clickCount == 3) {
                _phase = 2;
            }
            else if (_clickCount == 4) {
                _phase = 3;
                ActivateOnceExclamationMarkVFX(mouseClickPosition + new Vector3(1f, 1f, 0));
            }

            _spriteRenderer.sprite = LoadSpriteForPhase();


            Shaking();
        }

        private void Shaking() {
            _isShaking = true;

            // 根据破碎阶段计算强度（0..1）
            // phase: 1,2,3 -> level01: 0,0.5,1
            // 你的 _phase 是 1/2/3
            // 如果你希望“每次点击都逐步变大”，可以改成：Mathf.InverseLerp(1, 5, _clickCount)
            float level01 = (Mathf.Clamp(_phase, 1, 3) - 1) / 2f;

            // 计算本次幅度
            float k = Mathf.Clamp01(shakeConfig.amplitudeCurve.Evaluate(level01));
            float posAmp = Mathf.Lerp(shakeConfig.posAmplitudeRange.x, shakeConfig.posAmplitudeRange.y, k);
            float rotAmp = Mathf.Lerp(shakeConfig.rotAmplitudeRange.x, shakeConfig.rotAmplitudeRange.y, k);

            MoveShakePivotTo(mouseClickPosition == Vector3.zero ? shakeConfig.shakePivot.position : mouseClickPosition);
            shakeConfig.baseLocalPos = shakeConfig.shakePivot.localPosition;
            shakeConfig.baseLocalRot = shakeConfig.shakePivot.localRotation;

            // 你当前逻辑是"正在抖就忽略点击"，OnPointerClick 已经 return 了。
            // 这里再做一次兜底：确保不会残留协程。
            if (shakeConfig.shakeCoroutine != null) {
                StopCoroutine(shakeConfig.shakeCoroutine);
                shakeConfig.shakeCoroutine = null;
            }

            shakeConfig.shakeCoroutine = StartCoroutine(ShakeRoutine(posAmp, rotAmp, shakeConfig.duration,
                shakeConfig.frequencyHz, shakeConfig.algorithm));
        }

        private Sprite LoadSpriteForPhase() {
            if (phaseSprites != null && _phase >= 0 && _phase < phaseSprites.Length) {
                return phaseSprites[_phase];
            }

            Debug.LogWarning($"Phase {_phase} sprite not found!");
            return null;
        }


        private void DestroyPlug() {
            _rb.simulated = false;

            Vector2 VFXPosition = mouseClickPosition == Vector3.zero ? transform.position : mouseClickPosition;
            var plugBreakVFX = Instantiate(plugBreakVFXPrefab, VFXPosition, Quaternion.identity);
            var particleSystem = plugBreakVFX.GetComponentInChildren<ParticleSystem>();
            if (particleSystem != null) {
                particleSystem.Play();
            }

            gameObject.SetActive(false);
        }

        private IEnumerator ShakeRoutine(float posAmp, float rotAmp, float duration, float frequencyHz,
            ShakeConfig.ShakeAlgorithm algo) {
            float elapsed = 0f;

            // RandomNoise 用的采样节拍
            float sampleInterval = 1f / Mathf.Max(1f, frequencyHz);
            float nextSampleTime = 0f;

            // 随机方向/旋转符号（RandomNoise）
            Vector2 dir = Vector2.right;
            float rotSign = 1f;

            // DampedSine 用的随机相位
            float phase = Random.Range(0f, Mathf.PI * 2f);

            while (elapsed < duration) {
                float dt = shakeConfig.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += dt;

                float t01 = Mathf.Clamp01(elapsed / duration);
                float w = Mathf.Clamp01(shakeConfig.envelopeCurve.Evaluate(t01)); // 1 -> 0 衰减

                if (algo == ShakeConfig.ShakeAlgorithm.RandomNoise) {
                    // 按 frequencyHz 刷新一次随机方向，避免"每帧乱跳"的噪点感
                    if (elapsed >= nextSampleTime) {
                        nextSampleTime += sampleInterval;

                        dir = Random.insideUnitCircle;
                        if (dir.sqrMagnitude < 1e-5f) dir = Vector2.right;
                        dir.Normalize();

                        float s = Random.Range(-1f, 1f);
                        rotSign = (s >= 0f) ? 1f : -1f;
                    }

                    Vector3 posOffset = new Vector3(dir.x, dir.y, 0f) * (posAmp * w);
                    float rotOffset = rotSign * (rotAmp * w);

                    shakeConfig.shakePivot.localPosition = shakeConfig.baseLocalPos + posOffset;
                    shakeConfig.shakePivot.localRotation =
                        shakeConfig.baseLocalRot * Quaternion.Euler(0f, 0f, rotOffset);
                }
                else // DampedSine
                {
                    // 规律振荡 + 包络衰减：更像"弹簧回弹"
                    float angle = (Mathf.PI * 2f) * frequencyHz * elapsed + phase;

                    float x = Mathf.Cos(angle) * (posAmp * w);
                    float y = Mathf.Sin(angle) * (posAmp * 0.5f * w); // y 通常更小更自然
                    float rot = Mathf.Sin(angle) * (rotAmp * w);

                    shakeConfig.shakePivot.localPosition = shakeConfig.baseLocalPos + new Vector3(x, y, 0f);
                    shakeConfig.shakePivot.localRotation = shakeConfig.baseLocalRot * Quaternion.Euler(0f, 0f, rot);
                }

                yield return null;
            }

            // 结束还原
            shakeConfig.shakePivot.localPosition = shakeConfig.baseLocalPos;
            shakeConfig.shakePivot.localRotation = shakeConfig.baseLocalRot;

            shakeConfig.shakeCoroutine = null;
            _isShaking = false;
        }

        private void MoveShakePivotTo(Vector3 worldPos) {
            // 关键点：先缓存可视对象的世界姿态，移动 pivot 后再还原它，避免“整根 Plug 瞬移”
            // shakePivot 与 spriteRenderer 临时分离
            Transform visual = _spriteRenderer.transform;
            Vector3 visualWorldPos = visual.position;
            Quaternion visualWorldRot = visual.rotation;

            shakeConfig.shakePivot.position = worldPos; // 让 pivot 跟随点击世界坐标

            visual.position = visualWorldPos; // 还原可视对象世界姿态（保持静止不跳）
            visual.rotation = visualWorldRot;
        }
        
        private void ActivateOnceExclamationMarkVFX(Vector3 position) {
            if (exclamationMarkVFX != null) {
                exclamationMarkVFX.transform.position = position;
                exclamationMarkVFX.SetActive(true);
            }
        }

        private void ActivateOnceHammerStrikeVFX(Vector3 position) {
            if (_hammerController != null) {
                _hammerController.Strike(position);
            }
        }
        
        [ContextMenu("Setup Touch Zones")]
        public void SetupTouchZones() {
            if (shaftCollider == null || ringCollider == null ||
                shaftTouchZone == null || ringTouchZone == null) {
                Debug.LogWarning("[Plug] SetupTouchZones: 请先在 Inspector 中指定 ShaftCollider / RingCollider / ShaftTouchZone / RingTouchZone！");
                return;
            }

            ApplyTouchZone(
                source: shaftCollider,
                target: shaftTouchZone,
                scaleXMultiplier: 5f
            );

            ApplyTouchZone(
                source: ringCollider,
                target: ringTouchZone,
                scaleXMultiplier: 2f
            );

            Debug.Log("[Plug] SetupTouchZones 完成。");
        }

        private static void ApplyTouchZone(Transform source, Transform target, float scaleXMultiplier) {
            // 复制 position / rotation
            target.position = source.position;
            target.rotation = source.rotation;

            // scale: x * 倍率，y / z 不变
            Vector3 srcScale = source.localScale;
            target.localScale = new Vector3(srcScale.x * scaleXMultiplier, srcScale.y, srcScale.z);

            // 复制 BoxCollider2D 的 offset 和 size
            // 若 source 的 BoxCollider2D 不存在或未激活，则回退到 CapsuleCollider2D
            var srcBox = source.GetComponent<BoxCollider2D>();
            var dstCol = target.GetComponent<BoxCollider2D>();
            if (srcBox != null && srcBox.enabled) {
                dstCol.offset = srcBox.offset;
                dstCol.size   = srcBox.size;
            } else {
                // 回退：尝试 CapsuleCollider2D
                var srcCapsule = source.GetComponent<CapsuleCollider2D>();
                if (srcCapsule != null && srcCapsule.enabled) {
                    dstCol.offset = srcCapsule.offset;
                    dstCol.size   = srcCapsule.size;
                }
            }
        }

        private void OnDisable() {
            // 防止对象被禁用/销毁时残留偏移
            if (shakeConfig.shakePivot != null) {
                shakeConfig.shakePivot.localPosition = shakeConfig.baseLocalPos;
                shakeConfig.shakePivot.localRotation = shakeConfig.baseLocalRot;
            }

            if (shakeConfig.shakeCoroutine != null) {
                StopCoroutine(shakeConfig.shakeCoroutine);
                shakeConfig.shakeCoroutine = null;
            }

            _isShaking = false;
        }
    }
}