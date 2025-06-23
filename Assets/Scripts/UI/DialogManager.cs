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

    private List<DialogLine> currentDialog;
    private int currentIndex;
    private System.Action onDialogEnd;
    private bool isShowing = false;

    private void Awake()
    {
        Instance = this;
        if (dialogGroup != null) dialogGroup.alpha = 0;
        if (dialogPanel != null) dialogPanel.anchoredPosition = new Vector2(dialogPanel.anchoredPosition.x, -200);
    }

    public void ShowDialog(List<DialogLine> dialog, System.Action onEnd = null)
    {
        currentDialog = dialog;
        currentIndex = 0;
        onDialogEnd = onEnd;
        StartCoroutine(RunDialog());
    }

    private IEnumerator RunDialog()
    {
        isShowing = true;
        while (currentIndex < currentDialog.Count)
        {
            var line = currentDialog[currentIndex];
            yield return StartCoroutine(FadeInDialog());
            speakerText.text = line.speaker;
            dialogText.text = line.text;
            if (line.lockPlayerMovement) PlayerInputLock(true);
            if (line.voiceClip)
            {
                audioSource.clip = line.voiceClip;
                audioSource.Play();
                yield return new WaitForSeconds(line.voiceClip.length);
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }
            if (line.lockPlayerMovement) PlayerInputLock(false);
            yield return StartCoroutine(FadeOutDialog());
            currentIndex++;
        }
        isShowing = false;
        onDialogEnd?.Invoke();
    }

    private IEnumerator FadeInDialog()
    {
        float t = 0;
        Vector2 start = new Vector2(dialogPanel.anchoredPosition.x, -200); // Start below
        Vector2 end = new Vector2(dialogPanel.anchoredPosition.x, 0); // End at y=0
        while (t < 1)
        {
            t += Time.deltaTime * 4f;
            dialogGroup.alpha = Mathf.Lerp(0, 1, t);
            dialogPanel.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
        dialogGroup.alpha = 1;
        dialogPanel.anchoredPosition = end;
    }

    private IEnumerator FadeOutDialog()
    {
        float t = 0;
        Vector2 start = dialogPanel.anchoredPosition;
        Vector2 end = new Vector2(dialogPanel.anchoredPosition.x, -200); // Move back down
        while (t < 1)
        {
            t += Time.deltaTime * 4f;
            dialogGroup.alpha = Mathf.Lerp(1, 0, t);
            dialogPanel.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
        dialogGroup.alpha = 0;
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
