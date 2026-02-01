using HadoopCore.Scripts.Shared;
using UnityEngine;

namespace HadoopCore.Scripts.InterfaceAbility {
    public interface IExposeAbility {

        int GetRank() {
            return 0;
        }
        
        void Dead(GameObject killer);
        
        bool IsAlive();
        
        CharacterState GetState();

        void SetLogicState(CharacterState state);

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