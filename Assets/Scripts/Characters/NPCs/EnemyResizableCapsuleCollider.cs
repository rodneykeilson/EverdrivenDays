using UnityEngine;

namespace EverdrivenDays
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class EnemyResizableCapsuleCollider : MonoBehaviour
    {
        [field: SerializeField] public EnemyColliderData ColliderData { get; private set; }

        private CapsuleCollider capsuleCollider;

        private void Awake()
        {
            capsuleCollider = GetComponent<CapsuleCollider>();

            if (ColliderData == null)
            {
                Debug.LogError("EnemyColliderData not assigned to EnemyResizableCapsuleCollider");
                return;
            }

            // Initialize ground check
            ColliderData.Initialize();

            // Configure capsule collider
            UpdateColliderDimensions();
        }

        public void UpdateColliderDimensions()
        {
            if (capsuleCollider == null || ColliderData == null)
                return;

            capsuleCollider.height = ColliderData.Height;
            capsuleCollider.center = new Vector3(0, ColliderData.CenterY, 0);
            capsuleCollider.radius = ColliderData.Radius;
        }
    }
} 