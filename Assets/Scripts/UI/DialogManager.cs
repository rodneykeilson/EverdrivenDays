using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using EverdrivenDays;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance;

    [Header("UI References")]
    public CanvasGroup dialogGroup;
    public RectTransform dialogPanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogText;
    public AudioSource audioSource;

    [Header("Response UI")]
    public GameObject responsePanel;
    public TextMeshProUGUI responseText;

    private List<DialogLine> currentDialog;
    private int currentIndex;
    private System.Action onDialogEnd;
    private bool isShowing = false;
    private Vector2 originalAnchoredPosition;

    private void Awake()
    {
        Instance = this;
        if (dialogGroup != null) dialogGroup.alpha = 0;
        if (dialogPanel != null)
        {
            originalAnchoredPosition = dialogPanel.anchoredPosition;
            // Start off-screen to the right for fade-in
            dialogPanel.anchoredPosition = new Vector2(originalAnchoredPosition.x + 500, originalAnchoredPosition.y);
        }
    }

    public void ShowDialog(List<DialogLine> dialog, System.Action onEnd = null)
    {
        currentDialog = dialog;
        currentIndex = 0;
        onDialogEnd = onEnd;
        // Set first line's text and speaker before fade-in
        if (currentDialog != null && currentDialog.Count > 0)
        {
            var firstLine = currentDialog[0];
            speakerText.text = firstLine.speaker;
            dialogText.text = firstLine.text;
        }
        StartCoroutine(RunDialog());
    }

    private IEnumerator RunDialog()
    {
        isShowing = true;
        // Fade in at the start only
        if (currentDialog != null && currentDialog.Count > 0)
            yield return StartCoroutine(FadeInDialog());
        while (currentIndex < currentDialog.Count)
        {
            var line = currentDialog[currentIndex];
            // Only set text if not first line (already set before fade-in)
            if (currentIndex != 0)
            {
                speakerText.text = line.speaker;
                dialogText.text = line.text;
            }
            if (line.lockPlayerMovement) PlayerInputLock(true);
            bool voiceSkipped = false;
            // --- Wait for mouse up before starting line, to avoid holding click from previous skip ---
            while (Input.GetMouseButton(0)) yield return null;
            // Always play voiceline first if present
            if (line.voiceClip)
            {
                audioSource.clip = line.voiceClip;
                audioSource.Play();
                float timer = 0f;
                while (timer < line.voiceClip.length)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        audioSource.Stop();
                        voiceSkipped = true;
                        break;
                    }
                    timer += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                float timer = 0f;
                while (timer < 2f)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        voiceSkipped = true;
                        break;
                    }
                    timer += Time.deltaTime;
                    yield return null;
                }
            }
            // Add a 1-second pause after each dialog line (unless skipped)
            float pauseTimer = 0f;
            while (pauseTimer < 1f)
            {
                if (Input.GetMouseButtonDown(0)) break;
                pauseTimer += Time.deltaTime;
                yield return null;
            }
            // --- Wait for mouse up before accepting next click ---
            while (Input.GetMouseButton(0)) yield return null;
            // After voiceline, handle response if present
            if (!string.IsNullOrEmpty(line.playerResponse))
            {
                if (responsePanel != null) responsePanel.SetActive(true);
                if (responseText != null) responseText.text = line.playerResponse;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                bool waitingForClick = true;
                while (waitingForClick)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        waitingForClick = false;
                    }
                    yield return null;
                }
                if (responsePanel != null) responsePanel.SetActive(false);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                // Only unlock movement after response panel closes
                if (line.lockPlayerMovement) PlayerInputLock(false);
            }
            else
            {
                if (responsePanel != null) responsePanel.SetActive(false);
                // If no response panel, unlock movement after voiceline
                if (line.lockPlayerMovement) PlayerInputLock(false);
            }
            currentIndex++;
        }
        // Fade out at the end only
        yield return StartCoroutine(FadeOutDialog());
        isShowing = false;
        onDialogEnd?.Invoke();
    }

    private IEnumerator FadeInDialog()
    {
        float t = 0;
        float startAlpha = 0f;
        float endAlpha = 1f;
        Vector2 start = new Vector2(originalAnchoredPosition.x + 500, originalAnchoredPosition.y);
        Vector2 end = originalAnchoredPosition;
        while (t < 1)
        {
            t += Time.deltaTime * 4f;
            dialogGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            dialogPanel.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
        dialogGroup.alpha = endAlpha;
        dialogPanel.anchoredPosition = end;
    }

    private IEnumerator FadeOutDialog()
    {
        float t = 0;
        float startAlpha = 1f;
        float endAlpha = 0f;
        Vector2 start = originalAnchoredPosition;
        Vector2 end = new Vector2(originalAnchoredPosition.x + 500, originalAnchoredPosition.y);
        while (t < 1)
        {
            t += Time.deltaTime * 4f;
            dialogGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            dialogPanel.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
        dialogGroup.alpha = endAlpha;
        dialogPanel.anchoredPosition = end;
    }

    // Replace this with your actual player input script reference
    private void PlayerInputLock(bool locked)
    {
        var player = FindObjectOfType<Player>();
        if (player != null && player.Input != null)
        {
            if (locked)
                player.Input.DisableMovement();
            else
                player.Input.EnableMovement();
        }
    }
}
