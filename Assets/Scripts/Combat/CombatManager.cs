using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EverdrivenDays
{
    public class CombatManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Player player;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform combatUICanvas;
        [SerializeField] private RhythmGameController rhythmGameController;
        [SerializeField] private GameObject combatTransitionEffect;
        [SerializeField] private GameObject combatUI;
        
        [Header("Settings")]
        [SerializeField] private float transitionDuration = 1.0f;
        
        [Header("Camera")]
        [SerializeField] private Camera combatCamera;
        
        [Header("SFX")]
        [SerializeField] private AudioClip combatStartSFX;
        [SerializeField] private AudioClip combatWinSFX;
        [SerializeField] private AudioClip combatLoseSFX;
        
        private Enemy currentEnemy;
        private bool inCombat = false;
        private int currentDifficulty = 1;
        private AudioSource audioSource;
        
        // Singleton pattern for easy access
        public static CombatManager Instance { get; private set; }
        
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            // Only use DontDestroyOnLoad if absolutely necessary
            // DontDestroyOnLoad(gameObject);
            
            // Setup references if not assigned
            if (mainCamera == null)
                mainCamera = Camera.main;
                
            if (player == null)
                player = FindAnyObjectByType<Player>();
                
            if (combatUICanvas == null)
                combatUICanvas = transform.Find("CombatUICanvas");
                
            if (rhythmGameController == null)
                rhythmGameController = GetComponentInChildren<RhythmGameController>();
            
            // Find components if not set
            if (combatCamera == null)
            {
                combatCamera = FindAnyObjectByType<Camera>();
            }
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Set up initial state
            SetupInitialState();
        }
        
        private void SetupInitialState()
        {
            if (combatUI != null)
            {
                combatUI.SetActive(false);
            }
            
            if (combatTransitionEffect != null)
            {
                combatTransitionEffect.SetActive(false);
            }
            
            // Ensure main camera is active and has proper depth
            if (mainCamera != null)
            {
                mainCamera.gameObject.SetActive(true);
                mainCamera.depth = 0; // Use default depth
            }
            
            // Ensure combat camera is inactive until needed
            if (combatCamera != null)
            {
                combatCamera.gameObject.SetActive(false);
            }
            
            // Reset combat state
            inCombat = false;
        }
        
        private void Start()
        {
            // Make sure UI is disabled at start
            if (combatUICanvas != null)
                combatUICanvas.gameObject.SetActive(false);
                
            if (rhythmGameController != null)
                rhythmGameController.gameObject.SetActive(false);
            
            // Subscribe to rhythm game events
            if (rhythmGameController != null)
            {
                // Set up listener for when rhythm game ends
                StartCoroutine(WaitForRhythmGameController());
            }
            else
            {
                Debug.LogWarning("No RhythmGameController found. Combat transitions won't work properly.");
            }
        }
        
        private IEnumerator WaitForRhythmGameController()
        {
            // Wait until rhythm game controller is fully initialized
            yield return new WaitForSeconds(0.5f);
            
            // Check on a regular interval if the rhythm game has ended
            while (true)
            {
                if (inCombat && rhythmGameController != null && !rhythmGameController.IsGameActive)
                {
                    // Rhythm game has ended
                    bool playerWon = rhythmGameController.PlayerWon;
                    EndCombat(playerWon);
                    yield return new WaitForSeconds(1f);
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // Called by the player when attacking an enemy
        public void InitiateCombat(Enemy enemy)
        {
            if (inCombat) return;
            
            currentEnemy = enemy;
            inCombat = true;
            
            // Freeze the player and enemy
            if (player != null)
            {
                // Disable player input
                player.Input.InputActions.Disable();
            }
            
            StartCoroutine(TransitionToCombat());
        }
        
        private IEnumerator TransitionToCombat()
        {
            // Play transition sound
            if (audioSource != null && combatStartSFX != null)
            {
                audioSource.PlayOneShot(combatStartSFX);
            }
            
            // Show transition effect
            if (combatTransitionEffect != null)
            {
                combatTransitionEffect.SetActive(true);
            }
            
            // Pause player and enemy movement
            if (player != null)
            {
                // Disable player input
                player.Input.InputActions.Disable();
            }
            
            // Wait for transition
            yield return new WaitForSeconds(transitionDuration);
            
            // Switch cameras if applicable - but don't disable the main camera
            if (mainCamera != null && combatCamera != null)
            {
                // Don't set negative depth, just reduce priority
                mainCamera.depth = 0;
                
                // Activate combat camera with higher depth
                combatCamera.gameObject.SetActive(true);
                combatCamera.depth = 1; // Bring combat camera to foreground
            }
            
            // Show combat UI
            if (combatUICanvas != null)
                combatUICanvas.gameObject.SetActive(true);
                
            if (rhythmGameController != null)
            {
                rhythmGameController.gameObject.SetActive(true);
                
                // Setup difficulty based on enemy level
                currentDifficulty = 1; // Default
                if (currentEnemy != null)
                {
                    // TODO: Get enemy level
                }
                
                // Start the rhythm game
                rhythmGameController.StartGame(currentDifficulty);
            }
            
            // Hide transition effect
            if (combatTransitionEffect != null)
            {
                combatTransitionEffect.SetActive(false);
            }
            
            // Wait for rhythm game to complete
            while (rhythmGameController.IsGameActive)
            {
                yield return null;
            }
            
            // Handle results
            bool playerWon = rhythmGameController.PlayerWon;
            
            if (playerWon)
            {
                // Award experience and items
                // TODO: Implement rewards
                
                // Kill the enemy
                if (currentEnemy != null)
                {
                    currentEnemy.TakeDamage(9999); // Ensure enemy dies
                }
            }
            else
            {
                // Player lost - handle consequences
                // TODO: Implement consequences
            }
            
            // Return to normal gameplay
            yield return StartCoroutine(TransitionToNormalGameplay());
        }
        
        private IEnumerator TransitionToNormalGameplay()
        {
            // This method is just a wrapper for TransitionFromCombat
            // for backward compatibility
            yield return StartCoroutine(TransitionFromCombat(true));
        }
        
        private void EndCombat(bool playerWon)
        {
            if (!inCombat)
                return;
                
            // Start transition back
            StartCoroutine(TransitionFromCombat(playerWon));
        }
        
        private IEnumerator TransitionFromCombat(bool playerWon)
        {
            // Play win/lose sound
            if (audioSource != null)
            {
                AudioClip clip = playerWon ? combatWinSFX : combatLoseSFX;
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }
            
            // Show transition effect
            if (combatTransitionEffect != null)
            {
                combatTransitionEffect.SetActive(true);
            }
            
            // Wait a bit
            yield return new WaitForSeconds(transitionDuration);
            
            // Switch cameras back
            if (mainCamera != null && combatCamera != null)
            {
                // Ensure main camera is active with normal depth
                mainCamera.gameObject.SetActive(true);
                mainCamera.depth = 0;
                
                // Deactivate combat camera
                combatCamera.gameObject.SetActive(false);
            }
            
            // Hide combat UI
            if (combatUICanvas != null)
                combatUICanvas.gameObject.SetActive(false);
                
            if (rhythmGameController != null)
                rhythmGameController.gameObject.SetActive(false);
            
            // Hide transition effect
            if (combatTransitionEffect != null)
            {
                combatTransitionEffect.SetActive(false);
            }
            
            // Apply combat results
            ApplyCombatResults(playerWon);
            
            // Let the enemy know combat ended - use SendMessage for compatibility
            if (currentEnemy != null)
            {
                // Use SendMessage to invoke OnCombatEnd if it exists
                currentEnemy.SendMessage("OnCombatEnd", playerWon, SendMessageOptions.DontRequireReceiver);
                
                // Also check for EnemyAIController component and notify it
                var aiController = currentEnemy.GetComponent<EnemyAI>();
                if (aiController != null)
                {
                    aiController.OnCombatEnd(playerWon);
                }
            }
            
            // Re-enable player input
            if (player != null)
            {
                player.Input.InputActions.Enable();
            }
            
            // Reset combat state
            inCombat = false;
            currentEnemy = null;
        }
        
        private void ApplyCombatResults(bool playerWon)
        {
            if (playerWon)
            {
                // Player won - apply rewards, damage enemy, etc.
                if (currentEnemy != null)
                {
                    // Apply damage to enemy or destroy
                    // This would connect to your enemy health system
                }
                
                // Grant rewards, experience, etc.
                // Connect to your player progression system
            }
            else
            {
                // Player lost - apply consequences
                if (player != null)
                {
                    // Player takes damage
                    // Connect to your player health system
                }
            }
        }
        
        // Helper function to check if combat is active
        public bool IsInCombat()
        {
            return inCombat;
        }
        
        // For testing: Force start combat with a specific difficulty
        public void StartTestCombat(int difficulty)
        {
            if (Application.isEditor && !inCombat)
            {
                currentDifficulty = difficulty;
                StartCoroutine(TransitionToCombat());
            }
        }
    }
} 