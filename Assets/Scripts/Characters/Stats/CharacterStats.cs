using System;
using UnityEngine;

namespace EverdrivenDays
{
    [Serializable]
    public class Stat
    {
        [SerializeField] private int baseValue;
        private int modifiedValue;
        
        public int BaseValue 
        { 
            get => baseValue;
            set 
            {
                baseValue = value;
                CalculateModifiedValue();
            }
        }
        
        public int Value => modifiedValue;
        
        private int bonusFromItems;
        private int bonusFromBuffs;
        private int bonusFromLevel;
        
        public Stat(int baseValue)
        {
            this.baseValue = baseValue;
            CalculateModifiedValue();
        }
        
        public void AddItemBonus(int bonus)
        {
            bonusFromItems += bonus;
            CalculateModifiedValue();
        }
        
        public void RemoveItemBonus(int bonus)
        {
            bonusFromItems -= bonus;
            CalculateModifiedValue();
        }
        
        public void AddBuffBonus(int bonus)
        {
            bonusFromBuffs += bonus;
            CalculateModifiedValue();
        }
        
        public void RemoveBuffBonus(int bonus)
        {
            bonusFromBuffs -= bonus;
            CalculateModifiedValue();
        }
        
        public void SetLevelBonus(int bonus)
        {
            bonusFromLevel = bonus;
            CalculateModifiedValue();
        }
        
        private void CalculateModifiedValue()
        {
            modifiedValue = baseValue + bonusFromItems + bonusFromBuffs + bonusFromLevel;
            modifiedValue = Mathf.Max(0, modifiedValue); // Never negative
        }
    }
    
    public class CharacterStats : MonoBehaviour
    {
        [Header("Basic Stats")]
        [SerializeField] private string characterName = "Character";
        [SerializeField] private int level = 1;
        [SerializeField] private int experiencePoints = 0;
        [SerializeField] private int experienceToNextLevel = 100;
        
        [Header("Health and Resources")]
        [SerializeField] protected int baseHealth = 1000; // Editable in Inspector
        [SerializeField] protected Stat maxHealth = new Stat(100);
        [SerializeField] protected int currentHealth;
        [SerializeField] protected Stat maxMana = new Stat(50);
        [SerializeField] private int currentMana;
        
        [Header("Core Stats")]
        [SerializeField] private Stat strength = new Stat(10); // Physical damage, carry weight
        [SerializeField] private Stat defense = new Stat(10);  // Damage reduction
        [SerializeField] private Stat intelligence = new Stat(10); // Magic damage, mana pool
        [SerializeField] private Stat agility = new Stat(10);  // Speed, evasion, crit
        
        [Header("Derived Stats")]
        [SerializeField] private Stat attackPower = new Stat(0);  // Based on strength
        [SerializeField] private Stat magicPower = new Stat(0);   // Based on intelligence
        [SerializeField] private Stat critChance = new Stat(5);   // Base 5% + agility bonus
        [SerializeField] private Stat critDamage = new Stat(150); // Base 150% + strength/agility
        [SerializeField] private Stat moveSpeed = new Stat(5);    // Base movement speed
        
        [Header("Currency")]
        [SerializeField] private int gold = 0;
        
        // Events
        public Action<int, int> OnHealthChanged; // currentHealth, maxHealth
        public Action<int, int> OnManaChanged;   // currentMana, maxMana
        public Action<int> OnLevelUp;            // newLevel
        public Action<int> OnGoldChanged;        // newGold
        public Action<int, int> OnExperienceChanged; // currentExp, expToNextLevel
        
        protected virtual void Awake()
        {
            // Sync maxHealth base value with Inspector value
            maxHealth.BaseValue = baseHealth;
            // Initialize current values
            currentHealth = maxHealth.Value;
            currentMana = maxMana.Value;
            
            // Calculate derived stats
            RecalculateDerivedStats();
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep maxHealth in sync with Inspector
            if (maxHealth != null)
                maxHealth.BaseValue = baseHealth;
        }
        #endif
        
        protected virtual void Start()
        {
            // Nothing by default
        }
        
        #region Health and Mana Management
        
