using System;
using System.Collections;
using System.Collections.Generic;
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
	private float _lookSpeed = 20.0f;
	private float _smoothingAngle = 10.0f;

	[SerializeReference]
	private Animator _animator;


	private enum MovementState
	{
		Idle,
		Walk,
		Sprint
	};

	private MovementState _state;

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
		_rigidbody.detectCollisions = true;
	}


	// Start is called before the first frame update
	void Start()
	{
		_state = MovementState.Idle;
		_movementSpeed = 0.0f;
	}

	private void Update()
	{
		switch (_state)
		{
			case MovementState.Idle:
				{
					if (_movementSpeed == 0.0f) break;
					_movementSpeed = Mathf.Lerp(_movementSpeed, 0.0f, Time.deltaTime * _smoothingSpeed);
					if (_movementSpeed < 0.01f)
					{
						_movementSpeed = 0.0f;
					}
				}
				break;

			case MovementState.Walk:
				{
					_movementSpeed = Mathf.Lerp(_movementSpeed, _walkSpeed, Time.deltaTime * _smoothingSpeed);
				}
				break;

			case MovementState.Sprint:
				{
					_movementSpeed = Mathf.Lerp(_movementSpeed, _runSpeed, Time.deltaTime * _smoothingSpeed);
				}
				break;
		}

		//Debug.Log($"The state is {_state} and movement speed is {_movementSpeed}");

		_movementVector = Vector3.Lerp(_movementVector, _rawInputVector, Time.deltaTime * _smoothingSpeed);
		_lookVector = Vector3.Lerp(_lookVector, _rawLookVector, Time.deltaTime * _smoothingAngle);
		_animator.SetFloat("Speed", _movementSpeed);
	}

	// Update is called once per frame
	void FixedUpdate()
	{
		var movement = (_state == MovementState.Idle) ? Vector3.zero : _movementVector * _movementSpeed * Time.deltaTime;
		_rigidbody.MovePosition(transform.position + movement);

		var yawVector = _lookVector * _lookSpeed * Time.deltaTime;
		var yaw = Quaternion.Euler(yawVector.z, yawVector.x, yawVector.y);
		_rigidbody.MoveRotation(transform.rotation * yaw);
	}

	public void OnMovement(InputAction.CallbackContext context)
	{
		if (context.action.WasPressedThisFrame())
		{
			var inputVector = context.ReadValue<Vector2>();
			_state = MovementState.Walk;
			_rawInputVector = new Vector3(inputVector.x, 0.0f, inputVector.y);
		}
		else if (context.action.WasReleasedThisFrame())
		{
			_state = MovementState.Idle;
			_rawInputVector = Vector3.zero;
		}
	}

	public void OnSprint(InputAction.CallbackContext context)
	{
		if (context.action.WasPressedThisFrame())
		{
			_state = MovementState.Sprint;
		}
		else if (context.action.WasReleasedThisFrame())
		{
			if (_state == MovementState.Sprint)
			{
				_state = MovementState.Walk;
			}
			else
			{
				_state = MovementState.Idle;
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
				Debug.Log("[PlayerController][OnCollisionEnter] the wall is climbable. changing animation here");
			}
		}
	}
}
