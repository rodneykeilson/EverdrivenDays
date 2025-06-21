using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EverdrivenDays
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Enemy targetEnemy;

        [Header("Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;
        
        private Camera mainCamera;
        private int maxHealth;
        private int currentHealth;
        
        private void Awake()
        {
            mainCamera = Camera.main;
            
            // Ensure the canvas faces the camera
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = mainCamera;
            }
            
            // If no target enemy is set, try to get it from parent
            if (targetEnemy == null)
            {
                targetEnemy = GetComponentInParent<Enemy>();
            }
            
            // Initialize health values
            if (targetEnemy != null)
            {
                maxHealth = targetEnemy.MaxHealth;
                currentHealth = targetEnemy.CurrentHealth;
                UpdateHealthBar();
            }
            else
            {
                Debug.LogWarning("No target enemy assigned to health bar!");
                gameObject.SetActive(false);
            }
        }
        
        private void LateUpdate()
        {
            if (targetEnemy == null || mainCamera == null)
            {
                return;
            }
            
            // Update health values
            currentHealth = targetEnemy.CurrentHealth;
            UpdateHealthBar();
            
            // Position above enemy
            transform.position = targetEnemy.transform.position + offset;
            
            // Rotate to face camera
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        }
        
        private void UpdateHealthBar()
        {
            if (healthFillImage != null)
            {
                float healthPercent = (float)currentHealth / maxHealth;
                healthFillImage.fillAmount = healthPercent;
                
                // Interpolate color based on health percentage
                healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
            }
            
            if (healthText != null)
            {
                healthText.text = $"{currentHealth}/{maxHealth}";
            }
        }
        
        public void SetTarget(Enemy enemy)
        {
            targetEnemy = enemy;
            
            if (targetEnemy != null)
            {
                maxHealth = targetEnemy.MaxHealth;
                currentHealth = targetEnemy.CurrentHealth;
                UpdateHealthBar();
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void SetYOffset(float y)
        {
            offset.y = y;
        }
    }
}
