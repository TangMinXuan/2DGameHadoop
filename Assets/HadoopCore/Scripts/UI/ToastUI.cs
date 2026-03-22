using DG.Tweening;
using HadoopCore.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace HadoopCore.Scripts.UI {
    public class ToastUI : MonoBehaviour {
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private TextMeshProUGUI toastText;
        private CanvasGroup _canvasGroup;

        private Sequence _seq;

        private void Awake() {
            // 确保初始状态为隐藏
            UIUtil.SetUIVisible(_canvasGroup, false);
            _canvasGroup = toastPanel.GetComponent<CanvasGroup>();
        }

        private void OnDestroy() {
            _seq?.Kill();
        }

        public void ShowToastMsg(string message, float duration = 1f) {
            if (toastPanel == null) {
                return;
            }
            _seq?.Kill();

            UIUtil.SetUIVisible(_canvasGroup, true);
            toastText.text = message;
            
            toastPanel.transform.localScale = new Vector3(0f, 1f, 1f);
            _seq = DOTween.Sequence()
                .Append(toastPanel.transform.DOScaleX(1f, 0.3f).SetEase(Ease.OutBack))
                .AppendInterval(duration)
                .Append(toastPanel.transform.DOScaleX(0f, 0.3f).SetEase(Ease.InBack))
                .OnComplete(() => UIUtil.SetUIVisible(_canvasGroup, false))
                .SetLink(gameObject);
        }
    }
}

