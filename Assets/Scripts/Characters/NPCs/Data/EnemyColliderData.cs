using System;
using UnityEngine;

namespace EverdrivenDays
{
    [Serializable]
    public class EnemyColliderData
    {
        [Header("Main Collider")]
        [field: SerializeField] public float Height { get; private set; } = 1.8f;
        [field: SerializeField] public float CenterY { get; private set; } = 0.9f;
        [field: SerializeField] public float Radius { get; private set; } = 0.3f;
        
        [Header("Ground Check")]
        [field: SerializeField] public BoxCollider GroundCheckCollider { get; private set; }
        
        // Cache for performance
        private Vector3 groundCheckColliderExtents;
        
        public Vector3 GroundCheckColliderExtents 
        { 
            get { return groundCheckColliderExtents; } 
        }
        
        public void Initialize()
        {
            if (GroundCheckCollider != null)
            {
                // Force bounds update to ensure extents are correct
                if (GroundCheckCollider.gameObject.activeInHierarchy)
                {
                    // Cache extents (direct calculation in case bounds haven't updated)
                    groundCheckColliderExtents = new Vector3(
                        GroundCheckCollider.size.x * GroundCheckCollider.transform.lossyScale.x * 0.5f,
                        GroundCheckCollider.size.y * GroundCheckCollider.transform.lossyScale.y * 0.5f,
                        GroundCheckCollider.size.z * GroundCheckCollider.transform.lossyScale.z * 0.5f
                    );
                }
                else
                {
                    // Fallback if gameObject is inactive
                    groundCheckColliderExtents = new Vector3(
                        GroundCheckCollider.size.x * 0.5f,
                        GroundCheckCollider.size.y * 0.5f,
                        GroundCheckCollider.size.z * 0.5f
                    );
                    Debug.LogWarning("Ground Check Collider is inactive - bounds may not be accurate");
                }
            }
            else
            {
                Debug.LogError("Ground Check Collider not assigned in EnemyColliderData");
                // Set default values to avoid null reference exceptions
                groundCheckColliderExtents = new Vector3(0.3f, 0.1f, 0.3f);
            }
        }
    }
} 