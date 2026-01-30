using System;
using System.Collections.Generic;
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
        Attack = 4 // 攻击状态
    }

    public class EnemyAI : MonoBehaviour, IDeadAbility {
        private static readonly int Status = Animator.StringToHash("Status");
        [SerializeField] private Transform transformTemplate;

        [Header("移动设置")] [SerializeField] private float patrolSpeed = 1f;
        [SerializeField] private float chaseSpeed = 2f;
        [SerializeField] private float waypointReachDistance = 0.5f; // 到达路径点的判定距离

        [Header("检测设置")] [SerializeField] private float detectableRadius = 3f;
        [SerializeField] private float attackableRadius = 1.5f; // 进入攻击距离, 就去锁住Player的移动
        private Transform _headTransform; // 视线起点在敌人头部附近

        [Header("巡逻路径")] [SerializeField] private List<Vector2> patrolPaths; // 巡逻路径点列表

        private EnemyState _curState;
        private int _currentPathIndex = 0; // 当前目标路径点索引
        private Transform _targetPlayer;
        private Rigidbody2D _rb;
        private Animator _animator;
        private RaycastHit2D[] _hitBuffer = new RaycastHit2D[5];

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
            _targetPlayer = GameObject.FindGameObjectWithTag("Player").transform;
            _headTransform = Array.Find(GetComponentsInChildren<Transform>(), t => t.name == "head");
            _curState = EnemyState.Idle;
        }

        private void Update() {
            switch (_curState) {
                case EnemyState.Idle:
                    if (Patrol()) {
                        _curState = EnemyState.Patrol;
                    }

                    if (DetectPlayer()) {
                        // 空闲中发现了目标, 就去追
                        _curState = EnemyState.Chase;
                    }

                    break;
                case EnemyState.Patrol:
                    Patrol();
                    if (DetectPlayer()) {
                        // 巡逻中发现了目标, 就去追
                        _curState = EnemyState.Chase;
                    }

                    break;
                case EnemyState.Chase:
                    if (!DetectPlayer()) {
                        // 丢失目标, 返回巡逻
                        _curState = EnemyState.Patrol;
                    }

                    if (ChasePlayer() && MySugarUtil.IsGround(gameObject)) {
                        // 追到了, 开始攻击
                        _curState = EnemyState.Attack;
                    }

                    break;
                case EnemyState.Attack:
                    break;
                case EnemyState.Dead:
                    break;
            }

            _animator.SetInteger(Status, (int)_curState);
        }

        private void FixedUpdate() {
            switch (_curState) {
                case EnemyState.Idle:
                    _rb.velocity = Vector2.zero;
                    break;
                case EnemyState.Patrol:
                    if (!MySugarUtil.IsGround(gameObject)) {
                        break;
                    }
                    Vector2 patrolDirection = (patrolPaths[_currentPathIndex] - (Vector2)transform.position).normalized;
                    _rb.velocity = new Vector2(patrolDirection.x * patrolSpeed, _rb.velocity.y);
                    break;
                case EnemyState.Chase:
                    if (!MySugarUtil.IsGround(gameObject)) {
                        break;
                    }
                    Vector2 chaseDirection = (_targetPlayer.position - transform.position).normalized;
                    _rb.velocity = new Vector2(chaseDirection.x * chaseSpeed, _rb.velocity.y);
                    break;
                case EnemyState.Attack:
                    _rb.velocity = Vector2.zero; // 攻击时停止移动
                    break;
                case EnemyState.Dead:
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

        private bool DetectPlayer() {
            float distanceToPlayer = Vector2.Distance(_headTransform.position, _targetPlayer.position);
            if (distanceToPlayer > detectableRadius) {
                return false;
            }

            int hitCount = Physics2D.RaycastNonAlloc(
                _headTransform.position,
                (_targetPlayer.position - _headTransform.position).normalized,
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

        private bool ChasePlayer() {
            // 是否已经追到了
            float distanceToPlayer = Vector2.Distance(_headTransform.position, _targetPlayer.position);
            if (distanceToPlayer <= attackableRadius) {
                return true;
            }

            // 计算移动方向
            Vector2 direction = (_targetPlayer.position - transform.position).normalized;

            if (direction.x > 0) {
                transformTemplate.localRotation = Quaternion.Euler(0, 120, 0);
            }
            else if (direction.x < 0) {
                transformTemplate.localRotation = Quaternion.Euler(0, -120, 0);
            }

            return false;
        }

        [Override]
        public void Dead(GameObject killer) {
            Debug.Log("Skeleton Dead");
            _curState = EnemyState.Dead;
            _rb.velocity = Vector2.zero;
            // 可以添加死亡动画和销毁逻辑
            Destroy(gameObject, 2f);
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