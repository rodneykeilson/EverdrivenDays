using UnityEngine;
using System.Collections;
using TMPro;

namespace EverdrivenDays
{
    public class BossRhythmController : MonoBehaviour
    {
        [Header("Boss Encounter")]
        [SerializeField] private AudioClip bossSong;
        [SerializeField] private AudioClip bossEncounterSFX;
        [SerializeField] private string bossEncounterText = "Boss Battle!";
        [SerializeField] private GameObject encounterEffectUI;
        [SerializeField] private TextMeshProUGUI encounterText;

        private Player player;
        private bool gameActive = false;
        private bool playerSurvived = false;

        public void StartBossBattle(Player playerRef)
        {
            player = playerRef;
            StartCoroutine(BossEncounterSequence());
        }

        private IEnumerator BossEncounterSequence()
        {
            // Play encounter SFX and show text
            if (bossEncounterSFX != null)
                AudioSource.PlayClipAtPoint(bossEncounterSFX, transform.position);

            if (encounterEffectUI != null) encounterEffectUI.SetActive(true);
            if (encounterText != null) encounterText.text = bossEncounterText;

            yield return new WaitForSeconds(2f);

            if (encounterEffectUI != null) encounterEffectUI.SetActive(false);

            // Start rhythm game
            gameActive = true;
            StartCoroutine(RunBossRhythmGame());
        }

        private IEnumerator RunBossRhythmGame()
        {
            // Play the boss song (replace with your rhythm gameplay logic)
            if (bossSong != null && player != null)
            {
                AudioSource audioSource = player.GetComponent<AudioSource>();
                if (audioSource == null)
                    audioSource = player.gameObject.AddComponent<AudioSource>();
                audioSource.clip = bossSong;
                audioSource.Play();
                float songLength = bossSong.length;
                float timer = 0f;
                playerSurvived = true;
                while (timer < songLength && gameActive)
                {
                    // Here you would check for player failure (misses, health, etc.)
                    // If player fails, set playerSurvived = false; gameActive = false; break;
                    timer += Time.deltaTime;
                    yield return null;
                }
                EndBossBattle(playerSurvived);
            }
            else
            {
                // Fallback: just end after 10 seconds
                yield return new WaitForSeconds(10f);
                EndBossBattle(true);
            }
        }

        public void EndBossBattle(bool survived)
        {
            gameActive = false;
            if (survived)
            {
                // Boss dies, trigger victory logic
                Destroy(gameObject);
            }
            else
            {
                // Player lost, handle defeat (e.g., respawn, game over)
            }
        }
    }
}
