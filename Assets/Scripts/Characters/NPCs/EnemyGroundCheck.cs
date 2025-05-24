using UnityEngine;
using UnityEngine.AI;

namespace EverdrivenDays
{
    [RequireComponent(typeof(Enemy))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyGroundCheck : MonoBehaviour
    {
        [Header("Ground Check Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float raycastDistance = 0.3f;
        [SerializeField] private float raycastRadius = 0.2f;
        
        // References
        private Enemy enemy;
        private NavMeshAgent navMeshAgent;
        private Rigidbody rb;
        private EnemyResizableCapsuleCollider resizableCapsuleCollider;
        private CapsuleCollider mainCollider;
        
        // State
        private bool isGrounded;
        private Vector3 lastValidPosition;
        
        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            rb = GetComponent<Rigidbody>();
            resizableCapsuleCollider = GetComponent<EnemyResizableCapsuleCollider>();
            mainCollider = GetComponent<CapsuleCollider>();
            
            if (resizableCapsuleCollider == null || resizableCapsuleCollider.ColliderData == null)
            {
                Debug.LogError("EnemyResizableCapsuleCollider is missing or not properly configured!");
            }
            
            lastValidPosition = transform.position;
            
            // Make sure layer mask is set if not already
            if (groundLayer.value == 0)
            {
                groundLayer = LayerMask.GetMask("Ground", "Default");
                Debug.LogWarning("Ground layer was not set, defaulting to Ground and Default layers");
            }
        }
        
        private void Update()
        {
            CheckGround();
            
            if (isGrounded)
            {
                // Update last valid position when grounded
                lastValidPosition = transform.position;
            }
            else
            {
                // If falling too far, reset to last valid position
                if (transform.position.y < lastValidPosition.y - 10f)
                {
                    ResetToLastValidPosition();
                }
            }
        }
        
        private void CheckGround()
        {
            // Method 1: Box collider method (if configured)
            bool isGroundedBox = CheckGroundByBoxCollider();
            
            // Method 2: Direct raycast (more reliable fallback)
            bool isGroundedRay = CheckGroundByRaycast();
            
            // Enemy is grounded if either method returns true
            isGrounded = isGroundedBox || isGroundedRay;
        }
        
        private bool CheckGroundByBoxCollider()
        {
            if (resizableCapsuleCollider == null || 
                resizableCapsuleCollider.ColliderData == null || 
                resizableCapsuleCollider.ColliderData.GroundCheckCollider == null)
                return false;
                
            BoxCollider groundCheckCollider = resizableCapsuleCollider.ColliderData.GroundCheckCollider;
            
            // Use OverlapBox to check for ground
            Vector3 colliderCenter = groundCheckCollider.transform.position + groundCheckCollider.center;
            Vector3 colliderExtents = resizableCapsuleCollider.ColliderData.GroundCheckColliderExtents;
            
            Collider[] overlappedColliders = Physics.OverlapBox(
                colliderCenter,
                colliderExtents,
                groundCheckCollider.transform.rotation,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
            
            return overlappedColliders.Length > 0;
        }
        
        private bool CheckGroundByRaycast()
        {
            if (mainCollider == null) return false;
            
            // Calculate raycast origin (bottom of the capsule collider)
            Vector3 raycastOrigin = transform.position + 
                                   new Vector3(0, mainCollider.center.y - mainCollider.height * 0.5f + 0.05f, 0);
            
            // Use SphereCast for more reliable ground detection
            if (Physics.SphereCast(raycastOrigin, raycastRadius, Vector3.down, out RaycastHit hit, 
                                  raycastDistance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
            
            return false;
        }
        
        private void ResetToLastValidPosition()
        {
            transform.position = lastValidPosition;
            
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw raycast
            if (mainCollider != null)
            {
                Vector3 raycastOrigin = transform.position + 
                                     new Vector3(0, mainCollider.center.y - mainCollider.height * 0.5f + 0.05f, 0);
                                     
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(raycastOrigin, raycastRadius);
                Gizmos.DrawLine(raycastOrigin, raycastOrigin + Vector3.down * raycastDistance);
                Gizmos.DrawWireSphere(raycastOrigin + Vector3.down * raycastDistance, raycastRadius);
            }
            
            // Draw ground check box
            if (resizableCapsuleCollider != null && 
                resizableCapsuleCollider.ColliderData != null && 
                resizableCapsuleCollider.ColliderData.GroundCheckCollider != null)
            {
                BoxCollider groundCheckCollider = resizableCapsuleCollider.ColliderData.GroundCheckCollider;
                    
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.matrix = groundCheckCollider.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(groundCheckCollider.center, groundCheckCollider.size);
            }
        }
    }
} 