using HadoopCore.Scripts.InterfaceAbility;
using HadoopCore.Scripts.Manager;
using HadoopCore.Scripts.Shared;
using UnityEngine;

namespace HadoopCore.Scripts {
    public class Arrow : MonoBehaviour {
        [SerializeField] private float speed;

        private Rigidbody2D _rb;
        private Vector2 _direction;
        private Camera _mainCamera;

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _mainCamera = Camera.main;

            // 返回物体的局部 X 轴正方向(红色箭头方向)
            _direction = transform.right;
            
            LevelEventCenter.OnGameOver += StopShooting;
            LevelEventCenter.OnGameSuccess += StopShooting;
        }

        private void FixedUpdate() {
            _rb.velocity = _direction * speed;

            if (IsOutOfCameraBounds()) {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D triggerObj) {
            // if (triggerObj.TryGetComponent<IExposeAbility>(out var victimAbility)) {
            //     if (!victimAbility.IsAlive()) {
            //         return;
            //     }
            //     if (victimAbility.GetGameObject().tag == "Ballista") {
            //         return;
            //     }
            //     victimAbility.SetStateWithLock(CharacterState.Dead, true);
            // } else {
            //     Destroy(gameObject);
            // }
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            if (collision.rigidbody.gameObject.TryGetComponent<IExposeAbility>(out var victimAbility)) {
                if (!victimAbility.IsAlive()) {
                    return;
                }
                if (victimAbility.GetGameObject().tag == "Ballista") {
                    return;
                }
                victimAbility.SetStateWithLock(CharacterState.Dead, true);
                
            }
            Destroy(gameObject);
        }

        private bool IsOutOfCameraBounds() {
            // 将物体位置转换为视口坐标(0-1范围)
            Vector3 viewportPos = _mainCamera.WorldToViewportPoint(transform.position);

            return viewportPos.x < 0 || viewportPos.x > 1 ||
                   viewportPos.y < 0 || viewportPos.y > 1;
        }
        
        private void StopShooting() {
            Destroy(gameObject);
        }
        
        private void OnDestroy() {
            LevelEventCenter.OnGameOver -= StopShooting;
            LevelEventCenter.OnGameSuccess -= StopShooting;
        }
    }
}