using UnityEngine;
using TMPro;
using System.Collections;

namespace EverdrivenDays
{
    public class LevelUpFeedbackUI : MonoBehaviour
    {
        public TextMeshProUGUI levelUpText;
        public AudioSource audioSource;
        public AudioClip levelUpClip;
        public float showDuration = 1.5f;
        [Header("Blinking")]
        [Tooltip("How many times per second the text blinks")] public float blinkFrequency = 10f;

        private Coroutine blinkCoroutine;

        private void Awake()
        {
            if (levelUpText != null)
                levelUpText.gameObject.SetActive(false);
        }

        public void ShowLevelUpFeedback()
        {
            if (levelUpText != null)
            {
                levelUpText.gameObject.SetActive(true);
                levelUpText.text = "LEVEL UP!";
                levelUpText.transform.localScale = Vector3.one;
                if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
                blinkCoroutine = StartCoroutine(BlinkText(levelUpText, showDuration, blinkFrequency));
            }
            if (audioSource != null && levelUpClip != null)
            {
                audioSource.PlayOneShot(levelUpClip);
            }
            Invoke(nameof(HideLevelUp), showDuration);
        }

        private IEnumerator BlinkText(TextMeshProUGUI text, float duration, float frequency)
        {
            float elapsed = 0f;
            float interval = 1f / Mathf.Max(1f, frequency);
            bool visible = true;
            while (elapsed < duration)
            {
                text.enabled = visible;
                visible = !visible;
                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }
            text.enabled = true;
        }

        private void HideLevelUp()
        {
            if (levelUpText != null)
            {
                levelUpText.gameObject.SetActive(false);
                levelUpText.enabled = true;
            }
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
        }
    }
}
