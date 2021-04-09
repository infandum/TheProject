using System;
using System.Collections;
using UnityEngine;

namespace Character
{
    public enum MovementTransferOnJump
    {
        None = 0,           // The jump is not affected by velocity of floor at all.
        InitTransfer = 1,   // Jump gets its initial velocity from the floor, then gradually comes to a stop.
        PermaTransfer = 2,  // Jump gets its initial velocity from the floor and keeps that velocity until landing.
        PermaLocked = 3     // Jump is relative to the movement of the last touched floor and will move together with that floor.
    }


// We will contain all the jumping related variables in one helper class for clarity.


    [AddComponentMenu("Character Controller/Character Motor")]
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : MonoBehaviour
    {
        [Tooltip("Does this script currently respond to input?")]
        public bool CanControl;
        public bool UseFixedUpdate;

        // The current global direction we want the character to move in.
        [NonSerialized] public Vector3 InputMoveDirection;

        // Is the jump button held down? We use this interface instead of checking.
        // For the jump button directly so this script can also be used by AIs.
        [NonSerialized] public bool InputJump;

        public CharacterMotorMovement Movement;
        public CharacterMotorJumping Jumping;
        public CharacterMotorMovingPlatform MovingPlatform;
        public CharacterMotorSliding Sliding;

        [NonSerialized] public bool Grounded;
        [NonSerialized] public Vector3 GroundNormal;

        private Vector3 _lastGroundNormal;
        private Transform _transform;
        private CharacterController _controller;

        #region Unity Methods

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _transform = GetComponent<Transform>();
        }

        private void FixedUpdate()
        {
            if (MovingPlatform.Enabled)
            {
                if (MovingPlatform.ActivePlatform != null)
                {
                    if (!MovingPlatform.NewPlatform)
                    {
                        MovingPlatform.PlatformVelocity = (MovingPlatform.ActivePlatform.localToWorldMatrix.MultiplyPoint3x4(MovingPlatform.ActiveLocalPoint) - MovingPlatform.LastMatrix.MultiplyPoint3x4(MovingPlatform.ActiveLocalPoint)) / Time.deltaTime;
                    }
                    MovingPlatform.LastMatrix = MovingPlatform.ActivePlatform.localToWorldMatrix;
                    MovingPlatform.NewPlatform = false;
                }
                else
                {
                    MovingPlatform.PlatformVelocity = Vector3.zero;
                }
            }
            if (UseFixedUpdate)
            {
                UpdateFunction();
            }
        }

        private void Update()
        {
            if (!UseFixedUpdate)
            {
                UpdateFunction();
            }
        }

        #endregion

        #region Private Methods

