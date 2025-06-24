using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverdrivenDays
{
    public class PlayerInput : MonoBehaviour
    {
        public PlayerInputActions InputActions { get; private set; }
        public PlayerInputActions.PlayerActions PlayerActions { get; private set; }
        
        private bool movementEnabled = true;
        public bool IsMovementLocked { get; private set; } = false;

        private void Awake()
        {
            InputActions = new PlayerInputActions();

            PlayerActions = InputActions.Player;
        }

        private void OnEnable()
        {
            InputActions.Enable();
        }

        private void OnDisable()
        {
            InputActions.Disable();
        }

        public void DisableActionFor(InputAction action, float seconds)
        {
            StartCoroutine(DisableAction(action, seconds));
        }

        private IEnumerator DisableAction(InputAction action, float seconds)
        {
            action.Disable();

            yield return new WaitForSeconds(seconds);

            action.Enable();
        }
        
        /// <summary>
        /// Disables all movement-related input actions
        /// </summary>
        public void DisableMovement()
        {
            if (!movementEnabled) return; // Already disabled
            
            Debug.Log("Disabling player movement");
            movementEnabled = false;
            IsMovementLocked = true;
            
            // Disable all player actions
            InputActions.Disable();
        }
        
        /// <summary>
        /// Re-enables all movement-related input actions
        /// </summary>
        public void EnableMovement()
        {
            if (movementEnabled) return; // Already enabled
            
            Debug.Log("Re-enabling player movement");
            movementEnabled = true;
            IsMovementLocked = false;
            
            // Re-enable all player actions
            InputActions.Enable();
        }
    }
}