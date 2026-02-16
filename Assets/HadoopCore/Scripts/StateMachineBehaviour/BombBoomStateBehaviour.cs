using UnityEngine;

namespace HadoopCore.Scripts.StateMachineBehaviour {
    public class BombBoomStateBehaviour : UnityEngine.StateMachineBehaviour {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            // 锁定在半空中：冻结所有位置和旋转
            Rigidbody2D rb = animator.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null) {
                rb.velocity = Vector2.zero;            // 清除线性速度
                rb.angularVelocity = 0;                // 清除角速度
                rb.constraints = RigidbodyConstraints2D.FreezeAll;  // 冻结位置和旋转
            }
            animator.gameObject.GetComponent<Bomb>().ApplyBlastForce();
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            animator.gameObject.SetActive(false);
        }
    }
}