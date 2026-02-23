using DG.Tweening;
using HadoopCore.Scripts.Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HadoopCore.Scripts.SceneController {
    public class StartMenuController : MonoBehaviour {
        [Header("Button References")] [SerializeField]
        private Button btnStartGame;

        [SerializeField] private Button btnSettings;
        [SerializeField] private Button btnAbout;

        private Sequence _seq;

        private void Awake() {
            if (btnStartGame != null) {
                btnStartGame.onClick.AddListener(OnStartGameClicked);
            }

            if (btnSettings != null) {
                btnSettings.onClick.AddListener(OnSettingsClicked);
            }

            if (btnAbout != null) {
                btnAbout.onClick.AddListener(OnAboutClicked);
            }
        }

        private void OnStartGameClicked() {
            _seq = DOTween.Sequence()
                .SetId("StartGameBtnTween")
                .Append(btnStartGame.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(btnStartGame.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => GameManager.Instance.loadSceneSynchronously("LevelSelectMenu"))
                .SetLink(gameObject);
        }

        private void OnSettingsClicked() {
            _seq = DOTween.Sequence()
                .SetId("SettingsBtnTween")
                .Append(btnSettings.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(btnSettings.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => GameManager.Instance.loadSceneSynchronously("SettingsMenu", LoadSceneMode.Additive))
                .SetLink(gameObject);
        }

        private void OnAboutClicked() {
            _seq = DOTween.Sequence()
                .SetId("AboutBtnTween")
                .Append(btnSettings.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(btnSettings.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => GameManager.Instance.loadSceneSynchronously("AboutPage"))
                .SetLink(gameObject);
        }

        private void OnDestroy() {
            _seq?.Kill();
            _seq = null;

            // Clean up listeners
            if (btnStartGame != null) {
                btnStartGame.onClick.RemoveListener(OnStartGameClicked);
            }

            if (btnSettings != null) {
                btnSettings.onClick.RemoveListener(OnSettingsClicked);
            }

            if (btnAbout != null) {
                btnAbout.onClick.RemoveListener(OnAboutClicked);
            }
        }
    }
}