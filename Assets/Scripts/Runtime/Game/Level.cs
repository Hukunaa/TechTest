using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemy;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Hero;

namespace Game
{
    public class Level : MonoBehaviour
    {
        [SerializeField] private AssetReferenceGameObject _enemyPrefab;
        [SerializeField] private int _numberOfEnemiesToSpawn = 5;
        [SerializeField] private int _minSpawnX = -5;
        [SerializeField] private int _maxSpawnX = 5;
        [SerializeField] private int _minSpawnY = 6;
        [SerializeField] private int _maxSpawnY = 14;

        private List<EnemyController> _enemies = new List<EnemyController>();
        private HashSet<long> _occupied = new HashSet<long>();
        private int _enemiesKilled = 0;

        public IReadOnlyList<EnemyController> Enemies => _enemies;

        // Validate that the enemy addressable reference is set before runtime spawning.
        void Awake()
        {
            if (_enemyPrefab == null || !_enemyPrefab.RuntimeKeyIsValid())
                Debug.LogError("Level: Enemy prefab addressable reference is missing or invalid.", this);
        }

        // Spawn enemies for the level and hook into the game state so enemies can be enabled/disabled with the game flow.
        void Start()
        {
            StartCoroutine(SpawnEnemiesRoutine());
            GameManager.Instance.StateChanged += OnGameStateChanged;
        }

        // Cleanup: unsubscribe from game state and release all addressable enemy instances.
        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= OnGameStateChanged;

            for (int i = 0; i < _enemies.Count; i++)
            {
                EnemyController enemy = _enemies[i];
                if (enemy == null) continue;
                enemy.OnDead -= OnEnemyDied;
                Addressables.ReleaseInstance(enemy.gameObject);
            }

            _enemies.Clear();
            _occupied.Clear();
        }

        // Spawn enemies at random grid positions within the XZ range, ensuring no overlap
        private IEnumerator SpawnEnemiesRoutine()
        {
            _enemies.Clear();
            _occupied.Clear();
            _enemiesKilled = 0;

            if (_enemyPrefab == null || !_enemyPrefab.RuntimeKeyIsValid()) yield break;

            int minX = Mathf.Min(_minSpawnX, _maxSpawnX);
            int maxX = Mathf.Max(_minSpawnX, _maxSpawnX);
            int minZ = Mathf.Min(_minSpawnY, _maxSpawnY);
            int maxZ = Mathf.Max(_minSpawnY, _maxSpawnY);

            int width = maxX - minX + 1;
            int depth = maxZ - minZ + 1;
            int maxCells = Mathf.Max(0, width * depth);

            int toSpawn = Mathf.Clamp(_numberOfEnemiesToSpawn, 0, maxCells);
            if (toSpawn == 0) yield break;

            List<Vector2Int> cells = new List<Vector2Int>(maxCells);
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                    cells.Add(new Vector2Int(x, z));
            }

            for (int i = 0; i < cells.Count; i++)
            {
                int j = Random.Range(i, cells.Count);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }

            int spawned = 0;

            for (int i = 0; i < cells.Count && spawned < toSpawn; i++)
            {
                int x = cells[i].x;
                int z = cells[i].y;

                long key = MakeKey(x, z);
                if (_occupied.Contains(key)) continue;

                Vector3 pos = new Vector3(x, transform.position.y, z);

                AsyncOperationHandle<GameObject> handle = _enemyPrefab.InstantiateAsync(pos, Quaternion.identity, transform);
                yield return handle;

                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                    continue;
                }

                GameObject go = handle.Result;

                EnemyController controller = go.GetComponent<EnemyController>();
                if (controller == null)
                {
                    Addressables.ReleaseInstance(go);
                    continue;
                }

                _enemies.Add(controller);
                controller.OnDead += OnEnemyDied;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                        _occupied.Add(MakeKey(x + dx, z + dz));
                }

                spawned++;
            }
        }

        // Track kills for round completion and complete the level when all spawned enemies are dead.
        void OnEnemyDied(LivingUnit enemy)
        {
            _enemiesKilled++;
            if (_enemiesKilled >= _enemies.Count)
                GameManager.Instance.CompleteLevel();
        }

        // Enable/disable enemies based on the current game state.
        void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Running)
            {
                foreach (EnemyController enemy in _enemies)
                {
                    if (enemy != null)
                        enemy.Enable();
                }
            }
            else
            {
                foreach (EnemyController enemy in _enemies)
                {
                    if (enemy != null)
                        enemy.Disable();
                }
            }
        }

        // Pack X and Z grid coordinates into a single 64-bit key for fast occupancy checks.
        private static long MakeKey(int x, int z)
        {
            unchecked
            {
                return ((long)x << 32) | (uint)z;
            }
        }
    }
}
