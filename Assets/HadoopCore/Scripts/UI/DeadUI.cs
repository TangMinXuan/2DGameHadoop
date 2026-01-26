using System;
using Cinemachine;
using DG.Tweening;
using HadoopCore.Scripts.Annotation;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HadoopCore.Scripts.UI {
    [Serializable]
    internal class DeathFXRefs {
        public GameObject obj;
        [NonSerialized, DontNeedAutoFind] public Vignette vignette;
        [NonSerialized, DontNeedAutoFind] public Bloom bloom;
        [NonSerialized, DontNeedAutoFind] public ColorAdjustments colorAdjustments;
    }

    [Serializable]
    internal class DeathContentRefs {
        public GameObject obj;
        [NonSerialized, DontNeedAutoFind] public RectTransform centerBarRt;
        [NonSerialized, DontNeedAutoFind] public CanvasGroup centerBarCg;
        [NonSerialized, DontNeedAutoFind] public TMP_Text wastedTMP;
        [NonSerialized, DontNeedAutoFind] public CanvasGroup wastedCg;
    }


    public class DeadUI : MonoBehaviour {
        private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject cameraRig;
        private Sequence _seq;
        private CinemachineVirtualCamera _vCamDeath;
        private DOTweenAnimation MenuDOTweenAnimation;

        [SerializeField] private DeathFXRefs deathFXRefs;
        [SerializeField] private DeathContentRefs deathContentRefs;
        [SerializeField] private GameObject retryBtn;
        [SerializeField] private GameObject exitBtn;

        [SerializeField] private Camera uiCamera;

        private float _initialOrthographicSize = 8f;

        private void Awake() {
            MySugarUtil.AutoFindObjects(this, gameObject);
            
            _canvasGroup = GetComponent<CanvasGroup>();
            deathFXRefs.obj = MySugarUtil.TryToFindObject(gameObject, "DeathFX");
            deathFXRefs.obj.GetComponent<Volume>().profile.TryGet(out deathFXRefs.vignette);
            deathFXRefs.obj.GetComponent<Volume>().profile.TryGet(out deathFXRefs.bloom);
            deathFXRefs.obj.GetComponent<Volume>().profile.TryGet(out deathFXRefs.colorAdjustments);
            deathContentRefs.obj = MySugarUtil.TryToFindObject(gameObject, "DeathContent");
            deathContentRefs.centerBarRt =
                MySugarUtil.TryToFindComponent<RectTransform>(deathContentRefs.obj, "CenterBar");
            deathContentRefs.centerBarCg =
                MySugarUtil.TryToFindComponent<CanvasGroup>(deathContentRefs.obj, "CenterBar");
            deathContentRefs.wastedTMP =
                MySugarUtil.TryToFindComponent<TMP_Text>(deathContentRefs.obj, "WastedText");
            deathContentRefs.wastedCg =
                MySugarUtil.TryToFindComponent<CanvasGroup>(deathContentRefs.obj, "WastedText");
            _vCamDeath = cameraRig.GetComponentInChildren<CinemachineVirtualCamera>();
            MenuDOTweenAnimation = MySugarUtil.TryToFindComponent<DOTweenAnimation>(gameObject, "Menu");


            _initialOrthographicSize = _vCamDeath.m_Lens.OrthographicSize;

            LevelEventCenter.OnGameOver += GameOver;
            ResetDeathUI();
        }

        private void GameOver() {
            // 组件级的初始化 - 这些会在reset()中重置
            UIUtil.SetUIVisible(_canvasGroup, true);
            deathFXRefs.obj.GetComponent<Volume>().weight = 1;
            _vCamDeath.Priority = 1;

            // 1. 时间缩放 - 渐进式慢动作
            _seq = DOTween.Sequence()
                .SetId("DeathPresentation")
                .SetUpdate(true)
                .Join(TweenTimeScale(0.2f, 0.05f).SetEase(Ease.OutCubic));

            // 2. DOTween推: 镜头缓慢拉近 + 黑边缓慢增长 + 镜头缓慢模糊 + 镜头晃动
            _seq.Join(TweenZoomIn(5f).SetEase(Ease.Linear)); // (Hold) 镜头拉近 In = 慢启动，Out = 慢停止
            _seq.Join(TweenVignetteIn(0.5f).SetEase(Ease.Linear)); // (Hold) 黑边增长
            _seq.Join(TweenDutchShake(5f, 2f, 1).SetEase(Ease.Linear)); // 相机抖动
            _seq.AppendInterval(1f); // Hold 3 秒

            // 3. CenterBar + WastedText 由远到近飞入, 泛光
            _seq.Join(TweenBloomIn(1f).SetEase(Ease.Linear)); // 泛光增强
            _seq.Join(TweenColorAdjustmentsIn(0.25f).SetEase(Ease.Linear)); // 画面变亮
            _seq.Join(TweenShowCenterBarNew(deathContentRefs.centerBarRt, deathContentRefs.centerBarCg,
                targetHeight: 180f, targetAlpha: 0.35f));
            _seq.Join(TweenShowWastedTextNew(deathContentRefs.wastedTMP, deathContentRefs.wastedCg));

            // 4. Retry Menu
            _seq.AppendCallback(() => {
                if (MenuDOTweenAnimation != null) {
                    MenuDOTweenAnimation.DORestart();
                }
            });
        }

        public void onRetryBtnClick() {
            // 确保不会带着慢动作重载
            Time.timeScale = 1f;

            // 防止重复点击
            if (retryBtn != null) retryBtn.SetActive(false);
            if (exitBtn != null) exitBtn.SetActive(false);

            // Camera.current 在 UI 点击事件里经常是 null，这里用更稳定的 camera
            var cam = uiCamera;
            if (cam == null) {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) {
                    cam = canvas.worldCamera;
                }

                if (cam == null) cam = Camera.main;
            }

            TransitionUI.Instance.GenerateTransition(retryBtn, false)
                .OnComplete(() => { GameManager.Instance.ReloadCurrentScene(); });
        }

        public void onExitBtnClick() {
            TransitionUI.Instance.GenerateTransition(exitBtn, false)
                .OnComplete(() => { GameManager.Instance.LoadScene("LevelSelectMenu"); });
        }

        // Part1.1: 镜头拉近 
        private Tween TweenZoomIn(float orthographicSize, float durationAfterComplete = 1f) {
            return DOTween.To(
                () => _vCamDeath.m_Lens.OrthographicSize,
                x => _vCamDeath.m_Lens.OrthographicSize = x,
                orthographicSize,
                0.35f
            );
        }

        // Part1.2: 黑边增长
        private Tween TweenVignetteIn(float intensity, float durationAfterComplete = 1f) {
            return DOTween.To(
                () => deathFXRefs.vignette.intensity.value,
                x => deathFXRefs.vignette.intensity.value = x,
                intensity,
                0.35f
            );
        }

        // Part1.3: 镜头晃动
        private Tween TweenDutchShake(float maxDutchDeg, float durationPerHalfSwing, int rounds) {
            // .SetLoops(-1, LoopType.Yoyo)
            // rounds: 完整来回算 1 轮
            int loops = Mathf.Max(1, rounds) * 2; // 一轮=去+回=2段

            return DOVirtual.Float(
                    -maxDutchDeg,
                    +maxDutchDeg,
                    durationPerHalfSwing,
                    v => {
                        var lens = _vCamDeath.m_Lens;
                        lens.Dutch = v;
                        _vCamDeath.m_Lens = lens;
                    })
                .SetEase(Ease.InOutSine)
                .SetLoops(loops, LoopType.Yoyo)
                .OnComplete(() => {
                    // 这是一个临时方案: loop结束后，做一个收口动画
                    float start = _vCamDeath.m_Lens.Dutch;
                    DOVirtual.Float(
                            start,
                            0f,
                            Mathf.Clamp(durationPerHalfSwing * 0.5f, 0.05f, durationPerHalfSwing),
                            v => {
                                var lens = _vCamDeath.m_Lens;
                                lens.Dutch = v;
                                _vCamDeath.m_Lens = lens;
                            })
                        .SetUpdate(true)
                        .SetEase(Ease.InOutSine); // 与上面保持一致
                    var lens = _vCamDeath.m_Lens;
                    lens.Dutch = 0f; // 第 x 轮完成时强制归零收口
                    _vCamDeath.m_Lens = lens;
                });
        }

        // Part3.1: 泛光增强
        private Tween TweenBloomIn(float intensity, float durationAfterComplete = 1f) {
            return DOTween.Sequence().Append(DOTween.To(
                    () => deathFXRefs.bloom.intensity.value,
                    x => deathFXRefs.bloom.intensity.value = x,
                    intensity,
                    1f
                ))
                .AppendInterval(durationAfterComplete)
                .OnComplete(() => {
                    if (deathFXRefs.bloom != null)
                        deathFXRefs.bloom.intensity.value = 0;
                });
        }

        // Part3.2: 画面变亮
        private Tween TweenColorAdjustmentsIn(float postExposure, float durationAfterComplete = 1f) {
            return DOTween.Sequence().Append(DOTween.To(
                    () => deathFXRefs.colorAdjustments.postExposure.value,
                    x => deathFXRefs.colorAdjustments.postExposure.value = x,
                    postExposure,
                    1f
                ))
                .AppendInterval(durationAfterComplete)
                .OnComplete(() => {
                    if (deathFXRefs.colorAdjustments != null)
                        deathFXRefs.colorAdjustments.postExposure.value = 0;
                });
        }

        // Part3.3: CenterBar
        private Tween TweenShowCenterBarNew(RectTransform bar, CanvasGroup cg, float targetHeight,
            float targetAlpha = 0.35f, float durationAfterComplete = 3f) {
            // 初始状态：高度为 0（保持当前宽度），透明度为 0
            Vector2 size = bar.sizeDelta;
            bar.sizeDelta = new Vector2(size.x, 0f);
            cg.alpha = 0f;

            return DOTween.Sequence()
                // 1) 0.30s 内同时驱动：高度 0 -> targetHeight，alpha 0 -> targetAlpha
                .Append(DOTween.To(
                        () => 0f,
                        t => {
                            float h = Mathf.Lerp(0f, targetHeight, t);
                            float a = Mathf.Lerp(0f, targetAlpha, t);
                            bar.sizeDelta = new Vector2(size.x, h);
                            cg.alpha = a;
                        },
                        1f,
                        0.30f
                    )
                    .SetEase(Ease.OutCubic))
                // 2) overshoot：到 1.05x（0.08s）
                .Append(DOVirtual.Float(
                        targetHeight,
                        targetHeight * 1.05f,
                        0.08f,
                        h => bar.sizeDelta = new Vector2(size.x, h)
                    )
                    .SetEase(Ease.OutSine))
                // 3) 回落：到 targetHeight（0.10s）
                .Append(DOVirtual.Float(
                        targetHeight * 1.05f,
                        targetHeight,
                        0.10f,
                        h => bar.sizeDelta = new Vector2(size.x, h)
                    )
                    .SetEase(Ease.InSine))
                // 4) 可选：像 TweenColorAdjustmentsIn 一样在末尾多等一段再 Complete
                .AppendInterval(durationAfterComplete)
                .OnComplete(() => {
                    // 恢复初始状态（隐藏）
                    bar.sizeDelta = new Vector2(size.x, 0f);
                    cg.alpha = 0f;
                });
        }

        // Part3.4: WastedText
        private Tween TweenShowWastedTextNew(TMP_Text wastedText, CanvasGroup cg, float durationAfterComplete = 3f) {
            RectTransform rt = (RectTransform)wastedText.transform;

            // 初始状态
            cg.alpha = 0f;
            rt.localScale = Vector3.one * 1.2f;

            const float fadeScaleDuration = 0.10f;
            const float overshootScale = 1.04f;
            const float overshootDuration = 0.08f;
            const float settleDuration = 0.10f;

            return DOTween.Sequence()
                // 1) 0.10s：alpha 0 -> 1，同时 scale 1.2 -> 1
                .Append(DOTween.To(
                        () => 0f,
                        t => {
                            cg.alpha = Mathf.Lerp(0f, 1f, t);
                            float s = Mathf.Lerp(1.2f, 1f, t);
                            rt.localScale = Vector3.one * s;
                        },
                        1f,
                        fadeScaleDuration
                    )
                    .SetEase(Ease.OutCubic))
                // 2) 0.08s：scale 1 -> 1.04
                .Append(DOVirtual.Float(
                        1f,
                        overshootScale,
                        overshootDuration,
                        s => rt.localScale = Vector3.one * s
                    )
                    .SetEase(Ease.OutSine))
                // 3) 0.10s：scale 1.04 -> 1
                .Append(DOVirtual.Float(
                        overshootScale,
                        1f,
                        settleDuration,
                        s => rt.localScale = Vector3.one * s
                    )
                    .SetEase(Ease.InSine))
                // 4) 可选：末尾多等一段再 Complete（结构上对齐 TweenColorAdjustmentsIn）
                .AppendInterval(durationAfterComplete)
                .OnComplete(() => {
                    // 恢复初始状态（隐藏+回到初始 scale）
                    cg.alpha = 0f;
                    rt.localScale = Vector3.one * 1.2f;
                });
        }


        private Tween TweenTimeScale(float targetScale, float duration = 0.5f) {
            return DOVirtual.Float(Time.timeScale, targetScale, duration, value => Time.timeScale = value);
        }

        private void OnDisable() {
            ResetDeathUI();
        }

        private void OnDestroy() {
            LevelEventCenter.OnGameOver -= GameOver;
        }

        private void ResetDeathUI() {
            // 恢复时间流速
            Time.timeScale = 1f;

            // 只清理大维度的东西, tween改的参数应该在各自的OnKill里重置
            if (_seq != null && _seq.IsActive()) {
                _seq.Kill();
                _seq = null;
            }

            if (_vCamDeath != null) {
                _vCamDeath.m_Lens.OrthographicSize = _initialOrthographicSize;
            }

            if (deathFXRefs.vignette != null) {
                deathFXRefs.vignette.intensity.value = 0;
            }

            if (deathFXRefs != null && deathFXRefs.obj != null)
                deathFXRefs.obj.GetComponent<Volume>().weight = 0;

            if (_vCamDeath != null)
                _vCamDeath.Priority = 0;

            if (_canvasGroup != null)
                UIUtil.SetUIVisible(_canvasGroup, false);
        }
    }
}

