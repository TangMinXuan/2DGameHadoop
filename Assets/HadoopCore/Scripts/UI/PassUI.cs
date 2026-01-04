using System;
using Cinemachine;
using DG.Tweening;
using HadoopCore.Scripts.Utils;
using UnityEngine;


namespace HadoopCore.Scripts.UI
{
    public class PassUI : MonoBehaviour
    {
        [SerializeField] private GameObject cameraRig;
        private CinemachineVirtualCamera _vCamGameplay;
        private CanvasGroup _canvasGroup;
        private Sequence _seq;
        private float _initialOrthographicSize = 8f;
        
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _vCamGameplay = cameraRig.GetComponentInChildren<CinemachineVirtualCamera>();
            _initialOrthographicSize = _vCamGameplay.m_Lens.OrthographicSize;

            LevelEventCenter.OnGameSuccess += GameSuccess;
            UIUtil.SetUIVisible(_canvasGroup, false);
        }

        private void GameSuccess()
        {
            UIUtil.SetUIVisible(_canvasGroup, true);
            _seq = DOTween.Sequence()
                .SetId("PassPresentation")
                .SetUpdate(true);
            _seq.Join( TweenZoomIn(5f).SetEase(Ease.Linear) );
        }


        private Tween TweenZoomIn(float orthographicSize)
        {
            return DOTween.To(
                () => _vCamGameplay.m_Lens.OrthographicSize,
                x => _vCamGameplay.m_Lens.OrthographicSize = x,
                orthographicSize,
                0.35f
            );
        }

        private void OnDisable()
        {
            if (_seq != null && _seq.IsActive())
            {
                _seq.Kill();
                _seq = null;
            }
            UIUtil.SetUIVisible(_canvasGroup, false);
        }
        
        private void OnDestroy()
        {
            if (_vCamGameplay != null)
            {
                _vCamGameplay.m_Lens.OrthographicSize = _initialOrthographicSize;
            }
            UIUtil.SetUIVisible(_canvasGroup, false);
            LevelEventCenter.OnGameSuccess -= GameSuccess;
        }
    }
}