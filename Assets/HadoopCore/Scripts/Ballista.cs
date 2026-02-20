using System;
using System.Collections;
using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Shared;
using UnityEngine;

namespace HadoopCore.Scripts {
    public class Ballista : MonoBehaviour, IExposeAbility {
        [SerializeField] LayerMask groundLayers; // Ground层，用于检测地面
        [SerializeField] float shootDelay = 1.0f;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private GameObject ballistaBreakVFXPrefab;

        private Rigidbody2D _rb;
        private bool _grounded;
        private bool _cleanupStarted;
        private bool _isAlive = true;

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
        }

        // 砸到地面时被调用
        void OnCollisionEnter2D(Collision2D c) {
            if (IsGround(c.gameObject) && !_grounded) {
                _grounded = true;
                StartGroundLogic();
            }
        }

        private bool IsGround(GameObject go) {
            return ((groundLayers.value & (1 << go.layer)) != 0);
        }

        private void StartGroundLogic() {
            if (_cleanupStarted) {
                return;
            }

            _cleanupStarted = true;
            StartCoroutine(ShootArrow());
        }

        private IEnumerator ShootArrow() {
            while (true) {
                yield return new WaitForSeconds(shootDelay);
                // generate arrow
                if (arrowPrefab != null) {
                    Instantiate(arrowPrefab, transform.position, transform.rotation); // arrow 使用 ballista 的位置和朝向
                }
            }
        }

        private void BallistaBreak() {
            _isAlive = false;
            _rb.simulated = false; // 彻底不参与物理
            var ballistaBreakVFX = Instantiate(ballistaBreakVFXPrefab, transform.position, Quaternion.identity);
            var particleSystem = ballistaBreakVFX.GetComponentInChildren<ParticleSystem>();
            if (particleSystem != null) {
                particleSystem.Play();
            }
            gameObject.SetActive(false);
        }

        public bool IsAlive() {
            return _isAlive;
        }

        public CharacterState GetState() {
            throw new NotImplementedException();
        }

        public bool SetState(CharacterState state) {
            throw new NotImplementedException();
        }

        public void SetStateWithLock(CharacterState state, bool locked, IExposeAbility caller = null) {
            if (state == CharacterState.Dead) {
                BallistaBreak();
            }
        }
    }
}