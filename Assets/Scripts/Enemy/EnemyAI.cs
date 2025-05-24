using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace EverdrivenDays
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Animator animator;
        
        [Header("Detection Settings")]
        [SerializeField] private float detectionRadius = 10f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float fieldOfViewAngle = 120f;
        [SerializeField] private LayerMask obstacleLayerMask;
        
        [Header("Movement Settings")]
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField] private float rotationSpeed = 5f;
        
        [Header("Patrol Settings")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolWaitTime = 2f;
        [SerializeField] private bool randomPatrol = true;
        
        [Header("Combat")]
        [SerializeField] private int enemyDifficulty = 3; // 1-10 scale
        [SerializeField] private float combatCooldown = 5f; // Time before can engage in combat again
        
        // State
        private NavMeshAgent navAgent;
        private EnemyState currentState = EnemyState.Patrolling;
        private int currentPatrolIndex = 0;
        private float lastStateChangeTime;
        private float lastCombatTime = -1000f; // Initialize to allow immediate combat
        private Vector3 lastKnownPlayerPosition;
        
        // Animation parameters
        private static readonly int IsWalking = Animator.StringToHash("Walk");
        private static readonly int IsChasing = Animator.StringToHash("Chase");
        private static readonly int Attack = Animator.StringToHash("Attack");
        
        // References
        private RhythmGameController rhythmGameController;
        private CombatManager combatManager;
        private Enemy enemyComponent;
        
        // Properties for external access
        public EnemyState State => currentState;
        public bool CanEnterCombat => Time.time > lastCombatTime + combatCooldown;
        
        public enum EnemyState
        {
            Patrolling,
            Chasing,
            Searching,
            Attacking,
            Stunned,
            InCombat
        }
        
        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            
            if (animator == null)
                animator = GetComponent<Animator>();
            
            // Find player if not set
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
            }
            
            // Find game controllers
            rhythmGameController = FindAnyObjectByType<RhythmGameController>();
            combatManager = FindAnyObjectByType<CombatManager>();
            enemyComponent = GetComponent<Enemy>();
            
            if (enemyComponent == null)
            {
                Debug.LogWarning("Enemy component not found on GameObject. Make sure to add this controller to an object with the Enemy component.");
            }
            
            lastStateChangeTime = Time.time;
        }
        
        private void Start()
        {
            // Set initial state
            ChangeState(EnemyState.Patrolling);
            
            // Set up NavMeshAgent
            navAgent.speed = patrolSpeed;
            navAgent.angularSpeed = rotationSpeed * 100;
            navAgent.acceleration = 8;
            
            // Go to first patrol point if available
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                navAgent.SetDestination(patrolPoints[0].position);
            }
        }
        
        private void Update()
        {
            // Handle current state
            switch (currentState)
            {
                case EnemyState.Patrolling:
                    HandlePatrolling();
                    CheckForPlayerDetection();
                    break;
                    
                case EnemyState.Chasing:
                    HandleChasing();
                    break;
                    
                case EnemyState.Searching:
                    HandleSearching();
                    CheckForPlayerDetection();
                    break;
                    
                case EnemyState.Attacking:
                    HandleAttacking();
                    break;
                    
                case EnemyState.Stunned:
                    HandleStunned();
                    break;
                    
                case EnemyState.InCombat:
                    // Nothing to do here - managed by combat system
                    break;
            }
            
            // Update animation
            UpdateAnimation();
        }
        
        private void HandlePatrolling()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return;
                
            // Check if we've reached the patrol point
            if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
            {
                // Wait at the patrol point
                if (Time.time - lastStateChangeTime < patrolWaitTime)
                    return;
                    
                // Move to next patrol point
                currentPatrolIndex = randomPatrol ? 
                    UnityEngine.Random.Range(0, patrolPoints.Length) : 
                    (currentPatrolIndex + 1) % patrolPoints.Length;
                    
                navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
                lastStateChangeTime = Time.time;
            }
        }
        
        private void HandleChasing()
        {
            if (playerTransform == null)
                return;
                
            // Update last known position
            if (CanSeePlayer())
            {
                lastKnownPlayerPosition = playerTransform.position;
                navAgent.SetDestination(lastKnownPlayerPosition);
            }
            
            // Check if we're close enough to attack
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange && CanSeePlayer())
            {
                ChangeState(EnemyState.Attacking);
                return;
            }
            
            // Check if we've lost the player
            if (!CanSeePlayer() && navAgent.remainingDistance < 0.5f)
            {
                ChangeState(EnemyState.Searching);
                return;
            }
        }
        
        private void HandleSearching()
        {
            // If we've been searching for a while, go back to patrolling
            if (Time.time - lastStateChangeTime > 5f)
            {
                ChangeState(EnemyState.Patrolling);
            }
        }
        
        private void HandleAttacking()
        {
            if (playerTransform == null)
                return;
                
            // Face the player
            Vector3 direction = playerTransform.position - transform.position;
            direction.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // Start combat if we can
            if (CanEnterCombat)
            {
                StartCombat();
            }
            else
            {
                // Play attack animation
                if (animator != null)
                {
                    animator.SetTrigger(Attack);
                }
                
                // Go back to chasing after attack animation
                Invoke(nameof(ReturnToChasing), 1.5f);
            }
        }
        
        private void HandleStunned()
        {
            // Return to patrolling after stun time
            if (Time.time - lastStateChangeTime > 3f)
            {
                ChangeState(EnemyState.Patrolling);
            }
        }
        
        private void ReturnToChasing()
        {
            if (currentState == EnemyState.Attacking)
            {
                ChangeState(EnemyState.Chasing);
            }
        }
        
        private void StartCombat()
        {
            ChangeState(EnemyState.InCombat);
            lastCombatTime = Time.time;
            
            // Use existing enemy StartBattle method if available
            if (enemyComponent != null)
            {
                // Call StartBattle if the method exists
                enemyComponent.SendMessage("StartBattle", SendMessageOptions.DontRequireReceiver);
                return;
            }
            
            // Fallback to using the combat manager
            if (combatManager != null)
            {
                combatManager.SendMessage("InitiateCombat", enemyComponent, SendMessageOptions.DontRequireReceiver);
            }
            else if (rhythmGameController != null)
            {
                // Direct initiation with rhythm game if no combat manager
                rhythmGameController.StartGame(enemyDifficulty);
            }
        }
        
        private void CheckForPlayerDetection()
        {
            if (playerTransform == null)
                return;
                
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            // Check if player is within detection radius
            if (distanceToPlayer <= detectionRadius && CanSeePlayer())
            {
                lastKnownPlayerPosition = playerTransform.position;
                ChangeState(EnemyState.Chasing);
            }
        }
        
        private bool CanSeePlayer()
        {
            if (playerTransform == null)
                return false;
                
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            
            // Check if player is within field of view
            if (angle > fieldOfViewAngle * 0.5f)
                return false;
                
            // Cast ray to check for obstacles
            return !Physics.Raycast(transform.position, directionToPlayer.normalized, directionToPlayer.magnitude, obstacleLayerMask);
        }
        
        private void ChangeState(EnemyState newState)
        {
            // Don't change state if we're the same
            if (currentState == newState)
                return;
                
            // Exit current state
            switch (currentState)
            {
                case EnemyState.Patrolling:
                    break;
                    
                case EnemyState.Chasing:
                    break;
                    
                case EnemyState.Searching:
                    break;
                    
                case EnemyState.Attacking:
                    CancelInvoke(nameof(ReturnToChasing));
                    break;
                    
                case EnemyState.InCombat:
                    break;
            }
            
            // Enter new state
            switch (newState)
            {
                case EnemyState.Patrolling:
                    navAgent.speed = patrolSpeed;
                    
                    if (patrolPoints != null && patrolPoints.Length > 0)
                    {
                        currentPatrolIndex = randomPatrol ? 
                            UnityEngine.Random.Range(0, patrolPoints.Length) : 
                            0;
                            
                        navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
                    }
                    break;
                    
                case EnemyState.Chasing:
                    navAgent.speed = chaseSpeed;
                    if (playerTransform != null)
                    {
                        navAgent.SetDestination(playerTransform.position);
                    }
                    break;
                    
                case EnemyState.Searching:
                    navAgent.speed = patrolSpeed;
                    // Walk to last known player position
                    navAgent.SetDestination(lastKnownPlayerPosition);
                    break;
                    
                case EnemyState.Attacking:
                    navAgent.ResetPath();
                    break;
                    
                case EnemyState.Stunned:
                    navAgent.ResetPath();
                    break;
                    
                case EnemyState.InCombat:
                    navAgent.ResetPath();
                    break;
            }
            
            currentState = newState;
            lastStateChangeTime = Time.time;
        }
        
        private void UpdateAnimation()
        {
            if (animator == null)
                return;
                
            bool isMoving = navAgent.velocity.magnitude > 0.1f;
            bool isRunning = currentState == EnemyState.Chasing;
            
            // Check if parameters exist using TryGetParameter instead of HasParameter
            try
            {
                // Try to set the parameters, catch any exceptions if they don't exist
                animator.SetBool(IsWalking, isMoving && !isRunning);
                animator.SetBool(IsChasing, isRunning);
            }
            catch (Exception)
            {
                // Parameter doesn't exist, use some fallback animations if needed
                // or just continue silently
            }
        }
        
        public void OnCombatEnd(bool playerWon)
        {
            // Return from combat state
            if (currentState == EnemyState.InCombat)
            {
                if (playerWon)
                {
                    // Player won - enemy dies or gets stunned
                    ChangeState(EnemyState.Stunned);
                    
                    // If the enemy has the TakeDamage method, call it with a large value to kill it
                    if (enemyComponent != null)
                    {
                        enemyComponent.SendMessage("TakeDamage", 9999, SendMessageOptions.DontRequireReceiver);
                    }
                }
                else
                {
                    // Enemy won - resume chasing
                    ChangeState(EnemyState.Chasing);
                }
            }
        }
        
        // Called when the player successfully defends/parries
        public void Stun()
        {
            ChangeState(EnemyState.Stunned);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw field of view
            Gizmos.color = Color.blue;
            Vector3 rightDir = Quaternion.Euler(0, fieldOfViewAngle * 0.5f, 0) * transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -fieldOfViewAngle * 0.5f, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, rightDir * detectionRadius);
            Gizmos.DrawRay(transform.position, leftDir * detectionRadius);
            
            // Draw patrol path if available
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        // Draw point
                        Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                        
                        // Draw line to next point
                        if (patrolPoints.Length > 1)
                        {
                            int nextIndex = (i + 1) % patrolPoints.Length;
                            if (patrolPoints[nextIndex] != null)
                            {
                                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                            }
                        }
                    }
                }
            }
        }
    }
} 