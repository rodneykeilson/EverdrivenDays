using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EverdrivenDays
{
    public class NPCDialogueUI : MonoBehaviour
    {
        public GameObject dialoguePanel; // The chat UI
        public TMP_Text npcNameText; // Displays NPC's name
        public TMP_Text dialogueText; // Displays dialogue
        public Button nextButton; // Button to continue

        private string[] currentDialogue;
        private int dialogueIndex;

        private void Start()
        {
            dialoguePanel.SetActive(false); // Hide dialogue panel when game starts
        }

        public void StartDialogue(NPCInteractable npc)
        {
            dialoguePanel.SetActive(true);
            npcNameText.text = npc.npcName;
            currentDialogue = npc.dialogueLines;
            dialogueIndex = 0;
            ShowNextDialogue();
        }

        public void ShowNextDialogue()
        {
            if (dialogueIndex < currentDialogue.Length)
            {
                dialogueText.text = currentDialogue[dialogueIndex];
                dialogueIndex++;
            }
            else
            {
                CloseDialogue();
            }
        }

        public void CloseDialogue()
        {
            dialoguePanel.SetActive(false); // Hide when done
        }
    }
}
