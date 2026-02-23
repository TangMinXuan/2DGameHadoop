using HadoopCore.Scripts.Manager;
using UnityEngine;

namespace HadoopCore.Scripts.SceneController {
    public class LevelController : MonoBehaviour {
        
        [Header("关卡配置")]
        [SerializeField] private int totalEnemyCount;
        [SerializeField] private GameObject chestPrefab;
        [SerializeField] private Vector2 chestSpawnPosition;
        
        private int _deadEnemyCount;
        private bool _chestSpawned;

        private void OnEnable() {
            LevelEventCenter.OnOneEnemyDead += OnOneEnemyDead;
        }

        private void OnDisable() {
            LevelEventCenter.OnOneEnemyDead -= OnOneEnemyDead;
        }

        private void OnOneEnemyDead(GameObject enemy) {
            _deadEnemyCount++;
            Debug.Log($"[LevelController] Enemy dead: {enemy.name} ({_deadEnemyCount}/{totalEnemyCount})");
            
            if (_deadEnemyCount >= totalEnemyCount && !_chestSpawned) {
                SpawnChest();
            }
        }

        private void SpawnChest() {
            if (chestPrefab == null) {
                Debug.LogWarning("[LevelController] Chest prefab is not assigned!");
                return;
            }
            _chestSpawned = true;
            Instantiate(chestPrefab, chestSpawnPosition, Quaternion.identity);
            Debug.Log($"[LevelController] Chest spawned at {chestSpawnPosition}");
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(chestSpawnPosition, new Vector3(1f, 1f, 0f));
            Gizmos.DrawIcon(chestSpawnPosition, "d_Prefab Icon", true);
        }
    }
}

