using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EverdrivenDays
{
    public class GoldAndHealUI : MonoBehaviour
    {
        [Header("References")]
        public TextMeshProUGUI goldText;
        public Button healButton;
        public TextMeshProUGUI healCostText;
        public Player player;

        [Header("Settings")]
        public int healAmount = 50;
        public int healCost = 20;

        private void Start()
        {
            if (healButton != null)
                healButton.onClick.AddListener(OnHealButtonClicked);
            UpdateGoldUI();
            UpdateHealCostUI();
            if (player != null && player.Stats != null)
                player.Stats.OnGoldChanged += OnGoldChanged;
        }

        private void OnDestroy()
        {
            if (player != null && player.Stats != null)
                player.Stats.OnGoldChanged -= OnGoldChanged;
        }

        private void OnGoldChanged(int newGold)
        {
            UpdateGoldUI();
        }

        private void UpdateGoldUI()
        {
            if (goldText != null && player != null && player.Stats != null)
                goldText.text = $"Gold: {player.Stats.Gold}";
        }

        private void UpdateHealCostUI()
        {
            if (healCostText != null)
                healCostText.text = $"Heal ({healAmount} HP): {healCost} Gold";
        }

        private void OnHealButtonClicked()
        {
            if (player == null || player.Stats == null) return;
            if (player.Stats.CurrentHealth >= player.Stats.MaxHealth)
            {
                Debug.Log("Already at max health!");
                return;
            }
            if (player.Stats.SpendGold(healCost))
            {
                player.Stats.Heal(healAmount);
                Debug.Log($"Healed {healAmount} HP for {healCost} gold.");
            }
            else
            {
                Debug.Log("Not enough gold to heal!");
            }
        }
    }
}
