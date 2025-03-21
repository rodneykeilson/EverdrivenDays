using UnityEngine;

namespace EverdrivenDays
{
    public class CharacterSwitcher : MonoBehaviour
    {
        public GameObject[] characterModels; // Assign Gura, Korone, etc.
        private int currentIndex = 0;

        void Start()
        {
            ActivateModel(0);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab) && !IsAnyMovementKeyPressed()) // Press Tab to switch
            {

                SwitchCharacter();
            }
        }

        void SwitchCharacter()
        {
            // Get current and next animators
            Animator currentAnimator = characterModels[currentIndex].GetComponent<Animator>();

            // Store current animation state (to prevent reset)
            AnimatorStateInfo currentState = currentAnimator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = currentState.normalizedTime;

            // Disable current model
            characterModels[currentIndex].SetActive(false);

            // Move to the next model
            currentIndex = (currentIndex + 1) % characterModels.Length;

            // Enable new model
            characterModels[currentIndex].SetActive(true);

            // Get new model's Animator
            Animator newAnimator = characterModels[currentIndex].GetComponent<Animator>();

            // Set new model to same animation state & time
            newAnimator.Play(currentState.fullPathHash, 0, normalizedTime);

            // Reorder hierarchy
            ReorderModels();
        }

        void ActivateModel(int index)
        {
            // Disable all models
            foreach (GameObject model in characterModels)
            {
                model.SetActive(false);
            }

            // Enable the selected model
            characterModels[index].SetActive(true);

            // Ensure correct hierarchy order
            ReorderModels();
        }

        void ReorderModels()
        {
            // Move the active model to the top of the hierarchy
            characterModels[currentIndex].transform.SetSiblingIndex(0);
        }

        private bool IsAnyMovementKeyPressed()
        {
            return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                   Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
                   Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.LeftArrow) ||
                   Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.RightArrow);
        }
    }
}