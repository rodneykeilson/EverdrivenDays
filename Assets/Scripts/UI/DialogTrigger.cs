using System.Collections.Generic;
using UnityEngine;

public class DialogTrigger : MonoBehaviour
{
    public List<DialogLine> dialogLines;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;
            DialogManager.Instance.ShowDialog(dialogLines);
            Destroy(gameObject); // Remove trigger after use
        }
    }
}
