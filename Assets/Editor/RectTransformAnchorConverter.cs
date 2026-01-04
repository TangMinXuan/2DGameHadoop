// Assets/Editor/RectTransformAnchorConverter.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class RectTransformAnchorConverter
    {
        // 1) 正向：把当前摆好的Rect转换为 Stretch Anchors，并清零 offsets
        [MenuItem("Tools/UI/转换为响应式Anchors模式 Convert To Anchors (Zero Offsets, No Clamp)")]
        private static void ConvertSelected_ToStretchAnchors()
        {
            ConvertSelected(ConvertToStretchAnchors_NoClamp);
        }

        // 2) 反向：把当前Rect转换为 点锚(0.5,0.5) + sizeDelta/anchoredPosition
        [MenuItem("Tools/UI/转换为默认的中心点模式 Convert Back To Center Anchors (0.5,0.5)")]
        private static void ConvertSelected_ToCenterAnchors()
        {
            ConvertSelected(rt => ConvertToCenterAnchors(rt, new Vector2(0.5f, 0.5f)));
        }
        
        [MenuItem("Tools/UI/相较于父节点水平居中 Center Horizontally In Parent (Keep Anchors)")]
        private static void CenterHorizontally_KeepAnchors()
        {
            foreach (var t in Selection.transforms)
            {
                var rt = t as RectTransform;
                if (!rt) continue;

                var parentRt = rt.parent as RectTransform;
                if (!parentRt)
                {
                    Debug.LogWarning($"Skip '{rt.name}': parent is not a RectTransform.");
                    continue;
                }

                Undo.RecordObject(rt, "Center Horizontally In Parent");

                // 取当前外观在父本地坐标系下的包围盒
                Vector3[] wc = new Vector3[4];
                rt.GetWorldCorners(wc);

                float minX = float.PositiveInfinity;
                float maxX = float.NegativeInfinity;

                for (int i = 0; i < 4; i++)
                {
                    float x = parentRt.InverseTransformPoint(wc[i]).x;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                }

                float childCenterX = (minX + maxX) * 0.5f;

                // 父节点水平中心在本地坐标
                Rect pr = parentRt.rect;
                float parentCenterX = (pr.xMin + pr.xMax) * 0.5f;

                float dx = parentCenterX - childCenterX;

                // 只修正 X。用 anchoredPosition 最稳（不改 anchors）
                var ap = rt.anchoredPosition;
                rt.anchoredPosition = new Vector2(ap.x + dx, ap.y);

                EditorUtility.SetDirty(rt);
            }
        }
        
        [MenuItem("Tools/UI/相较于父节点垂直居中 Center Vertically In Parent (Keep Anchors)")]
        private static void CenterVertically_KeepAnchors()
        {
            foreach (var t in Selection.transforms)
            {
                var rt = t as RectTransform;
                if (!rt) continue;

                var parentRt = rt.parent as RectTransform;
                if (!parentRt) continue;

                Undo.RecordObject(rt, "Center Vertically In Parent");

                // 当前外观在父本地坐标系下的包围盒（只取Y）
                Vector3[] wc = new Vector3[4];
                rt.GetWorldCorners(wc);

                float minY = float.PositiveInfinity;
                float maxY = float.NegativeInfinity;

                for (int i = 0; i < 4; i++)
                {
                    float y = parentRt.InverseTransformPoint(wc[i]).y;
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }

                float childCenterY = (minY + maxY) * 0.5f;

                // 父节点垂直中心（本地坐标）
                Rect pr = parentRt.rect;
                float parentCenterY = (pr.yMin + pr.yMax) * 0.5f;

                float dy = parentCenterY - childCenterY;

                // 只修正Y，不动 anchors
                Vector2 ap = rt.anchoredPosition;
                rt.anchoredPosition = new Vector2(ap.x, ap.y + dy);

                EditorUtility.SetDirty(rt);
            }
        }

        // ----------------- Core -----------------

        private static void ConvertSelected(System.Action<RectTransform> convertOne)
        {
            var transforms = Selection.transforms;
            if (transforms == null || transforms.Length == 0)
            {
                Debug.LogWarning("No selection. Please select one or more UI objects with RectTransform.");
                return;
            }

            int converted = 0;

            foreach (var t in transforms)
            {
                if (!t) continue;

                var rt = t as RectTransform;
                if (!rt) continue;

                var parentRt = rt.parent as RectTransform;
                if (!parentRt)
                {
                    Debug.LogWarning($"Skip '{rt.name}': parent is not a RectTransform.");
                    continue;
                }

                // Layout系统会覆盖（如果有）
                // 这里仍执行转换，但你需要确保该节点不受 LayoutGroup/ContentSizeFitter 驱动。
                convertOne(rt);
                converted++;
            }

            Debug.Log($"Converted RectTransforms: {converted}");
        }

        /// <summary>
        /// 正向：按当前视觉结果计算 anchors（允许超出[0,1]），并置 offsets=0
        /// </summary>
        private static void ConvertToStretchAnchors_NoClamp(RectTransform rt)
        {
            var parentRt = rt.parent as RectTransform;
            if (!parentRt) return;

            if (rt.localEulerAngles != Vector3.zero)
            {
                Debug.LogWarning($"'{rt.name}' has rotation. Conversion uses AABB and may change perceived size.");
            }

            Undo.RecordObject(rt, "Convert RectTransform to Stretch Anchors");

            GetRectInParentLocal(rt, parentRt, out float minX, out float maxX, out float minY, out float maxY);

            Rect pr = parentRt.rect;
            float pw = pr.width;
            float ph = pr.height;

            if (pw <= 0.0001f || ph <= 0.0001f)
            {
                Debug.LogWarning($"Skip '{rt.name}': parent rect size is zero.");
                return;
            }

            Vector2 aMin = new Vector2(
                (minX - pr.xMin) / pw,
                (minY - pr.yMin) / ph
            );

            Vector2 aMax = new Vector2(
                (maxX - pr.xMin) / pw,
                (maxY - pr.yMin) / ph
            );

            // 不Clamp：忠实保持外观（允许超出[0,1]）
            if (aMin.x < 0 || aMin.y < 0 || aMax.x > 1 || aMax.y > 1)
            {
                Debug.LogWarning(
                    $"'{rt.name}' extends outside parent '{parentRt.name}'. " +
                    $"Anchors will be outside [0,1] to preserve appearance. aMin={aMin}, aMax={aMax}"
                );
            }

            rt.anchorMin = aMin;
            rt.anchorMax = aMax;

            // 清零 offsets（等价于 Left/Right/Top/Bottom = 0）
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            EditorUtility.SetDirty(rt);
        }

        /// <summary>
        /// 反向：把当前视觉结果转换为点锚（anchorMin=anchorMax=targetAnchor），并用 sizeDelta/anchoredPosition 精确复现
        /// </summary>
        private static void ConvertToCenterAnchors(RectTransform rt, Vector2 targetAnchor)
        {
            var parentRt = rt.parent as RectTransform;
            if (!parentRt) return;

            if (rt.localEulerAngles != Vector3.zero)
            {
                Debug.LogWarning($"'{rt.name}' has rotation. Conversion uses AABB and may change perceived size.");
            }

            Undo.RecordObject(rt, "Convert RectTransform to Center Anchors");

            GetRectInParentLocal(rt, parentRt, out float minX, out float maxX, out float minY, out float maxY);

            float width = maxX - minX;
            float height = maxY - minY;

            // 目标锚点在父RectTransform本地坐标的位置
            Rect pr = parentRt.rect;
            Vector2 anchorRefLocal = new Vector2(
                Mathf.Lerp(pr.xMin, pr.xMax, targetAnchor.x),
                Mathf.Lerp(pr.yMin, pr.yMax, targetAnchor.y)
            );

            // 当前元素 pivot 在父本地坐标的位置：
            // pivotLocal = bottomLeft + (width * pivot.x, height * pivot.y)
            Vector2 pivotLocal = new Vector2(
                minX + width * rt.pivot.x,
                minY + height * rt.pivot.y
            );

            // anchoredPosition 是 pivot 相对锚点参考点的偏移
            Vector2 anchoredPos = pivotLocal - anchorRefLocal;

            // 写回：点锚 + sizeDelta + anchoredPosition
            rt.anchorMin = targetAnchor;
            rt.anchorMax = targetAnchor;

            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = anchoredPos;

            EditorUtility.SetDirty(rt);
        }

        /// <summary>
        /// 取 RectTransform 当前外观在“父节点本地坐标系”下的轴对齐包围盒（AABB）
        /// </summary>
        private static void GetRectInParentLocal(RectTransform rt, RectTransform parentRt,
            out float minX, out float maxX, out float minY, out float maxY)
        {
            Vector3[] wc = new Vector3[4];
            rt.GetWorldCorners(wc);

            minX = maxX = minY = maxY = 0f;

            for (int i = 0; i < 4; i++)
            {
                Vector3 lc = parentRt.InverseTransformPoint(wc[i]);
                if (i == 0)
                {
                    minX = maxX = lc.x;
                    minY = maxY = lc.y;
                }
                else
                {
                    minX = Mathf.Min(minX, lc.x);
                    maxX = Mathf.Max(maxX, lc.x);
                    minY = Mathf.Min(minY, lc.y);
                    maxY = Mathf.Max(maxY, lc.y);
                }
            }
        }
    }
}
#endif
