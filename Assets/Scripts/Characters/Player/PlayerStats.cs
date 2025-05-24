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
            rhythmAccuracyBonus = 1.0f + accuracy;
        }
        
        public int CalculateDamage(int baseDamage)
        {
            // Calculate damage based on attack power, combo, and rhythm performance
            float comboDamageMultiplier = 1.0f + (currentComboCount * 0.2f);
            
            int finalDamage = Mathf.RoundToInt(baseDamage * AttackPower / 10f * comboDamageMultiplier * rhythmAccuracyBonus);
            
            // Add critical chance
            if (Random.Range(0, 100) < CritChance)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * (CritDamage / 100f));
                Debug.Log("Critical hit!");
            }
            
            return finalDamage;
        }
        
        protected override void Die()
        {
            base.Die();
            
            // Handle player death
            Debug.Log("Player died!");
            
            // Don't destroy the player, instead trigger game over
            // GameManager.Instance?.GameOver();
        }
        
        protected override void ApplyLevelUpBonuses()
        {
            base.ApplyLevelUpBonuses();
            
            // Additional bonuses for player
            maxStamina += 5;
            currentStamina = maxStamina; // Refill stamina on level up
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