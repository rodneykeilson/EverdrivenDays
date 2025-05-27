using System.Collections;
using UnityEngine;

namespace EverdrivenDays
{
    public class CombatEffectsManager : MonoBehaviour
    {
        [Header("Death Effect")]
        [SerializeField] private GameObject deathEffectPrefab;
        [SerializeField] private float deathEffectDuration = 2f;
        
        [Header("Knockback Effect")]
        [SerializeField] private GameObject knockbackEffectPrefab;
        [SerializeField] private float knockbackEffectDuration = 1f;
        
        [Header("Encounter Effect")]
        [SerializeField] private GameObject encounterEffectPrefab;
        [SerializeField] private float encounterEffectDuration = 1.5f;
        
        [Header("Impact Effect")]
        [SerializeField] private GameObject impactEffectPrefab;
        [SerializeField] private float impactEffectDuration = 1f;
        
        // Singleton pattern
        public static CombatEffectsManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            Debug.Log("CombatEffectsManager initialized");
            
            // Log available effects
            if (deathEffectPrefab != null) Debug.Log("Death effect prefab is assigned");
            if (knockbackEffectPrefab != null) Debug.Log("Knockback effect prefab is assigned");
            if (encounterEffectPrefab != null) Debug.Log("Encounter effect prefab is assigned");
            if (impactEffectPrefab != null) Debug.Log("Impact effect prefab is assigned");
        }
        
        /// <summary>
        /// Creates a death explosion effect at the specified position
        /// </summary>
        public void PlayDeathEffect(Vector3 position)
        {
            if (deathEffectPrefab == null) return;
            
            GameObject effectInstance = Instantiate(deathEffectPrefab, position, Quaternion.identity);
            Destroy(effectInstance, deathEffectDuration);
        }
        
        /// <summary>
        /// Creates a knockback impact effect at the specified position
        /// </summary>
        public void PlayKnockbackEffect(Vector3 position)
        {
            if (knockbackEffectPrefab == null) return;
            
            GameObject effectInstance = Instantiate(knockbackEffectPrefab, position, Quaternion.identity);
            Destroy(effectInstance, knockbackEffectDuration);
        }
        
        /// <summary>
        /// Creates an encounter effect at the specified position
        /// </summary>
        public void PlayEncounterEffect(Vector3 position)
        {
            if (encounterEffectPrefab == null) return;
            
            GameObject effectInstance = Instantiate(encounterEffectPrefab, position, Quaternion.identity);
            Destroy(effectInstance, encounterEffectDuration);
        }
        
        /// <summary>
        /// Apply knockback to an enemy with visual effect
        /// </summary>
        public void ApplyKnockbackWithEffect(Enemy enemy, Vector3 direction, float force)
        {
            if (enemy == null) return;
            
            // Apply knockback to enemy
            enemy.ApplyKnockback(direction, force);
            
            // Play knockback effect
            PlayKnockbackEffect(enemy.transform.position);
        }
        
        /// <summary>
        /// Creates an impact effect at the specified position (used for rhythm game hits)
        /// </summary>
        public void PlayImpactEffect(Vector3 position)
        {
            Debug.Log($"Playing impact effect at {position}");
            
            // If impact effect prefab is not set, use knockback effect as fallback
            GameObject prefabToUse = impactEffectPrefab != null ? impactEffectPrefab : knockbackEffectPrefab;
            
            if (prefabToUse == null)
            {
                Debug.LogWarning("No impact effect or knockback effect prefab assigned!");
                return;
            }
            
            GameObject effectInstance = Instantiate(prefabToUse, position, Quaternion.identity);
            Destroy(effectInstance, impactEffectPrefab != null ? impactEffectDuration : knockbackEffectDuration);
        }
    }
}
