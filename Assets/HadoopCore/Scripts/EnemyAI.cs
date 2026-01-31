using System;
using System.Collections.Generic;
using DG.Tweening;
using HadoopCore.Scripts.Attribute;
using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Utils;
using UnityEngine;

namespace HadoopCore.Scripts {
    public enum EnemyState {
        Dead = 0, // 死亡状态
        Idle = 1, // 空闲状态
        Patrol = 2, // 巡逻状态
        Chase = 3, // 追击状态
        Attack = 4, // 攻击状态
        Static = 5 // 攻击后短暂的静止状态
    }

    public class EnemyAI : MonoBehaviour, IDeadAbility {        
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

        [Header("移动设置")] [SerializeField] private float patrolSpeed = 1f;
        [SerializeField] private float chaseSpeed = 2f;
        [SerializeField] private float waypointReachDistance = 0.5f; // 到达路径点的判定距离

        [Header("检测设置")] [SerializeField] private float detectableRadius = 3f;
        [SerializeField] private float attackableRadius = 1.5f; // 进入攻击距离, 就去锁住Player的移动
        private Transform _headTransform; // 视线起点在敌人头部附近

        [Header("巡逻路径")] [SerializeField] private List<Vector2> patrolPaths; // 巡逻路径点列表

        [SerializeField] private HitScratchSettings hitScratchSettings = new HitScratchSettings();
        
        public GameObject chaseTarget { get; private set; }
        public EnemyState curState { get; set; }
        
        private int _currentPathIndex = 0; // 当前目标路径点索引
        private Rigidbody2D _rb;
        private Animator _animator;
        private RaycastHit2D[] _hitBuffer = new RaycastHit2D[5];
        
        // Hit scratch state
        private Vector3 _hitScratchBaseLocalScale;
        private Color _hitScratchBaseColor;
        private float _hitScratchLocalOffsetY; // 存储原始的 Y 偏移量


        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
            chaseTarget = GameObject.FindGameObjectWithTag("Player");
            _headTransform = Array.Find(GetComponentsInChildren<Transform>(), t => t.name == "head");
            curState = EnemyState.Idle;
            
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
            // if ((int)curState != _animator.GetInteger(Status)) {
            //     curState = (EnemyState)_animator.GetInteger(Status);
            // }
            _animator.SetInteger(StatusKey, (int)curState);
            switch (curState) {
                case EnemyState.Static:
                    break; // 攻击后短暂的静止状态，等待动画完成事件(OnComplete)中切换回Idle
                case EnemyState.Idle:
                    if (Patrol()) {
                        curState = EnemyState.Patrol;
                    }

                    if (DetectTarget()) {
                        // 空闲中发现了目标, 就去追
                        curState = EnemyState.Chase;
                    }

                    break;
                case EnemyState.Patrol:
                    Patrol();
                    if (DetectTarget()) {
                        // 巡逻中发现了目标, 就去追
                        curState = EnemyState.Chase;
                    }

                    break;
                case EnemyState.Chase:
                    if (!DetectTarget()) {
                        // 丢失目标, 返回巡逻
                        curState = EnemyState.Patrol;
                    }

                    if (ChaseTarget() && MySugarUtil.IsGround(gameObject)) {
                        // 追到了, 开始攻击
                        curState = EnemyState.Attack;
                        _animator.SetTrigger("AttackTrigger");
                    }

                    break;
                case EnemyState.Attack:
                    break;
                case EnemyState.Dead:
                    break;
            }
        }

        private void FixedUpdate() {
            switch (_animator.GetInteger(StatusKey)) {
                case 1:
                    _rb.velocity = Vector2.zero;
                    break;
                case 2:
                    if (!MySugarUtil.IsGround(gameObject)) {
                        break;
                    }
                    Vector2 patrolDirection = (patrolPaths[_currentPathIndex] - (Vector2)transform.position).normalized;
                    _rb.velocity = new Vector2(patrolDirection.x * patrolSpeed, _rb.velocity.y);
                    break;
                case 3:
                    if (!MySugarUtil.IsGround(gameObject)) {
                        break;
                    }
                    Vector2 chaseDirection = (chaseTarget.transform.position - transform.position).normalized;
                    _rb.velocity = new Vector2(chaseDirection.x * chaseSpeed, _rb.velocity.y);
                    break;
                case 4:
                    _rb.velocity = Vector2.zero; // 攻击时停止移动
                    break;
                case 0:
                    _rb.velocity = Vector2.zero; // 死亡时停止移动
                    break;
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
            chaseTarget.transform.gameObject.TryGetComponent(out IDeadAbility deadAbility);
            if (deadAbility == null || !deadAbility.IsAlive()) {
                // 死了的目标不去检测
                return false;
            }
            float distanceToPlayer = Vector2.Distance(_headTransform.position, chaseTarget.transform.position);
            if (distanceToPlayer > detectableRadius) {
                return false;
            }

            int hitCount = Physics2D.RaycastNonAlloc(
                _headTransform.position,
                (chaseTarget.transform.position - _headTransform.position).normalized,
                _hitBuffer,
                distanceToPlayer
            );

            for (int i = 0; i < hitCount; i++) {
                if (_hitBuffer[i].rigidbody != null) {
                    if (_hitBuffer[i].rigidbody.CompareTag("Plug")) {
                        break;
                    }
                    if (_hitBuffer[i].rigidbody.CompareTag("Player")) {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ChaseTarget() {
            // 是否已经追到了
            float distanceToPlayer = Vector2.Distance(_headTransform.position, chaseTarget.transform.position);
            if (distanceToPlayer <= attackableRadius) {
                return true;
            }

            // 计算移动方向
            Vector2 direction = (chaseTarget.transform.position - transform.position).normalized;

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
            Vector2 direction = (chaseTarget.transform.position - transform.position).normalized;
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
        public void Dead(GameObject killer) {
            Debug.Log("Skeleton Dead");
            curState = EnemyState.Dead;
            _rb.velocity = Vector2.zero;
            // 可以添加死亡动画和销毁逻辑
            Destroy(gameObject, 2f);
        }
        
        [Override]
        public bool IsAlive() {
            return curState != EnemyState.Dead;
        }

        private void OnDrawGizmosSelected() {
            // 可视化检测范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectableRadius);

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