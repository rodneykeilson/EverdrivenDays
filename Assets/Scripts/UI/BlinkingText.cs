using UnityEngine;
using TMPro;

public class BlinkingText : MonoBehaviour
{
    public float blinkInterval = 0.5f;
    public float fadeDuration = 0.3f;
    private TextMeshProUGUI text;
    private float timer;
    private bool fadingOut = true;
    private float alpha = 1f;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            var c = text.color;
            c.a = 1f;
            text.color = c;
        }
    }

    void Update()
    {
        if (text == null) return;

        timer += Time.unscaledDeltaTime;
        float fadeSpeed = Time.unscaledDeltaTime / fadeDuration;

        if (fadingOut)
        {
            alpha -= fadeSpeed;
            if (alpha <= 0f)
            {
                alpha = 0f;
                fadingOut = false;
                timer = 0f;
            }
        }
        else
        {
            alpha += fadeSpeed;
            if (alpha >= 1f)
            {
                alpha = 1f;
                if (timer >= blinkInterval)
                {
                    fadingOut = true;
                    timer = 0f;
                }
            }
        }
        var color = text.color;
        color.a = Mathf.Clamp01(alpha);
        text.color = color;
    }
}
