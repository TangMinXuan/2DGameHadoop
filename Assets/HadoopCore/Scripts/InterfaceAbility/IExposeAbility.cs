using HadoopCore.Scripts.Shared;
using UnityEngine;

namespace HadoopCore.Scripts.InterfaceAbility {
    public interface IExposeAbility {
        bool IsAlive() {
            if (GetState() == CharacterState.Dead) {
                return false;
            }
            if (GetState() == CharacterState.UnderAttack) {
                return false;
            }
            return true;
        }
        
        CharacterState GetState();

        bool SetState(CharacterState state);

        void SetStateWithLock(CharacterState state, bool locked, IExposeAbility caller = null);

        IExposeAbility GetChaseTargetExposeAbility() {
            return null;
        }
        
        Transform GetTransform() {
            return ((MonoBehaviour)this).transform;
        }

        GameObject GetGameObject() {
            return ((MonoBehaviour)this).gameObject;
        }
    }
}