        private void UpdateFunction()
        {
            // We copy the actual velocity into a temporary variable that we can manipulate.
            var velocity = Movement.Velocity;

            // Update velocity based on input
            velocity = ApplyInputVelocityChange(velocity);

            // Apply gravity and jumping force
            velocity = ApplyGravityAndJumping(velocity);

            // Moving platform support

            if (MoveWithPlatform())
            {
                var newGlobalPoint = MovingPlatform.ActivePlatform.TransformPoint(MovingPlatform.ActiveLocalPoint);
                var moveDistance = newGlobalPoint - MovingPlatform.ActiveGlobalPoint;

                if (moveDistance != Vector3.zero)
                {
                    _controller.Move(moveDistance);
                }

                // Support moving platform rotation as well:
                Quaternion newGlobalRotation = MovingPlatform.ActivePlatform.rotation * MovingPlatform.ActiveLocalRotation;
                Quaternion rotationDiff = newGlobalRotation * Quaternion.Inverse(MovingPlatform.ActiveGlobalRotation);
                float yRotation = rotationDiff.eulerAngles.y;

                if (yRotation != 0)
                {
                    // Prevent rotation of the local up vector
                    _transform.Rotate(0, yRotation, 0);
                }
            }

            // Save lastPosition for velocity calculation.
            Vector3 lastPosition = _transform.position;

            // We always want the movement to be frame-rate independent.  Multiplying by Time.deltaTime does this.
            Vector3 currentMovementOffset = velocity * Time.deltaTime;

            // Find out how much we need to push towards the ground to avoid loosing grounding
            // when walking down a step or over a sharp change in slope.
            float pushDownOffset = Mathf.Max(_controller.stepOffset, new Vector3(currentMovementOffset.x, 0, currentMovementOffset.z).magnitude);

            if (Grounded)
            {
                currentMovementOffset = currentMovementOffset - (pushDownOffset * Vector3.up);
            }

            // Reset variables that will be set by collision function
            MovingPlatform.HitPlatform = null;
            GroundNormal = Vector3.zero;

            // Move our character!
            Movement.CollisionFlags = _controller.Move(currentMovementOffset);
            Movement.LastHitPoint = Movement.HitPoint;
            _lastGroundNormal = GroundNormal;

            if (MovingPlatform.Enabled && (MovingPlatform.ActivePlatform != MovingPlatform.HitPlatform))
            {
                if (MovingPlatform.HitPlatform != null)
                {
                    MovingPlatform.ActivePlatform = MovingPlatform.HitPlatform;
                    MovingPlatform.LastMatrix = MovingPlatform.HitPlatform.localToWorldMatrix;
                    MovingPlatform.NewPlatform = true;
                }
            }

            // Calculate the velocity based on the current and previous position.
            // This means our velocity will only be the amount the character actually moved as a result of collisions.
            Vector3 oldHVelocity = new Vector3(velocity.x, 0, velocity.z);
            Movement.Velocity = (_transform.position - lastPosition) / Time.deltaTime;
            Vector3 newHVelocity = new Vector3(Movement.Velocity.x, 0, Movement.Velocity.z);

            // The CharacterController can be moved in unwanted directions when colliding with things.
            // We want to prevent this from influencing the recorded velocity.
            if (oldHVelocity == Vector3.zero)
            {
                Movement.Velocity = new Vector3(0, Movement.Velocity.y, 0);
            }
            else
            {
                float projectedNewVelocity = Vector3.Dot(newHVelocity, oldHVelocity) / oldHVelocity.sqrMagnitude;
                Movement.Velocity = (oldHVelocity * Mathf.Clamp01(projectedNewVelocity)) + (Movement.Velocity.y * Vector3.up);
            }

            if (Movement.Velocity.y < (velocity.y - 0.001f))
            {
                if (Movement.Velocity.y < 0)
                {
                    // Something is forcing the CharacterController down faster than it should.
                    // Ignore this
                    Movement.Velocity.y = velocity.y;
                }
                else
                {
                    // The upwards movement of the CharacterController has been blocked.
                    // This is treated like a ceiling collision - stop further jumping here.
                    Jumping.HoldingJumpButton = false;
                }
            }

            // We were grounded but just loosed grounding
            if (Grounded && !IsGroundedTest())
            {
                Grounded = false;

                // Apply inertia from platform
                if (MovingPlatform.Enabled && ((MovingPlatform.MovementTransfer == MovementTransferOnJump.InitTransfer) || (MovingPlatform.MovementTransfer == MovementTransferOnJump.PermaTransfer)))
                {
                    Movement.FrameVelocity = MovingPlatform.PlatformVelocity;
                    Movement.Velocity = Movement.Velocity + MovingPlatform.PlatformVelocity;
                }

                //SendMessage ("OnFall", SendMessageOptions.DontRequireReceiver);

                // We pushed the character down to ensure it would stay on the ground if there was any.
                // But there wasn't so now we cancel the downwards offset to make the fall smoother.
                _transform.position = _transform.position + (pushDownOffset * Vector3.up);
            }
            else // We were not grounded but just landed on something
            {
                if (!Grounded && IsGroundedTest())
                {
                    Grounded = true;
                    Jumping.Jumping = false;
                    StartCoroutine(SubtractNewPlatformVelocity());
                    //SendMessage ("OnLand", SendMessageOptions.DontRequireReceiver);
                }
            }

            // Moving platforms support
            if (MoveWithPlatform())
            {
                // Use the center of the lower half sphere of the capsule as reference point.
                // This works best when the character is standing on moving tilting platforms.
                MovingPlatform.ActiveGlobalPoint = _transform.position + (Vector3.up * ((_controller.center.y - (_controller.height * 0.5f)) + _controller.radius));
                MovingPlatform.ActiveLocalPoint = MovingPlatform.ActivePlatform.InverseTransformPoint(MovingPlatform.ActiveGlobalPoint);

                // Support moving platform rotation as well:
                MovingPlatform.ActiveGlobalRotation = _transform.rotation;
                MovingPlatform.ActiveLocalRotation = Quaternion.Inverse(MovingPlatform.ActivePlatform.rotation) * MovingPlatform.ActiveGlobalRotation;
            }
        }

