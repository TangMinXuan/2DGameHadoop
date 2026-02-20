using System;
using Cinemachine;
using HadoopCore.Scripts.Attribute;
using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Shared;
using HadoopCore.Scripts.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HadoopCore.Scripts {
    
    public class Player : MonoBehaviour, IExposeAbility {
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
        [SerializeField] private float moveSpeedWhileFalling = 1f;
        [SerializeField] private float raycastDistance; // 射线检测的最大距离
        [SerializeField] private Transform transformTemplate;
        [SerializeField] private CameraShakeSettings cameraShakeSettings = new CameraShakeSettings();

        private static readonly int StatusKey = Animator.StringToHash("Status");

        private Vector2 _moveInput;
        private Rigidbody2D _rb;
        private Animator _animator;
        private Collider2D _collider;
        private float _curSpeed = 0f;

        // Camera shake state
        private CinemachineBrain _brain;

        private CharacterState _curState;
        private bool _animLock = false;

        void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _brain = FindObjectOfType<CinemachineBrain>();

            _animator = GetComponentInChildren<Animator>();
            if (_animator != null) {
                _animator.applyRootMotion = false; // 动画不带位移
                _animator.updateMode = AnimatorUpdateMode.Normal;
            }
            
            SetState(CharacterState.Idle);
        }

        void Update() {
            if (_animLock) {
                return;
            }
            
        }

        void FixedUpdate() {
            if (_animLock) {
                return;
            }
            _rb.velocity = new Vector2(_moveInput.x * _curSpeed, _rb.velocity.y); // 这同时也会锁住水平速度，bomb的冲击波就无效了
            
            if (Mathf.Abs(_rb.velocity.y) > 3f && GetState() != CharacterState.Fall) {
                _curSpeed = moveSpeedWhileFalling;
                SetState(CharacterState.Fall);
            } else if (Mathf.Abs(_rb.velocity.y) <= 0.1f && GetState() == CharacterState.Fall) {
                _curSpeed = 0;
                SetState(CharacterState.Idle);
            } else if (Mathf.Abs(_moveInput.x) > 0.1f && GetState() == CharacterState.Idle) {
                _curSpeed = moveSpeed;
                SetState(CharacterState.Walk);
            } else if (Mathf.Abs(_moveInput.x) <= 0.1f && GetState() == CharacterState.Walk) {
                _curSpeed = 0;
                SetState(CharacterState.Idle);
            }
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
                // StopMovement(); 会导致卡脚
            }
        }

        private void OnTriggerEnter2D(Collider2D triggerObj) {
            if (!IsAlive()) {
                return;
            }
            IPlayerPickup pickedUpObj = triggerObj.GetComponentInParent<IPlayerPickup>() ??
                                        triggerObj.GetComponent<IPlayerPickup>();
            if (pickedUpObj != null) {
                pickedUpObj.OnPlayerPickedUp(gameObject);
                StopMovement();
            }
        }
        
        [Override]
        public bool IsAlive() {
            if (GetState() == CharacterState.Dead) {
                return false;
            }
            if (GetState() == CharacterState.UnderAttack) {
                return false;
            }
            return true;
        }
        
        [Override]
        public CharacterState GetState() {
            return _curState;
        }
        
        [Override]
        public bool SetState(CharacterState state) {
            if (_animLock) {
                Debug.LogWarning("[Player - SetState] 动画已被加锁, 禁止修改.");
                return false;
            }
            if (state == CharacterState.Dead || state == CharacterState.UnderAttack || state == CharacterState.Attack) {
                Debug.LogWarning($"[Player - SetState] ${state}状态必须使用 SetStateWithLock 方法设置!");
                return false;
            }
            _curState = state;
            _animator.SetInteger(StatusKey, (int)GetState());
            return true;
        }
        
        [Override]
        public void SetStateWithLock(CharacterState state, bool locked, IExposeAbility caller = null) {
            _animLock = locked;
            _curState = state;
            _animator.SetInteger(StatusKey, (int)GetState());
            if (state == CharacterState.Dead) {
                StopMovement();
                // 根据killer的相对位置计算击飞方向
                if (caller != null) {
                    float horizontalDirection = caller.GetTransform().position.x < transform.position.x ? 1f : -1f;
                    Vector2 knockbackDirection = new Vector2(horizontalDirection, 1f).normalized;
                    float knockbackForce = 10f;
                    _rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                }
                LevelEventCenter.TriggerGameOver();
            }
        }
        
        // Input System 回调：绑定到 Actions 里的 Move
        public void OnMove(InputAction.CallbackContext context) {
            _moveInput = context.ReadValue<Vector2>();
            HandleMovementInput();
        }

        private void HandleMovementInput() {
            if (GetState() == CharacterState.Dead || GetState() == CharacterState.UnderAttack) {
                return;
            }

            if (GetState() == CharacterState.Fall) {
                _curSpeed = moveSpeedWhileFalling;
            } else {
                _curSpeed = moveSpeed;
            }
            
            if (_moveInput.x > 0) {
                transformTemplate.localRotation = Quaternion.Euler(0, 120, 0);
            } else if (_moveInput.x < 0) {
                transformTemplate.localRotation = Quaternion.Euler(0, -120, 0);
            }
        }

        private void StopMovement() {
            _moveInput = Vector2.zero;
            _rb.velocity = Vector2.zero;
        }
    }
}