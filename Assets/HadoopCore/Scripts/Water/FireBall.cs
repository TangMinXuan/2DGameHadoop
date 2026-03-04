using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Shared;
using HadoopCore.Scripts.Utils;
using UnityEngine;

namespace HadoopCore.Scripts.Water {
    public class FireBall : MonoBehaviour {
        [SerializeField] private GameObject dustConvertVFX;

        public bool enableBurn = true;

        private void OnCollisionEnter2D(Collision2D collision) {
            if (!enableBurn) return;

            if (MySugarUtil.TryToFindComponent<IExposeAbility>(collision.gameObject, out var victimAbility,
                    ComponentSearchLocation.Parent, ComponentSearchLocation.Self)) {
                if (!victimAbility.IsAlive()) {
                    return;
                }
                victimAbility.SetStateWithLock(CharacterState.Dead, true);
                if (dustConvertVFX != null) {
                    dustConvertVFX.SetActive(true);
                    dustConvertVFX.GetComponentInChildren<ParticleSystem>()?.Play();
                }
            }
            
            if (collision.rigidbody.tag == "Chest") {
                collision.rigidbody.gameObject.SetActive(false);
                if (dustConvertVFX != null) {
                    dustConvertVFX.SetActive(true);
                    dustConvertVFX.GetComponentInChildren<ParticleSystem>()?.Play();
                }
                LevelEventCenter.TriggerGameOver();
            }
            
            if (collision.rigidbody.tag == "Spike") {
                collision.rigidbody.gameObject.SetActive(false);
                if (dustConvertVFX != null) {
                    dustConvertVFX.SetActive(true);
                    dustConvertVFX.GetComponentInChildren<ParticleSystem>()?.Play();
                }
            }
        }
    }
}

