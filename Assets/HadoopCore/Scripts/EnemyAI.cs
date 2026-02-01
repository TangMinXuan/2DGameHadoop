using System;
using System.Collections.Generic;
using DG.Tweening;
using HadoopCore.Scripts.Attribute;
using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Shared;
using HadoopCore.Scripts.Utils;
using UnityEngine;

namespace HadoopCore.Scripts {

    public class EnemyAI : MonoBehaviour, IExposeAbility {
        [Serializable]
        internal class HitScratchSettings {
            [Header("Sprite Reference")]
            [SerializeField] private SpriteRenderer spriteRenderer;

            [Header("Fade In")]
            [SerializeField] private float fadeInDuration = 0.03f;

            [Header("Scale Up (Impact)")]
            [SerializeField] private float scaleUpDelay = 0.05f;
            [SerializeField] private float scaleUpDuration = 0.10f;
            [SerializeField] private float scaleUpMultiplier = 1.08f;

            [Header("Fade Out")]
            [SerializeField] private float fadeOutDelay = 0.20f;
            [SerializeField] private float fadeOutDuration = 0.30f;

            [Header("Randomization")]
            [SerializeField] private float rotationRange = 25f;
            [SerializeField] private float positionOffsetRange = 0.05f;

            public SpriteRenderer SpriteRenderer { get => spriteRenderer; set => spriteRenderer = value; }
            public float FadeInDuration => fadeInDuration;
            public float ScaleUpDelay => scaleUpDelay;
            public float ScaleUpDuration => scaleUpDuration;
            public float ScaleUpMultiplier => scaleUpMultiplier;
            public float FadeOutDelay => fadeOutDelay;
            public float FadeOutDuration => fadeOutDuration;
            public float RotationRange => rotationRange;
            public float PositionOffsetRange => positionOffsetRange;
        }

        private static readonly int StatusKey = Animator.StringToHash("Status");
        [SerializeField] private Transform transformTemplate;

        [Header("移动设置")] 
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float chaseSpeed = 3;
        [SerializeField] private float waypointReachDistance = 0.5f; // 到达路径点的判定距离

        [Header("检测设置")] 
        [SerializeField] private float detectableRadius = 3f;
        [SerializeField] private float attackableRadius = 1.5f; // 进入攻击距离, 就去锁住Player的移动
        [SerializeField] private float detectRayOffsetX = 3f; // 射线起始位置X偏移量
        [SerializeField] private float detectRayOffsetY = 1f; // 射线起始位置Y偏移量

        [Header("巡逻路径")] [SerializeField] private List<Vector2> patrolPaths; // 巡逻路径点列表

        [SerializeField] private HitScratchSettings hitScratchSettings = new HitScratchSettings();
        
        public CharacterState curState { get; set; }
        
        private int _currentPathIndex = 0; // 当前目标路径点索引
        private Rigidbody2D _rb;
        private Animator _animator;
        private RaycastHit2D[] _hitBuffer = new RaycastHit2D[1];
        private Sequence _hitScratchSeq;
        
        private IExposeAbility _chaseTargetExposeAbility;
        
        // Hit scratch state
        private Vector3 _hitScratchBaseLocalScale;
        private Color _hitScratchBaseColor;
        private float _hitScratchLocalOffsetY; // 存储原始的 Y 偏移量


        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
            curState = CharacterState.Idle;
            
            // Initialize hit scratch
            InitializeHitScratch();
        }
        
        private void InitializeHitScratch() {
            if (hitScratchSettings.SpriteRenderer == null) return;

            // 存储原始 Y 偏移量，用于动态计算位置
            _hitScratchLocalOffsetY = hitScratchSettings.SpriteRenderer.transform.localPosition.y;
            _hitScratchBaseLocalScale = hitScratchSettings.SpriteRenderer.transform.localScale;
            _hitScratchBaseColor = hitScratchSettings.SpriteRenderer.color;

            // Start invisible
            Color c = hitScratchSettings.SpriteRenderer.color;
            c.a = 0f;
            hitScratchSettings.SpriteRenderer.color = c;
        }

        private void Update() {
            
            _animator.SetInteger(StatusKey, (int)curState);
            
            if (curState == CharacterState.Static) {
                // 攻击后短暂的静止状态，等待动画完成事件(OnComplete)中切换回Idle
                Debug.Log("[EnemyAI] Static State - waiting for attack animation to complete");
            } else if (curState == CharacterState.Idle) {
                if (Patrol()) {
                    curState = CharacterState.Patrol;
                }

                if (DetectTarget()) {
                    curState = CharacterState.Chase; // 空闲中发现了目标, 就去追
                }
            } else if (curState == CharacterState.Patrol) {
                Patrol();
                if (DetectTarget()) {
                    curState = CharacterState.Chase; // 巡逻中发现了目标, 就去追
                }
            } else if (curState == CharacterState.Chase) {
                if (!DetectTarget()) {
                    curState = CharacterState.Patrol; // 丢失目标, 返回巡逻
                }
                if (ChaseTarget()) {
                    curState = CharacterState.Attack; // 追到了, 开始攻击
                }
            } else if (curState == CharacterState.Attack) {
                // 攻击状态
                _animator.SetTrigger("AttackTrigger");
                curState = CharacterState.Static; // 攻击后必须立马转入其他状态, 防止反复触发攻击
            } else if (curState == CharacterState.Dead) {
                // 死亡状态
            }
        }

