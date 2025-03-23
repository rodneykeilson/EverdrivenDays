using UnityEngine;

namespace EverdrivenDays
{
    public class NPCInteractable : MonoBehaviour
    {
        public void Interact() {
            Debug.Log("Interact");
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}