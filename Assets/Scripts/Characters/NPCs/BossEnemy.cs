using UnityEngine;

namespace EverdrivenDays
{
    public class BossEnemy : Enemy
    {
        [SerializeField] private BossRhythmController bossRhythmController;
        private bool hasStartedEncounter = false;

        private void OnTriggerEnter(Collider other)
        {
            if (!hasStartedEncounter && other.CompareTag("Player"))
            {
                hasStartedEncounter = true;
                bossRhythmController.StartBossEncounter(other.GetComponent<Player>());
            }
        }
    }
}
