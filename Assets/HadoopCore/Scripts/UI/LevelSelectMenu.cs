using System.Collections.Generic;
using HadoopCore.Scripts.Annotation;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace HadoopCore.Scripts.UI {
    public class LevelSelectMenu : MonoBehaviour {
        
        [SerializeField] private GameObject levelGridContainer;
        [SerializeField] private GameObject startsValue;
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
            
            // 1) 右上角的星星数量
            saveData.UpdateTotalStars();
            startsValue.GetComponent<TMP_Text>().text = saveData.TotalStarts.ToString();
            
            /**
             * TODO:
             * 1. 分页
             * 2. 根据星星总数解锁关卡
             */
            int totalLevels = 20; 
            for (int i = 1; i <= totalLevels; i++) {
                string levelId = i.ToString();
                GameObject levelItem = Instantiate(levelItemPrefab, levelGridContainer.transform);
                levelItem.name = $"Level_{i}";
                
                // Set level number text
                var levelNumberText = levelItem.transform.Find("LevelNumber").GetComponentInChildren<TMP_Text>();
                levelNumberText.text = levelId;
                
                // Check if level is unlocked
                bool isUnlocked = saveData.Levels.ContainsKey(levelItem.name) && saveData.Levels[levelItem.name].Unlocked;
                int stars = isUnlocked ? saveData.Levels[levelItem.name].BestStars : 0;
                
                // Configure level item based on unlock status
                ConfigureLevelItem(levelItem, isUnlocked, stars, levelItem.name);
                
                _levelItems.Add(levelItem);
            }
        }

        private void ConfigureLevelItem(GameObject levelItem, bool isUnlocked, int stars, string level) {
            // Get references
            var bgImage = levelItem.transform.Find("Bg").GetComponent<Image>();
            var lockIcon = levelItem.transform.Find("LockIcon").gameObject;
            var starBar = levelItem.transform.Find("StarBar").gameObject;

            // Button moved to root of LevelItem prefab
            var button = levelItem.GetComponent<Button>();
            var buttonGraphic = levelItem.GetComponent<Image>(); // transparent raycast surface

            if (isUnlocked) {
                // Unlocked state
                bgImage.sprite = bgUnlockSprite;
                lockIcon.SetActive(false);
                starBar.SetActive(true);

                if (buttonGraphic != null)
                    buttonGraphic.raycastTarget = true;

                if (button != null) {
                    button.enabled = true;
                    button.interactable = true;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => PlayClickFeedbackAndEnterLevel(levelItem.transform, button, level));
                }

                // Configure stars (0-3)
                ConfigureStars(starBar.transform, stars);
            } else {
                // Locked state
                bgImage.sprite = bgLockSprite;
                lockIcon.SetActive(true);
                starBar.SetActive(false);

                if (button != null) {
                    button.onClick.RemoveAllListeners();
                    button.interactable = false;
                    button.enabled = false;
                }

                if (buttonGraphic != null)
                    buttonGraphic.raycastTarget = false;
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
                    starImage.sprite = (i <= earnedStars) ? starFilledSprite : starEmptySprite;
                }
            }
        }

        private void PlayClickFeedbackAndEnterLevel(Transform levelItemTransform, Button button, string level)
        {
            if (levelItemTransform == null)
            {
                OnLevelButtonClicked(level);
                return;
            }

            // Prevent double click during feedback
            if (button != null)
                button.interactable = false;

            string tweenId = $"LevelItemClickScale_{levelItemTransform.GetInstanceID()}";
            DOTween.Kill(tweenId);

            levelItemTransform.localScale = Vector3.one;

            Sequence seq = DOTween.Sequence()
                .SetId(tweenId)
                .Append(levelItemTransform.DOScale(1.1f, 0.08f).SetEase(Ease.OutQuad))
                .Append(levelItemTransform.DOScale(1.0f, 0.08f).SetEase(Ease.InQuad))
                .OnComplete(() => OnLevelButtonClicked(level));

            seq.Play();
        }

        public void OnLevelButtonClicked(string level) {
            LevelManager.Instance.LoadScene(level);
        }
    }
}
