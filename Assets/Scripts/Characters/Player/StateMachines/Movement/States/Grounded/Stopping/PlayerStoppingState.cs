using UnityEngine.InputSystem;

namespace EverdrivenDays
{
    public class PlayerStoppingState : PlayerGroundedState
    {
        public PlayerStoppingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            stateMachine.ReusableData.MovementSpeedModifier = 0f;

            SetBaseCameraRecenteringData();

            base.Enter();

            StartAnimation(stateMachine.Player.AnimationData.StoppingParameterHash);
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(stateMachine.Player.AnimationData.StoppingParameterHash);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            RotateTowardsTargetRotation();

            if (!IsMovingHorizontally())
            {
                return;
            }

            DecelerateHorizontally();
        }

        public override void OnAnimationTransitionEvent()
        {
            stateMachine.ChangeState(stateMachine.IdlingState);
        }

        protected override void AddInputActionsCallbacks()
        {
            base.AddInputActionsCallbacks();

            stateMachine.Player.Input.PlayerActions.Movement.started += OnMovementStarted;
            stateMachine.Player.Input.PlayerActions.Attack.started += OnAttackStarted;
            stateMachine.Player.Input.PlayerActions.StrongAttack.started += OnStrongAttackStarted;
            
        }

        protected override void RemoveInputActionsCallbacks()
        {
            base.RemoveInputActionsCallbacks();

            stateMachine.Player.Input.PlayerActions.Movement.started -= OnMovementStarted;
            stateMachine.Player.Input.PlayerActions.Attack.started -= OnAttackStarted;
            stateMachine.Player.Input.PlayerActions.StrongAttack.started -= OnStrongAttackStarted;
        }

        private void OnMovementStarted(InputAction.CallbackContext context)
        {
            OnMove();
        }

        protected virtual void OnAttackStarted(InputAction.CallbackContext context)
        {
            stateMachine.ChangeState(stateMachine.AttackingState);
        }
        protected virtual void OnStrongAttackStarted(InputAction.CallbackContext context)
        {
            stateMachine.ChangeState(stateMachine.StrongAttackingState);
        }
    }
}