using UnityEngine;
using UnityEngine.InputSystem;

namespace EverdrivenDays
{
    public class PlayerAttackingState : PlayerGroundedState
    {
        private float startTime;
        private bool shouldKeepRotating;

        public PlayerAttackingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            stateMachine.ReusableData.MovementSpeedModifier = groundedData.AttackData.SpeedModifier;

            base.Enter();

            StartAnimation(stateMachine.Player.AnimationData.AttackParameterHash);

            stateMachine.ReusableData.CurrentJumpForce = airborneData.JumpData.StrongForce;

            stateMachine.ReusableData.RotationData = groundedData.AttackData.RotationData;

            Attack();

            shouldKeepRotating = stateMachine.ReusableData.MovementInput != Vector2.zero;
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(stateMachine.Player.AnimationData.AttackParameterHash);

            SetBaseRotationData();
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (!shouldKeepRotating)
            {
                return;
            }

            RotateTowardsTargetRotation();
        }

        public override void OnAnimationTransitionEvent()
        {
            if (stateMachine.ReusableData.MovementInput == Vector2.zero)
            {
                stateMachine.ChangeState(stateMachine.HardStoppingState);

                return;
            }

            stateMachine.ChangeState(stateMachine.SprintingState);
        }

        protected override void AddInputActionsCallbacks()
        {
            base.AddInputActionsCallbacks();

            stateMachine.Player.Input.PlayerActions.Movement.performed += OnMovementPerformed;

        }

        protected override void RemoveInputActionsCallbacks()
        {
            base.RemoveInputActionsCallbacks();

            stateMachine.Player.Input.PlayerActions.Movement.performed -= OnMovementPerformed;
        }

        protected override void OnMovementPerformed(InputAction.CallbackContext context)
        {
            base.OnMovementPerformed(context);

            shouldKeepRotating = true;
        }

        private void Attack()
        {
            Vector3 attackDirection = stateMachine.Player.transform.forward;

            attackDirection.y = 0f;

            UpdateTargetRotation(attackDirection, false);

            if (stateMachine.ReusableData.MovementInput != Vector2.zero)
            {
                UpdateTargetRotation(GetMovementInputDirection());

                attackDirection = GetTargetRotationDirection(stateMachine.ReusableData.CurrentTargetRotation.y);
            }

            stateMachine.Player.Rigidbody.linearVelocity = attackDirection * GetMovementSpeed(false);
        }

        protected override void OnAttackStarted(InputAction.CallbackContext context)
        {
        }
    }
}