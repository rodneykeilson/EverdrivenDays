using UnityEngine;
using UnityEngine.AI;

namespace EverdrivenDays
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(EnemyResizableCapsuleCollider))]
    [RequireComponent(typeof(EnemyGroundCheck))]
    public class Enemy : CharacterStats
    {
        [Header("Enemy Settings")]
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 2f;

        [Header("Movement")]
        [SerializeField] private float patrolRadius = 10f;
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float chaseSpeed = 3.5f;

        [Header("Combat")]
        [SerializeField] private int experienceReward = 50;
        [SerializeField] private int goldReward = 25;
        [SerializeField] private string[] possibleItemDrops;
        [SerializeField] private float itemDropChance = 0.3f;

        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Animator animator;

        private NavMeshAgent navMeshAgent;
        private Vector3 startPosition;
        private bool isPlayerDetected = false;
        private bool isDead = false;
        private float patrolTimer = 0f;
        private float patrolWaitTime = 3f;
        
        private EnemyResizableCapsuleCollider resizableCapsuleCollider;
        private EnemyGroundCheck groundCheck;
        
        // Enemy state machine (simplified)
        private enum EnemyState
        {
            Patrolling,
            Chasing,
            Attacking,
            Waiting,
            Dead
        }
        
        private EnemyState currentState = EnemyState.Patrolling;

        protected override void Awake()
        {
            base.Awake();
            navMeshAgent = GetComponent<NavMeshAgent>();
            resizableCapsuleCollider = GetComponent<EnemyResizableCapsuleCollider>();
            groundCheck = GetComponent<EnemyGroundCheck>();
            
            // Ensure proper Rigidbody setup
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false; // We need physics for ground detection
                rb.freezeRotation = true; // Prevent tipping over
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            
            // No need to configure the collider here as that's now done by EnemyResizableCapsuleCollider
            
            if (animator == null)
                animator = GetComponent<Animator>();
                
            startPosition = transform.position;
        }

        protected override void Start()
        {
            base.Start();
            SetRandomDestination();
        }

        private void Update()
        {
            if (isDead) return;

            switch (currentState)
            {
                case EnemyState.Patrolling:
                    Patrol();
                    break;
                case EnemyState.Chasing:
                    ChasePlayer();
                    break;
                case EnemyState.Attacking:
                    AttackPlayer();
                    break;
                case EnemyState.Waiting:
                    Wait();
                    break;
                case EnemyState.Dead:
                    // Do nothing when dead
                    break;
            }

            CheckForPlayerInRange();
        }

        private void Patrol()
        {
            // Set patrol speed
            navMeshAgent.speed = walkSpeed;
            
            // If close to destination or no path, get new random position
            if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance < 0.5f)
            {
                currentState = EnemyState.Waiting;
                patrolTimer = 0f;
            }
        }

        private void Wait()
        {
            patrolTimer += Time.deltaTime;
            
            if (patrolTimer >= patrolWaitTime)
            {
                SetRandomDestination();
                currentState = EnemyState.Patrolling;
            }
        }

        private void ChasePlayer()
        {
            if (playerTransform == null) return;
            
            // Update destination to player position and set chase speed
            navMeshAgent.SetDestination(playerTransform.position);
            navMeshAgent.speed = chaseSpeed;
            
            // If within attack range, switch to attacking
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange)
            {
                currentState = EnemyState.Attacking;
            }
            // If player is too far, go back to patrolling
            else if (distanceToPlayer > detectionRange)
            {
                isPlayerDetected = false;
                currentState = EnemyState.Patrolling;
                SetRandomDestination();
            }
        }

        private void AttackPlayer()
        {
            if (playerTransform == null) return;
            
            // Make sure we're facing the player
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            
            // Stop moving
            navMeshAgent.isStopped = true;
            
            // Play attack animation
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            
            // Check distance - if player moved out of range, chase again
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > attackRange)
            {
                navMeshAgent.isStopped = false;
                currentState = EnemyState.Chasing;
            }
        }

        private void CheckForPlayerInRange()
        {
            if (playerTransform == null)
            {
                // Try to find player if not assigned
                var player = FindAnyObjectByType<Player>();
                if (player != null)
                {
                    playerTransform = player.transform;
                }
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            // If player is in detection range and not already detected, start chasing
            if (distanceToPlayer <= detectionRange && !isPlayerDetected)
            {
                isPlayerDetected = true;
                currentState = EnemyState.Chasing;
            }
        }

        private void SetRandomDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += startPosition;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            {
                navMeshAgent.SetDestination(hit.position);
            }
        }

        // Override the TakeDamage method from CharacterStats
        public new bool TakeDamage(int damage)
        {
            if (isDead) return false;
            
            bool died = base.TakeDamage(damage);
            
            // Play hit animation
            if (animator != null)
            {
                animator.SetTrigger("Hit");
            }
            
            if (died)
            {
                Die();
                return true; // Enemy died
            }
            
            return false; // Enemy still alive
        }

        // Override the Die method from CharacterStats
        protected override void Die()
        {
            base.Die();
            
            isDead = true;
            currentState = EnemyState.Dead;
            
            // Stop movement
            navMeshAgent.isStopped = true;
            
            // Play death animation
            if (animator != null)
            {
                animator.SetTrigger("Death");
            }
            
            // Disable colliders
            Collider[] colliders = GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
            
            // Give rewards to player
            GiveRewardsToPlayer();
            
            // Enemy will be removed after the complete rhythm game
        }
        
        private void GiveRewardsToPlayer()
        {
            var player = FindAnyObjectByType<Player>();
            if (player == null) return;
            
            // Get the player's stats component
            CharacterStats playerStats = player.GetComponent<CharacterStats>();
            if (playerStats == null) return;
            
            // Give experience
            playerStats.AddExperience(experienceReward);
            
            // Give gold
            playerStats.AddGold(goldReward);
            
            // Random chance to drop an item
            if (possibleItemDrops != null && possibleItemDrops.Length > 0 && Random.value <= itemDropChance)
            {
                string randomItemId = possibleItemDrops[Random.Range(0, possibleItemDrops.Length)];
                
                // Try to get the inventory system
                InventorySystem inventory = InventorySystem.Instance;
                if (inventory != null)
                {
                    inventory.AddItem(randomItemId);
                }
            }
        }

        // This method will be called when initiating the rhythm game battle
        public void StartBattle()
        {
            // Will be implemented to transition to rhythm game mode
            CombatManager.Instance?.InitiateCombat(this);
        }
        
        // For debugging
        private void OnDrawGizmosSelected()
        {
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Patrol radius
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(startPosition, patrolRadius);
        }
    }
} 