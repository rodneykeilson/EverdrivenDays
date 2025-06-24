using UnityEngine;
// felix's test
namespace EverdrivenDays
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerResizableCapsuleCollider))]
    [RequireComponent(typeof(PlayerStats))]
    public class Player : MonoBehaviour
    {
        [field: Header("References")]
        [field: SerializeField] public PlayerSO Data { get; private set; }

        [field: Header("Collisions")]
        [field: SerializeField] public PlayerLayerData LayerData { get; private set; }

        [field: Header("Camera")]
        [field: SerializeField] public PlayerCameraRecenteringUtility CameraRecenteringUtility { get; private set; }

        [field: Header("Animations")]
        [field: SerializeField] public PlayerAnimationData AnimationData { get; private set; }

        [field: Header("Combat")]
        [field: SerializeField] private float attackRange = 2f;
        [field: SerializeField] private LayerMask enemyLayers;

        public Rigidbody Rigidbody { get; private set; }
        public Animator[] Animators { get; private set; } // Store multiple animators
        public PlayerStats Stats { get; private set; }

        public PlayerInput Input { get; private set; }
        public PlayerResizableCapsuleCollider ResizableCapsuleCollider { get; private set; }

        public Transform MainCameraTransform { get; private set; }

        private PlayerMovementStateMachine movementStateMachine;
        private Enemy currentTarget;

        private Vector3 respawnPoint;
        private Quaternion respawnRotation;

        private void Awake()
        {
            if (CameraRecenteringUtility != null)
                CameraRecenteringUtility.Initialize();
                
            if (AnimationData != null)
                AnimationData.Initialize();

            Rigidbody = GetComponent<Rigidbody>();
            Animators = GetComponentsInChildren<Animator>(); // Get all child animators
            Stats = GetComponent<PlayerStats>();

            Input = GetComponent<PlayerInput>();
            ResizableCapsuleCollider = GetComponent<PlayerResizableCapsuleCollider>();

            // Fix null reference by checking if Camera.main exists
            if (Camera.main != null)
            {
                MainCameraTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogError("No main camera found in the scene! Please ensure a camera is tagged as 'MainCamera'.");
            }

            movementStateMachine = new PlayerMovementStateMachine(this);
        }

        private void Start()
        {
            movementStateMachine.ChangeState(movementStateMachine.IdlingState);

            // Set initial respawn point
            respawnPoint = transform.position;
            respawnRotation = transform.rotation;

            // Hook up to UI Manager
            if (UIManager.Instance != null)
            {
                Stats.OnHealthChanged += (current, max) => UIManager.Instance.UpdatePlayerHUD();
                Stats.OnManaChanged += (current, max) => UIManager.Instance.UpdatePlayerHUD();
                Stats.OnLevelUp += (level) => UIManager.Instance.UpdatePlayerHUD();
                Stats.OnGoldChanged += (gold) => UIManager.Instance.UpdatePlayerHUD();
                Stats.OnExperienceChanged += (exp, expToNext) => UIManager.Instance.UpdatePlayerHUD();
                Stats.OnStaminaChanged += (current, max) => UIManager.Instance.UpdatePlayerHUD();
            }
        }

        private void Update()
        {
            movementStateMachine.HandleInput();
            movementStateMachine.Update();
            
            // Check for enemy detection
            DetectEnemies();
        }

        private void FixedUpdate()
        {
            movementStateMachine.PhysicsUpdate();
        }

        private void OnTriggerEnter(Collider collider)
        {
            
                
            movementStateMachine.OnTriggerEnter(collider);
        }

        private void OnTriggerExit(Collider collider)
        {
           
                
            movementStateMachine.OnTriggerExit(collider);
        }

        // Apply animation state to all animators
        public void SetBool(int parameterHash, bool value)
        {
            foreach (var animator in Animators)
            {
                animator.SetBool(parameterHash, value);
            }
        }

        public void OnMovementStateAnimationEnterEvent()
        {
            movementStateMachine.OnAnimationEnterEvent();
        }

        public void OnMovementStateAnimationExitEvent()
        {
            movementStateMachine.OnAnimationExitEvent();
        }

        public void OnMovementStateAnimationTransitionEvent()
        {
            movementStateMachine.OnAnimationTransitionEvent();
        }
        
        // Combat methods
        private void DetectEnemies()
        {
            // Cast a sphere to detect enemies
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayers);
            
            if (hitColliders.Length > 0)
            {
                // Find the closest enemy
                float closestDistance = float.MaxValue;
                Enemy closestEnemy = null;
                
                foreach (var hitCollider in hitColliders)
                {
                    Enemy enemy = hitCollider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        float distance = Vector3.Distance(transform.position, enemy.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestEnemy = enemy;
                        }
                    }
                }
                
                // If we found an enemy
                if (closestEnemy != null && closestEnemy != currentTarget)
                {
                    currentTarget = closestEnemy;
                    
                    // Update UI (enemy UI removed)
                    // if (UIManager.Instance != null)
                    // {
                    //     UIManager.Instance.SetTargetedEnemy(currentTarget);
                    // }
                }
            }
            else if (currentTarget != null)
            {
                // Clear target if no enemies in range
                currentTarget = null;
                
                // Update UI (enemy UI removed)
                // if (UIManager.Instance != null)
                // {
                //     UIManager.Instance.SetTargetedEnemy(null);
                // }
            }
        }
        
        public void Attack()
        {
            if (!Stats.CanAttack()) return;
            
            // Apply stamina cost
            Stats.AttackPerformed(10); // 10 stamina per attack
            
            // Check if we hit an enemy
            if (currentTarget != null)
            {
                // Calculate damage
                int damage = Stats.CalculateDamage(15); // Base damage of 15
                
                // Apply damage
                bool enemyDied = currentTarget.TakeDamage(damage);
                
                // If enemy died
                if (enemyDied)
                {
                    // Enemy is automatically destroyed after rhythm game
                }
                else
                {
                    // Start rhythm game combat
                    currentTarget.StartBattle();
                }
            }
        }

        public void SetRespawnPoint(Vector3 position, Quaternion rotation)
        {
            respawnPoint = position;
            respawnRotation = rotation;
        }

        public void Respawn()
        {
            // Move player to respawn point and reset velocity
            transform.position = respawnPoint;
            transform.rotation = respawnRotation;
            if (Rigidbody != null)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
            }
            if (Stats != null)
            {
                Stats.FullyRestore(); // You may need to implement this to restore HP/MP/Stamina
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
