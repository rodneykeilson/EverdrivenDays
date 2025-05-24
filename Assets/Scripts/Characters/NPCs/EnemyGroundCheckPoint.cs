using UnityEngine;

namespace EverdrivenDays
{
    // Simple helper component to visually show the ground check point
    public class EnemyGroundCheckPoint : MonoBehaviour
    {
        [SerializeField] private float radius = 0.2f;
        [SerializeField] private Color color = Color.green;
        
        private void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
} 