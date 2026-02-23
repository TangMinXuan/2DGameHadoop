using DG.Tweening;
using HadoopCore.Scripts.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HadoopCore.Scripts.SceneController {
    public class SettingsMenuController : MonoBehaviour {
        [Header("BGM Controls")]
        [SerializeField] private Button btnBGM_Speaker;

        [SerializeField] private Slider sliderBGM;

        [Header("SFX Controls")] 
        [SerializeField] private Button btnSFX_Speaker;

        [SerializeField] private Slider sliderSFX;
        
        [SerializeField] private Sprite unMuteIcon;
        [SerializeField] private Sprite muteIcon;

        [Header("Navigation")] [SerializeField]
        private Button btnDone;

        private Sequence _seq;

        // Mute state tracking
        private bool _isBgmMuted;
        private bool _isSfxMuted;
        private float _bgmVolumeBeforeMute;
        private float _sfxVolumeBeforeMute;
        private Image _bgmBtnImage;
        private Image _sfxBtnImage;

        private void Awake() {
            RegisterButtonListeners();
        }

        private void OnEnable() {
            // Initialize sliders to current AudioManager volumes
            if (AudioManager.Instance != null) {
                if (sliderBGM != null) {
                    sliderBGM.SetValueWithoutNotify(AudioManager.Instance.GetBgmVolume());
                }

                if (sliderSFX != null) {
                    sliderSFX.SetValueWithoutNotify(AudioManager.Instance.GetSfxVolume());
                }
            }
            _bgmBtnImage = btnBGM_Speaker.GetComponent<Image>();
            _sfxBtnImage = btnSFX_Speaker.GetComponent<Image>();

            // 1. 确保场景中存在 EventSystem
            if (FindObjectOfType<EventSystem>() == null) {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Debug.Log("SettingsMenuController: Created EventSystem (none found in scene).");
            }

            // 2. 确保 Main Camera 上有 AudioListener
            var mainCam = Camera.main;
            if (mainCam != null) {
                if (mainCam.GetComponent<AudioListener>() == null) {
                    mainCam.gameObject.AddComponent<AudioListener>();
                    Debug.Log("SettingsMenuController: Added AudioListener to Main Camera.");
                }
            }
            else {
                Debug.LogWarning("SettingsMenuController: No Main Camera found in scene!");
            }
        }

        private void RegisterButtonListeners() {
            if (btnBGM_Speaker != null) {
                btnBGM_Speaker.onClick.AddListener(OnBGMSpeakerClicked);
            }

            if (btnSFX_Speaker != null) {
                btnSFX_Speaker.onClick.AddListener(OnSFXSpeakerClicked);
            }

            if (btnDone != null) {
                btnDone.onClick.AddListener(OnDoneClicked);
            }

            if (sliderBGM != null) {
                sliderBGM.onValueChanged.AddListener(OnBGMSliderChanged);
            }

            if (sliderSFX != null) {
                sliderSFX.onValueChanged.AddListener(OnSFXSliderChanged);
            }
        }

        private void OnBGMSpeakerClicked() {
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .Append(btnBGM_Speaker.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(btnBGM_Speaker.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .SetLink(gameObject)
                .OnComplete(() => {
                    if (_isBgmMuted) {
                        // Unmute: restore previous volume
                        _isBgmMuted = false;
                        AudioManager.Instance.SetBgmVolume(_bgmVolumeBeforeMute);
                        sliderBGM.SetValueWithoutNotify(_bgmVolumeBeforeMute);
                    }
                    else {
                        // Mute: save current volume and set to 0
                        _isBgmMuted = true;
                        _bgmVolumeBeforeMute = AudioManager.Instance.GetBgmVolume();
                        AudioManager.Instance.SetBgmVolume(0f);
                        sliderBGM.SetValueWithoutNotify(0f);
                    }
                    _bgmBtnImage.sprite = _isBgmMuted ? muteIcon : unMuteIcon;
                });
        }

        private void OnSFXSpeakerClicked() {
            _seq = DOTween.Sequence()
                .SetUpdate(true)
                .Append(btnSFX_Speaker.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(btnSFX_Speaker.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .SetLink(gameObject)
                .OnComplete(() => {
                    if (_isSfxMuted) {
                        // Unmute: restore previous volume
                        _isSfxMuted = false;
                        AudioManager.Instance.SetSfxVolume(_sfxVolumeBeforeMute);
                        sliderSFX.SetValueWithoutNotify(_sfxVolumeBeforeMute);
                    } else {
                        // Mute: save current volume and set to 0
                        _isSfxMuted = true;
                        _sfxVolumeBeforeMute = AudioManager.Instance.GetSfxVolume();
                        AudioManager.Instance.SetSfxVolume(0f);
                        sliderSFX.SetValueWithoutNotify(0f);
                    }
                    _sfxBtnImage.sprite = _isSfxMuted ? muteIcon : unMuteIcon;
                });
        }

        private void OnDoneClicked() {
            _seq = DOTween.Sequence()
                .SetId("DoneBtnTween")
                .SetUpdate(true)
                .Append(btnDone.transform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(btnDone.transform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .SetLink(gameObject)
                .OnComplete(() => SceneManager.UnloadSceneAsync("SettingsMenu"));
        }

        private void OnBGMSliderChanged(float value) {
            if (AudioManager.Instance == null) return;

            // When user drags the slider, clear mute state
            _isBgmMuted = false;
            AudioManager.Instance.SetBgmVolume(value);
        }

        private void OnSFXSliderChanged(float value) {
            if (AudioManager.Instance == null) return;

            // When user drags the slider, clear mute state
            _isSfxMuted = false;
            AudioManager.Instance.SetSfxVolume(value);
        }

        private void OnDestroy() {
            _seq?.Kill();

            // Clean up button listeners
            if (btnBGM_Speaker != null) {
                btnBGM_Speaker.onClick.RemoveListener(OnBGMSpeakerClicked);
            }

            if (btnSFX_Speaker != null) {
                btnSFX_Speaker.onClick.RemoveListener(OnSFXSpeakerClicked);
            }

            if (btnDone != null) {
                btnDone.onClick.RemoveListener(OnDoneClicked);
            }

            // Clean up slider listeners
            if (sliderBGM != null) {
                sliderBGM.onValueChanged.RemoveListener(OnBGMSliderChanged);
            }

            if (sliderSFX != null) {
                sliderSFX.onValueChanged.RemoveListener(OnSFXSliderChanged);
            }
        }
    }
}