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
        [SerializeField] private UnityEngine.UI.Image playerHealthFillImage;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private UnityEngine.UI.Image playerExpFillImage;
        [SerializeField] private TextMeshProUGUI playerMoneyText;
        [SerializeField] private TextMeshProUGUI playerExpText;
        
        [Header("Menu Panels")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject pausePanel;
        
        [Header("Stats Panel")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerStatsText;
        
        [Header("Health and Exp Bars")]
        [SerializeField] private Slider playerHealthBarSlider;
        [SerializeField] private Slider playerExpBarSlider;
        
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
            var stats = player.Stats;
            if (stats == null) return;

            int currentHealth = stats.CurrentHealth;
            int maxHealth = stats.MaxHealth;
            int currentLevel = stats.Level;
            int currentExp = stats.Experience;
            int expToNextLevel = stats.ExperienceToNextLevel;
            int money = stats.Gold;

            if (playerHealthText != null)
                playerHealthText.text = $"{currentHealth}/{maxHealth}";

            // Use Slider for health bar
            if (playerHealthBarSlider != null)
            {
                playerHealthBarSlider.maxValue = maxHealth;
                playerHealthBarSlider.value = currentHealth;
            }

            if (playerLevelText != null)
                playerLevelText.text = $"{currentLevel}";

            // Use Slider for exp bar
            if (playerExpBarSlider != null)
            {
                playerExpBarSlider.maxValue = expToNextLevel;
                playerExpBarSlider.value = currentExp;
            }

            if (playerExpText != null)
                playerExpText.text = $"{currentExp}/{expToNextLevel}";

            if (playerMoneyText != null)
                playerMoneyText.text = $"{money}";
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
            var stats = player.Stats;
            if (stats == null) return;

            string playerName = stats.Name;
            int level = stats.Level;
            int hp = stats.CurrentHealth;
            int mp = stats.CurrentMana;
            int strength = stats.Strength;
            int defense = stats.Defense;
            int agility = stats.Agility;
            int intelligence = stats.Intelligence;

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