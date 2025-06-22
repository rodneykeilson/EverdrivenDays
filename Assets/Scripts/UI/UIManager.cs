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
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject pausePanel;
        
        [Header("Stats Panel")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerStatsText;
        
        // References
        private Player player;
        
        // Singleton instance
        public static UIManager Instance { get; private set; }
        
        private void Awake()
        {
            Debug.Log($"[UIManager] Awake called on {gameObject.name}, activeSelf={gameObject.activeSelf}, Instance={Instance}");
            // Setup singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[UIManager] Duplicate detected, disabling {gameObject.name}");
                gameObject.SetActive(false);
                return;
            }
            Instance = this;
            
            // Find player if not assigned
            if (player == null)
                player = FindAnyObjectByType<Player>();
                
            // Disable panels by default
            CloseAllPanels();
        }

        private void OnEnable()
        {
            Debug.Log($"[UIManager] OnEnable called on {gameObject.name}");
        }

        private void OnDisable()
        {
            Debug.LogWarning($"[UIManager] OnDisable called on {gameObject.name}");
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
        
        public void ToggleInventory()
        {
            if (optionsPanel != null)
            {
                bool isActive = optionsPanel.activeSelf;
                CloseAllPanels();
                optionsPanel.SetActive(!isActive);
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
            // Only close menu panels, not the main HUD
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (statsPanel != null) statsPanel.SetActive(false);
            // Do NOT deactivate hudCanvas here!
            // Make sure the game is running (in case we're coming from pause)
            Time.timeScale = 1f;
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