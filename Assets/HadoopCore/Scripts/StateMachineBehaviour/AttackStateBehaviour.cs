using DG.Tweening;
using HadoopCore.Scripts.InterfaceAbility;
using UnityEngine;

namespace HadoopCore.Scripts.StateMachineBehaviour {

    /// <summary>
    /// 攻击状态的状态机行为，用于在 Animator 中挂载到 Attack State 上
    /// </summary>
    public class AttackStateBehaviour : UnityEngine.StateMachineBehaviour {
        private static readonly int Status = Animator.StringToHash("Status");
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Debug.Log($"[Enemy] Enter Attack - {animator.gameObject.name}");
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Debug.Log($"[Enemy] Exit Attack - {animator.gameObject.name}");
            EnemyAI attackerAIScript = animator.gameObject.GetComponentInParent<EnemyAI>();
            GameObject attacker = attackerAIScript.gameObject;
            
            attackerAIScript.curState = EnemyState.Static; // Attack -> Static
            attackerAIScript.chaseTarget.TryGetComponent<IDeadAbility>(out var deadAbility);
            // 1) 播放攻击VFX
            // 2) 调用受害者的DeadAbility.Attack方法
            attackerAIScript.PlayHitScratch()
                .OnComplete(() => {
                    animator.SetInteger(Status, (int)EnemyState.Idle);
                    attackerAIScript.curState = EnemyState.Idle; // Static -> Idle
                    deadAbility.Dead(attacker);
                });
            
        }
    }
}