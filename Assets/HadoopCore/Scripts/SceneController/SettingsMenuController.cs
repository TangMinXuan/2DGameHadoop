using DG.Tweening;
using HadoopCore.Scripts.Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controller for the Settings Menu scene.
/// Handles volume controls and Done button interactions.
/// </summary>
public class SettingsMenuController : MonoBehaviour
{
    [Header("BGM Controls")]
    [SerializeField] private Button btnBGM_Speaker;
    [SerializeField] private Slider sliderBGM;

    [Header("SFX Controls")]
    [SerializeField] private Button btnSFX_Speaker;
    [SerializeField] private Slider sliderSFX;

    [Header("Navigation")]
    [SerializeField] private Button btnDone;

    private Sequence _seq;

    private void Awake()
    {
        RegisterButtonListeners();
        RegisterSliderListeners();
    }

    private void RegisterButtonListeners()
    {
        if (btnBGM_Speaker != null)
        {
            btnBGM_Speaker.onClick.AddListener(OnBGMSpeakerClicked);
        }
        else
        {
            Debug.LogWarning("SettingsMenuController: BtnBGM_Speaker reference is not assigned!");
        }

        if (btnSFX_Speaker != null)
        {
            btnSFX_Speaker.onClick.AddListener(OnSFXSpeakerClicked);
        }
        else
        {
            Debug.LogWarning("SettingsMenuController: BtnSFX_Speaker reference is not assigned!");
        }

        if (btnDone != null)
        {
            btnDone.onClick.AddListener(OnDoneClicked);
        }
        else
        {
            Debug.LogWarning("SettingsMenuController: BtnDone reference is not assigned!");
        }
    }

    private void RegisterSliderListeners()
    {
        if (sliderBGM != null)
        {
            sliderBGM.onValueChanged.AddListener(OnBGMSliderChanged);
        }
        else
        {
            Debug.LogWarning("SettingsMenuController: SliderBGM reference is not assigned!");
        }

        if (sliderSFX != null)
        {
            sliderSFX.onValueChanged.AddListener(OnSFXSliderChanged);
        }
        else
        {
            Debug.LogWarning("SettingsMenuController: SliderSFX reference is not assigned!");
        }
    }

    private void OnBGMSpeakerClicked()
    {
        Debug.Log("Clicked: BGM Speaker");
    }

    private void OnSFXSpeakerClicked()
    {
        Debug.Log("Clicked: SFX Speaker");
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

    private void OnBGMSliderChanged(float value)
    {
        Debug.Log($"BGM Slider: {value:0.00}");
    }

    private void OnSFXSliderChanged(float value)
    {
        Debug.Log($"SFX Slider: {value:0.00}");
    }

    private void OnDestroy() {
        _seq?.Kill();
        
        // Clean up button listeners
        if (btnBGM_Speaker != null)
        {
            btnBGM_Speaker.onClick.RemoveListener(OnBGMSpeakerClicked);
        }

        if (btnSFX_Speaker != null)
        {
            btnSFX_Speaker.onClick.RemoveListener(OnSFXSpeakerClicked);
        }

        if (btnDone != null)
        {
            btnDone.onClick.RemoveListener(OnDoneClicked);
        }

        // Clean up slider listeners
        if (sliderBGM != null)
        {
            sliderBGM.onValueChanged.RemoveListener(OnBGMSliderChanged);
        }

        if (sliderSFX != null)
        {
            sliderSFX.onValueChanged.RemoveListener(OnSFXSliderChanged);
        }
    }
}
