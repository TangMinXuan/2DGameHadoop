using System;
using UnityEngine;

namespace HadoopCore.Scripts {
    public class HammerController : MonoBehaviour {
        
        private Animator _animator;
        void Awake() {
            _animator = GetComponent<Animator>();
        }
        
        // 外部调用的方法
        public void Strike(Vector3 strikePosition) {
            transform.position = strikePosition;
            // 1. 确保物体是激活的 (防止第一次是关闭状态)
            if (!gameObject.activeSelf) {
                gameObject.SetActive(true);
            }
            
            // 2. 直接触发 Trigger
            // 由于配置了 AnyState 和 CanTransitionToSelf，
            // 无论当前是在 Idle 还是在 Striking 播放中途，都会瞬间重头播放攻击
            _animator.SetTrigger("Hit");
        }
    }
}