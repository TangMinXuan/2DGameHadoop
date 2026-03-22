using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HadoopCore.Scripts.Manager {
    
    /// <summary>
    /// Serializable entry for scene-to-BGM mapping.
    /// </summary>
    [Serializable]
    public class SceneBgmEntry {
        [Tooltip("Scene name (case-insensitive match)")]
        public string sceneName;
        
        [Tooltip("BGM clip to play for this scene")]
        public AudioClip bgmClip;
    }
    /// <summary>
    /// Minimal audio system for BGM and SFX.
    /// Singleton with DontDestroyOnLoad.
    /// </summary>
    public class AudioManager : MonoBehaviour {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [Tooltip("AudioSource for background music (should be set to loop)")]
        [SerializeField] private AudioSource bgmSource;
        
        [Tooltip("AudioSource for sound effects (one-shot playback)")]
        [SerializeField] private AudioSource sfxSource;

        [Header("BGM Configuration")]
        [Tooltip("Map scene names to their background music clips")]
        [SerializeField] private List<SceneBgmEntry> sceneBgmMappings = new List<SceneBgmEntry>();
        
        [SerializeField] private AudioClip btnClickedSfx;

        // Runtime dictionary built from the serialized list for O(1) lookup
        private Dictionary<string, AudioClip> _sceneBgmDict;

        [Header("Fade Settings")]
        [Tooltip("Enable fade transition when switching BGM")]
        [SerializeField] private bool enableFade = true;
        
        [Tooltip("Duration of fade out/in transition in seconds")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Default Volumes")]
        [SerializeField] private float bgmVolume = 0.8f;
        [SerializeField] private float sfxVolume = 0.8f;

        private Coroutine _fadeCoroutine;

        void Awake() {
            // Singleton pattern: prevent duplicates across scene loads
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Build runtime lookup dictionary from serialized list
            _sceneBgmDict = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in sceneBgmMappings) {
                if (entry != null && !string.IsNullOrEmpty(entry.sceneName) && entry.bgmClip != null) {
                    _sceneBgmDict[entry.sceneName] = entry.bgmClip;
                }
            }

            // Subscribe to scene changes
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Initialize volumes
            if (bgmSource != null) {
                bgmSource.volume = bgmVolume;
                bgmSource.loop = true;
            }
            if (sfxSource != null) {
                sfxSource.volume = sfxVolume;
            }
        }

        void Start() {
            // Play BGM for the initial scene
            string currentSceneName = SceneManager.GetActiveScene().name;
            PlayBgmForScene(currentSceneName, false); // No fade on initial load
        }

        void OnDestroy() {
            // Only cleanup if this is the actual singleton instance
            if (Instance != this) {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        #region Public API
        
        public void PlayBtnSfx() {
            PlaySfx(btnClickedSfx);
        }

        public void PlaySfx(AudioClip clip) {
            if (sfxSource == null) {
                Debug.LogError("[AudioManager] sfxSource is not assigned");
                return;
            }
            sfxSource.PlayOneShot(clip);
        }

        private void PlayBgm(AudioClip clip, bool useFade = true) {
            if (bgmSource == null) {
                Debug.LogError("[AudioManager] bgmSource is not assigned");
                return;
            }
            // 如果当前处于暂停状态且播放的 clip 与目标 clip 相同，则继续播放
            if (bgmSource.clip == clip && !bgmSource.isPlaying) {
                bgmSource.UnPause();
                return;
            }

            // Skip if same clip is already playing
            if (bgmSource.clip == clip && bgmSource.isPlaying) {
                return;
            }

            if (useFade && enableFade && bgmSource.isPlaying) {
                if (_fadeCoroutine != null) {
                    StopCoroutine(_fadeCoroutine);
                }
                _fadeCoroutine = StartCoroutine(FadeBgm(clip));
            } else {
                bgmSource.Stop();
                bgmSource.clip = clip;
                bgmSource.volume = bgmVolume;
                if (clip != null) {
                    bgmSource.Play();
                }
            }
        }

        public void StopBgm(bool useFade = true) {
            if (bgmSource == null) return;

            if (useFade && enableFade && bgmSource.isPlaying) {
                if (_fadeCoroutine != null) {
                    StopCoroutine(_fadeCoroutine);
                }
                _fadeCoroutine = StartCoroutine(FadeBgm());
            } else {
                bgmSource.Stop();
            }
        }
        
        public void PauseBgm() {
            if (bgmSource == null) return;
            bgmSource.Pause();
        }

        public void ResumeBgm() {
            if (bgmSource == null) return;
            bgmSource.UnPause();
        }

        public float GetBgmVolume() => bgmVolume;

        public float GetSfxVolume() => sfxVolume;

        public void SetBgmVolume(float volume) {
            bgmVolume = Mathf.Clamp01(volume);
            if (bgmSource != null) {
                bgmSource.volume = bgmVolume;
            }
        }

        public void SetSfxVolume(float volume) {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null) {
                sfxSource.volume = sfxVolume;
            }
        }

        #endregion

        #region Private Methods

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            PlayBgmForScene(scene.name, true);
        }

        private void PlayBgmForScene(string sceneName, bool useFade) {
            AudioClip clip = GetBgmClipForScene(sceneName);
            if (clip != null) {
                PlayBgm(clip, useFade);
            }
        }

        private AudioClip GetBgmClipForScene(string sceneName) {
            if (string.Equals(sceneName, "SettingsMenu", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sceneName, "LoadingPage", StringComparison.OrdinalIgnoreCase)) {
                return null;
            }
            if (_sceneBgmDict.TryGetValue(sceneName, out AudioClip clip)) {
                return clip;
            }
            // Fallback: if scene name starts with "Level_", use "default"; otherwise use "panel"
            string fallbackKey = sceneName.StartsWith("Level_", StringComparison.OrdinalIgnoreCase)
                ? "level"
                : "panel";
            if (_sceneBgmDict.TryGetValue(fallbackKey, out AudioClip fallbackClip)) {
                return fallbackClip;
            }
            return null;
        }

        private IEnumerator FadeBgm(AudioClip newClip = null) {
            float startVolume = bgmSource.volume;

            // Fade out
            float elapsed = 0f;
            while (elapsed < fadeDuration) {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }
            bgmSource.volume = 0f;
            bgmSource.Stop();

            // If newClip provided, switch and fade in
            if (newClip != null) {
                bgmSource.clip = newClip;
                bgmSource.Play();

                // Fade in
                elapsed = 0f;
                while (elapsed < fadeDuration) {
                    elapsed += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(0f, bgmVolume, elapsed / fadeDuration);
                    yield return null;
                }
                bgmSource.volume = bgmVolume;
            } else {
                // Fade-out only: restore default volume for next play
                bgmSource.volume = bgmVolume;
            }

            _fadeCoroutine = null;
        }

        #endregion
    }
}
