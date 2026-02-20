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
        [SerializeField] private float impactThreshold = 2f;

        private Rigidbody2D _rb;
        private bool _hasTriggered = false;

        void Awake() {
            _rb = GetComponent<Rigidbody2D>();
        }

        void OnCollisionEnter2D(Collision2D collision) {
            // 0. 如果已经触发过，直接返回
            if (_hasTriggered) return;

            // 1. 获取碰撞时的相对速度力度
            // relativeVelocity会自动计算两个物体本身的运动差
            float impactForce = collision.relativeVelocity.magnitude;

            // 2. 判断力度是否足够大
            // (可选：你也可以判断碰撞对象的标签，比如 if (collision.gameObject.CompareTag("Ground")))
            if (collision.gameObject.CompareTag("Plug")) {
                impactForce -= 3f;
            }
            // Debug.Log($"碰撞力度 {impactForce}");
            if (impactForce > impactThreshold) {
                _hasTriggered = true; // 3. 标记为已触发，并启动引信
                if (MySugarUtil.TryToFindComponent<IExposeAbility>(collision.gameObject, out var victimAbility, 
                        ComponentSearchLocation.Parent, ComponentSearchLocation.Self)) {
                    if (!victimAbility.IsAlive()) {
                        return;
                    }
                    victimAbility.SetStateWithLock(CharacterState.Dead, true);
                }
                RockBreak();
            }
        }

        private void RockBreak() {
            _rb.simulated = false; // 彻底不参与物理
            var rockBreakVFX = Instantiate(rockBreakVFXPrefab, transform.position, Quaternion.identity);
            var particleSystem = rockBreakVFX.GetComponentInChildren<ParticleSystem>();
            if (particleSystem != null) {
                particleSystem.Play();
            }
            gameObject.SetActive(false);
        }
    }
}