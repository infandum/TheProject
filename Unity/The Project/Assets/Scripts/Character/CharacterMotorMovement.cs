using System;
using UnityEngine;

namespace Character
{
    [Serializable]
    public class CharacterMotorMovement
    {
        // The maximum horizontal speed when moving.
        public float maxForwardSpeed;
        public float maxSidewaysSpeed;
        public float maxBackwardsSpeed;

        [Tooltip("Curve for multiplying speed based on slope (negative = downwards).")]
        public AnimationCurve slopeSpeedMultiplier;

        [Tooltip("How fast does the character change speed?  Higher is faster.")]
        public float maxGroundAcceleration;
        public float maxAirAcceleration;

        // The gravity for the character.
        public float gravity;
        public float maxFallSpeed;

        // The last collision flags returned from controller.Move.
        [NonSerialized] public CollisionFlags CollisionFlags;

        // We will keep track of the character's current velocity.
        [NonSerialized] public Vector3 Velocity;

        // This keeps track of our current velocity while we're not grounded.
        [NonSerialized] public Vector3 FrameVelocity;
        [NonSerialized] public Vector3 HitPoint;
        [NonSerialized] public Vector3 LastHitPoint;

        public CharacterMotorMovement()
        {
            // The maximum horizontal speed when moving.
            maxForwardSpeed = 10f;
            maxSidewaysSpeed = 10f;
            maxBackwardsSpeed = 10f;

            // Curve for multiplying speed based on slope (negative = downwards).
            slopeSpeedMultiplier = new AnimationCurve(new Keyframe[] { new Keyframe(-90, 1), new Keyframe(0, 1), new Keyframe(90, 0) });
            maxGroundAcceleration = 30f;
            maxAirAcceleration = 20f;
            gravity = 10f;
            maxFallSpeed = 20f;
            FrameVelocity = Vector3.zero;
            HitPoint = Vector3.zero;
            LastHitPoint = new Vector3(Mathf.Infinity, 0, 0);
        }
    }
}