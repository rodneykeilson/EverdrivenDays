using UnityEngine;
using UnityEngine.UI;

namespace EverdrivenDays
{
    public class PlayerInteract : MonoBehaviour
    {
        [SerializeField] private float interactRange = 2f;
        [SerializeField] private GameObject interactUI; // UI element ("Talk (E)")
        private NPCInteractable currentNPC; // Stores the NPC in range

        private void Awake()
        {
            // Safety check: prevent interactUI from being assigned to UIManager
            if (interactUI != null && interactUI.GetComponent<UIManager>() != null)
            {
                Debug.LogError($"[PlayerInteract] interactUI is assigned to UIManager GameObject! This will break the UI. Please assign only the intended UI element (e.g., the interaction prompt) to interactUI.");
                interactUI = null; // Prevent accidental disabling
            }
        }

        private void Update()
        {
            DetectNPC();

            if (Input.GetKeyDown(KeyCode.E) && currentNPC != null)
            {
                currentNPC.Interact();
            }
        }

        private void DetectNPC()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, interactRange);
            currentNPC = null;
            if (interactUI != null)
                interactUI.SetActive(false);

            foreach (Collider collider in colliders)
            {
                if (collider.TryGetComponent<NPCInteractable>(out NPCInteractable npc))
                {
                    currentNPC = npc;
                    if (interactUI != null)
                        interactUI.SetActive(true); // Show "Talk (E)"
                    return; // Stop checking once an NPC is found
                }
            }
        }
    }
}
