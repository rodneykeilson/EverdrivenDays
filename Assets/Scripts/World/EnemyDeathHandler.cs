using UnityEngine;

namespace EverdrivenDays.World
{
    public class EnemyDeathHandler : MonoBehaviour
    {
        public EnemySpawner spawner;

        // Call this when the enemy dies
        public void OnDeath()
        {
            if (spawner != null)
            {
                spawner.OnEnemyDied(gameObject);
            }
            Destroy(gameObject);
        }
    }
}
