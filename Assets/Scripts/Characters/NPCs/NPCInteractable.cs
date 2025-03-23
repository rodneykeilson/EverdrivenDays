using UnityEngine;
using UnityEngine.Events;

namespace EverdrivenDays
{
    public class NPCInteractable : MonoBehaviour
    {
        public string npcName; // NPC name
        [TextArea(3, 5)]
        public string[] dialogueLines; // Multiple dialogue lines

        public UnityEvent onInteract; // Event to trigger UI

        public void Interact()
        {
            onInteract?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}