        public void SetHealth(int amount)
        {
            currentHealth = Mathf.Clamp(amount, 0, maxHealth.Value);
            OnHealthChanged?.Invoke(currentHealth, maxHealth.Value);
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        public bool TakeDamage(int damage)
        {
            // Apply defense reduction
            float damageReduction = defense.Value / 100f;
            int actualDamage = Mathf.Max(1, Mathf.RoundToInt(damage * (1 - damageReduction)));
            
            int newHealth = currentHealth - actualDamage;
            SetHealth(newHealth);
            
            return currentHealth <= 0;
        }
        
        public void Heal(int amount)
        {
            SetHealth(currentHealth + amount);
        }
        
        public void SetMana(int amount)
        {
            currentMana = Mathf.Clamp(amount, 0, maxMana.Value);
            OnManaChanged?.Invoke(currentMana, maxMana.Value);
        }
        
        public bool UseMana(int amount)
        {
            if (currentMana >= amount)
            {
                SetMana(currentMana - amount);
                return true;
            }
            return false;
        }
        
        public void RestoreMana(int amount)
        {
            SetMana(currentMana + amount);
        }
        
        protected virtual void Die()
        {
            // Override in derived classes
            Debug.Log($"{characterName} has died!");
        }
        
        #endregion
        
        #region Experience and Leveling
        
        public void AddExperience(int amount)
        {
            experiencePoints += amount;
            OnExperienceChanged?.Invoke(experiencePoints, experienceToNextLevel);
            
            // Check for level up
            CheckLevelUp();
        }
        
        private void CheckLevelUp()
        {
            if (experiencePoints >= experienceToNextLevel)
            {
                // Level up
                experiencePoints -= experienceToNextLevel;
                level++;
                
                // Increase stats with level
                ApplyLevelUpBonuses();
                
                // Calculate new experience threshold
                experienceToNextLevel = CalculateExperienceForNextLevel();
                
                // Fully restore health and mana
                SetHealth(maxHealth.Value);
                SetMana(maxMana.Value);
                
                // Notify listeners
                OnLevelUp?.Invoke(level);
                
                // Check if we should level up again
                CheckLevelUp();
            }
        }
        
        protected virtual void ApplyLevelUpBonuses()
        {
            // Base stat increases per level - override in derived classes for different formulas
            maxHealth.BaseValue += 10;
            maxMana.BaseValue += 5;
            strength.BaseValue += 1;
            defense.BaseValue += 1;
            intelligence.BaseValue += 1;
            agility.BaseValue += 1;
            
            // Recalculate derived stats
            RecalculateDerivedStats();
            
            // Show level up feedback UI if present
            var feedback = GameObject.FindObjectOfType<EverdrivenDays.LevelUpFeedbackUI>();
            if (feedback != null)
                feedback.ShowLevelUpFeedback();
        }
        
        protected virtual int CalculateExperienceForNextLevel()
        {
            // Simple formula: each level requires more exp
            return (level + 1) * 100;
        }
        
        #endregion
        
        #region Stat Management
        
        public void RecalculateDerivedStats()
        {
            // Calculate attack power from strength
            attackPower.BaseValue = strength.Value * 2;
            
            // Calculate magic power from intelligence
            magicPower.BaseValue = intelligence.Value * 2;
            
            // Calculate crit chance from agility
            critChance.BaseValue = 5 + Mathf.FloorToInt(agility.Value / 5);
            
            // Calculate crit damage from strength and agility
            critDamage.BaseValue = 150 + Mathf.FloorToInt((strength.Value + agility.Value) / 2);
            
            // Calculate move speed from agility
            moveSpeed.BaseValue = 5 + Mathf.FloorToInt(agility.Value / 10);
        }
        
        // Apply stat bonuses from equipment
        public void ApplyItemStatBonus(string statName, int bonus)
        {
            switch (statName.ToLower())
            {
                case "health":
                case "maxhealth":
                    maxHealth.AddItemBonus(bonus);
                    break;
                case "mana":
                case "maxmana":
                    maxMana.AddItemBonus(bonus);
                    break;
                case "strength":
                    strength.AddItemBonus(bonus);
                    break;
                case "defense":
                    defense.AddItemBonus(bonus);
                    break;
                case "intelligence":
                    intelligence.AddItemBonus(bonus);
                    break;
                case "agility":
                    agility.AddItemBonus(bonus);
                    break;
                case "attackpower":
                    attackPower.AddItemBonus(bonus);
                    break;
                case "magicpower":
                    magicPower.AddItemBonus(bonus);
                    break;
                case "critchance":
                    critChance.AddItemBonus(bonus);
                    break;
                case "critdamage":
                    critDamage.AddItemBonus(bonus);
                    break;
                case "movespeed":
                    moveSpeed.AddItemBonus(bonus);
                    break;
            }
            
            // Recalculate derived stats since base stats might have changed
            RecalculateDerivedStats();
        }
        
        // Remove stat bonuses from equipment
        public void RemoveItemStatBonus(string statName, int bonus)
        {
            switch (statName.ToLower())
            {
                case "health":
                case "maxhealth":
                    maxHealth.RemoveItemBonus(bonus);
                    break;
                case "mana":
                case "maxmana":
                    maxMana.RemoveItemBonus(bonus);
                    break;
                case "strength":
                    strength.RemoveItemBonus(bonus);
                    break;
                case "defense":
                    defense.RemoveItemBonus(bonus);
                    break;
                case "intelligence":
                    intelligence.RemoveItemBonus(bonus);
                    break;
                case "agility":
                    agility.RemoveItemBonus(bonus);
                    break;
                case "attackpower":
                    attackPower.RemoveItemBonus(bonus);
                    break;
                case "magicpower":
                    magicPower.RemoveItemBonus(bonus);
                    break;
                case "critchance":
                    critChance.RemoveItemBonus(bonus);
                    break;
                case "critdamage":
                    critDamage.RemoveItemBonus(bonus);
                    break;
                case "movespeed":
                    moveSpeed.RemoveItemBonus(bonus);
                    break;
            }
            
            // Recalculate derived stats since base stats might have changed
            RecalculateDerivedStats();
        }
        
        #endregion
        
        #region Currency
        
        public void AddGold(int amount)
        {
            gold += amount;
            OnGoldChanged?.Invoke(gold);
        }
        
        public bool SpendGold(int amount)
        {
            if (gold >= amount)
            {
                gold -= amount;
                OnGoldChanged?.Invoke(gold);
                return true;
            }
            return false;
        }
        
        #endregion
        
        #region Getters
        
        public string Name => characterName;
        public int Level => level;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth.Value;
        public int CurrentMana => currentMana;
        public int MaxMana => maxMana.Value;
        public int Strength => strength.Value;
        public int Defense => defense.Value;
        public int Intelligence => intelligence.Value;
        public int Agility => agility.Value;
        public int AttackPower => attackPower.Value;
        public int MagicPower => magicPower.Value;
        public int CritChance => critChance.Value;
        public int CritDamage => critDamage.Value;
        public int MoveSpeed => moveSpeed.Value;
        public int Gold => gold;
        public int Experience => experiencePoints;
        public int ExperienceToNextLevel => experienceToNextLevel;
        
        #endregion
    }
}