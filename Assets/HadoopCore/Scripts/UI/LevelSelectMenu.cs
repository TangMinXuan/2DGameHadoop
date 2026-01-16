using System.Collections.Generic;
using HadoopCore.Scripts.Annotation;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HadoopCore.Scripts.UI {
    public class LevelSelectMenu : MonoBehaviour {
        
        [SerializeField] private GameObject levelGridContainer;
        [SerializeField, DontNeedAutoFind] private GameObject levelItemPrefab;
        
        // Sprites for level item background
        [SerializeField] private Sprite bgUnlockSprite;
        [SerializeField] private Sprite bgLockSprite;
        
        // Sprites for stars
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;
        
        private List<GameObject> _levelItems = new();

        private void Awake() {
            MySugarUtil.AutoFindObjects(this, gameObject);
        }

        private void Start() {
            GameSaveData saveData = LevelManager.Instance.GetSaveData();
            
            int totalLevels = 20;
            for (int i = 1; i <= totalLevels; i++) {
                string levelId = i.ToString();
                GameObject levelItem = Instantiate(levelItemPrefab, levelGridContainer.transform);
                levelItem.name = $"Level_{i}";
                
                // Set level number text
                var levelNumberText = levelItem.transform.Find("LevelNumber").GetComponentInChildren<TMP_Text>();
                levelNumberText.text = levelId;
                
                // Check if level is unlocked
                bool isUnlocked = saveData.Levels.ContainsKey(levelId) && saveData.Levels[levelId].Unlocked;
                int stars = isUnlocked ? saveData.Levels[levelId].Stars : 0;
                
                // Configure level item based on unlock status
                ConfigureLevelItem(levelItem, isUnlocked, stars, levelId);
                
                _levelItems.Add(levelItem);
            }
        }

        private void ConfigureLevelItem(GameObject levelItem, bool isUnlocked, int stars, string levelId) {
            // Get references
            var bgImage = levelItem.transform.Find("Bg").GetComponent<Image>();
            var lockIcon = levelItem.transform.Find("LockIcon").gameObject;
            var starBar = levelItem.transform.Find("StarBar").gameObject;
            var button = levelItem.transform.Find("Button").GetComponent<Button>();

            if (isUnlocked) {
                // Unlocked state
                bgImage.sprite = bgUnlockSprite;
                lockIcon.SetActive(false);
                starBar.SetActive(true);
                button.gameObject.SetActive(true);
                button.interactable = true;
                
                // Configure stars (0-3)
                ConfigureStars(starBar.transform, stars);
                
                // Setup button click
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnLevelButtonClicked(levelId));
            } else {
                // Locked state
                bgImage.sprite = bgLockSprite;
                lockIcon.SetActive(true);
                starBar.SetActive(false);
                button.gameObject.SetActive(false);
            }
        }

        private void ConfigureStars(Transform starBarTransform, int earnedStars) {
            var starsContainer = starBarTransform.Find("Stars");
            if (starsContainer == null) return;
            
            // Star children are named Start_1, Start_2, Start_3
            for (int i = 1; i <= 3; i++) {
                var starTransform = starsContainer.Find($"Start_{i}");
                if (starTransform == null) continue;
                
                var starImage = starTransform.GetComponent<Image>();
                if (starImage != null) {
                    // If star index <= earned stars, show filled; otherwise show empty
                    starImage.sprite = (i <= earnedStars) ? starFilledSprite : starEmptySprite;
                }
            }
        }

        public void OnLevelButtonClicked(string levelId) {
            LevelManager.Instance.JumpToLevel(levelId);
        }
    }
}
