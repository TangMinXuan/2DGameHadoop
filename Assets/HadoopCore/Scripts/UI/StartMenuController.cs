using DG.Tweening;
using HadoopCore.Scripts.Manager;
using UnityEngine;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour {
    [Header("Button References")]
    [SerializeField] private Button btnStartGame;
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
        Debug.Log("Clicked: Start Game");
        
        _seq = DOTween.Sequence()
            .SetId("StartGameBtnTween")
            .Append(btnStartGame.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
            .Append(btnStartGame.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
            .OnComplete(() => GameManager.Instance.loadSceneSynchronously("LevelSelectMenu"));
    }

    private void OnSettingsClicked() {
        Debug.Log("Clicked: Settings");
        
        _seq = DOTween.Sequence()
            .SetId("SettingsBtnTween")
            .Append(btnSettings.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
            .Append(btnSettings.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
            .OnComplete(() => GameManager.Instance.loadSceneSynchronously("SettingsMenu"));
    }

    private void OnAboutClicked() {
        Debug.Log("Clicked: About");

        _seq = DOTween.Sequence()
            .SetId("AboutBtnTween")
            .Append(btnSettings.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
            .Append(btnSettings.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad));
    }

    private void OnDestroy() {
        _seq?.Kill();
        
        // Clean up listeners
        if (btnStartGame != null)
        {
            btnStartGame.onClick.RemoveListener(OnStartGameClicked);
        }

        if (btnSettings != null)
        {
            btnSettings.onClick.RemoveListener(OnSettingsClicked);
        }

        if (btnAbout != null)
        {
            btnAbout.onClick.RemoveListener(OnAboutClicked);
        }
    }
}