        private Vector3 ApplyInputVelocityChange(Vector3 velocity)
        {
            if (!CanControl)
            {
                InputMoveDirection = Vector3.zero;
            }

            // Find desired velocity.
            Vector3 desiredVelocity;

            if (Grounded && TooSteep())
            {
                // The direction we're sliding in.
                desiredVelocity = new Vector3(GroundNormal.x, 0, GroundNormal.z).normalized;

                // Find the input movement direction projected onto the sliding direction.
                Vector3 projectedMoveDir = Vector3.Project(InputMoveDirection, desiredVelocity);

                // Add the sliding direction, the speed control, and the sideways control vectors.
                desiredVelocity = (desiredVelocity + (projectedMoveDir * Sliding.SpeedControl)) + ((InputMoveDirection - projectedMoveDir) * Sliding.SidewaysControl);

                // Multiply with the sliding speed.
                desiredVelocity = desiredVelocity * Sliding.SlidingSpeed;
            }
            else
            {
                desiredVelocity = GetDesiredHorizontalVelocity();
            }

            if (MovingPlatform.Enabled && (MovingPlatform.MovementTransfer == MovementTransferOnJump.PermaTransfer))
            {
                desiredVelocity = desiredVelocity + Movement.FrameVelocity;
                desiredVelocity.y = 0;
            }

            if (Grounded)
            {
                desiredVelocity = AdjustGroundVelocityToNormal(desiredVelocity, GroundNormal);
            }
            else
            {
                velocity.y = 0;
            }

            // Enforce max velocity change
            float maxVelocityChange = GetMaxAcceleration(Grounded) * Time.deltaTime;
            Vector3 velocityChangeVector = desiredVelocity - velocity;

            if (velocityChangeVector.sqrMagnitude > (maxVelocityChange * maxVelocityChange))
            {
                velocityChangeVector = velocityChangeVector.normalized * maxVelocityChange;
            }

            // If we're in the air and don't have control, don't apply any velocity change at all.
            // If we're on the ground and don't have control we do apply it - it will correspond to friction.
            if (Grounded || CanControl)
            {
                velocity += velocityChangeVector;
            }

            if (Grounded)
            {
                // When going uphill, the CharacterController will automatically move up by the needed amount.
                // Not moving it upwards manually prevent risk of lifting off from the ground.
                // When going downhill, DO move down manually, as gravity is not enough on steep hills.
                velocity.y = Mathf.Min(velocity.y, 0);
            }

            return velocity;
        }

