using System;
using DG.Tweening;
using UnityEngine;

namespace HadoopCore.Scripts {
    public class ExclamationMarkVFX : MonoBehaviour {
        
        [SerializeField] private float duration = 0.3f;
        
        private Sequence _seq;

        private void OnEnable() {
            transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            transform.localRotation = Quaternion.Euler(0, 0, 45f);
            playVFX();
        }

        private void playVFX() {
            _seq?.Kill(); // 如果之前有动画正在播放，先杀掉它
            _seq = DOTween.Sequence()
                .Join( transform.DOScale(0.5f, duration).SetEase(Ease.OutBack) )
                .Join( transform.DORotate(Vector3.zero, duration).SetEase(Ease.OutBack) )
                .OnComplete( () => gameObject.SetActive(false) )
                .SetLink(gameObject); // DOTween 会在 gameObject 销毁时自动 Kill
        }
        
        private void OnDestroy() {
            _seq?.Kill();
            _seq = null;
        }
    }
}