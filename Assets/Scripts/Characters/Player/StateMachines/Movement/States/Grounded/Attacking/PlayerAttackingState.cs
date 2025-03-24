using UnityEngine;
using UnityEngine.InputSystem;

namespace EverdrivenDays
{
    public class PlayerAttackingState : PlayerStoppingState
    {
        public PlayerAttackingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("PlayerAttackingState Enter");
            StartAnimation(stateMachine.Player.AnimationData.AttackParameterHash);

            stateMachine.ReusableData.MovementDecelerationForce = groundedData.StopData.HardDecelerationForce;

            stateMachine.ReusableData.CurrentJumpForce = airborneData.JumpData.StrongForce;
        }

        public override void Exit()
        {
            base.Exit();

            Debug.Log("PlayerAttackingState Exit");
            StopAnimation(stateMachine.Player.AnimationData.AttackParameterHash);
        }

        protected override void OnMove()
        {
            if (stateMachine.ReusableData.ShouldWalk)
            {
                return;
            }

            stateMachine.ChangeState(stateMachine.RunningState);
        }

        protected override void OnAttackStarted(InputAction.CallbackContext context)
        {
        }
    }
}