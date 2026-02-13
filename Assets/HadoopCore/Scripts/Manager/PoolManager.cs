using System.Collections.Generic;
using HadoopCore.Scripts.InterfaceAbility;
using UnityEngine;

namespace HadoopCore.Scripts.Manager {
    public class PoolManager : MonoBehaviour {
        public static PoolManager Instance { get; private set; }

        private Dictionary<GameObject, Stack<GameObject>> _pool = new();
        private Transform _poolRoot;

        #if UNITY_EDITOR
        private HashSet<GameObject> _despawnedSet = new();
        #endif

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create PoolRoot as a child of PoolManager
            var poolRootGO = new GameObject("PoolRoot");
            poolRootGO.transform.SetParent(transform);
            _poolRoot = poolRootGO.transform;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) {
            if (prefab == null) {
                Debug.LogError("[PoolManager] Spawn failed: prefab is null.");
                return null;
            }

            GameObject obj = null;

            // 1) 先尝试从池子拿
            if (_pool.TryGetValue(prefab, out var stack)) {
                // Remove destroyed references
                while (stack.Count > 0 && stack.Peek() == null) {
                    stack.Pop();
                }

                if (stack.Count > 0) {
                    obj = stack.Pop();

                    #if UNITY_EDITOR
                    _despawnedSet.Remove(obj);
                    #endif

                    obj.SetActive(false);
                    obj.transform.SetPositionAndRotation(position, rotation);
                    obj.transform.SetParent(parent, true);
                    obj.SetActive(true);

                    // 调用 obj 的 IPoolable.OnSpawned (AFTER SetActive)
                    InvokeOnSpawned(obj);

                    return obj;
                }
            }

            // 2) 如果没有则实例化一个新的
            obj = Instantiate(prefab, position, rotation, parent);
            return obj;
        }

        public void Despawn(GameObject prefab, GameObject obj) {
            if (prefab == null) {
                Debug.LogError("[PoolManager] Despawn failed: prefab is null.");
                return;
            }

            if (obj == null) {
                Debug.LogError("[PoolManager] Despawn failed: obj is null.");
                return;
            }

#if UNITY_EDITOR
            if (_despawnedSet.Contains(obj)) {
                Debug.LogError($"[PoolManager] Double-despawn detected for object: {obj.name}");
                return;
            }

            _despawnedSet.Add(obj);
#endif

            // 调用 obj 的 IPoolable.OnDespawned (BEFORE SetActive(false))
            InvokeOnDespawned(obj);

            // Reparent to PoolRoot (DDOL scene)
            obj.transform.SetParent(_poolRoot, false);
            obj.SetActive(false);

            if (!_pool.ContainsKey(prefab)) {
                _pool[prefab] = new Stack<GameObject>();
            }

            _pool[prefab].Push(obj);
        }

        private void InvokeOnSpawned(GameObject obj) {
            var components = obj.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < components.Length; i++) {
                if (components[i] is IPoolable poolable) {
                    poolable.OnSpawned();
                }
            }
        }

        private void InvokeOnDespawned(GameObject obj) {
            var components = obj.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < components.Length; i++) {
                if (components[i] is IPoolable poolable) {
                    poolable.OnDespawned();
                }
            }
        }
    }
}