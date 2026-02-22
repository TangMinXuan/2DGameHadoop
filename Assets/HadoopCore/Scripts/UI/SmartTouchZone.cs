using UnityEngine;

namespace HadoopCore.Scripts.UI {
    public class SmartTouchZone : MonoBehaviour, ICanvasRaycastFilter {
        [Header("设置")] [Tooltip("只在点击这些层级时触发移动 (例如 Ground/Floor)")]
        public LayerMask walkableLayer; // 这里只勾选你的 地板/地形 Layer

        private Camera _mainCamera;

        void Start() {
            _mainCamera = Camera.main;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) {
            // 1. 从点击位置发射射线 (用 3D Ray 检测 2D 碰撞体)
            Ray ray = _mainCamera.ScreenPointToRay(screenPoint);
            // 2. 使用 Physics2D.GetRayIntersection 检测 2D 碰撞体
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
            if (hit.collider != null) {
                // 3. 检查打到的物体，是否属于 "WalkableLayer" (地板)
                if (((1 << hit.collider.gameObject.layer) & walkableLayer) != 0) {
                    // 是地板 -> 返回 true (UI 拦截点击 -> 角色移动)
                    return true;
                }

                // 打到了其他东西 (比如 Plug) -> 返回 false (UI 忽略点击 -> 触发 Plug 交互)
                return false;
            }

            // 4. 如果射线什么都没打到 (比如点到了天空盒)
            // 通常返回 true 让角色移动，或者 false 不动，看你需求。这里默认移动。
            return true;
        }
    }
}
