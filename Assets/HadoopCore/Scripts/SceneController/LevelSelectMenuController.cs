using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using HadoopCore.Scripts.Annotation;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HadoopCore.Scripts.SceneController {
    public class LevelSelectMenuController : MonoBehaviour {
        
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
        [SerializeField] private List<GameObject> _levelFixedItems = new();
        private Sequence _seq;

        private void Awake() {
            MySugarUtil.AutoFindObjects(this, gameObject);
        }

        private void Start() {
            GameSaveData saveData = GameManager.Instance.GetSaveData();
            initLevelFixedItems(saveData);
            RefreshLevelContent(saveData);
        }

        private void initLevelFixedItems(GameSaveData saveData) {
            Dictionary<string, LevelProgress> levelDic = saveData.LevelDic;
            int totalCount = levelDic.Count;
            
            // 清理旧的items（如果有）
            _levelFixedItems.Clear();
            foreach (Transform child in levelGridContainer.transform) {
                Destroy(child.gameObject);
            }
            
            // 按 Level_{i} 顺序遍历，i 从 1 到 totalCount
            for (int i = 1; i <= totalCount; i++) {
                string key = $"Level_{i}";
                if (!levelDic.ContainsKey(key)) continue;
                
                GameObject item = Instantiate(levelItemPrefab, levelGridContainer.transform);
                item.name = key;
                _levelFixedItems.Add(item);
            }
        }

        private void RefreshLevelContent(GameSaveData saveData) {
            Dictionary<string, LevelProgress> levelDic = saveData.LevelDic;
            int layerCnt = (levelDic.Count + 4) / 5; // 向上取整
            
            // 1) 右上角的星星数量
            starsValue.GetComponent<TMP_Text>().text = CalTotalStars(saveData).ToString();
            
            // 2) 静态的LevelItem
            for (int layer = 1; layer <= layerCnt; layer++) {
                int caseCondition = CalOneLayer(layer, saveData);
            }
            
            // 3) 动态的LevelItem (针对新解锁case)
            // if (caseCondition == 2) {
            //     int startLevelId = (layer - 1) * 5 + 1;
            //     int endLevelId = layer * 5;
            //     _seq = DOTween.Sequence()
            //         .SetUpdate(true)
            //         .SetLink(gameObject)
            //         .SetAutoKill(false).Pause()
            //         .SetId("UnlockLevelsSequence");
            //     
            //     for (int i = startLevelId; i <= endLevelId; i++) {
            //         levelDic[$"Level_{i}"].Unlocked = true;
            //         saveData.LevelDic = levelDic;
            //         _seq.AppendInterval(1f)
            //             .Append(PadlockAnim(_levelFixedItems[i]))
            //             .AppendCallback(() => ConfigUnlockedLevelItems(i, levelDic[$"Level_{i}"].BestStars))
            //             .AppendInterval(0.5f);
            //     }
            // }
            
            // 统一保存一下 (totalStart 和 Unlocked状态)
            GameManager.Instance.SaveGameDataAsync(saveData);
        }
        
        private int CalOneLayer(int layerId, GameSaveData saveData) {
            int startLevelId = (layerId - 1) * 5 + 1;
            int endLevelId = layerId * 5;
            Dictionary<string, LevelProgress> LevelDic = saveData.LevelDic;
            int caseCondition = 1;
            if (LevelDic[$"Level_{startLevelId}"].Unlocked) {
                caseCondition = 3;
            } else if (TryUnlockLayer(layerId, saveData)) {
                // 之前没解锁, 新解锁
                caseCondition = 2;
            }

            // case1: 完全没有解锁
            if (caseCondition == 1) {
                for (int i = startLevelId; i <= endLevelId; i++) {
                    ConfigLockedLevelItems(_levelFixedItems[i - 1], i.ToString(), LevelDic[$"Level_{i}"].RequiredStars);
                }
            }
            
            // case2: 之前没解锁, 新解锁
            if (caseCondition == 2) {
                for (int i = startLevelId; i <= endLevelId; i++) {
                    ConfigUnlockedLevelItems(_levelFixedItems[i - 1], i.ToString(), LevelDic[$"Level_{i}"].BestStars);
                    LevelDic[$"Level_{i}"].Unlocked = true;
                }
            }

            // case3: 已经解锁
            if (caseCondition == 3) {
                for (int i = startLevelId; i <= endLevelId; i++) {
                    ConfigUnlockedLevelItems(_levelFixedItems[i - 1], i.ToString(), LevelDic[$"Level_{i}"].BestStars);
                }
            }
            
            return caseCondition;
        }
        
        private void ConfigUnlockedLevelItems(GameObject assemblingLevelItem, string levelNumStr, int bestStars) {
            // 1) Bg
            MySugarUtil.TryToFindComponent<Image>(assemblingLevelItem, "Bg").sprite = bgUnlockSprite;
                    
            // 2) LockIcon
            MySugarUtil.TryToFindObject(assemblingLevelItem, "LockIcon").SetActive(false);
                    
            // 3) LevelNumberText
            TMP_Text levelNumberText = MySugarUtil.TryToFindObject(assemblingLevelItem, "LevelNumber")
                .GetComponentInChildren<TMP_Text>();
            levelNumberText.text = levelNumStr;

            // 4) Stars or RequiredStars
            MySugarUtil.TryToFindObject(assemblingLevelItem, "RequiredStars").SetActive(false);
            var starsObj = MySugarUtil.TryToFindObject(assemblingLevelItem, "Stars");
            starsObj.SetActive(true);
            ConfigureStars(starsObj, bestStars);
            
            // 5) Button
            var button = assemblingLevelItem.GetComponent<Button>();
            var buttonGraphic = assemblingLevelItem.GetComponent<Image>(); // transparent raycast surface
            if (buttonGraphic != null)
                buttonGraphic.raycastTarget = true;
            if (button != null) {
                button.enabled = true;
                button.interactable = true;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => PlayClickFeedbackAndEnterLevel(
                    assemblingLevelItem.transform, button, $"Level_{levelNumStr}"));
                button.onClick.AddListener(() => AudioManager.Instance.PlayBtnSfx());
            }
        }
        
        private void ConfigLockedLevelItems(GameObject assemblingLevelItem, string levelNumStr, int requiredStars) {
            // 1) Bg
            MySugarUtil.TryToFindComponent<Image>(assemblingLevelItem, "Bg").sprite = bgLockSprite;
                    
            // 2) LockIcon
            MySugarUtil.TryToFindObject(assemblingLevelItem, "LockIcon").SetActive(true);
                    
            // 3) LevelNumberText
            TMP_Text levelNumberText = MySugarUtil.TryToFindObject(assemblingLevelItem, "LevelNumber")
                .GetComponentInChildren<TMP_Text>();
            levelNumberText.text = levelNumStr;

            // 4) Stars or RequiredStars
            MySugarUtil.TryToFindObject(assemblingLevelItem, "Stars").SetActive(false);
            var requiredStarsObj = MySugarUtil.TryToFindObject(assemblingLevelItem, "RequiredStars");
            requiredStarsObj.SetActive(true);
            TMP_Text requiredStarsText = requiredStarsObj.GetComponentInChildren<TMP_Text>();
            requiredStarsText.text = requiredStars.ToString();
        }
        
        private void ConfigureStars(GameObject starsObj, int bestStars) {
            for (int i = 1; i <= 3; i++) {
                var starImage = MySugarUtil.TryToFindComponent<Image>(starsObj, $"Star_{i}");
                if (starImage != null) {
                    starImage.sprite = (i <= bestStars) ? starFilledSprite : starEmptySprite;
                }
            }
        }

        private int CalTotalStars(GameSaveData saveData) {
            // 1) 将已通关关卡的BestStar累加起来
            int newTotal = saveData.LevelDic
                .Where(entry => entry.Value.IsPass)
                .Sum(entry => entry.Value.BestStars);
            
            // 2) 如果大于TotalStarts, 则调用Game Manager回写存档
            if (newTotal > saveData.TotalStarts) {
                saveData.TotalStarts = newTotal;
            }

            // 3) 返回累加好的totalStar
            return newTotal;
        }
        
        /**
         * 同时满足以下两个条件才解锁：
         *  1) 星星数足够
         *  2) 上一层的关卡都已完成(isPass = true)
         */
        private bool TryUnlockLayer(int wantUnlockLayerId, GameSaveData saveData) {
            int startLevelId = (wantUnlockLayerId - 1) * 5 + 1;
            int endLevelId = wantUnlockLayerId * 5;
            if (saveData.TotalStarts >= saveData.LevelDic[$"Level_{startLevelId}"].RequiredStars) {
                for (int i = startLevelId - 5; i <= endLevelId - 5; i++) { // 检查上一层的关卡是否都已完成(因此是 -5)
                    if (!saveData.LevelDic[$"Level_{i}"].IsPass) {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        
        private void PlayClickFeedbackAndEnterLevel(Transform levelItemTransform, Button button, string level) {
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

        private Sequence PadlockAnim(GameObject levelItem) {
            string tweenId = $"PadlockUnlockTween_{levelItem.name}";
            DOTween.Kill(tweenId);

            DOTweenAnimation levelItemDOTweenComponent =
                MySugarUtil.TryToFindComponent<DOTweenAnimation>(levelItem, "LockIcon");
            Image LockIconImage =
                MySugarUtil.TryToFindComponent<Image>(levelItem, "LockIcon");

            return DOTween.Sequence()
                .SetId(tweenId)
                .Append(levelItemDOTweenComponent.GetTweens()[0])
                .AppendInterval(0.1f)
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
            foreach (var levelItem in _levelFixedItems) {
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
