using UnityEngine;
using UnityEngine.InputSystem;

namespace EverdrivenDays
{
    public class PlayerStrongAttackingState : PlayerGroundedState
    {
        private float startTime;
        private int consecutiveStrongAttacksUsed;
        private bool shouldKeepRotating;

        public PlayerStrongAttackingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            stateMachine.ReusableData.MovementSpeedModifier = groundedData.StrongAttackData.SpeedModifier;

            base.Enter();

            StartAnimation(stateMachine.Player.AnimationData.StrongAttackParameterHash);

            stateMachine.ReusableData.CurrentJumpForce = airborneData.JumpData.StrongForce;

            stateMachine.ReusableData.RotationData = groundedData.StrongAttackData.RotationData;

            StrongAttack();

            shouldKeepRotating = stateMachine.ReusableData.MovementInput != Vector2.zero;
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(stateMachine.Player.AnimationData.StrongAttackParameterHash);

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
            // Only transition if not in the middle of a combo attack
            if (IsConsecutive() /*&& stateMachine.Player.Input.PlayerActions.StrongAttack.IsPressed()*/)
            {
                return;
            }
            
            if (stateMachine.ReusableData.MovementInput == Vector2.zero)
            {
                stateMachine.ChangeState(stateMachine.IdlingState);

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

        private void StrongAttack()
        {
            startTime = Time.time;
            UpdateConsecutiveStrongAttacks();
            
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

        private void UpdateConsecutiveStrongAttacks()
        {
            if (!IsConsecutive())
            {
                consecutiveStrongAttacksUsed = 0;
            }

            ++consecutiveStrongAttacksUsed;

            if (consecutiveStrongAttacksUsed == groundedData.StrongAttackData.ConsecutiveStrongAttacksLimitAmount)
            {
                consecutiveStrongAttacksUsed = 0;
            }
        }

        private bool IsConsecutive()
        {
            return Time.time < startTime + groundedData.StrongAttackData.TimeToBeConsideredConsecutive;
        }

        protected override void OnStrongAttackStarted(InputAction.CallbackContext context)
        {
            // Don't transition if already in strong attacking state
            if (!(stateMachine.CurrentState is PlayerStrongAttackingState))
            {
                stateMachine.ChangeState(stateMachine.StrongAttackingState);
            }
        }
    }
}