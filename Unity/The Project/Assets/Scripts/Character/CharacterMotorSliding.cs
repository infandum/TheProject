using System;
using UnityEngine;

[Serializable]
public class CharacterMotorSliding
{
    [Tooltip("Does the character slide on too steep surfaces?")]
    public bool Enabled;

    [Tooltip("How fast does the character slide on steep surfaces?")]
    public float SlidingSpeed;

    [Tooltip("How much can the player control the sliding direction? If the value is 0.5 the player can slide sideways with half the speed of the downwards sliding speed.")]
    [Range(0, 1)]
    public float SidewaysControl;

    [Tooltip("How much can the player influence the sliding speed? If the value is 0.5 the player can speed the sliding up to 150% or slow it down to 50%.")]
    [Range(0, 1)]
    public float SpeedControl;

    public CharacterMotorSliding()
    {
        Enabled = true;
        SlidingSpeed = 15;
        SidewaysControl = 1f;
        SpeedControl = 0.4f;
    }
}