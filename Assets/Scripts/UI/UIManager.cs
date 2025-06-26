using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // <-- Added for scene reloading

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
        
        [Header("Loading Screen")]
        [SerializeField] private AdvancedLoadingBar loadingBar;
        
        [Header("Exit Button")]
        [SerializeField] private Button exitButton;
        [Tooltip("Scene name to load when exiting to title screen.")]
        [SerializeField] private string titleScreenSceneName = "TitleScreen";
        
        [Header("Death/Respawn")]
        [SerializeField] private GameObject youDiedPanel;
        [SerializeField] private float fadeDuration = 1.5f;
        [SerializeField] private float youDiedDisplayTime = 5f;
        
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
             Debug.Log("[UIManager] Start called, exitButton=" + (exitButton != null));
 
            // Initialize UI elements
            UpdatePlayerHUD();
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(ExitToTitleScreen); // Prevent duplicate listeners
                exitButton.onClick.AddListener(ExitToTitleScreen);
            }
        }
        
        private void Update()
        {
            // Check for input to toggle UI panels
            if (Input.GetKeyDown(KeyCode.I))
                ToggleInventory();
            if (Input.GetKeyDown(KeyCode.C))
                ToggleStats();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsPausePanelOpen())
                    TogglePause(); // Resume
                else
                    TogglePause(); // Open pause
            }
            if (IsPausePanelOpen() && Input.GetKeyDown(KeyCode.Q))
            {
                ExitToTitleScreen();
            }
            // --- QUICK FIX: Always update HUD ---
            UpdatePlayerHUD();
        }
        
        private bool IsPausePanelOpen()
        {
            return pausePanel != null && pausePanel.activeSelf;
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
                playerHealthText.text = $"{currentHealth:N0} <color=#ffc9d6>/</color> {maxHealth:N0}";

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
        
        /// <summary>
        /// Exits to the TitleScreen scene using AdvancedLoadingBar for a loading screen.
        /// Hook this to the pause panel's Exit button.
        /// </summary>
        public void ExitToTitleScreen()
        {
            Debug.Log($"[UIManager] ExitToTitleScreen called, loading scene: {titleScreenSceneName}");
            if (pausePanel != null)
                pausePanel.SetActive(false); // Hide pause panel before loading
            Time.timeScale = 1f; // Unpause before loading
            if (loadingBar != null)
            {
                loadingBar.sceneToLoad = titleScreenSceneName;
                loadingBar.StartLoading();
            }
            else
            {
                Debug.LogError("[UIManager] AdvancedLoadingBar reference not set in inspector.");
            }
        }

        public void PlayerDeathSequence()
        {
            // Cancel rhythm encounter if active
            var rhythm = FindAnyObjectByType<SmallEnemyRhythmController>();
            if (rhythm != null)
            {
                rhythm.ForceEndGameOnPlayerDeath();
            }
            StartCoroutine(DeathAndRespawnRoutine());
        }

        private System.Collections.IEnumerator DeathAndRespawnRoutine()
        {
            // Fade in 'YOU DIED' panel
            if (youDiedPanel != null)
            {
                CanvasGroup cg = youDiedPanel.GetComponent<CanvasGroup>();
                if (cg == null) cg = youDiedPanel.AddComponent<CanvasGroup>();
                youDiedPanel.SetActive(true);
                cg.alpha = 0f;
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    cg.alpha = Mathf.Clamp01(t / fadeDuration);
                    yield return null;
                }
                cg.alpha = 1f;
                // Hold the panel fully visible for a bit longer
                yield return new WaitForSecondsRealtime(youDiedDisplayTime);
            }
            // --- Save player stats before reload ---
            if (player != null && player.Stats != null)
            {
                var stats = player.Stats;
                PlayerSaveData.Level = stats.Level;
                PlayerSaveData.Experience = stats.Experience;
                PlayerSaveData.ExperienceToNextLevel = stats.ExperienceToNextLevel;
                PlayerSaveData.MaxHealth = stats.MaxHealth;
                PlayerSaveData.CurrentHealth = stats.CurrentHealth;
                PlayerSaveData.Strength = stats.Strength;
                PlayerSaveData.Defense = stats.Defense;
                PlayerSaveData.Agility = stats.Agility;
                PlayerSaveData.Intelligence = stats.Intelligence;
                PlayerSaveData.Gold = stats.Gold;
                PlayerSaveData.SaveToPrefs(); // Persist to disk
            }
            // --- Reload the current scene ---
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}