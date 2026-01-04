using System;
using HadoopCore.Scripts.Attribute;
using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HadoopCore.Scripts
{
    public class Player : MonoBehaviour, IPointerClickHandler, IDeadAbility {
        [SerializeField] private float moveSpeed;
        [SerializeField] private float raycastDistance; // 射线检测的最大距离
        [SerializeField] private Transform transformTemplate;
        
        private static readonly int Status = Animator.StringToHash("Status");

        private Vector2 _moveInput;
        private Rigidbody2D _rb;
        private Animator _anim;
        private Collider2D _collider;
        

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>(); 
            _collider = GetComponent<Collider2D>();
            
            _anim = GetComponentInChildren<Animator>(); 
            if (_anim != null) {
                _anim.applyRootMotion = false; // 动画不带位移
                _anim.updateMode = AnimatorUpdateMode.Normal;
            }
        }

        void FixedUpdate()
        {
            _rb.velocity = new Vector2(_moveInput.x * moveSpeed, _rb.velocity.y);
        }

        private void OnCollisionEnter2D(Collision2D collision2D)
        {
            if (collision2D.gameObject.CompareTag("Plug"))
            {
                StopMovement();
            }
        }

        private void OnTriggerEnter2D(Collider2D triggerObj)
        {
            IPlayerPickup pickedUpObj = triggerObj.GetComponentInParent<IPlayerPickup>() ?? triggerObj.GetComponent<IPlayerPickup>();
            if (pickedUpObj != null)
            {
                pickedUpObj.OnPlayerPickedUp(gameObject);
                StopMovement();
            }
        }

        [Override]
        public void Dead(GameObject killer)
        {
            _anim.SetInteger(Status, 2);
        }


        // Input System 回调：绑定到 Actions 里的 Move
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>(); 
            HandleMovementInput();
        }
        
        [Override]
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_moveInput != Vector2.zero)
            {
                return;
            }
            // 向右打一条很短的射线, 如果碰撞到了plug, 说明玩家在贴着plug操作, 就保持不动
            RaycastHit2D[] hits = new RaycastHit2D[1];
            int hitCount = _collider.Cast(Vector2.right, hits, raycastDistance);
            if (hitCount > 0 && hits[0].collider.CompareTag("Plug"))
            {
                return;
            }
            
            _moveInput = Vector2.right;
            
            HandleMovementInput();
        }
        
        private void HandleMovementInput()
        {
            int status = _moveInput != Vector2.zero ? 1 : 0;
            _anim.SetInteger(Status, status);
            if (status == 1)
            {
                if (_moveInput.x > 0)
                {
                    transformTemplate.localRotation = Quaternion.Euler(0, 120, 0);
                }
                else if (_moveInput.x < 0)
                {
                    transformTemplate.localRotation = Quaternion.Euler(0, -120, 0);
                }
            }
        }
        
        private void StopMovement()
        {
            _moveInput = Vector2.zero;
            _rb.velocity = Vector2.zero;
            _anim.SetInteger(Status, 0);
        }
    }
}