        private Vector3 ApplyGravityAndJumping(Vector3 velocity)
        {
            if (!InputJump || !CanControl)
            {
                Jumping.HoldingJumpButton = false;
                Jumping.LastButtonDownTime = -100;
            }

            if ((InputJump && (Jumping.LastButtonDownTime < 0)) && CanControl)
            {
                Jumping.LastButtonDownTime = Time.time;
            }

            if (Grounded)
            {
                velocity.y = Mathf.Min(0, velocity.y) - (Movement.gravity * Time.deltaTime);
            }
            else
            {
                velocity.y = Movement.Velocity.y - (Movement.gravity * Time.deltaTime);

                // When jumping up we don't apply gravity for some time when the user is holding the jump button.
                // This gives more control over jump height by pressing the button longer.
                if (Jumping.Jumping && Jumping.HoldingJumpButton)
                {
                    // Calculate the duration that the extra jump force should have effect.
                    // If we're still less than that duration after the jumping time, apply the force.
                    if (Time.time < (Jumping.LastStartTime + (Jumping.ExtraHeight / CalculateJumpVerticalSpeed(Jumping.BaseHeight))))
                    {
                        // Negate the gravity we just applied, except we push in jumpDir rather than jump upwards.
                        velocity += ((Jumping.JumpDir * Movement.gravity) * Time.deltaTime);
                    }
                }

                // Make sure we don't fall any faster than maxFallSpeed. This gives our character a terminal velocity.
                velocity.y = Mathf.Max(velocity.y, -Movement.maxFallSpeed);
            }

            if (Grounded)
            {
                // Jump only if the jump button was pressed down in the last 0.2 seconds.
                // We use this check instead of checking if it's pressed down right now because players will often
                // try to jump in the exact moment when hitting the ground after a jump and if they hit the button
                // a fraction of a second too soon and no new jump happens as a consequence, it's confusing and it
                // feels like the game is buggy.
                if ((Jumping.Enabled && CanControl) && ((Time.time - Jumping.LastButtonDownTime) < 0.2f))
                {
                    Grounded = false;
                    Jumping.Jumping = true;
                    Jumping.LastStartTime = Time.time;
                    Jumping.LastButtonDownTime = -100;
                    Jumping.HoldingJumpButton = true;

                    // Calculate the jumping direction.
                    if (TooSteep())
                    {
                        Jumping.JumpDir = Vector3.Slerp(Vector3.up, GroundNormal, Jumping.SteepPerpAmount);
                    }
                    else
                    {
                        Jumping.JumpDir = Vector3.Slerp(Vector3.up, GroundNormal, Jumping.PerpAmount);
                    }

                    // Apply the jumping force to the velocity. Cancel any vertical velocity first.
                    velocity.y = 0;
                    velocity = velocity + (Jumping.JumpDir * CalculateJumpVerticalSpeed(Jumping.BaseHeight));

                    // Apply inertia from platform.
                    if (MovingPlatform.Enabled && ((MovingPlatform.MovementTransfer == MovementTransferOnJump.InitTransfer)
                                                   || (MovingPlatform.MovementTransfer == MovementTransferOnJump.PermaTransfer)))
                    {
                        Movement.FrameVelocity = MovingPlatform.PlatformVelocity;
                        velocity = velocity + MovingPlatform.PlatformVelocity;
                    }

                    //SendMessage ("OnJump", SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    Jumping.HoldingJumpButton = false;
                }
            }
            return velocity;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (((hit.normal.y > 0) && (hit.normal.y > GroundNormal.y)) && (hit.moveDirection.y < 0))
            {
                if (((hit.point - Movement.LastHitPoint).sqrMagnitude > 0.001f) || (_lastGroundNormal == Vector3.zero))
                {
                    GroundNormal = hit.normal;
                }
                else
                {
                    GroundNormal = _lastGroundNormal;
                }

                MovingPlatform.HitPlatform = hit.collider.transform;
                Movement.HitPoint = hit.point;
                Movement.FrameVelocity = Vector3.zero;
            }
        }

        private IEnumerator SubtractNewPlatformVelocity()
        {
            // When landing, subtract the velocity of the new ground from the character's velocity
            // since movement in ground is relative to the movement of the ground.
            if (MovingPlatform.Enabled &&
                ((MovingPlatform.MovementTransfer == MovementTransferOnJump.InitTransfer) ||
                 (MovingPlatform.MovementTransfer == MovementTransferOnJump.PermaTransfer)))
            {
                // If we landed on a new platform, we have to wait for two FixedUpdates
                // before we know the velocity of the platform under the character
                if (MovingPlatform.NewPlatform)
                {
                    Transform platform = MovingPlatform.ActivePlatform;
                    yield return new WaitForFixedUpdate();
                    yield return new WaitForFixedUpdate();
                    if (Grounded && (platform == MovingPlatform.ActivePlatform))
                    {
                        yield return 1;
                    }
                }

                Movement.Velocity = Movement.Velocity - MovingPlatform.PlatformVelocity;
            }
        }

