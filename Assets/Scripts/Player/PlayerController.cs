﻿using System;
using Player;
using Player.AnimationRelated;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [FormerlySerializedAs("controller")] public CharacterController _controller;
    private Vector3 direction;
    public float speed = 8;

    [Space(10)] [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)] [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // player
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    [SerializeField] private Camera _mainCamera;
    public float MoveSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;

    [Range(0.0f, 0.3f)] public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    private bool _isActive = true;

    [SerializeField] private PlayerAnimationHander _animationHander;
    [SerializeField] private GroundChecker _groundChecker;

    private void Start()
    {
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    public void Enable(bool on)
    {
        if (!on)
        {
            _isActive = false;
            direction = Vector3.zero;
            _controller.Move(direction);
        }
        else
        {
            _isActive = true;
        }
    }

    void Update()
    {
        if (!_isActive)
        {
            return;
        }

        float _hInput = Input.GetAxis("Horizontal");
        direction.x = _hInput * speed;


        JumpAndGravity();
        Move();

        bool isGrounded = _groundChecker.IsGraunded;
        _animationHander.SetGrounded(isGrounded);
    }


    private void Move()
    {
        float hInput = Input.GetAxis("Horizontal");
        float targetSpeed = MoveSpeed;

        if (hInput == 0f) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, 0.0f).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = 1; //Mathf.Abs(Input.GetAxis("Horizontal"))  ;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);


        Vector3 inputDirection = new Vector3(hInput, 0.0f, 0f).normalized;
        if (hInput != 0f)
        {
            _targetRotation = Mathf.Atan2(0f, inputDirection.x) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);


            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.right;
        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        _animationHander.SetBlendSpeed(_animationBlend);
        _animationHander.SetMotionSpeed(inputMagnitude);
    }


    private void JumpAndGravity()
    {
        bool isGrounded = _groundChecker.IsGraunded;

        if (isGrounded)
        {
            _fallTimeoutDelta = FallTimeout;

            _animationHander.SetJump(false);
            _animationHander.SetFreeFall(false);


            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (Input.GetKeyDown(KeyCode.Space) && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                _animationHander.SetJump(true);
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _animationHander.SetFreeFall(true);
            }

            // if we are not grounded, do not jump
            //	_input.jump = false; ****
        }

        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }
}