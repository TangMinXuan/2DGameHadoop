using HadoopCore.Scripts.Utils;
using UnityEngine;

namespace HadoopCore.Scripts.UI
{
    public class UIBase
    {
        [ContextMenu("HideUI")]
        private void HideUI(CanvasGroup canvasGroup)
        {
            UIUtil.SetUIVisible(canvasGroup, true);
        }
    }
}