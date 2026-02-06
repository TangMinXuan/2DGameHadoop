using System.Collections.Generic;
using UnityEngine;

namespace HadoopCore.Scripts.Manager {
    public class PoolManager : MonoBehaviour{
        public static PoolManager Instance { get; private set; }

        private Dictionary<string, Stack<GameObject>> pool = new();
        
        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public GameObject Spawn(string prefabKey, Vector3 position, Quaternion rotation, Transform parent = null) {
            return null;
        }

        public void Despawn(string prefabKey, GameObject obj) {
            
        }
    }
}