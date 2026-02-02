using System.Collections;
using HadoopCore.Scripts.Attribute;
using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Shared;
using HadoopCore.Scripts.UI;
using HadoopCore.Scripts.Utils;
using UnityEngine;

namespace HadoopCore.Scripts {
    public class Rock : MonoBehaviour {
        [SerializeField] private GameObject rockBreakVFXPrefab;

        private bool _grounded;
        private bool _cleanupStarted;
        private Rigidbody2D _rb;

        void Awake() {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (other.TryGetComponent<IExposeAbility>(out var victimAbility)) {
                if (!victimAbility.IsAlive()) {
                    return;
                }
                victimAbility.SetStateWithLock(CharacterState.Dead, true);
            }
        }

        void OnCollisionEnter2D(Collision2D c) {
            if (MySugarUtil.IsGroundObj(c.gameObject) && !_grounded) {
                _grounded = true;

                _rb.simulated = false; // 彻底不参与物理

                var rockBreakVFX = Instantiate(rockBreakVFXPrefab, transform.position, Quaternion.identity);
                var particleSystem = rockBreakVFX.GetComponentInChildren<ParticleSystem>();
                if (particleSystem != null) {
                    particleSystem.Play();
                }

                Destroy(gameObject);
            }
        }
    }
}