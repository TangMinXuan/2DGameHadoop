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
            victimAbility.SetLogicState(CharacterState.UnderAttack);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Debug.Log($"[AttackStateBehaviour] Exit Attack - {animator.gameObject.name}");

            EnemyAI enemyAI = animator.gameObject.GetComponentInParent<EnemyAI>();
            IExposeAbility attackerAbility = animator.gameObject.GetComponentInParent<IExposeAbility>();
            IExposeAbility victimAbility = attackerAbility.GetChaseTargetExposeAbility();
            enemyAI.PlayHitScratch().OnComplete(() => {
                attackerAbility.SetLogicState(CharacterState.Idle);
                victimAbility.Dead(attackerAbility.GetGameObject());
            });
        }
    }
}