        private void FixedUpdate() {
            CharacterState curAnimState = CharacterState.FromValue(_animator.GetInteger(StatusKey));
            if (curAnimState == CharacterState.Idle) {
                _rb.velocity = Vector2.zero;
            } else if (curAnimState == CharacterState.Patrol) {
                if (!MySugarUtil.IsGround(gameObject)) {
                    return;
                }
                Vector2 patrolDirection = (patrolPaths[_currentPathIndex] - (Vector2)transform.position).normalized;
                _rb.velocity = new Vector2(patrolDirection.x * patrolSpeed, _rb.velocity.y);
            } else if (curAnimState == CharacterState.Chase) {
                if (!MySugarUtil.IsGround(gameObject)) {
                    return;
                }
                Transform targetTransform = _chaseTargetExposeAbility.GetTransform();
                Vector2 chaseDirection = (targetTransform.position - transform.position).normalized;
                _rb.velocity = new Vector2(chaseDirection.x * chaseSpeed, _rb.velocity.y);
            } else if (curAnimState == CharacterState.Attack) {
                _rb.velocity = Vector2.zero; // 攻击时停止移动
            } else if (curAnimState == CharacterState.Dead) {
                _rb.velocity = Vector2.zero; // 死亡时停止移动
            }
        }

        private bool Patrol() {
            // 如果没有路径点，保持Idle状态
            if (patrolPaths == null || patrolPaths.Count == 0) return false;

            Vector2 currentPos = transform.position;
            Vector2 targetPos = patrolPaths[_currentPathIndex];

            // 检查是否到达当前路径点
            if (Vector2.Distance(currentPos, targetPos) <= waypointReachDistance) {
                // 移动到下一个路径点，循环遍历
                _currentPathIndex = (_currentPathIndex + 1) % patrolPaths.Count;
                targetPos = patrolPaths[_currentPathIndex];
            }

            // 计算移动方向
            Vector2 direction = (targetPos - currentPos).normalized;

            if (direction.x > 0) {
                transformTemplate.localRotation = Quaternion.Euler(0, 120, 0);
            }
            else if (direction.x < 0) {
                transformTemplate.localRotation = Quaternion.Euler(0, -120, 0);
            }

            return true;
        }

        private bool DetectTarget() {
            // 根据 transformTemplate.localRotation.y 确定射线方向和偏移
            bool facingRight = transformTemplate.localRotation.y >= 0;
            Vector2 direction = facingRight ? Vector2.right : Vector2.left;
            float offsetX = facingRight ? detectRayOffsetX : -detectRayOffsetX;
            Vector2 rayOrigin = (Vector2)transformTemplate.position + new Vector2(offsetX, detectRayOffsetY);
            int hitCount = Physics2D.RaycastNonAlloc(
                rayOrigin,
                direction,
                _hitBuffer,
                detectableRadius
            );

            if (_hitBuffer[0].rigidbody != null) {
                string tag = _hitBuffer[0].rigidbody.tag;
                if (tag == null || !CharacterRank.ContainsTag(tag)) {
                    // 只检测Character的tag, 如果中途有其他障碍物(例如:Plug)挡住了, 就停止检测
                    return false;
                }
                if (CharacterRank.GetRank(tag) >= CharacterRank.GetRank(gameObject.tag)) {
                    // 碰到比自己等级高的角色, 就停止检测
                    // 高等级的角色死亡后, 记得Destroy!!! 不然会一直挡住低等级角色的检测
                    return false;
                }
                if (CharacterRank.GetRank(tag) < CharacterRank.GetRank(gameObject.tag)) {
                    // 碰到比自己等级低的角色, 就去追
                    _chaseTargetExposeAbility = _hitBuffer[0].rigidbody.GetComponent<IExposeAbility>();
                    if (!_chaseTargetExposeAbility.IsAlive()) {
                        return false;
                    }
                    Debug.Log($"[EnemyAI] Detected target: {_chaseTargetExposeAbility.GetGameObject().name}");
                    return true;
                }
            }

            return false;
        }

