using System;
using HadoopCore.Scripts.Attribute;
using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.UI;
using UnityEngine;

namespace HadoopCore.Scripts {
    public class Chest : MonoBehaviour, IPlayerPickup {
        [SerializeField] private GameObject chestFireworksVFXPrefab;
        private static readonly int PickStatus = Animator.StringToHash("PickStatus");
        private Animator _animator;

        private void Awake() {
        }

        void Start() {
            _animator = GetComponentInChildren<Animator>();
        }

        [Override]
        public void OnPlayerPickedUp(GameObject playerObj) {
            if (_animator.GetInteger(PickStatus) != 0 || !playerObj.CompareTag("Player")) {
                return;
            }

            _animator.SetInteger(PickStatus, 1);

            var chestFireworksVFX = Instantiate(chestFireworksVFXPrefab, gameObject.transform);
            var particleSystem = chestFireworksVFX.GetComponent<ParticleSystem>();
            if (particleSystem != null) {
                particleSystem.Play();
            }

            LevelEventCenter.TriggerGameSuccess();
        }
    }
}