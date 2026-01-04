using System.Collections.Generic;
using UnityEngine;

namespace HadoopCore.Scripts.UI
{
    
     // TODO: 
     // 还是会有上下颠倒的问题, 需要后面花时间重点看一下代码
     
    [ExecuteAlways]
    public class AnchorDistributor : MonoBehaviour
    {
        [Header("Container (default: this)")] public RectTransform container;

        [Header("Items to distribute (default: direct children)")]
        public List<RectTransform> items;

        public enum Order
        {
            TopToBottom,
            BottomToTop
        }

        [Header("Count & Fill")] [Min(1)] public int count = 2; // how many slots vertically
        [Range(0f, 1f)] public float fillX = 0.90f; // item width as % of container

        [Range(0f, 1f)]
        public float fillY = 0.60f; // item height as % of its slot (make this smaller -> thinner buttons)

        [Range(0f, 0.5f)] public float rowSpacing = 0f; // extra gap between rows (normalized to container height)
        public Order order = Order.TopToBottom; // match list order by default

        [Header("Outer paddings (normalized)")] [Range(0f, 0.5f)]
        public float paddingTop = 0.05f;

        [Range(0f, 0.5f)] public float paddingBottom = 0.05f;

        [Header("Editor")] public bool autoUpdate = true;

        void OnValidate()
        {
            if (autoUpdate) Apply();
        }

        void Reset()
        {
            Apply();
        }

        [ContextMenu("Apply Now")]
        public void Apply()
        {
            if (container == null) container = transform as RectTransform;

            // auto collect direct children if empty
            if (items == null || items.Count == 0)
            {
                items = new List<RectTransform>();
                foreach (Transform c in container)
                {
                    if (c is RectTransform rt) items.Add(rt);
                }
            }

            if (count <= 0) count = Mathf.Max(1, items.Count);

            // horizontal anchors: centered with fillX
            float minX = (1f - Mathf.Clamp01(fillX)) * 0.5f;
            float maxX = 1f - minX;

            // vertical available space after outer paddings and row spacings
            float startY = Mathf.Clamp01(paddingBottom);
            float endY = Mathf.Clamp01(1f - paddingTop);

            float totalGap = Mathf.Clamp01(rowSpacing) * Mathf.Max(0, (items.Count - 1));
            float availH = Mathf.Max(0f, (endY - startY) - totalGap);
            float cellH = availH / Mathf.Max(1, count);

            // how tall an item is inside its slot
            float clampedFillY = Mathf.Clamp01(fillY);
            float innerHalf = clampedFillY * cellH * 0.5f;

            for (int i = 0; i < items.Count; i++)
            {
                var rt = items[i];
                if (rt == null) continue;

                // choose which index goes to which row depending on order
                int rowIndex = (order == Order.TopToBottom) ? i : (items.Count - 1 - i);

                // row base min (bottom of this slot), factoring rowSpacing between rows
                float gapBefore = rowSpacing * rowIndex;
                float slotMin = startY + gapBefore + cellH * rowIndex;
                float slotMax = slotMin + cellH;

                // center of this slot (0..1 from bottom to top)
                float center = (slotMin + slotMax) * 0.5f;
                float minY = Mathf.Clamp01(center - innerHalf);
                float maxY = Mathf.Clamp01(center + innerHalf);

                rt.anchorMin = new Vector2(minX, minY);
                rt.anchorMax = new Vector2(maxX, maxY);
                rt.pivot = new Vector2(0.5f, 0.5f);

                // fully driven by anchors
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition3D = new Vector3(0, 0, rt.anchoredPosition3D.z);
            }
        }
    }
}