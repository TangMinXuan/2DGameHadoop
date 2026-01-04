using System;
using Cinemachine;
using DG.Tweening;
using HadoopCore.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HadoopCore.Scripts.UI
{
    [Serializable]
    internal class DeathFXRefs
    {
        public GameObject obj;
        [NonSerialized] public Vignette vignette;
        [NonSerialized] public Bloom bloom;
        [NonSerialized] public ColorAdjustments colorAdjustments;
    }
    
    [Serializable]
    internal class DeathContentRefs
    {
        public GameObject obj;
        [NonSerialized] public RectTransform centerBarRt;
        [NonSerialized] public CanvasGroup centerBarCg;
        [NonSerialized] public TMP_Text wastedTMP;
        [NonSerialized] public CanvasGroup wastedCg;
    }
    
    
    public class DeadUI : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject cameraRig;
        private Sequence _seq;
        private CinemachineVirtualCamera _vCamDeath;
        
        [SerializeField] private DeathFXRefs deathFXRefs;
        [SerializeField] private DeathContentRefs deathContentRefs;
        
        private float _initialOrthographicSize = 8f;
        
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            deathFXRefs.obj = MySugarUtil.TryToFindObject(gameObject, "DeathFX", deathFXRefs.obj);
            deathFXRefs.obj.GetComponent<Volume>().profile.TryGet(out deathFXRefs.vignette);
            deathFXRefs.obj.GetComponent<Volume>().profile.TryGet(out deathFXRefs.bloom);
            deathFXRefs.obj.GetComponent<Volume>().profile.TryGet(out deathFXRefs.colorAdjustments);
            deathContentRefs.obj = MySugarUtil.TryToFindObject(gameObject, "DeathContent", deathContentRefs.obj);
            deathContentRefs.centerBarRt = MySugarUtil.TryToFindComponent(deathContentRefs.obj, "CenterBar", deathContentRefs.centerBarRt);
            deathContentRefs.centerBarCg = MySugarUtil.TryToFindComponent(deathContentRefs.obj, "CenterBar", deathContentRefs.centerBarCg);
            deathContentRefs.wastedTMP = MySugarUtil.TryToFindComponent(deathContentRefs.obj, "WastedText", deathContentRefs.wastedTMP);
            deathContentRefs.wastedCg = MySugarUtil.TryToFindComponent(deathContentRefs.obj, "WastedText", deathContentRefs.wastedCg);
            _vCamDeath = cameraRig.GetComponentInChildren<CinemachineVirtualCamera>();
            
            _initialOrthographicSize = _vCamDeath.m_Lens.OrthographicSize;
            
            LevelEventCenter.OnGameOver += GameOver;
            ResetDeathUI();
        }

        private void GameOver()
        {
            // 组件级的初始化 - 这些会在reset()中重置
            UIUtil.SetUIVisible(_canvasGroup, true);
            deathFXRefs.obj.GetComponent<Volume>().weight = 1;
            _vCamDeath.Priority = 1;
            
            // 1. 时间缩放 - 渐进式慢动作
            _seq = DOTween.Sequence()
                .SetId("DeathPresentation")
                .SetUpdate(true)
                .Join(TweenTimeScale(0.05f, 0.05f).SetEase(Ease.OutCubic));
            
            // 2. DOTween推: 镜头缓慢拉近 + 黑边缓慢增长 + 镜头缓慢模糊 + 镜头晃动

            _seq.Join( TweenZoomIn(5f).SetEase(Ease.Linear) );              // 镜头拉近 In = 慢启动，Out = 慢停止
            _seq.Join( TweenVignetteIn(0.5f).SetEase(Ease.Linear) );               // 黑边增长
            _seq.Join( TweenDutchShake(5f, 2f).SetEase(Ease.Linear));                // 相机抖动

            // 3. CenterBar + WastedText 由远到近飞入, 泛光
            _seq.Insert(3f,TweenBloomIn(1f).SetEase(Ease.Linear) );                    // 泛光增强
            _seq.Insert(3f,TweenColorAdjustmentsIn(0.25f).SetEase(Ease.Linear) );   // 画面变亮
            _seq.Insert(3f, TweenShowCenterBar(deathContentRefs.centerBarRt, deathContentRefs.centerBarCg, targetHeight: 180f, targetAlpha: 0.35f));
            _seq.Insert(3f, TweenShowWastedText(deathContentRefs.wastedTMP, deathContentRefs.wastedCg));
        }
        
        private Tween TweenZoomIn(float orthographicSize)
        {
            return DOTween.To(
                () => _vCamDeath.m_Lens.OrthographicSize,
                x => _vCamDeath.m_Lens.OrthographicSize = x, 
                orthographicSize, 
                0.35f
            ).OnKill(() =>
            {
                if (_vCamDeath != null)
                {
                    _vCamDeath.m_Lens.OrthographicSize = _initialOrthographicSize;
                }
            });
        }
        
        private Tween TweenVignetteIn(float intensity)
        {
            return DOTween.To(
                () => deathFXRefs.vignette.intensity.value,
                x => deathFXRefs.vignette.intensity.value = x, 
                intensity, 
                0.35f
            ).OnKill(() =>
            {
                if (deathFXRefs.vignette != null)
                    deathFXRefs.vignette.intensity.value = 0;
            });
        }
        
        private Tween TweenBloomIn(float intensity)
        {
            return DOTween.To(
                () => deathFXRefs.bloom.intensity.value,
                x => deathFXRefs.bloom.intensity.value = x, 
                intensity, 
                1f
            ).OnKill(() =>
            {
                if (deathFXRefs.bloom != null)
                    deathFXRefs.bloom.intensity.value = 0;
            });
        }
        
        private Tween TweenColorAdjustmentsIn(float postExposure)
        {
            return DOTween.To(
                () => deathFXRefs.colorAdjustments.postExposure.value,
                x => deathFXRefs.colorAdjustments.postExposure.value = x, 
                postExposure, 
                1f
            ).OnKill(() =>
            {
                if (deathFXRefs.colorAdjustments != null)
                    deathFXRefs.colorAdjustments.postExposure.value = 0;
            });
        }
        
        private Tween TweenDutchShake(float maxDutchDeg, float freqHz)
        {
            return DOVirtual.Float(-maxDutchDeg, +maxDutchDeg, freqHz, v =>
                {
                    var lens = _vCamDeath.m_Lens;
                    lens.Dutch = v;
                    _vCamDeath.m_Lens = lens;
                })
                .SetLoops(10, LoopType.Yoyo)    // 10次来回
                .OnKill(() =>
                {
                    var lens = _vCamDeath.m_Lens;
                    lens.Dutch = 0f;
                    _vCamDeath.m_Lens = lens;
                });
        }

        private Sequence TweenShowWastedText(TMP_Text wastedText, CanvasGroup cg)
        {
            RectTransform rt = (RectTransform)wastedText.transform;

            // 初始状态
            cg.alpha = 0f;
            rt.localScale = Vector3.one * 1.2f;

            var seq = DOTween.Sequence();

            // 0.10s：淡入 + 缩放回到 1
            seq.Join(cg.DOFade(1f, 0.10f).SetEase(Ease.OutCubic));
            seq.Join(rt.DOScale(1f, 0.10f).SetEase(Ease.OutCubic));

            // 0.08s：轻微放大到 1.04
            seq.Append(rt.DOScale(1.04f, 0.08f).SetEase(Ease.OutSine));
            // 0.10s：回到 1
            seq.Append(rt.DOScale(1f, 0.10f).SetEase(Ease.InSine));

            return seq;
        }

        private Sequence TweenShowCenterBar(RectTransform bar, CanvasGroup cg, float targetHeight, float targetAlpha = 0.35f)
        {
            // 初始状态：高度为 0（保持当前宽度）
            Vector2 size = bar.sizeDelta;
            bar.sizeDelta = new Vector2(size.x, 0f);
            cg.alpha = 0f;

            var seq = DOTween.Sequence();

            // 高度增长（你要"缓慢变高"）
            seq.Join(bar.DOSizeDelta(new Vector2(size.x, targetHeight), 0.30f).SetEase(Ease.OutCubic));
            // 透明度淡入（到目标 alpha）
            seq.Join(cg.DOFade(targetAlpha, 0.25f).SetEase(Ease.OutCubic));

            // 可选：轻微 overshoot（更有"压迫感"）
            seq.Append(bar.DOSizeDelta(new Vector2(size.x, targetHeight * 1.05f), 0.08f).SetEase(Ease.OutSine));
            seq.Append(bar.DOSizeDelta(new Vector2(size.x, targetHeight), 0.10f).SetEase(Ease.InSine));

            return seq;
        }
        
        private Tween TweenTimeScale(float targetScale, float duration = 0.5f)
        {
            return DOVirtual.Float(Time.timeScale, targetScale, duration, value => Time.timeScale = value);
        }
        
        private void OnDisable()
        {
            ResetDeathUI();
        }

        private void OnDestroy()
        {
            LevelEventCenter.OnGameOver -= GameOver;
        }
        
        private void ResetDeathUI()
        {
            // 恢复时间流速
            Time.timeScale = 1f;
            
            // 只清理大维度的东西, tween改的参数应该在各自的OnKill里重置
            if (_seq != null && _seq.IsActive())
            {
                _seq.Kill();
                _seq = null;
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