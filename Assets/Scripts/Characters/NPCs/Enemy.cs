using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace EverdrivenDays
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(EnemyResizableCapsuleCollider))]
    [RequireComponent(typeof(EnemyGroundCheck))]
    public class Enemy : CharacterStats
    {
        public enum EnemyType
        {
            Small,
            Boss
        }

        [Header("Enemy Settings")]
        [SerializeField] private EnemyType enemyType = EnemyType.Small;
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private GameObject healthBarPrefab;
        [SerializeField] private float healthBarYOffset = 3.0f; // Editable in Inspector

        [Header("Movement")]
        [SerializeField] private float patrolRadius = 10f;
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float chaseSpeed = 3.5f;

        [Header("Combat")]
        [SerializeField] private int experienceReward = 50;
        [SerializeField] private int goldReward = 25;

        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Animator animator;

        private NavMeshAgent navMeshAgent;
        private Vector3 startPosition;
        private bool isPlayerDetected = false;
        private bool isDead = false;
        private float patrolTimer = 0f;
        private float patrolWaitTime = 3f;
        private bool canStartBattle = true; // Controls whether the enemy can start a new battle

        private EnemyResizableCapsuleCollider resizableCapsuleCollider;
        private EnemyGroundCheck groundCheck;
        private GameObject healthBarInstance;
        private bool isInCombat = false;
        private bool isKnockedBack = false;
        private GoblinAnimationController goblinAnim;

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

        // Static flags to track player combat state
        private static bool playerInRhythmCombat = false;
        private static float playerCombatIFrameEnd = 0f;

        protected override void Awake()
        {
            base.Awake();
            navMeshAgent = GetComponent<NavMeshAgent>();
            resizableCapsuleCollider = GetComponent<EnemyResizableCapsuleCollider>();
            groundCheck = GetComponent<EnemyGroundCheck>();
            goblinAnim = GetComponent<GoblinAnimationController>();

            // Ensure proper Rigidbody setup
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Use kinematic for NavMeshAgent control
                rb.freezeRotation = false; // Allow all rotations
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            // Double the speeds
            walkSpeed *= 2f;
            chaseSpeed *= 2f;

            // No need to configure the collider here as that's now done by EnemyResizableCapsuleCollider

            if (animator == null)
                animator = GetComponent<Animator>();

            startPosition = transform.position;

            // Create health bar
            CreateHealthBar();
        }

        protected override void Start()
        {
            base.Start();
            SetRandomDestination();
        }

        private void Update()
        {
            if (isDead) return;
            if (isInCombat) return;
            if (isKnockedBack) return;

            // If player is in rhythm combat or in i-frame, wait
            if (playerInRhythmCombat || Time.time < playerCombatIFrameEnd)
            {
                if (!navMeshAgent.isStopped)
                    navMeshAgent.isStopped = true;
                if (currentState != EnemyState.Waiting)
                    currentState = EnemyState.Waiting;
                return;
            }
            else if (navMeshAgent.isStopped)
            {
                navMeshAgent.isStopped = false;
                if (currentState == EnemyState.Waiting)
                    currentState = EnemyState.Patrolling;
            }

            // Animation state logic
            if (goblinAnim != null)
            {
                if (currentState == EnemyState.Patrolling || currentState == EnemyState.Waiting)
                    goblinAnim.PlayIdle();
                else if (currentState == EnemyState.Chasing)
                    goblinAnim.PlayRun();
            }

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
            // If player is in rhythm combat or in i-frame, wait
            if (playerInRhythmCombat || Time.time < playerCombatIFrameEnd)
            {
                navMeshAgent.isStopped = true;
                currentState = EnemyState.Waiting;
                return;
            }
            // Update destination to player position and set chase speed
            navMeshAgent.SetDestination(playerTransform.position);
            navMeshAgent.speed = chaseSpeed;

            // If within attack range, switch to attacking
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange)
            {
                currentState = EnemyState.Attacking;

                // Try to start battle if we're close enough and not on cooldown
                if (canStartBattle && !isInCombat)
                {
                    StartBattle();
                }
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

            // Update health bar
            UpdateHealthBar();

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
            isDead = true;
            if (goblinAnim != null) goblinAnim.PlayDeath();

            // Stop movement
            navMeshAgent.isStopped = true;

            // Disable colliders
            Collider[] colliders = GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }

            // Hide health bar
            if (healthBarInstance != null)
            {
                healthBarInstance.SetActive(false);
            }

            // Give rewards to player
            GiveRewardsToPlayer();

            // Respawn logic
            var respawner = GetComponent<EnemyRespawner>();
            if (respawner != null) respawner.OnEnemyDeath();

            // Destroy the enemy after a delay
            Destroy(gameObject, 3f);
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
        }

        // This method will be called when initiating the rhythm game battle
        public void StartBattle()
        {
            // Check if battle can be started (cooldown period)
            if (!canStartBattle)
            {
                Debug.Log("Enemy is on battle cooldown, cannot start battle yet");
                return;
            }

            Debug.Log("Starting battle with enemy");

            isInCombat = true;
            playerInRhythmCombat = true;

            // Stop movement during combat
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
            }

            // Different behavior based on enemy type
            if (enemyType == EnemyType.Small)
            {
                SmallEnemyRhythmController controller = FindObjectOfType<SmallEnemyRhythmController>();
                if (controller != null)
                {
                    Player player = FindObjectOfType<Player>();
                    controller.StartGame(this, player);
                }
                else
                {
                    CombatManager.Instance?.InitiateCombat(this);
                }
            }
            else // Boss
            {
                BossRhythmController bossController = FindObjectOfType<BossRhythmController>();
                if (bossController != null)
                {
                    Player player = FindObjectOfType<Player>();
                    // Set the current enemy reference for encounter text
                    var field = bossController.GetType().GetField("currentEnemy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) field.SetValue(bossController, this);
                    bossController.StartBossEncounter(player);
                }
                else
                {
                    CombatManager.Instance?.InitiateCombat(this);
                }
            }
        }

        // Called when combat ends
        public void OnCombatEnd(bool playerWon)
        {
            isInCombat = false;
            playerInRhythmCombat = false;
            playerCombatIFrameEnd = 0f; // Remove i-frame freeze for all enemies
            isPlayerDetected = false; // Force re-detection after combat
            // --- Fix: Reset all enemies' detection and state ---
            foreach (var enemy in FindObjectsOfType<Enemy>())
            {
                if (enemy == null || enemy == this) continue;
                enemy.isPlayerDetected = false;
                if (!enemy.isDead && enemy.navMeshAgent != null)
                {
                    enemy.navMeshAgent.isStopped = false;
                    enemy.currentState = EnemyState.Patrolling;
                }
            }
            // Resume movement if still alive
            if (!isDead && navMeshAgent != null)
            {
                navMeshAgent.isStopped = false;
                currentState = EnemyState.Patrolling;
                Debug.Log("Enemy resuming patrol after combat");
            }
            // Set a cooldown before the next battle can occur
            StartCoroutine(BattleCooldown());
        }

        // Cooldown timer for battles
        private IEnumerator BattleCooldown()
        {
            canStartBattle = false;
            Debug.Log("Battle cooldown started");

            yield return new WaitForSeconds(5f); // 5 second cooldown

            canStartBattle = true;
            Debug.Log("Battle cooldown ended, enemy can engage again");
        }

        // Reset the battle cooldown to allow immediate re-engagement
        public void ResetBattleCooldown()
        {
            StopAllCoroutines(); // Stop any existing cooldown
            canStartBattle = true;
        }

        // Apply knockback effect
        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (isDead)
            {
                Debug.Log("Cannot apply knockback - enemy is dead");
                return;
            }

            Debug.Log($"Enemy {name} - Starting knockback with direction {direction} and force {force}");

            // Start knockback coroutine
            StartCoroutine(KnockbackCoroutine(direction, force));
        }

        private IEnumerator KnockbackCoroutine(Vector3 direction, float force)
        {
            isKnockedBack = true;
            Debug.Log($"Enemy {name} - Knockback started");

            // Stop navigation agent
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                Debug.Log("Navigation agent stopped for knockback");
            }

            // Apply force
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Make sure rigidbody is not kinematic during knockback
                bool wasKinematic = rb.isKinematic;
                rb.isKinematic = false;

                // Apply the actual force
                rb.AddForce(direction * force, ForceMode.Impulse);
                Debug.Log($"Applied force: {direction * force} to enemy rigidbody");

                // Wait for knockback to complete
                yield return new WaitForSeconds(0.5f);

                // Restore original kinematic state
                rb.isKinematic = wasKinematic;
            }
            else
            {
                // If no rigidbody, just wait
                Debug.LogWarning("No rigidbody found on enemy for knockback");
                yield return new WaitForSeconds(0.5f);
            }

            isKnockedBack = false;
            Debug.Log($"Enemy {name} - Knockback completed");

            // Resume navigation if not dead or in combat
            if (!isDead && !isInCombat && navMeshAgent != null)
            {
                navMeshAgent.isStopped = false;
                Debug.Log("Navigation agent resumed after knockback");
            }
        }

        // Create health bar for the enemy
        private void CreateHealthBar()
        {
            if (healthBarPrefab != null)
            {
                CapsuleCollider capsule = GetComponent<CapsuleCollider>();
                float yOffset = healthBarYOffset;
                if (capsule != null && healthBarYOffset <= 0f)
                    yOffset = capsule.height * 1.5f; // fallback if not set
                Vector3 healthBarPosition = transform.position + Vector3.up * yOffset;
                healthBarInstance = Instantiate(healthBarPrefab, healthBarPosition, Quaternion.identity);
                healthBarInstance.transform.SetParent(transform);
                healthBarInstance.transform.localPosition = new Vector3(0, yOffset, 0);
                EnemyHealthBar healthBar = healthBarInstance.GetComponent<EnemyHealthBar>();
                if (healthBar != null)
                {
                    healthBar.SetTarget(this);
                    healthBar.SetYOffset(yOffset); // Set the offset in the health bar script
                }
                Debug.Log($"Created health bar at height offset: {yOffset}");
            }
        }

        // Update health bar
        private void UpdateHealthBar()
        {
            if (healthBarInstance != null)
            {
                // The EnemyHealthBar component will handle the update in its LateUpdate
                healthBarInstance.SetActive(true);
            }
        }

        // Properties
        public EnemyType Type => enemyType;

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

        // Reset enemy state for respawn system
        public void ResetEnemy()
        {
            isDead = false;
            currentHealth = maxHealth.Value;
            SetHealth(currentHealth);
            isInCombat = false;
            isKnockedBack = false;
            currentState = EnemyState.Patrolling;
            if (goblinAnim != null) goblinAnim.PlayIdle();
            // ...reset other state as needed...
        }
    }
}