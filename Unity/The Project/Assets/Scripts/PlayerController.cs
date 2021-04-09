// Component to sit next to PlayerInput.

using System;
using Character;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterMotor))]
public class PlayerController : MonoBehaviour
{
    public GameObject projectilePrefab;

    private CharacterMotor _motor;
    private PlayerInput _playerInput;

    private InputAction _jumpAction;
    private InputAction _moveAction;
    private InputAction _fireAction;

    private void Start()
    {
        if (_motor != null) return;
            _motor = GetComponent<CharacterMotor>();

        if (_playerInput != null) return;
        _playerInput = GetComponent<PlayerInput>();

        _fireAction = _playerInput.actions["fire"];
        _jumpAction = _playerInput.actions["jump"];
        _moveAction = _playerInput.actions["move"];
    }

    private void Update()
    {
        // First update we look up all the data we need.
        // NOTE: We don't do this in OnEnable as PlayerInput itself performing some
        //       initialization work in OnEnable.


        var move = _moveAction.ReadValue<Vector2>();
        var jump = _jumpAction.ReadValue<float>();


        _motor.InputMoveDirection = new Vector3(move.x, 0, move.y);
        _motor.InputJump = jump > 0.0f;
    }
}