        private bool MoveWithPlatform()
        {
            return (MovingPlatform.Enabled && (Grounded
                                               || (MovingPlatform.MovementTransfer == MovementTransferOnJump.PermaLocked)))
                   && (MovingPlatform.ActivePlatform != null);
        }

        private Vector3 GetDesiredHorizontalVelocity()
        {
            // Find desired velocity
            Vector3 desiredLocalDirection = _transform.InverseTransformDirection(InputMoveDirection);
            float maxSpeed = MaxSpeedInDirection(desiredLocalDirection);

            if (Grounded)
            {
                // Modify max speed on slopes based on slope speed multiplier curve
                float movementSlopeAngle = Mathf.Asin(Movement.Velocity.normalized.y) * Mathf.Rad2Deg;
                maxSpeed = maxSpeed * Movement.slopeSpeedMultiplier.Evaluate(movementSlopeAngle);
            }

            return _transform.TransformDirection(desiredLocalDirection * maxSpeed);
        }

        private Vector3 AdjustGroundVelocityToNormal(Vector3 hVelocity, Vector3 groundNormal)
        {
            Vector3 sideways = Vector3.Cross(Vector3.up, hVelocity);
            return Vector3.Cross(sideways, groundNormal).normalized * hVelocity.magnitude;
        }

        private bool IsGroundedTest()
        {
            return GroundNormal.y > 0.01f;
        }

        #endregion

        #region Public  Methods

        public float GetMaxAcceleration(bool grounded)
        {
            // Maximum acceleration on ground and in air
            if (grounded)
            {
                return Movement.maxGroundAcceleration;
            }
            else
            {
                return Movement.maxAirAcceleration;
            }
        }

        public float CalculateJumpVerticalSpeed(float targetJumpHeight)
        {
            // From the jump height and gravity we deduce the upwards speed
            // for the character to reach at the apex.
            return Mathf.Sqrt((2 * targetJumpHeight) * Movement.gravity);
        }

        public bool IsJumping()
        {
            return Jumping.Jumping;
        }

        public bool IsSliding()
        {
            return (Grounded && Sliding.Enabled) && TooSteep();
        }

        public bool IsTouchingCeiling()
        {
            return (Movement.CollisionFlags & CollisionFlags.CollidedAbove) != 0;
        }

        public bool IsGrounded()
        {
            return Grounded;
        }

        public bool TooSteep()
        {
            return GroundNormal.y <= Mathf.Cos(_controller.slopeLimit * Mathf.Deg2Rad);
        }

        public Vector3 GetDirection()
        {
            return InputMoveDirection;
        }

        public void SetControllable(bool controllable)
        {
            CanControl = controllable;
        }

        // Project a direction onto elliptical quarter segments based on forward, sideways, and backwards speed.
        // The function returns the length of the resulting vector.
        public float MaxSpeedInDirection(Vector3 desiredMovementDirection)
        {
            if (desiredMovementDirection == Vector3.zero)
            {
                return 0;
            }
            else
            {
                float zAxisEllipseMultiplier = (desiredMovementDirection.z > 0 ? Movement.maxForwardSpeed : Movement.maxBackwardsSpeed) / Movement.maxSidewaysSpeed;
                Vector3 temp = new Vector3(desiredMovementDirection.x, 0, desiredMovementDirection.z / zAxisEllipseMultiplier).normalized;
                float length = new Vector3(temp.x, 0, temp.z * zAxisEllipseMultiplier).magnitude * Movement.maxSidewaysSpeed;
                return length;
            }
        }

        public void SetVelocity(Vector3 velocity)
        {
            Grounded = false;
            Movement.Velocity = velocity;
            Movement.FrameVelocity = Vector3.zero;
            //SendMessage ("OnExternalVelocity");
        }

        public CharacterMotor()
        {
            CanControl = true;
            UseFixedUpdate = true;
            InputMoveDirection = Vector3.zero;
            Movement = new CharacterMotorMovement();
            Jumping = new CharacterMotorJumping();
            MovingPlatform = new CharacterMotorMovingPlatform();
            Sliding = new CharacterMotorSliding();
            Grounded = true;
            GroundNormal = Vector3.zero;
            _lastGroundNormal = Vector3.zero;
        }

        #endregion


    }
}