using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace HadoopCore.Scripts.UI
{
    public class GridPageSnap : MonoBehaviour, IEndDragHandler, IBeginDragHandler
    {
        [Header("设置")]
        public ScrollRect scrollRect;
        public GridLayoutGroup gridLayout;
        public int itemsPerPage = 5; // 每页显示几个
        public float snapSpeed = 10f; // 吸附速度

        [Header("动态布局")]
        [Tooltip("cell 之间的间距占 cell 宽度的比例，例如 0.5 表示间距 = cellSize * 0.5")]
        public float spacingRatio = 1.0f; // 默认间距等于cellSize（与原始300:300比例一致）

        [Tooltip("左右 padding 占 spacing 的比例，例如 0.5 表示 padding = spacing * 0.5")]
        public float paddingRatio = 0.5f; // 左右留半个 spacing 的边距

        private float pageWidth;
        private float targetX;
        private bool isSnapping = false;
        private int maxPage; // 最大页索引

        void Start()
        {
            AdjustLayout();
        }

        /// <summary>
        /// 根据 viewport 实际宽度，动态计算 cellSize、spacing 和 padding，
        /// 确保每页恰好显示 itemsPerPage 个完整的 item。
        /// </summary>
        private void AdjustLayout()
        {
            // 强制在当前帧重建布局，确保 RectTransform 尺寸已经正确
            Canvas.ForceUpdateCanvases();

            // 1. 获取 viewport 的实际宽度（在 Canvas 坐标系下）
            RectTransform viewportRect = scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();
            float viewportWidth = viewportRect.rect.width;

            // 2. 重置 content（LevelGridContainer）的 RectTransform，使其不再有固定偏移
            //    让 GridLayoutGroup 的 padding 来控制边距
            RectTransform contentRect = scrollRect.content;
            contentRect.anchoredPosition = new Vector2(0, contentRect.anchoredPosition.y);
            contentRect.sizeDelta = new Vector2(0, contentRect.sizeDelta.y);

            // 重新获取 viewport 宽度（content 改变后可能影响）
            Canvas.ForceUpdateCanvases();
            viewportWidth = viewportRect.rect.width;

            // 3. 根据 viewport 宽度和每页数量，动态计算 cellSize 和 spacing
            //
            // 布局模型（一页内）:
            //   [padding_left] [cell] [spacing] [cell] [spacing] [cell] [spacing] [cell] [spacing] [cell] [padding_right]
            //
            // 设 cellWidth = c, spacing = c * spacingRatio, padding = c * spacingRatio * paddingRatio
            //
            // viewportWidth = 2 * padding + itemsPerPage * c + (itemsPerPage - 1) * spacing
            //               = 2 * c * spacingRatio * paddingRatio + itemsPerPage * c + (itemsPerPage - 1) * c * spacingRatio
            //               = c * (2 * spacingRatio * paddingRatio + itemsPerPage + (itemsPerPage - 1) * spacingRatio)
            //
            // c = viewportWidth / (2 * spacingRatio * paddingRatio + itemsPerPage + (itemsPerPage - 1) * spacingRatio)

            float denominator = 2f * spacingRatio * paddingRatio
                                + itemsPerPage
                                + (itemsPerPage - 1) * spacingRatio;
            float cellWidth = viewportWidth / denominator;
            float spacing = cellWidth * spacingRatio;
            int padding = Mathf.RoundToInt(spacing * paddingRatio);

            // 4. 应用到 GridLayoutGroup
            gridLayout.cellSize = new Vector2(cellWidth, cellWidth);
            gridLayout.spacing = new Vector2(spacing, 0);
            gridLayout.padding = new RectOffset(padding, padding, 0, 0);

            // 5. 计算 pageWidth
            // 一页的内容宽度 = padding_left + itemsPerPage * cellWidth + (itemsPerPage - 1) * spacing + padding_right
            //                = viewportWidth（恰好等于 viewport 宽度，这是我们设计的目标）
            // 但是翻页时，content 需要移动的距离 = 一页内容宽度 + 页间间距
            // 页间间距 = 从第一页最后一个 item 的右边缘到第二页第一个 item 的左边缘
            //          = padding_right + spacing + padding_left（右padding + 两页item之间的spacing + 左padding）
            // 但在 GridLayoutGroup 中，padding 只在整个 content 的首尾生效，item 之间只有 spacing
            // 所以实际上：第5个item到第6个item之间的距离 = spacing（和其他item之间一样）
            // 
            // 因此 pageWidth = 从第1个item中心到第6个item中心的距离
            //                = itemsPerPage * (cellWidth + spacing)
            pageWidth = itemsPerPage * (cellWidth + spacing);

            // 6. 根据子物体总数计算最大页索引
            int totalItems = scrollRect.content.childCount;
            maxPage = Mathf.CeilToInt((float)totalItems / itemsPerPage) - 1;
            if (maxPage < 0) maxPage = 0;

            // 7. 初始目标设为当前位置
            targetX = scrollRect.content.anchoredPosition.x;

            Debug.Log($"[GridPageSnap] viewportWidth={viewportWidth:F1}, cellWidth={cellWidth:F1}, " +
                      $"spacing={spacing:F1}, padding={padding}, pageWidth={pageWidth:F1}, maxPage={maxPage}");
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 开始拖拽时，打断吸附，允许用户自由滑动
            isSnapping = false;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            scrollRect.velocity = Vector2.zero;
            float currentX = scrollRect.content.anchoredPosition.x;

            // 先把当前X钳制到合法范围，防止拖出边界导致页码计算错误
            currentX = Mathf.Clamp(currentX, -maxPage * pageWidth, 0f);

            // 算出不考虑甩动的最近页
            int currentPage = Mathf.RoundToInt(Mathf.Abs(currentX) / pageWidth);

            // 检测甩动方向 (eventData.delta.x)
            if (eventData.delta.x < -10)
            {
                currentPage++;
            }
            else if (eventData.delta.x > 10 && currentPage > 0)
            {
                currentPage--;
            }

            // 限制页码在 [0, maxPage] 范围内，防止越界
            currentPage = Mathf.Clamp(currentPage, 0, maxPage);

            targetX = -currentPage * pageWidth;
            isSnapping = true;
        }

        void Update()
        {
            if (isSnapping)
            {
                // 平滑移动到目标位置
                float newX = Mathf.Lerp(scrollRect.content.anchoredPosition.x, targetX,
                    Time.deltaTime * snapSpeed);
                Vector2 newPos = scrollRect.content.anchoredPosition;
                newPos.x = newX;
                scrollRect.content.anchoredPosition = newPos;

                // 如果非常接近了，就直接对齐并停止计算，节省性能
                if (Mathf.Abs(newX - targetX) < 1f)
                {
                    newPos.x = targetX;
                    scrollRect.content.anchoredPosition = newPos;
                    isSnapping = false;
                }
            }
        }
    }
}
