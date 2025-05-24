using System;
using System.Collections.Generic;
using UnityEngine;

namespace EverdrivenDays
{
    [Serializable]
    public class InventoryItem
    {
        public string id;
        public string name;
        public string description;
        public int count;
        public Sprite icon;
        public ItemType type;
        public ItemRarity rarity;
        public Dictionary<string, int> stats = new Dictionary<string, int>();
        public bool isEquippable;
        public bool isEquipped;
        public bool isConsumable;
        
        public InventoryItem(string id, string name, string description, ItemType type, ItemRarity rarity, bool isConsumable, bool isEquippable)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.type = type;
            this.rarity = rarity;
            this.isConsumable = isConsumable;
            this.isEquippable = isEquippable;
            count = 1;
            isEquipped = false;
        }
        
        public InventoryItem Clone()
        {
            InventoryItem clone = new InventoryItem(id, name, description, type, rarity, isConsumable, isEquippable)
            {
                count = count,
                icon = icon,
                isEquipped = isEquipped
            };
            
            foreach (var stat in stats)
            {
                clone.stats[stat.Key] = stat.Value;
            }
            
            return clone;
        }
    }
    
    public enum ItemType
    {
        Weapon,
        Armor,
        Accessory,
        Consumable,
        Material,
        Quest
    }
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    public class InventorySystem : MonoBehaviour
    {
        [SerializeField] private int inventorySize = 30;
        [SerializeField] private ItemDatabase itemDatabase; // Reference to a scriptable object with all item templates
        
        private List<InventoryItem> items = new List<InventoryItem>();
        private Dictionary<string, InventoryItem> equippedItems = new Dictionary<string, InventoryItem>();
        
        // Slots: "Weapon", "Head", "Body", "Hands", "Legs", "Feet", "Accessory1", "Accessory2"
        
        // Events
        public Action OnInventoryChanged;
        public Action<InventoryItem> OnItemAdded;
        public Action<InventoryItem> OnItemRemoved;
        public Action<InventoryItem> OnItemEquipped;
        public Action<InventoryItem> OnItemUnequipped;
        public Action<InventoryItem> OnItemUsed;
        
        // Singleton instance
        public static InventorySystem Instance { get; private set; }
        
        private void Awake()
        {
            // Setup singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            // Load saved inventory if exists
            LoadInventory();
        }
        
        private void Start()
        {
            if (itemDatabase == null)
            {
                Debug.LogWarning("Item Database not assigned to Inventory System!");
            }
        }
        
        public List<InventoryItem> GetAllItems()
        {
            return items;
        }
        
        public Dictionary<string, InventoryItem> GetEquippedItems()
        {
            return equippedItems;
        }
        
        public bool AddItem(string itemId, int count = 1)
        {
            // Check if we have the item database
            if (itemDatabase == null)
            {
                Debug.LogError("Cannot add item: Item Database is missing");
                return false;
            }
            
            // Get the item template from the database
            InventoryItem itemTemplate = itemDatabase.GetItem(itemId);
            if (itemTemplate == null)
            {
                Debug.LogError($"Item with ID {itemId} not found in the database");
                return false;
            }
            
            // Clone the item template
            InventoryItem newItem = itemTemplate.Clone();
            newItem.count = count;
            
            return AddItem(newItem);
        }
        
        public bool AddItem(InventoryItem item)
        {
            // Check if inventory is full
            if (items.Count >= inventorySize && !HasItem(item.id))
            {
                Debug.Log("Inventory is full");
                return false;
            }
            
            // Check if the item is stackable
            if (item.isConsumable || item.type == ItemType.Material)
            {
                // Look for an existing stack
                InventoryItem existingItem = items.Find(i => i.id == item.id && !i.isEquipped);
                if (existingItem != null)
                {
                    // Add to existing stack
                    existingItem.count += item.count;
                    OnInventoryChanged?.Invoke();
                    OnItemAdded?.Invoke(existingItem);
                    return true;
                }
            }
            
            // Add new item
            items.Add(item);
            OnInventoryChanged?.Invoke();
            OnItemAdded?.Invoke(item);
            return true;
        }
        
        public bool RemoveItem(string itemId, int count = 1)
        {
            InventoryItem item = items.Find(i => i.id == itemId);
            if (item == null)
            {
                Debug.LogWarning($"Item with ID {itemId} not found in inventory");
                return false;
            }
            
            return RemoveItem(item, count);
        }
        
        public bool RemoveItem(InventoryItem item, int count = 1)
        {
            // Check if we have enough of the item
            if (item.count < count)
            {
                Debug.LogWarning("Not enough items to remove");
                return false;
            }
            
            // Remove from stack
            item.count -= count;
            
            // If stack is empty, remove the item
            if (item.count <= 0)
            {
                // If equipped, unequip first
                if (item.isEquipped)
                {
                    UnequipItem(item);
                }
                
                items.Remove(item);
            }
            
            OnInventoryChanged?.Invoke();
            OnItemRemoved?.Invoke(item);
            return true;
        }
        
        public bool HasItem(string itemId, int count = 1)
        {
            int totalCount = 0;
            
            foreach (var item in items)
            {
                if (item.id == itemId)
                {
                    totalCount += item.count;
                    if (totalCount >= count)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        public int GetItemCount(string itemId)
        {
            int totalCount = 0;
            
            foreach (var item in items)
            {
                if (item.id == itemId)
                {
                    totalCount += item.count;
                }
            }
            
            return totalCount;
        }
        
        public bool UseItem(InventoryItem item)
        {
            if (!item.isConsumable)
            {
                Debug.LogWarning("This item cannot be consumed");
                return false;
            }
            
            // Apply item effects (to be implemented based on the specific item)
            
            // Trigger event
            OnItemUsed?.Invoke(item);
            
            // Remove one from stack
            return RemoveItem(item, 1);
        }
        
        public bool EquipItem(InventoryItem item)
        {
            if (!item.isEquippable)
            {
                Debug.LogWarning("This item cannot be equipped");
                return false;
            }
            
            // Determine equipment slot based on item type
            string slot = GetSlotForItemType(item.type);
            
            // Check if something is already equipped in that slot
            if (equippedItems.ContainsKey(slot))
            {
                // Unequip the current item
                UnequipItem(equippedItems[slot]);
            }
            
            // Equip the new item
            item.isEquipped = true;
            equippedItems[slot] = item;
            
            // Apply item stats (to be implemented based on player stats system)
            
            OnInventoryChanged?.Invoke();
            OnItemEquipped?.Invoke(item);
            return true;
        }
        
        public bool UnequipItem(InventoryItem item)
        {
            if (!item.isEquipped)
            {
                Debug.LogWarning("This item is not equipped");
                return false;
            }
            
            // Find which slot it's equipped in
            string slot = null;
            foreach (var pair in equippedItems)
            {
                if (pair.Value == item)
                {
                    slot = pair.Key;
                    break;
                }
            }
            
            if (slot == null)
            {
                Debug.LogError("Item marked as equipped but not found in equipped items");
                return false;
            }
            
            // Remove stats (to be implemented based on player stats system)
            
            // Remove from equipped items
            item.isEquipped = false;
            equippedItems.Remove(slot);
            
            OnInventoryChanged?.Invoke();
            OnItemUnequipped?.Invoke(item);
            return true;
        }
        
        private string GetSlotForItemType(ItemType type)
        {
            switch (type)
            {
                case ItemType.Weapon:
                    return "Weapon";
                case ItemType.Armor:
                    return "Body"; // Simplification - in a real system you might have more slots
                case ItemType.Accessory:
                    // For simplicity, always use the first accessory slot
                    return "Accessory1";
                default:
                    Debug.LogError($"Item type {type} cannot be equipped");
                    return null;
            }
        }
        
        public void SaveInventory()
        {
            // To be implemented - save inventory to PlayerPrefs or a file
        }
        
        public void LoadInventory()
        {
            // To be implemented - load inventory from PlayerPrefs or a file
            
            // For testing, add some items
            AddTestItems();
        }
        
        private void AddTestItems()
        {
            // Add some test items (this would be removed in a real implementation)
            InventoryItem sword = new InventoryItem("weapon_sword", "Iron Sword", "A basic iron sword", ItemType.Weapon, ItemRarity.Common, false, true);
            sword.stats["Attack"] = 10;
            
            InventoryItem potion = new InventoryItem("potion_health", "Health Potion", "Restores 50 HP", ItemType.Consumable, ItemRarity.Common, true, false);
            potion.count = 5;
            
            InventoryItem armor = new InventoryItem("armor_leather", "Leather Armor", "Basic protection", ItemType.Armor, ItemRarity.Common, false, true);
            armor.stats["Defense"] = 15;
            
            AddItem(sword);
            AddItem(potion);
            AddItem(armor);
        }
    }
    
    // This would typically be in its own file
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();
        
        private Dictionary<string, InventoryItem> itemLookup;
        
        private void OnEnable()
        {
            itemLookup = new Dictionary<string, InventoryItem>();
            foreach (var item in items)
            {
                if (itemLookup.ContainsKey(item.id))
                {
                    Debug.LogError($"Duplicate item ID: {item.id}");
                }
                else
                {
                    itemLookup[item.id] = item;
                }
            }
        }
        
        public InventoryItem GetItem(string id)
        {
            if (itemLookup.TryGetValue(id, out InventoryItem item))
            {
                return item;
            }
            return null;
        }
    }
} 