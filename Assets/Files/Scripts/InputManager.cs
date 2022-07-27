using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
	public static Subject<Vector2> JoyStick = new();
	public static Subject<int> Jump = new();
	public static Subject<int> Attack = new();

	[SerializeField] FixedJoystick fixedJoystick;


	public void InputJump() 
	{
		Jump.OnNext(0);
	}

	public void InputAttack()
	{
		Attack.OnNext(0);
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.X)) InputJump();
		else if (Input.GetKeyDown(KeyCode.C)) InputAttack();
	}

	void FixedUpdate()
	{
		JoyStick.OnNext(new Vector2(fixedJoystick.Horizontal + Input.GetAxisRaw("Horizontal"), fixedJoystick.Vertical + +Input.GetAxisRaw("Vertical")));
		
	}
}
