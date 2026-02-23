using UnityEngine;

namespace HadoopCore.Scripts.Water {
    /// <summary>
    /// 挂到 WaterBall / FireBall 上，检测水球与火球的碰撞。
    /// 碰撞后将双方的 Layer 切换到 DustLayer，
    /// 由 DustCam → DustRT → MAT_Metaball_DustBall 渲染管线自动呈现灰烬色。
    /// </summary>
    public class BallCollisionHandler : MonoBehaviour {
        [Header("Layer 配置")] [Tooltip("水球所在 Layer 索引")]
        public int waterLayer = 9;

        [Tooltip("火球所在 Layer 索引")] public int fireLayer = 11;
        [Tooltip("尘球（灰烬）所在 Layer 索引")] public int dustLayer = 12;
        
        [SerializeField] private GameObject dustConvertVFX; 

        internal bool _converted;

        private void OnCollisionEnter2D(Collision2D collision) {
            if (_converted) return;

            int myLayer = gameObject.layer;
            int otherLayer = collision.gameObject.layer;

            // 水球碰火球 或 火球碰水球
            bool isWaterHitsFire = myLayer == waterLayer && otherLayer == fireLayer;
            bool isFireHitsWater = myLayer == fireLayer && otherLayer == waterLayer;

            if (!isWaterHitsFire && !isFireHitsWater) return;

            // 将自身转为尘球
            ConvertToDust(gameObject);

            // 将对方也转为尘球（兜底：对方可能没有挂 BallCollisionHandler）
            var otherHandler = collision.gameObject.GetComponent<BallCollisionHandler>();
            if (otherHandler != null && !otherHandler._converted) {
                otherHandler.ConvertToDust(collision.gameObject);
            }
        }

        private void ConvertToDust(GameObject ball) {
            _converted = true;
            ball.layer = dustLayer;

            if (dustConvertVFX != null) {
                dustConvertVFX.SetActive(true);
                dustConvertVFX.GetComponentInChildren<ParticleSystem>()?.Play();
            }

            var component = ball.GetComponent<FireBall>();
            if (component != null) component.enableBurn = false;
        }
    }
}