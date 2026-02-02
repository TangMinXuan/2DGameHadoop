using HadoopCore.Scripts.InterfaceAbility;
using UnityEngine;

namespace HadoopCore.Scripts.StateMachineBehaviour {
    public class EnemyPostDeadStateBehaviour : UnityEngine.StateMachineBehaviour{
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            IExposeAbility victimAbility = animator.gameObject.GetComponentInParent<IExposeAbility>();
            Destroy(victimAbility.GetGameObject());
        }
    }
}