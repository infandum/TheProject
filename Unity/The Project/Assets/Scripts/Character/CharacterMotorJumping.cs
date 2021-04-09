using System;
using UnityEngine;

namespace Character
{
    [Serializable]
    public class CharacterMotorJumping
    {
        [Tooltip("Can the character jump?")]
        public bool Enabled = true;

        [Tooltip("How high do we jump when pressing jump and letting go immediately.")]
        public float BaseHeight = 1f;

        [Tooltip("We add extraHeight units (meters) on top when holding the button down longer while jumping.")]
        public float ExtraHeight = 2f;

        [Tooltip("How much does the character jump out perpendicular to walkable surfaces. 0 = fully vertical, 1 = fully perpendicular.")]
        [Range(0, 1)]
        public float PerpAmount;

        [Tooltip("How much does the character jump out perpendicular to steep surfaces. 0 = fully vertical, 1 = fully perpendicular.")]
        [Range(0, 1)]
        public float SteepPerpAmount = 0.5f;

        // Are we jumping? (Initiated with jump button and not grounded yet)
        // To see if we are just in the air (initiated by jumping OR falling) see the grounded variable.
        [NonSerialized] public bool Jumping;
        [NonSerialized] public bool HoldingJumpButton;

        // The time we jumped at (Used to determine for how long to apply extra jump power after jumping.)
        [NonSerialized] public float LastStartTime;
        [NonSerialized] public float LastButtonDownTime = -100;
        [NonSerialized] public Vector3 JumpDir = Vector3.up;
    }
}