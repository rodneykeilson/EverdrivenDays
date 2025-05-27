using System.Collections;
using UnityEngine;

namespace EverdrivenDays
{
    public class ImpactEffect : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float initialScale = 0.5f;
        [SerializeField] private float maxScale = 1.5f;
        [SerializeField] private float expandDuration = 0.2f;
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private Color startColor = Color.white;
        [SerializeField] private Color endColor = new Color(0.5f, 0.5f, 1f, 0); // Blue to transparent
        
        [Header("Components")]
        [SerializeField] private ParticleSystem particleSystem;
        [SerializeField] private Light impactLight;
        
        private Renderer[] renderers;
        
        private void Awake()
        {
            // Get all renderers in children
            renderers = GetComponentsInChildren<Renderer>();
            
            // Set initial scale
            transform.localScale = Vector3.one * initialScale;
            
            // Set initial color
            SetColor(startColor);
            
            // Start impact animation
            StartCoroutine(ImpactAnimation());
        }
        
        private IEnumerator ImpactAnimation()
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
                
                // Adjust light intensity if available
                if (impactLight != null)
                {
                    impactLight.intensity = Mathf.Lerp(1f, 3f, t);
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Fade out phase
            elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                float t = elapsedTime / fadeDuration;
                Color currentColor = Color.Lerp(startColor, endColor, t);
                SetColor(currentColor);
                
                // Fade out light if available
                if (impactLight != null)
                {
                    impactLight.intensity = Mathf.Lerp(3f, 0f, t);
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
