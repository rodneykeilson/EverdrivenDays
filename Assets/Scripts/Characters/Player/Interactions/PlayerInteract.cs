using UnityEngine;

namespace EverdrivenDays
{
    public class PlayerInteract : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                float interactRange = 2f;
                Collider[] colliderArray = Physics.OverlapSphere(transform.position, interactRange);
                foreach (Collider collider in colliderArray) {
                    collider.GetComponent<NPCInteractable>()?.Interact();
                }
            }
        }
    }
}