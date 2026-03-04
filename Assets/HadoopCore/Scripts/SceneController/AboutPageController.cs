using DG.Tweening;
using HadoopCore.Scripts.Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HadoopCore.Scripts.SceneController {
    public class AboutPageController : MonoBehaviour {
        
        [SerializeField] private Button btnDone;
        
        private Sequence _seq;
        
        private void Awake() {
            btnDone.onClick.AddListener(OnDoneClicked);
            btnDone.onClick.AddListener(() => AudioManager.Instance.PlayBtnSfx());
        }
        
        private void OnDoneClicked() {
            _seq = DOTween.Sequence()
                .SetId("DoneBtnTween")
                .SetUpdate(true)
                .Append(btnDone.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(btnDone.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .SetLink(gameObject)
                .OnComplete(() => GameManager.Instance.loadSceneSynchronously("GameStartPage"));
        }

        private void OnDestroy() {
            _seq?.Kill();
        }
        
    }
}