        private bool ChaseTarget() {
            // 是否已经追到了
            Transform targetTransform = _chaseTargetExposeAbility.GetTransform();
            float distanceToPlayer = Vector2.Distance(transformTemplate.position, targetTransform.position);
            if (distanceToPlayer <= attackableRadius) {
                return true;
            }

            // 计算移动方向
            Vector2 direction = (targetTransform.position - transform.position).normalized;
            if (direction.x > 0) {
                transformTemplate.localRotation = Quaternion.Euler(0, 120, 0);
            }
            else if (direction.x < 0) {
                transformTemplate.localRotation = Quaternion.Euler(0, -120, 0);
            }

            return false;
        }
        
        
        public Sequence PlayHitScratch() {
            if (hitScratchSettings.SpriteRenderer == null) return null;

            Transform scratchTransform = hitScratchSettings.SpriteRenderer.transform;

            // 根据当前朝向和攻击距离计算位置
            Transform targetTransform = _chaseTargetExposeAbility.GetTransform();
            Vector2 direction = (targetTransform.position - transform.position).normalized;
            float facingOffsetX = direction.x > 0 ? attackableRadius : -attackableRadius;
            Vector3 baseLocalPosition = new Vector3(facingOffsetX, _hitScratchLocalOffsetY, scratchTransform.localPosition.z);

            // Reset to base state
            scratchTransform.localScale = _hitScratchBaseLocalScale;
            Color c = _hitScratchBaseColor;
            c.a = 0f;
            hitScratchSettings.SpriteRenderer.color = c;

            // Randomize rotation
            float randomRotation = UnityEngine.Random.Range(-hitScratchSettings.RotationRange, hitScratchSettings.RotationRange);
            scratchTransform.localRotation = Quaternion.Euler(0f, 0f, randomRotation);

            // Randomize position offset
            float offsetX = UnityEngine.Random.Range(-hitScratchSettings.PositionOffsetRange, hitScratchSettings.PositionOffsetRange);
            float offsetY = UnityEngine.Random.Range(-hitScratchSettings.PositionOffsetRange, hitScratchSettings.PositionOffsetRange);
            scratchTransform.localPosition = baseLocalPosition + new Vector3(offsetX, offsetY, 0f);

            // Randomize horizontal flip
            hitScratchSettings.SpriteRenderer.flipX = UnityEngine.Random.value > 0.5f;

            // Build DOTween sequence
            Vector3 scaledUp = _hitScratchBaseLocalScale * hitScratchSettings.ScaleUpMultiplier;
            return DOTween.Sequence()
                .SetUpdate(true)
                .Append(hitScratchSettings.SpriteRenderer.DOFade(1f, hitScratchSettings.FadeInDuration)) // Fade in (0.00s - 0.03s)
                .Insert(hitScratchSettings.ScaleUpDelay, scratchTransform.DOScale(scaledUp, hitScratchSettings.ScaleUpDuration)
                    .SetEase(Ease.OutQuad)) // Scale up for impact (0.05s - 0.15s)
                .Insert(hitScratchSettings.FadeOutDelay, hitScratchSettings.SpriteRenderer.DOFade(0f, hitScratchSettings.FadeOutDuration)
                    .SetEase(Ease.InQuad)); // Fade out (0.20s - 0.50s)
        }

        [Override]
        public int GetRank() {
            return 1;
        }

        [Override]
        public void Dead(GameObject killer) {
            Debug.Log("Skeleton Dead");
            curState = CharacterState.Dead;
            _rb.velocity = Vector2.zero;
            // 可以添加死亡动画和销毁逻辑
            Destroy(gameObject, 2f);
        }
        
        [Override]
        public CharacterState GetState() {
            return curState;
        }
        
        [Override]
        public void SetLogicState(CharacterState state) {
            curState = state;
        }
        
        [Override]
        public bool IsAlive() {
            return curState != CharacterState.Dead;
        }

        [Override]
        public IExposeAbility GetChaseTargetExposeAbility() {
            return _chaseTargetExposeAbility;
        }
        private void OnDrawGizmosSelected() {
            // 可视化检测范围（考虑offset偏移）
            if (transformTemplate != null) {
                bool facingRight = transformTemplate.localRotation.y >= 0;
                float offsetX = facingRight ? detectRayOffsetX : -detectRayOffsetX;
                Vector2 rayOrigin = (Vector2)transformTemplate.position + new Vector2(offsetX, detectRayOffsetY);
                Vector2 direction = facingRight ? Vector2.right : Vector2.left;
                Vector2 rayEnd = rayOrigin + direction * detectableRadius;
                
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(rayOrigin, 0.1f); // 射线起点
                Gizmos.DrawLine(rayOrigin, rayEnd); // 射线
                Gizmos.DrawWireSphere(rayEnd, 0.15f); // 射线终点
            }

            // 可视化巡逻路径
            if (patrolPaths != null && patrolPaths.Count > 1) {
                Gizmos.color = Color.blue;
                for (int i = 0; i < patrolPaths.Count; i++) {
                    Vector2 current = patrolPaths[i];
                    Vector2 next = patrolPaths[(i + 1) % patrolPaths.Count];

                    Gizmos.DrawSphere(current, 0.2f);
                    Gizmos.DrawLine(current, next);
                }
            }
        }
    }
}