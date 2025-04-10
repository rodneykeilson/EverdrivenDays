using UnityEngine;
using UnityEngine.InputSystem;

namespace EverdrivenDays
{
    public class PlayerAttackingState : PlayerGroundedState
    {
        private float startTime;
        private int consecutiveAttacksUsed;
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
            // Only transition if not in the middle of a combo attack
            if (IsConsecutive() && stateMachine.Player.Input.PlayerActions.Attack.IsPressed())
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

        private void Attack()
        {
            startTime = Time.time;
            UpdateConsecutiveAttacks();
            
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

        private void UpdateConsecutiveAttacks()
        {
            if (!IsConsecutive())
            {
                consecutiveAttacksUsed = 0;
            }

            ++consecutiveAttacksUsed;

            if (consecutiveAttacksUsed == groundedData.AttackData.ConsecutiveAttacksLimitAmount)
            {
                consecutiveAttacksUsed = 0;

                stateMachine.Player.Input.DisableActionFor(stateMachine.Player.Input.PlayerActions.Attack, groundedData.AttackData.AttackLimitReachedCooldown);
            }
        }

        private bool IsConsecutive()
        {
            return Time.time < startTime + groundedData.AttackData.TimeToBeConsideredConsecutive;
        }

        protected override void OnAttackStarted(InputAction.CallbackContext context)
        {
            // Don't transition if already in attacking state
            if (!(stateMachine.CurrentState is PlayerAttackingState))
            {
                stateMachine.ChangeState(stateMachine.AttackingState);
            }
        }
    }
}