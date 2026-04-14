using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

using StateType = StateComponent.StateType;

public enum EvadeDirection
{
	Forward = 0, Backward, Left, Right,
}

public class PlayerMovingComponent : MonoBehaviour
{
	[SerializeField]
	private float walkSpeed = 2.0f;

	[SerializeField]
	private float runSpeed = 4.0f;

	[SerializeField]
	private float sensitivity = 10.0f;

	[SerializeField]
	private float deadZone = 0.001f;


	private bool bCanMove = true;

	private Animator animator;
	private WeaponComponent weapon;
	private StateComponent state;
	private PlayerPotionUseController potionUseController;

	private Vector2 inputMove;
	public Vector2 MoveValue { get => inputMove; }

	private Vector2 currInputMove;
	private bool bRun;

    public void Move()
    {
		bCanMove = true;
    }

	public void Stop()
	{
		bCanMove = false;
	}

	private void Awake()
    {
		animator = GetComponent<Animator>();
		weapon = GetComponent<WeaponComponent>();
		potionUseController = GetComponent<PlayerPotionUseController>();
		
		state = GetComponent<StateComponent>();
		state.OnStateTypeChanged += OnStateTypeChanged;


		PlayerInput input = GetComponent<PlayerInput>();
		InputActionMap actionMap = input.actions.FindActionMap("Player");
		
		InputAction moveAction = actionMap.FindAction("Move");
		moveAction.performed += Input_Move_Performed;
		moveAction.canceled += Input_Move_Cancled;

		InputAction lookAction = actionMap.FindAction("Look");
		lookAction.performed += Input_Look_Performed;


		InputAction runAction = actionMap.FindAction("Run");
		runAction.started += Input_Run_Started;
		runAction.canceled += Input_Run_Cancled;		

		
		actionMap.FindAction("Evade").started += context =>
		{
			if (weapon.UnarmedMode == false)
				return;

			if (state.IdleMode == false)
				return;

			state.SetEvadeMode();
		};
	}

	private void Input_Move_Performed(InputAction.CallbackContext context)
    {
		inputMove = context.ReadValue<Vector2>();
	}

	private void Input_Move_Cancled(InputAction.CallbackContext context)
	{
		inputMove = Vector3.zero;
	}

	private void Input_Run_Started(InputAction.CallbackContext context)
	{
		bRun = true;
	}

	private void Input_Run_Cancled(InputAction.CallbackContext context)
	{
		bRun = false;
	}


	private Vector2 mouseRotation;
	private void Input_Look_Performed(InputAction.CallbackContext context)
    {
		Vector2 mouse = context.ReadValue<Vector2>();
		float x = mouse.x * Time.deltaTime * 1.0f;
		float y = mouse.y * Time.deltaTime * 1.0f;
		
		mouseRotation.x -= y;
		mouseRotation.x = Mathf.Clamp(mouseRotation.x, -45.0f, +45.0f);

		mouseRotation.y += mouse.x;

		//transform.localRotation = Quaternion.Euler(0, -mouseRotation.y, 0.0f);
	}

	private Vector2 velocity;
	private void Update()
    {

		//float horizontal = Input.GetAxis("Horizontal");
		//float vertical = Input.GetAxis("Vertical");

		//Vector3 direction = (Vector3.right * horizontal) + (Vector3.forward * vertical);
		//direction = direction.normalized * walkSpeed;		

		currInputMove = Vector2.SmoothDamp(currInputMove, inputMove, ref velocity, 1.0f / sensitivity);

		if (bCanMove == false)
			return;

		if (potionUseController != null && potionUseController.IsDrinking)
		{
			animator.SetFloat("SpeedX", 0.0f);
			animator.SetFloat("SpeedY", 0.0f);
			return;
		}

		
		Vector3 direction = Vector3.zero;

		float speed = bRun ? runSpeed : walkSpeed;
		if (currInputMove.magnitude > deadZone)
        {
			direction = (Vector3.right * currInputMove.x) + (Vector3.forward * currInputMove.y);
			direction = direction.normalized * speed;
		}

		transform.Translate(direction * Time.deltaTime);
		//controller.Move(direction * Time.deltaTime);

		
		if(weapon.UnarmedMode)
        {
			animator.SetFloat("SpeedY", direction.magnitude);

			return;
		}

		animator.SetFloat("SpeedX", currInputMove.x * speed);
		animator.SetFloat("SpeedY", currInputMove.y * speed);
	}

	
	private Quaternion? evadeRotation = null;

	private void OnStateTypeChanged(StateType prevType, StateType newType)
	{
		switch (newType)
		{
			case StateType.Evade:
			{
				Vector2 value = MoveValue;

				EvadeDirection direction = EvadeDirection.Forward;
				if (value.y == 0.0f)
				{
					direction = EvadeDirection.Forward;

					if (value.x < 0.0f)
						direction = EvadeDirection.Left;
					else if (value.x > 0.0f)
						direction = EvadeDirection.Right;

				}
				else if (value.y >= 0.0f)
				{
					direction = EvadeDirection.Forward;

					if (value.x < 0.0f)
					{
						evadeRotation = transform.rotation;
						transform.Rotate(Vector3.up, -45.0f);
					}
					else if (value.x > 0.0f)
					{
						evadeRotation = transform.rotation;
						transform.Rotate(Vector3.up, +45.0f);
					}
				}
				else
				{
					direction = EvadeDirection.Backward;
				}

				animator.SetInteger("Direction", (int)direction);
				animator.SetTrigger("Evade");
			}
			return;
		}
	}

	private void End_Evade()
	{
		if (evadeRotation.HasValue)
			StartCoroutine(Reset_EvadeRotation());

		state.SetIdleMode();
	}

	private IEnumerator Reset_EvadeRotation()
	{
		float delta = 0.0f;

		while (true)
		{
			float angle = Quaternion.Angle(transform.rotation, evadeRotation.Value);

			if (angle < 2.0f)
				break;

			delta += Time.deltaTime * 100.0f;
			transform.rotation = Quaternion.RotateTowards(transform.rotation, evadeRotation.Value, delta);

			yield return new WaitForFixedUpdate();
		}

		transform.rotation = evadeRotation.Value;
	}
}