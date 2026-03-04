using System;
using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Shared;
using HadoopCore.Scripts.Utils;
using UnityEngine;

namespace HadoopCore.Scripts {
    public class Spike : MonoBehaviour {
        private void OnCollisionEnter2D(Collision2D collision) {
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