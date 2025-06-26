using System.Collections;
using System.Collections.Generic;
using System.Linq; // For OrderBy
using UnityEngine;

namespace EverdrivenDays.World
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        public GameObject enemyPrefab;
        public int mobCap = 10;
        public string spawnTag = "Overworld";
        public float spawnInterval = 10f;
        public Collider[] safeZones;
        public float spawnRadius = 30f;
        // Tag to use for spawn points
        public string spawnPointTag = "EnemySpawnPoint";
        public float safeZoneRadius = 5f; // Radius for safe zone exclusion

        private List<GameObject> activeEnemies = new List<GameObject>();
        private WaitForSeconds waitInterval;

        private void Awake()
        {
            waitInterval = new WaitForSeconds(spawnInterval);
        }

        private void Start()
        {
            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                yield return waitInterval;
                if (activeEnemies.Count < mobCap)
                {
                    TrySpawnEnemy();
                }
            }
        }

        private void TrySpawnEnemy()
        {
            Vector3 pos;
            if (FindValidSpawnPosition(out pos))
            {
                GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
                // Set name to prefab name (removes (Clone))
                enemy.name = enemyPrefab.name;
                // Adjust Y so feet are on the ground if collider exists
                Collider col = enemy.GetComponent<Collider>();
                if (col != null)
                {
                    float bottomOffset = col.bounds.center.y - col.bounds.min.y;
                    enemy.transform.position = new Vector3(pos.x, pos.y + bottomOffset, pos.z);
                }
                // Ensure Rigidbody is present and enabled
                Rigidbody rb = enemy.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = enemy.AddComponent<Rigidbody>();
                }
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.WakeUp();
                activeEnemies.Add(enemy);
                var deathHandler = enemy.AddComponent<EnemyDeathHandler>();
                deathHandler.spawner = this;
            }
        }

        public void OnEnemyDied(GameObject enemy)
        {
            activeEnemies.Remove(enemy);
        }

        private bool FindValidSpawnPosition(out Vector3 pos)
        {
            var spawnPoints = GameObject.FindGameObjectsWithTag(spawnPointTag);
            if (spawnPoints.Length == 0)
            {
                pos = Vector3.zero;
                Debug.LogWarning("No spawn points found with tag '" + spawnPointTag + "'.");
                return false;
            }

            // Pick a random spawn point and check if it's in a safe zone
            var shuffled = spawnPoints.OrderBy(x => Random.value).ToArray();
            foreach (var point in shuffled)
            {
                Vector3 candidate = point.transform.position;
                bool inSafeZone = false;
                foreach (var safeZone in safeZones)
                {
                    if (Vector3.Distance(candidate, safeZone.transform.position) < safeZoneRadius)
                    {
                        inSafeZone = true;
                        break;
                    }
                }
                if (!inSafeZone)
                {
                    pos = candidate;
                    return true;
                }
            }
            // Fallback: use the first spawn point if all are in safe zones
            pos = spawnPoints[0].transform.position;
            return true;
        }
    }
}
