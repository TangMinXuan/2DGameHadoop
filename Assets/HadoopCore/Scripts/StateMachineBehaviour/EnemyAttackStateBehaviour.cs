using DG.Tweening;
using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Shared;
using UnityEngine;

namespace HadoopCore.Scripts.StateMachineBehaviour {

    /// <summary>
    /// 攻击状态的状态机行为，用于在 Animator 中挂载到 Attack State 上
    /// </summary>
    public class EnemyAttackStateBehaviour : UnityEngine.StateMachineBehaviour {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Debug.Log($"[AttackStateBehaviour] Enter Attack - {animator.gameObject.name}");
            IExposeAbility attackerAbility = animator.gameObject.GetComponentInParent<IExposeAbility>();
            IExposeAbility victimAbility = attackerAbility.GetChaseTargetExposeAbility();
            victimAbility.SetStateWithLock(CharacterState.UnderAttack, true, attackerAbility);
            attackerAbility.SetStateWithLock(CharacterState.Idle, true); // 进入Idle状态并加锁，防止被打断
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Debug.Log($"[AttackStateBehaviour] Exit Attack - {animator.gameObject.name}");

            EnemyAI enemyAI = animator.gameObject.GetComponentInParent<EnemyAI>();
            IExposeAbility attackerAbility = animator.gameObject.GetComponentInParent<IExposeAbility>();
            IExposeAbility victimAbility = attackerAbility.GetChaseTargetExposeAbility();
            enemyAI.PlayHitScratch().OnComplete(() => {
                attackerAbility.SetStateWithLock(CharacterState.Idle, false); // 解除攻击者的状态锁定，允许其进行其他操作
                victimAbility.SetStateWithLock(CharacterState.Dead, true, attackerAbility); // UnderAttack -> Dead 保持加锁状态
                victimAbility.Dead(attackerAbility.GetGameObject());
            });
        }
    }
}