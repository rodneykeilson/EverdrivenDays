using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace EverdrivenDays
{
    public class UIManager : MonoBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private GameObject hudCanvas;
        [SerializeField] private TextMeshProUGUI playerHealthText;
        [SerializeField] private Slider playerHealthSlider;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private Slider playerExpSlider;
        [SerializeField] private TextMeshProUGUI playerMoneyText;
        
        [Header("Menu Panels")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject pausePanel;
        
        [Header("Inventory UI")]
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        
        [Header("Stats Panel")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerStatsText;
        
        [Header("Enemy UI")]
        [SerializeField] private GameObject enemyInfoPanel;
        [SerializeField] private TextMeshProUGUI enemyNameText;
        [SerializeField] private Slider enemyHealthSlider;
        [SerializeField] private TextMeshProUGUI enemyLevelText;
        
        [Header("Interaction UI")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TextMeshProUGUI interactionText;
        
        // References
        private Player player;
        private InventorySystem inventorySystem;
        private Enemy currentTargetedEnemy;
        
        // Singleton instance
        public static UIManager Instance { get; private set; }
        
        private void Awake()
        {
            // Setup singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            // Find player if not assigned
            if (player == null)
                player = FindAnyObjectByType<Player>();
                
            // Find inventory system if not assigned
            if (inventorySystem == null)
                inventorySystem = FindAnyObjectByType<InventorySystem>();
                
            // Disable panels by default
            CloseAllPanels();
        }
        
        private void Start()
        {
            // Initialize UI elements
            UpdatePlayerHUD();
        }
        
        private void Update()
        {
            // Check for input to toggle UI panels
            if (Input.GetKeyDown(KeyCode.I))
                ToggleInventory();
                
            if (Input.GetKeyDown(KeyCode.C))
                ToggleStats();
                
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
                
            // Update targeted enemy info if applicable
            if (currentTargetedEnemy != null)
                UpdateEnemyInfo();
        }
        
        public void UpdatePlayerHUD()
        {
            if (player == null) return;
            
            // For now we're using placeholder data
            // In a real implementation, you would get this from the player
            int currentHealth = 100;
            int maxHealth = 100;
            int currentLevel = 1;
            int currentExp = 50;
            int expToNextLevel = 100;
            int money = 500;
            
            // Update UI elements
            if (playerHealthText != null)
                playerHealthText.text = $"{currentHealth}/{maxHealth}";
                
            if (playerHealthSlider != null)
            {
                playerHealthSlider.maxValue = maxHealth;
                playerHealthSlider.value = currentHealth;
            }
            
            if (playerLevelText != null)
                playerLevelText.text = $"Lv. {currentLevel}";
                
            if (playerExpSlider != null)
            {
                playerExpSlider.maxValue = expToNextLevel;
                playerExpSlider.value = currentExp;
            }
            
            if (playerMoneyText != null)
                playerMoneyText.text = $"{money} Gold";
        }
        
        public void SetTargetedEnemy(Enemy enemy)
        {
            currentTargetedEnemy = enemy;
            
            if (enemy != null)
            {
                // Show enemy UI
                if (enemyInfoPanel != null)
                    enemyInfoPanel.SetActive(true);
                    
                UpdateEnemyInfo();
            }
            else
            {
                // Hide enemy UI
                if (enemyInfoPanel != null)
                    enemyInfoPanel.SetActive(false);
            }
        }
        
        private void UpdateEnemyInfo()
        {
            if (currentTargetedEnemy == null) return;
            
            // For now using placeholder data - in a real implementation, get from enemy
            string enemyName = "Enemy";
            int currentHealth = 80;
            int maxHealth = 100;
            int level = 1;
            
            // Update UI elements
            if (enemyNameText != null)
                enemyNameText.text = enemyName;
                
            if (enemyHealthSlider != null)
            {
                enemyHealthSlider.maxValue = maxHealth;
                enemyHealthSlider.value = currentHealth;
            }
            
            if (enemyLevelText != null)
                enemyLevelText.text = $"Lv. {level}";
        }
        
        public void ShowInteractionPrompt(string promptText)
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);
                
            if (interactionText != null)
                interactionText.text = promptText;
        }
        
        public void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }
        
        public void ToggleInventory()
        {
            if (inventoryPanel != null)
            {
                bool isActive = inventoryPanel.activeSelf;
                CloseAllPanels();
                inventoryPanel.SetActive(!isActive);
                
                if (!isActive && inventorySystem != null)
                {
                    // Refresh inventory display
                    RefreshInventoryUI();
                }
            }
        }
        
        public void ToggleStats()
        {
            if (statsPanel != null)
            {
                bool isActive = statsPanel.activeSelf;
                CloseAllPanels();
                statsPanel.SetActive(!isActive);
                
                if (!isActive)
                {
                    // Update stats panel with player's current stats
                    UpdateStatsPanel();
                }
            }
        }
        
        public void ToggleOptions()
        {
            if (optionsPanel != null)
            {
                bool isActive = optionsPanel.activeSelf;
                CloseAllPanels();
                optionsPanel.SetActive(!isActive);
            }
        }
        
        public void TogglePause()
        {
            if (pausePanel != null)
            {
                bool isActive = pausePanel.activeSelf;
                
                if (isActive)
                {
                    pausePanel.SetActive(false);
                    Time.timeScale = 1f; // Resume game
                }
                else
                {
                    CloseAllPanels();
                    pausePanel.SetActive(true);
                    Time.timeScale = 0f; // Pause game
                }
            }
        }
        
        private void CloseAllPanels()
        {
            // Close all panels but keep HUD visible
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            if (statsPanel != null) statsPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            
            // Don't close pause panel here, it has special handling
            
            // Make sure the game is running (in case we're coming from pause)
            Time.timeScale = 1f;
        }
        
        private void RefreshInventoryUI()
        {
            // Clear existing items
            if (itemsContainer != null)
            {
                foreach (Transform child in itemsContainer)
                {
                    Destroy(child.gameObject);
                }
                
                // This would typically use data from the inventory system
                // For now, we'll create placeholder items
                CreateInventoryItemSlot("Health Potion", "Restores 50 HP", 5);
                CreateInventoryItemSlot("Mana Potion", "Restores 50 MP", 3);
                CreateInventoryItemSlot("Bronze Sword", "+10 Attack", 1);
                CreateInventoryItemSlot("Leather Armor", "+15 Defense", 1);
            }
            
            // Clear item description
            if (itemDescriptionText != null)
                itemDescriptionText.text = "Select an item to see its description.";
        }
        
        private void CreateInventoryItemSlot(string itemName, string description, int count)
        {
            if (itemSlotPrefab == null || itemsContainer == null) return;
            
            GameObject itemSlot = Instantiate(itemSlotPrefab, itemsContainer);
            
            // Set up the item slot UI
            Transform nameTransform = itemSlot.transform.Find("ItemName");
            Transform countTransform = itemSlot.transform.Find("ItemCount");
            Button itemButton = itemSlot.GetComponent<Button>();
            
            if (nameTransform != null)
            {
                TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                    nameText.text = itemName;
            }
            
            if (countTransform != null)
            {
                TextMeshProUGUI countText = countTransform.GetComponent<TextMeshProUGUI>();
                if (countText != null)
                    countText.text = count > 1 ? count.ToString() : "";
            }
            
            // Add click listener to show item description
            if (itemButton != null)
            {
                itemButton.onClick.AddListener(() => {
                    if (itemDescriptionText != null)
                        itemDescriptionText.text = $"{itemName}\n{description}";
                });
            }
        }
        
        private void UpdateStatsPanel()
        {
            if (player == null) return;
            
            // Placeholder player stats
            string playerName = "Player";
            int level = 1;
            int hp = 100;
            int mp = 50;
            int strength = 10;
            int defense = 8;
            int agility = 12;
            int intelligence = 9;
            
            if (playerNameText != null)
                playerNameText.text = playerName;
                
            if (playerStatsText != null)
            {
                playerStatsText.text = $"Level: {level}\n" +
                                      $"HP: {hp}\n" +
                                      $"MP: {mp}\n" +
                                      $"Strength: {strength}\n" +
                                      $"Defense: {defense}\n" +
                                      $"Agility: {agility}\n" +
                                      $"Intelligence: {intelligence}";
            }
        }
    }
} 