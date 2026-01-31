using System;
using Cinemachine;
using HadoopCore.Scripts.Attribute;
using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HadoopCore.Scripts {
    public enum PlayerState {
        Idle = 0, // 空闲状态
        Walk = 1,  // 行走状态
        Dead = 2, // 死亡状态
    }
    
    public class Player : MonoBehaviour, IPointerClickHandler, IDeadAbility {
        [Serializable]
        internal class CameraShakeSettings {
            [Header("Speed")]
            [SerializeField] private float walkThreshold = 0.1f;

            [Header("Noise Gains (very subtle)")]
            [SerializeField] private float ampWalk = 0.5f;
            [SerializeField] private float freqWalk = 1.0f;

            public float WalkThreshold => walkThreshold;
            public float AmpWalk => ampWalk;
            public float FreqWalk => freqWalk;
        }
        
        [SerializeField] private float moveSpeed;
        [SerializeField] private float raycastDistance; // 射线检测的最大距离
        [SerializeField] private Transform transformTemplate;
        [SerializeField] private CameraShakeSettings cameraShakeSettings = new CameraShakeSettings();

        private static readonly int Status = Animator.StringToHash("Status");

        private Vector2 _moveInput;
        private Rigidbody2D _rb;
        private Animator _anim;
        private Collider2D _collider;

        // Camera shake state
        private CinemachineBrain _brain;

        void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _brain = FindObjectOfType<CinemachineBrain>();

            _anim = GetComponentInChildren<Animator>();
            if (_anim != null) {
                _anim.applyRootMotion = false; // 动画不带位移
                _anim.updateMode = AnimatorUpdateMode.Normal;
            }
        }

        void FixedUpdate() {
            _rb.velocity = new Vector2(_moveInput.x * moveSpeed, _rb.velocity.y);
        }

        private void LateUpdate() {
            UpdateCameraShake();
        }

        private void UpdateCameraShake() {
            if (_brain == null) return;

            ICinemachineCamera activeVcam = _brain.ActiveVirtualCamera;
            if (activeVcam == null) return;

            CinemachineVirtualCamera vcam = activeVcam as CinemachineVirtualCamera;
            if (vcam == null) return;

            CinemachineBasicMultiChannelPerlin perlin = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (perlin == null) return;

            float vx = Mathf.Abs(_rb.velocity.x);

            if (vx > cameraShakeSettings.WalkThreshold) {
                perlin.m_AmplitudeGain = cameraShakeSettings.AmpWalk;
                perlin.m_FrequencyGain = cameraShakeSettings.FreqWalk;
            } else {
                perlin.m_AmplitudeGain = 0f;
                perlin.m_FrequencyGain = 0f;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision2D) {
            if (collision2D.gameObject.CompareTag("Plug")) {
                StopMovement();
            }
        }

        private void OnTriggerEnter2D(Collider2D triggerObj) {
            IPlayerPickup pickedUpObj = triggerObj.GetComponentInParent<IPlayerPickup>() ??
                                        triggerObj.GetComponent<IPlayerPickup>();
            if (pickedUpObj != null) {
                pickedUpObj.OnPlayerPickedUp(gameObject);
                StopMovement();
            }
        }

        [Override]
        public void Dead(GameObject killer) {
            StopMovement();
            _anim.SetInteger(Status, (int)PlayerState.Dead);
            LevelEventCenter.TriggerGameOver();
        }
        
        [Override]
        public bool IsAlive() {
            return _anim.GetInteger(Status) != (int)PlayerState.Dead;
        }
        
        // Input System 回调：绑定到 Actions 里的 Move
        public void OnMove(InputAction.CallbackContext context) {
            _moveInput = context.ReadValue<Vector2>();
            HandleMovementInput();
        }

        [Override]
        public void OnPointerClick(PointerEventData eventData) {
            if (_moveInput != Vector2.zero) {
                return;
            }

            // 向右打一条很短的射线, 如果碰撞到了plug, 说明玩家在贴着plug操作, 就保持不动
            RaycastHit2D[] hits = new RaycastHit2D[1];
            int hitCount = _collider.Cast(Vector2.right, hits, raycastDistance);
            if (hitCount > 0 && hits[0].collider.CompareTag("Plug")) {
                return;
            }

            _moveInput = Vector2.right;

            HandleMovementInput();
        }

        private void HandleMovementInput() {
            if (!MySugarUtil.IsGround(gameObject)) {
                return;
            }
            PlayerState status = _moveInput != Vector2.zero ? PlayerState.Walk : PlayerState.Idle;
            _anim.SetInteger(Status, (int)status);
            if (status == PlayerState.Walk) {
                if (_moveInput.x > 0) {
                    transformTemplate.localRotation = Quaternion.Euler(0, 120, 0);
                }
                else if (_moveInput.x < 0) {
                    transformTemplate.localRotation = Quaternion.Euler(0, -120, 0);
                }
            }
        }

        private void StopMovement() {
            _moveInput = Vector2.zero;
            _rb.velocity = Vector2.zero;
            _anim.SetInteger(Status, (int)PlayerState.Idle);
        }
    }
}