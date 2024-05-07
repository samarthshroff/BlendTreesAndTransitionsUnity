using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Vector3 _movementVector;
    private Vector3 _rawInputVector;
    private float _movementSpeed;
    private float _walkSpeed = 0.5f;
    private float _runSpeed = 1.5f;
    private float _smoothingSpeed = 1.0f;

    private Vector3 _lookVector;
    private Vector3 _rawLookVector;
    private float _lookSpeed = 50.0f;
    private float _smoothingAngle = 50.0f;

    [SerializeReference]
    private Animator _animator;

    private enum ActionState
    {
        Grounded,
		WallClimbIdle,        
        WallClimb,
    };

    private enum GroundedState
    {
        Idle,
        Walk,
        Sprint,
    }

    private ActionState _state;
    private GroundedState _groundedState;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.detectCollisions = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        _state = ActionState.Grounded;
        _groundedState = GroundedState.Idle;
        _movementSpeed = 0.0f;
    }

    private void Update()
    {
        switch (_state)
        {
            case ActionState.Grounded:
			{
				CalculateGroundedVelocity();
				_movementVector = Vector3.Lerp(_movementVector, _rawInputVector, Time.deltaTime * _smoothingSpeed);
			}
			break;

            case ActionState.WallClimb:
			{
				_movementSpeed = Mathf.Lerp(_movementSpeed, _walkSpeed, Time.deltaTime * _smoothingSpeed);
				_movementVector = Vector3.Lerp(_movementVector, _rawInputVector, Time.deltaTime * _smoothingSpeed);
			}
			break;

			case ActionState.WallClimbIdle:
			{
				_movementSpeed = 0.0f;
			}
			break;
        }
        //Debug.Log($"The state is {_state} and movement speed is {_movementSpeed}");

        
        _lookVector = Vector3.Lerp(_lookVector, _rawLookVector, Time.deltaTime * _smoothingAngle);
        _animator.SetFloat("Speed", _movementSpeed);
    }

    void CalculateGroundedVelocity()
    {
        switch (_groundedState)
        {
            case GroundedState.Idle:
			{
				if (_movementSpeed == 0.0f)
					break;
				_movementSpeed = Mathf.Lerp(
					_movementSpeed,
					0.0f,
					Time.deltaTime * _smoothingSpeed
				);
				if (_movementSpeed < 0.01f)
				{
					_movementSpeed = 0.0f;
				}
			}
			break;

            case GroundedState.Walk:
			{
				_movementSpeed = Mathf.Lerp(
					_movementSpeed,
					_walkSpeed,
					Time.deltaTime * _smoothingSpeed
				);
			}
			break;

            case GroundedState.Sprint:
			{
				_movementSpeed = Mathf.Lerp(
					_movementSpeed,
					_runSpeed,
					Time.deltaTime * _smoothingSpeed
				);
			}
			break;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
		var movement = _movementVector * _movementSpeed * Time.deltaTime;
		if ((_state == ActionState.Grounded && _groundedState == GroundedState.Idle) ||
		_state == ActionState.WallClimbIdle)
		{
			movement = Vector3.zero;
		}
		if (movement != Vector3.zero)
		{
			_rigidbody.MovePosition(transform.position + movement);
		}

        var yawVector = _lookVector * _lookSpeed * Time.deltaTime;
        var yaw = Quaternion.Euler(yawVector.z, yawVector.x, yawVector.y);
        //_rigidbody.MoveRotation(transform.rotation * yaw);
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
		//Debug.Log($"OnMovement the inputvector is {context.ReadValue<Vector2>()}");
        if (_state == ActionState.Grounded)
        {
            if (context.action.WasPressedThisFrame() && _groundedState == GroundedState.Idle)
            {
                _state = ActionState.Grounded;
                _groundedState = GroundedState.Walk;
                var inputVector = context.ReadValue<Vector2>();
                _rawInputVector = new Vector3(inputVector.x, 0.0f, inputVector.y);				
            }
            else if (context.action.WasReleasedThisFrame())
            {
                _state = ActionState.Grounded;
                _groundedState = GroundedState.Idle;
                _rawInputVector = Vector3.zero;
            }
        }
        else
		if(_state == ActionState.WallClimb || _state == ActionState.WallClimbIdle)
        {
            if (context.action.WasPressedThisFrame() && _state == ActionState.WallClimbIdle )
            {				
				_animator.speed = 1.0f;
                _state = ActionState.WallClimb;
                var inputVector = context.ReadValue<Vector2>();
                _rawInputVector = new Vector3(inputVector.x, inputVector.y, 0.0f);
            }
            else 
			if (context.action.WasReleasedThisFrame() && _state == ActionState.WallClimb) // wall climb idle
            {
				_state = ActionState.WallClimbIdle;
                _rawInputVector = Vector3.zero;
                _animator.speed = 0.0f;
            }
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
		if (_state != ActionState.Grounded) return;
        if (context.action.WasPressedThisFrame())
        {
            _state = ActionState.Grounded;
            _groundedState = GroundedState.Sprint;
        }
        else if (context.action.WasReleasedThisFrame())
        {
            if (_state == ActionState.Grounded && _groundedState == GroundedState.Sprint)
            {
                _state = ActionState.Grounded;
                _groundedState = GroundedState.Walk;
            }
            else
            {
                _state = ActionState.Grounded;
                _groundedState = GroundedState.Idle;
                _rawInputVector = Vector3.zero;
            }
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.action.WasPressedThisFrame())
        {
            _animator.SetTrigger("Jump");
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        var lookVector = context.ReadValue<Vector2>();
        _rawLookVector = new Vector3(lookVector.x, 0.0f, 0.0f);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Walls"))
        {
            var wallType = other.gameObject.GetComponent<WallBase>().WallType;
            if (wallType == WallType.Climbable)
            {
                _state = ActionState.WallClimb;
				_rawInputVector = new Vector3(_rawInputVector.x, 1.0f, 0.0f);
				_rigidbody.useGravity = false;
                _animator.SetTrigger("WallClimb");
            }
        }
    }
}
