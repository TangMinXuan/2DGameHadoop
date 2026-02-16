using System.Collections;
using System.Collections.Generic;
using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Shared;
using UnityEngine;

namespace HadoopCore.Scripts {
    public class Bomb : MonoBehaviour {
        [SerializeField] private float impactThreshold = 5.0f;
        [SerializeField] private float blastRadius = 3f;      // 爆炸范围
        [SerializeField] private float blastForce = 500f;     // 冲击力度

        // 防止重复触发
        private bool hasTriggered = false;
        
        private Animator _animator;

        void Awake() {
            _animator = GetComponent<Animator>();
        }

        void OnCollisionEnter2D(Collision2D collision) {
            // 0. 如果已经触发过，直接返回
            if (hasTriggered) return;

            // 1. 获取碰撞时的相对速度力度
            // relativeVelocity会自动计算两个物体本身的运动差
            float impactForce = collision.relativeVelocity.magnitude;

            // 2. 判断力度是否足够大
            // (可选：你也可以判断碰撞对象的标签，比如 if (collision.gameObject.CompareTag("Ground")))
            if (impactForce > impactThreshold) {
                // 3. 标记为已触发，并启动引信
                hasTriggered = true;
                _animator.SetTrigger("TriggerBoom");
            } else {
                // 打印一下碰撞力度，方便调试
                Debug.Log($"碰撞力度 {impactForce} 不足以引爆炸弹");
            }
        }
        
        public void ApplyBlastForce() {
            // 获取范围内所有 Collider2D
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, blastRadius);
            
            // 记录已处理的 Rigidbody，避免同一物体多次受力
            HashSet<Rigidbody2D> processed = new HashSet<Rigidbody2D>();

            foreach (Collider2D hit in hits) {
                Rigidbody2D rb = hit.attachedRigidbody;
                
                // 跳过没有 Rigidbody 或已处理的物体
                if (rb == null || processed.Contains(rb)) continue;
                
                // 跳过自身
                if (rb.gameObject == gameObject) continue;
                
                // 跳过没有IExposeAbility接口的物体（假设只有玩家和敌人会受影响）
                if (!rb.TryGetComponent<IExposeAbility>(out var exposeAbility)) continue;

                processed.Add(rb);
                
                exposeAbility.SetStateWithLock(CharacterState.Dead, true);

                // 计算从炸弹指向目标的方向
                Vector2 direction = (rb.transform.position - transform.position).normalized;
                
                // 强制添加保底值
                // float minX = 0.3f;
                // float minY = 0.2f;
                // float signX = direction.x >= 0 ? 1f : -1f;
                // float x = signX * Mathf.Max(Mathf.Abs(direction.x), minX);
                // float y = Mathf.Max(direction.y, minY);
                // direction = new Vector2(x, y).normalized;
                // rb.AddForce(new Vector2(-1f, 1f) * blastForce, ForceMode2D.Impulse);
                
                rb.AddForce(direction * blastForce, ForceMode2D.Impulse);
            }
        }
    }
}