using System.Collections;
using UnityEngine;

namespace EverdrivenDays
{
    public class EncounterEffect : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float initialScale = 0.1f;
        [SerializeField] private float maxScale = 3.0f;
        [SerializeField] private float expandDuration = 0.5f;
        [SerializeField] private float holdDuration = 0.2f;
        [SerializeField] private float fadeDuration = 0.8f;
        [SerializeField] private Color startColor = new Color(1f, 1f, 0.5f, 1f); // Yellow
        [SerializeField] private Color midColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange
        [SerializeField] private Color endColor = new Color(1f, 0f, 0f, 0f); // Red to transparent
        
        [Header("Components")]
        [SerializeField] private ParticleSystem particleSystem;
        [SerializeField] private Light encounterLight;
        
        private Renderer[] renderers;
        
        private void Awake()
        {
            // Get all renderers in children
            renderers = GetComponentsInChildren<Renderer>();
            
            // Set initial scale
            transform.localScale = Vector3.one * initialScale;
            
            // Set initial color
            SetColor(startColor);
            
            // Start encounter animation
            StartCoroutine(EncounterAnimation());
        }
        
        private IEnumerator EncounterAnimation()
        {
            // Play particle system if available
            if (particleSystem != null)
            {
                particleSystem.Play();
            }
            
            // Expand phase
            float elapsedTime = 0f;
            while (elapsedTime < expandDuration)
            {
                float t = elapsedTime / expandDuration;
                float scale = Mathf.Lerp(initialScale, maxScale, t);
                transform.localScale = Vector3.one * scale;
                
                // Transition from start to mid color
                Color currentColor = Color.Lerp(startColor, midColor, t);
                SetColor(currentColor);
                
                // Adjust light intensity if available
                if (encounterLight != null)
                {
                    encounterLight.intensity = Mathf.Lerp(1f, 4f, t);
                    encounterLight.color = currentColor;
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Hold at max scale
            transform.localScale = Vector3.one * maxScale;
            SetColor(midColor);
            yield return new WaitForSeconds(holdDuration);
            
            // Fade out phase
            elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                float t = elapsedTime / fadeDuration;
                Color currentColor = Color.Lerp(midColor, endColor, t);
                SetColor(currentColor);
                
                // Fade out light if available
                if (encounterLight != null)
                {
                    encounterLight.intensity = Mathf.Lerp(4f, 0f, t);
                    encounterLight.color = currentColor;
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final state
            SetColor(endColor);
            
            // Destroy after animation completes
            Destroy(gameObject);
        }
        
        private void SetColor(Color color)
        {
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    material.color = color;
                }
            }
        }
    }
}
