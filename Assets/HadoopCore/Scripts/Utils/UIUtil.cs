using UnityEngine;

namespace HadoopCore.Scripts.Utils
{
    public static class UIUtil
    {
        /// <summary>
        /// 通过CanvasGroup控制UI的显示与隐藏
        /// </summary>
        /// <param name="uiGameObject">UI的GameObject</param>
        /// <param name="isVisible">true显示，false隐藏</param>
        public static void SetUIVisible(GameObject uiGameObject, bool isVisible)
        {
            if (uiGameObject == null) return;
            
            var canvasGroup = uiGameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Debug.Log($"Canvas group is null for {uiGameObject.name}");
            }
            
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }
        
        public static void SetUIVisible(CanvasGroup canvasGroup, bool isVisible)
        {
            if (canvasGroup == null) return;
            
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }
    }
}