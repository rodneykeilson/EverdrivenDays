using UnityEngine;
using UnityEngine.InputSystem;

namespace EverdrivenDays
{
    public class PlayerStrongAttackingState : PlayerGroundedState
    {
        public PlayerStrongAttackingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            StartAnimation(stateMachine.Player.AnimationData.StrongAttackParameterHash);

            stateMachine.ReusableData.MovementDecelerationForce = groundedData.StopData.HardDecelerationForce;

            stateMachine.ReusableData.CurrentJumpForce = airborneData.JumpData.StrongForce;
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(stateMachine.Player.AnimationData.StrongAttackParameterHash);
        }

        protected override void OnMove()
        {
            if (stateMachine.ReusableData.ShouldWalk)
            {
                return;
            }

            stateMachine.ChangeState(stateMachine.RunningState);
        }

        protected override void OnStrongAttackStarted(InputAction.CallbackContext context)
        {
            // Prevent re-triggering strong attack while already attacking
        }
    }
}