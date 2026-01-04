using UnityEngine;
using UnityEngine.EventSystems;

namespace HadoopCore.Scripts.Utils
{
    public static class MouseClickPositionUtil
    {
        public static Vector3 get(PointerEventData eventData)
        {
            Vector3 worldPos = eventData.pointerPressRaycast.worldPosition;

            // 某些情况下 worldPos 可能是 (0,0,0)，就用 screenPos 兜底
            if (worldPos == Vector3.zero)
            {
                Camera cam = eventData.pressEventCamera ?? Camera.main;
                Vector2 screenPos = eventData.position;

                float z = -cam.transform.position.z; // 2D 常见：相机在 z=-10 => z=10
                worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
                worldPos.z = 0;
            }
            
            return worldPos;
        }
    }
}