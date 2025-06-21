using UnityEngine;
using System.Collections;

namespace EverdrivenDays
{
    // Attach this to your Goblin prefab
    [RequireComponent(typeof(Enemy))]
    public class EnemyRespawner : MonoBehaviour
    {
        public float respawnDelay = 10f;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Enemy enemy;
        private GoblinAnimationController animController;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            animController = GetComponent<GoblinAnimationController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        public void OnEnemyDeath()
        {
            StartCoroutine(RespawnCoroutineWrapper());
        }

        private IEnumerator RespawnCoroutineWrapper()
        {
            yield return StartCoroutine(RespawnCoroutine());
        }

        private IEnumerator RespawnCoroutine()
        {
            gameObject.SetActive(false);
            yield return new WaitForSeconds(respawnDelay);
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            if (enemy != null) enemy.ResetEnemy();
            if (animController != null) animController.PlayIdle();
            gameObject.SetActive(true);
        }
    }
}
