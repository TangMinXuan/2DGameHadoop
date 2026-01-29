using System;
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
        [SerializeField] private GameObject starsValue;
        [SerializeField, DontNeedAutoFind] private GameObject levelItemPrefab;
        
        // Sprites for level item background
        [SerializeField] private Sprite bgUnlockSprite;
        [SerializeField] private Sprite bgLockSprite;
        
        // Sprites for stars
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;
        
        // Sprites for padlock
        [SerializeField] private Sprite unlockSprite;
        
        private List<GameObject> _levelItems = new();
        private Sequence _seq;

        private void Awake() {
            MySugarUtil.AutoFindObjects(this, gameObject);
        }

        private void Start() {
            GameSaveData saveData = GameManager.Instance.GetSaveData();
            
            // 1) 右上角的星星数量
            saveData.UpdateTotalStars();
            starsValue.GetComponent<TMP_Text>().text = saveData.TotalStarts.ToString();
            
            /**
             * TODO:
             * 1. 分页
             * 2. 根据星星总数解锁关卡 - Done
             */
            int totalLevels = 20;
            for (int i = 1; i <= totalLevels; i++) {
                string levelId = i.ToString();
                GameObject levelItem = Instantiate(levelItemPrefab, levelGridContainer.transform);
                levelItem.name = $"Level_{i}";
                
                // Set level number text
                TMP_Text levelNumberText = MySugarUtil.TryToFindComponent<TMP_Text>(levelItem, "TMP");
                levelNumberText.text = levelId;
                
                // Check if level is unlocked: 先按照 unlocked 渲染出来, 再按照 TotalStarts 播放解锁动画, 所以拆成了2个循环
                bool isUnlocked = saveData.Levels[levelItem.name].Unlocked;
                int stars = isUnlocked ? saveData.Levels[levelItem.name].BestStars : 0;

                // Configure level item based on unlock status
                // case1. 之前解锁，直接配置 
                // case2: 新解锁, 有解锁动画
                ConfigureLevelItem(levelItem, isUnlocked, stars, levelItem.name, saveData.Levels[levelItem.name].RequiredStars);
                
                _levelItems.Add(levelItem);
            }
            
            // Play unlock animations based on TotalStarts
            _seq = DOTween.Sequence()
                .SetId("UnlockLevelsSequence");
            _levelItems.ForEach(levelItem => {
                if (!saveData.Levels[levelItem.name].Unlocked && 
                    saveData.TotalStarts >= saveData.Levels[levelItem.name].RequiredStars) {
                    saveData.Levels[levelItem.name].Unlocked = true;
                    _seq.AppendInterval(1f)
                        .Append(UnlockLevelAnimation(levelItem))
                        .AppendCallback(() =>
                            ConfigureLevelItem(levelItem,
                                saveData.Levels[levelItem.name].Unlocked,
                                saveData.Levels[levelItem.name].BestStars,
                                levelItem.name))
                        // 星星入场动画有点丑, 暂时不加上
                        // for (int i = 1; i <= saveData.Levels[levelItem.name].BestStars; i++) {
                        //     DOTweenAnimation starShowDOTweenComponent =
                        //         MySugarUtil.TryToFindComponent<DOTweenAnimation>(levelItem, $"Star_{i}");
                        //     _seq.Append(starShowDOTweenComponent.GetTweens()[0]);
                        // }
                        .AppendInterval(0.5f);
                }
            });
            
            // TODO: 暂时不要回写 Unlocked = true 用于测试解锁动画
            // saveData.Save();
        }

        private void ConfigureLevelItem(GameObject levelItem, bool isUnlocked, int bestStars, string levelSceneName, int requiredStars = 0) {
            // Get references
            var bgImage = levelItem.transform.Find("Bg").GetComponent<Image>();
            GameObject lockIcon = MySugarUtil.TryToFindObject(levelItem, "LockIcon");
            GameObject starsObj = MySugarUtil.TryToFindObject(levelItem, "Stars");
            GameObject requiredStarsObj = MySugarUtil.TryToFindObject(levelItem, "RequiredStars");

            // Button moved to root of LevelItem prefab
            var button = levelItem.GetComponent<Button>();
            var buttonGraphic = levelItem.GetComponent<Image>(); // transparent raycast surface

            if (isUnlocked) {
                // Unlocked state
                bgImage.sprite = bgUnlockSprite;
                lockIcon.SetActive(false);
                starsObj.SetActive(true);
                requiredStarsObj.SetActive(false);

                if (buttonGraphic != null)
                    buttonGraphic.raycastTarget = true;

                if (button != null) {
                    button.enabled = true;
                    button.interactable = true;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => PlayClickFeedbackAndEnterLevel(levelItem.transform, button, levelSceneName));
                }

                // Configure stars (0-3)
                ConfigureStars(starsObj, bestStars);
            } else {
                // Locked state
                bgImage.sprite = bgLockSprite;
                lockIcon.SetActive(true);
                starsObj.SetActive(false);
                requiredStarsObj.SetActive(true);

                if (button != null) {
                    button.onClick.RemoveAllListeners();
                    button.interactable = false;
                    button.enabled = false;
                }

                if (buttonGraphic != null)
                    buttonGraphic.raycastTarget = false;
                
                // Set required stars text
                TMP_Text requiredStarsText = requiredStarsObj.GetComponentInChildren<TMP_Text>();
                requiredStarsText.SetText(requiredStars.ToString());
            }
        }

        private void ConfigureStars(GameObject starsObj, int bestStars) {
            for (int i = 1; i <= 3; i++) {
                var starImage = MySugarUtil.TryToFindComponent<Image>(starsObj, $"Star_{i}");
                if (starImage != null) {
                    starImage.sprite = (i <= bestStars) ? starFilledSprite : starEmptySprite;
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

        private Sequence UnlockLevelAnimation(GameObject levelItem) {
            string tweenId = $"UnlockTween_{levelItem.name}";
            DOTween.Kill(tweenId);

            DOTweenAnimation levelItemDOTweenComponent =
                MySugarUtil.TryToFindComponent<DOTweenAnimation>(levelItem, "LockIcon");
            Image LockIconImage =
                MySugarUtil.TryToFindComponent<Image>(levelItem, "LockIcon");

            return DOTween.Sequence()
                .SetId(tweenId)
                .Append(levelItemDOTweenComponent.GetTweens()[0])
                .AppendCallback(() => LockIconImage.sprite = unlockSprite);
        }
        
        public void OnLevelButtonClicked(string level) {
            GameManager.Instance.LoadScene(level);
        }
        
        private void OnDestroy() {
            // 清理解锁关卡的序列动画
            if (_seq != null && _seq.IsActive()) {
                _seq.Kill();
                _seq = null;
            }
            
            // 清理所有关卡项的点击反馈动画
            foreach (var levelItem in _levelItems) {
                if (levelItem != null) {
                    string tweenId = $"LevelItemClickScale_{levelItem.transform.GetInstanceID()}";
                    DOTween.Kill(tweenId);
                    
                    string unlockTweenId = $"UnlockTween_{levelItem.name}";
                    DOTween.Kill(unlockTweenId);
                }
            }
        }
        
    }
}
