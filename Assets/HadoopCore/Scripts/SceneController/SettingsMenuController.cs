using System.Collections.Generic;
using DG.Tweening;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Shared;
using TMPro;
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

        [Header("Display Settings")]
        [SerializeField] private TMP_Dropdown dropdownDisplayMode;
        [SerializeField] private TMP_Dropdown dropdownResolution;

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
            InitDisplayDropdowns();
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

        private void InitDisplayDropdowns() {
            if (BuildEnvConfig.Instance.CurrentTarget != PlatformTarget.PC) {
                return;
            }
            // --- Display Mode Dropdown ---
            if (dropdownDisplayMode != null) {
                dropdownDisplayMode.ClearOptions();
                dropdownDisplayMode.AddOptions(new List<string> {
                    "Borderless Fullscreen", "Windowed"
                });
                // Set current value based on actual screen mode
                int modeIndex = Screen.fullScreenMode switch {
                    FullScreenMode.FullScreenWindow => 0,
                    FullScreenMode.Windowed => 1,
                    _ => 2
                };
                dropdownDisplayMode.SetValueWithoutNotify(modeIndex);
                dropdownDisplayMode.onValueChanged.AddListener(OnDisplayModeChanged);
            }

            // --- Resolution Dropdown ---
            if (dropdownResolution != null) {
                dropdownResolution.ClearOptions();
                var labels = new List<string>();
                int currentIndex = 0;
                List<Vector2Int> availableResolutions = DisplaySettingTool.GetAvailableResolutionOptions();
                for (int i = 0; i < availableResolutions.Count; i++) {
                    labels.Add($"{availableResolutions[i].x} x {availableResolutions[i].y}");
                    if (Screen.width == availableResolutions[i].x && Screen.height == availableResolutions[i].y)
                        currentIndex = i;
                }

                if (Screen.fullScreenMode  == FullScreenMode.FullScreenWindow) {
                    dropdownResolution.interactable = false;
                } else {
                    dropdownResolution.AddOptions(labels);
                    dropdownResolution.SetValueWithoutNotify(currentIndex);
                    dropdownResolution.onValueChanged.AddListener(OnResolutionChanged);
                }
            }
        }

        private void OnDisplayModeChanged(int index) {
            FullScreenMode mode = index switch {
                0 => FullScreenMode.FullScreenWindow,
                1 => FullScreenMode.Windowed,
                _ => Screen.fullScreenMode, // No change
            };
            if (mode == FullScreenMode.FullScreenWindow) {
                DisplaySettingTool.ApplyDisplayMode(mode, Vector2Int.zero);
                GameSaveData gameSaveData = GameManager.Instance.GetSaveData();
                gameSaveData.Settings["displayMode"] = FullScreenMode.FullScreenWindow.ToString();
                GameManager.Instance.SaveGameDataAsync(gameSaveData);
            }
        }

        private void OnResolutionChanged(int index) {
            string label = dropdownResolution.options[index].text;
            int width = label.Split('x')[0].Trim() == "" ? Screen.width : int.Parse(label.Split('x')[0].Trim());
            int height = label.Split('x')[1].Trim() == "" ? Screen.height : int.Parse(label.Split('x')[1].Trim());
            DisplaySettingTool.ApplyDisplayMode(FullScreenMode.Windowed, new Vector2Int(width, height));
            GameSaveData gameSaveData = GameManager.Instance.GetSaveData();
            gameSaveData.Settings["resolution"] = $"{width}x{height}";
            gameSaveData.Settings["displayMode"] = FullScreenMode.Windowed.ToString();
            GameManager.Instance.SaveGameDataAsync(gameSaveData);
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
            
            btnBGM_Speaker.onClick.AddListener(() => AudioManager.Instance.PlayBtnSfx());
            btnSFX_Speaker.onClick.AddListener(() => AudioManager.Instance.PlayBtnSfx());
            btnDone.onClick.AddListener(() => AudioManager.Instance.PlayBtnSfx());
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
            // TODO 设置写进存档
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

            // Clean up dropdown listeners
            if (dropdownDisplayMode != null) {
                dropdownDisplayMode.onValueChanged.RemoveListener(OnDisplayModeChanged);
            }
            if (dropdownResolution != null) {
                dropdownResolution.onValueChanged.RemoveListener(OnResolutionChanged);
            }
        }
    }
}