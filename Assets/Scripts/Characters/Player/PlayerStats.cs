using UnityEngine;

namespace EverdrivenDays
{
    public class PlayerStats : CharacterStats
    {
        [Header("Player-Specific Stats")]
        [SerializeField] private float staminaRegenRate = 10f;
        [SerializeField] private float manaRegenRate = 5f;
        [SerializeField] private int maxStamina = 100;
        [SerializeField] private int currentStamina;
        
        [Header("Combat")]
        [SerializeField] private float attackCooldown = 0.5f;
        [SerializeField] private float comboWindow = 1.5f;
        [SerializeField] private int maxCombo = 3;
        
        // Rhythm game stats
        [SerializeField] private float rhythmAccuracyBonus = 1.0f; // Multiplier for damage based on rhythm performance
        
        // Events
        public System.Action<int, int> OnStaminaChanged; // currentStamina, maxStamina
        
        // Timers
        private float lastAttackTime;
        private float comboResetTime;
        private int currentComboCount;
        
        private bool lockBaseHP = false;
        private int lockedBaseHP = 0;

        protected override void Awake()
        {
            base.Awake();
            
            // Initialize stamina
            currentStamina = maxStamina;
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Hook up to inventory system
            InventorySystem inventorySystem = InventorySystem.Instance;
            if (inventorySystem != null)
            {
                inventorySystem.OnItemEquipped += HandleItemEquipped;
                inventorySystem.OnItemUnequipped += HandleItemUnequipped;
            }
        }
        
        private void Update()
        {
            // Regen stamina
            if (currentStamina < maxStamina)
            {
                RegenerateStamina(staminaRegenRate * Time.deltaTime);
            }
            
            // Regen mana
            if (CurrentMana < MaxMana)
            {
                RegenerateMana(manaRegenRate * Time.deltaTime);
            }
            
            // Reset combo if time expired
            if (currentComboCount > 0 && Time.time > comboResetTime)
            {
                ResetCombo();
            }
        }
        
        private void OnDestroy()
        {
            // Unhook from inventory system
            InventorySystem inventorySystem = InventorySystem.Instance;
            if (inventorySystem != null)
            {
                inventorySystem.OnItemEquipped -= HandleItemEquipped;
                inventorySystem.OnItemUnequipped -= HandleItemUnequipped;
            }
        }
        
        // Stamina management
        public void UseStamina(int amount)
        {
            currentStamina = Mathf.Max(0, currentStamina - amount);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
        
        public void RegenerateStamina(float amount)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + Mathf.RoundToInt(amount));
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
        
        public void RegenerateMana(float amount)
        {
            SetMana(CurrentMana + Mathf.RoundToInt(amount));
        }
        
        // Combat methods
        public bool CanAttack()
        {
            return Time.time >= lastAttackTime + attackCooldown && currentStamina > 0;
        }
        
        public void AttackPerformed(int staminaCost)
        {
            lastAttackTime = Time.time;
            UseStamina(staminaCost);
            
            // Increment combo
            currentComboCount = Mathf.Min(currentComboCount + 1, maxCombo);
            comboResetTime = Time.time + comboWindow;
        }
        
        public void ResetCombo()
        {
            currentComboCount = 0;
        }
        
        public int GetComboCount()
        {
            return currentComboCount;
        }
        
        // Apply rhythm game performance to damage
        public void SetRhythmAccuracyBonus(float accuracy)
        {
            // Accuracy is 0-1 where 1 is perfect
            // Scale it to a range of 1.0-2.0 for the damage multiplier
            if (accuracy <= 0f)
            {
                Debug.LogWarning("rhythmAccuracyBonus was not set. Defaulting to 1.0.");
                rhythmAccuracyBonus = 1.0f;
            }
            else
            {
                rhythmAccuracyBonus = 1.0f + accuracy;
            }
        }

        // Calculate damage based on attack power, combo, and rhythm performance
        // New: Overload for rhythm results
        public int CalculateDamage(int baseDamage, bool fullCombo, bool allPerfect)
        {
            float attackFactor = baseDamage * AttackPower / 10f;
            float finalDamage = attackFactor;

            if (allPerfect)
            {
                finalDamage *= 5f;
                Debug.Log("[DMG] All Perfect! Quintuple damage.");
            }
            else if (fullCombo)
            {
                finalDamage *= 2f;
                Debug.Log("[DMG] Full Combo! Double damage.");
            }
            else
            {
                float accuracyBonus = rhythmAccuracyBonus;
                if (accuracyBonus <= 0f)
                {
                    Debug.LogWarning("rhythmAccuracyBonus was not set. Defaulting to 1.0.");
                    accuracyBonus = 1.0f;
                }
                finalDamage *= accuracyBonus;
                Debug.Log($"[DMG] Partial combo. Scaled by accuracy: {accuracyBonus}");
            }

            // Optionally, log score if available
            int score = -1;
            if (this.GetType().GetMethod("GetLastRhythmScore") != null)
            {
                score = (int)this.GetType().GetMethod("GetLastRhythmScore").Invoke(this, null);
                Debug.Log($"[DMG] Rhythm Score included in calculation: {score}");
            }
            else
            {
                Debug.LogWarning("[DMG] No rhythm score found in PlayerStats. Only using accuracy bonus.");
            }

            // Add critical chance
            if (Random.Range(0, 100) < CritChance)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * (CritDamage / 100f));
                Debug.Log("[DMG] Critical hit! CritDamage multiplier: " + (CritDamage / 100f));
            }

            Debug.Log($"[DMG] Final damage dealt: {finalDamage}");
            return Mathf.RoundToInt(finalDamage);
        }

        // Keep the old version for compatibility if needed
        public int CalculateDamage(int baseDamage)
        {
            return CalculateDamage(baseDamage, false, false);
        }
        
        protected override void Die()
        {
            base.Die();
            Debug.Log("Player died!");
            // Trigger respawn sequence via UIManager
            if (UIManager.Instance != null)
            {
                UIManager.Instance.PlayerDeathSequence();
            }
        }

        // Call this on respawn to fully restore player
        public void FullyRestore()
        {
            SetHealth(MaxHealth);
            SetMana(MaxMana);
            currentStamina = maxStamina;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
        
        // Call this to set the base HP and lock it (used by difficulty system)
        public void SetAndLockBaseHP(int hp)
        {
            baseHealth = hp;
            maxHealth.BaseValue = hp;
            lockBaseHP = true;
            lockedBaseHP = hp;
            SetHealth(hp);
        }

        protected override void ApplyLevelUpBonuses()
        {
            base.ApplyLevelUpBonuses();
            
            // Additional bonuses for player
            maxStamina += 5;
            currentStamina = maxStamina; // Refill stamina on level up
            // Prevent HP from increasing if locked by difficulty
            if (lockBaseHP)
            {
                baseHealth = lockedBaseHP;
                maxHealth.BaseValue = lockedBaseHP;
            }
        }
        
        // Inventory integration
        private void HandleItemEquipped(InventoryItem item)
        {
            // Apply stat bonuses from the item
            foreach (var statPair in item.stats)
            {
                ApplyItemStatBonus(statPair.Key, statPair.Value);
            }
        }
        
        private void HandleItemUnequipped(InventoryItem item)
        {
            // Remove stat bonuses from the item
            foreach (var statPair in item.stats)
            {
                RemoveItemStatBonus(statPair.Key, statPair.Value);
            }
        }
        
        // Getters
        public int CurrentStamina => currentStamina;
        public int MaxStamina => maxStamina;
        public float RhythmAccuracyBonus => rhythmAccuracyBonus;
    }
}