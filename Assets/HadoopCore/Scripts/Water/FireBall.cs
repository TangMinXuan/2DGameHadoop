using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Shared;
using HadoopCore.Scripts.Utils;
using UnityEngine;

namespace HadoopCore.Scripts.Water {
    public class FireBall : MonoBehaviour {

        public bool enableBurn = true;

        private void OnCollisionEnter2D(Collision2D collision) {
            if (!enableBurn) return;

            if (MySugarUtil.TryToFindComponent<IExposeAbility>(collision.gameObject, out var victimAbility,
                    ComponentSearchLocation.Parent, ComponentSearchLocation.Self)) {
                if (!victimAbility.IsAlive()) {
                    return;
                }
                victimAbility.SetStateWithLock(CharacterState.Dead, true);
            }
        }
    }
}

