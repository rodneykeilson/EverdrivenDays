using UnityEngine;

namespace EverdrivenDays
{
    // Attach this to your Goblin prefab
    [RequireComponent(typeof(Animator))]
    public class GoblinAnimationController : MonoBehaviour
    {
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void PlayIdle()
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isDead", false);
            animator.SetBool("isKnockedBack", false);
            animator.Play("Goblin_Idle01");
        }

        public void PlayRun()
        {
            animator.SetBool("isRunning", true);
            animator.SetBool("isDead", false);
            animator.SetBool("isKnockedBack", false);
            animator.Play("Goblin_Run_Forward");
        }

        public void PlayDeath()
        {
            animator.SetBool("isDead", true);
            animator.Play("Goblin_Stagger01");
        }

        public void PlayKnockback()
        {
            animator.SetBool("isKnockedBack", true);
            animator.Play("Goblin_Stagger02"); // Or another suitable anim
        }
    }
}
