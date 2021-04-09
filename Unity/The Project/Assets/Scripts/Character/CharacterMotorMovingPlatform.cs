using System;
using UnityEngine;

namespace Character
{
    [Serializable]
    public class CharacterMotorMovingPlatform
    {
        public bool Enabled;
        public MovementTransferOnJump MovementTransfer;
        [NonSerialized] public Transform HitPlatform;
        [NonSerialized] public Transform ActivePlatform;
        [NonSerialized] public Vector3 ActiveLocalPoint;
        [NonSerialized] public Vector3 ActiveGlobalPoint;
        [NonSerialized] public Quaternion ActiveLocalRotation;
        [NonSerialized] public Quaternion ActiveGlobalRotation;
        [NonSerialized] public Matrix4x4 LastMatrix;
        [NonSerialized] public Vector3 PlatformVelocity;
        [NonSerialized] public bool NewPlatform;

        public CharacterMotorMovingPlatform()
        {
            Enabled = true;
            MovementTransfer = MovementTransferOnJump.PermaTransfer;
        }
    }
}