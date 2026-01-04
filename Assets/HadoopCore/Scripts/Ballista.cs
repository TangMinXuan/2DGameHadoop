using System;
using System.Collections;
using UnityEngine;

namespace HadoopCore.Scripts
{
    public class Ballista : MonoBehaviour
    {
        [SerializeField] LayerMask groundLayers; // Ground层，用于检测地面
        [SerializeField] float shootDelay = 1.0f;
        [SerializeField] private GameObject arrowPrefab;

        private Rigidbody2D _rb;
        private bool _grounded;
        private bool _cleanupStarted;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        // 砸到地面时被调用
        void OnCollisionEnter2D(Collision2D c)
        {
            if (IsGround(c.gameObject) && !_grounded)
            {
                _grounded = true;
                StartGroundLogic(); 
            }
        }
        
        private bool IsGround(GameObject go)
        {
            return ((groundLayers.value & (1 << go.layer)) != 0);
        }

        private void StartGroundLogic()
        {
            if (_cleanupStarted)
            {
                return;
            }
            _cleanupStarted = true;
            StartCoroutine(ShootArrow());
        }
        
        private IEnumerator ShootArrow()
        {
            while (true)
            {
                yield return new WaitForSeconds(shootDelay);

                // generate arrow
                if (arrowPrefab != null)
                {
                    // arrow 使用 ballista 的位置和朝向
                    Instantiate(arrowPrefab, transform.position, transform.rotation);
                }
            }
        }
    }
}