using UnityEngine;

namespace EverdrivenDays
{
    public class PlayerAnimationEventTrigger : MonoBehaviour
    {
        private Player player;

        private void Awake()
        {
            player = transform.parent.GetComponent<Player>();
        }

        public void TriggerOnMovementStateAnimationEnterEvent()
        {
            if (IsInAnimationTransition())
            {
                return;
            }

            player.OnMovementStateAnimationEnterEvent();
        }

        public void TriggerOnMovementStateAnimationExitEvent()
        {
            if (IsInAnimationTransition())
            {
                return;
            }

            player.OnMovementStateAnimationExitEvent();
        }

        public void TriggerOnMovementStateAnimationTransitionEvent()
        {
            if (IsInAnimationTransition())
            {
                return;
            }

            player.OnMovementStateAnimationTransitionEvent();
        }

        private bool IsInAnimationTransition(int layerIndex = 0)
        {
            foreach (var animator in player.Animators)
            {
                if (animator.IsInTransition(layerIndex))
                {
                    return true;
                }
            }
            return false;

        }
    }
}